using Common.Helpers;
using Common.Interfaces;
using Models.Login;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Services
{
    public class CompanySeedService : ICompanySeedService
    {
        private static readonly TimeSpan GlobalTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

        public async Task<CompanySeedResultModel> RunSeedsAsync(
            int companyId,
            IProgress<string> progress,
            CancellationToken cancellationToken = default)
        {
            var result = new CompanySeedResultModel();
            var refCounter = new RefCounter();

            using var globalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            globalCts.CancelAfter(GlobalTimeout);
            CancellationToken ct = globalCts.Token;

            progress.Report("Preparando la configuración inicial...");

            string baseUrl = ConnectionConfig.LoginAPIUrl;
            var httpUri = new Uri(baseUrl);
            string wsUrl = $"wss://{httpUri.Host}/socket/websocket";

            using var ws = new ClientWebSocket();
            ws.Options.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;

            try
            {
                await ws.ConnectAsync(new Uri(wsUrl), ct);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"No se pudo conectar al servidor de configuración ({wsUrl}): {ex.Message}", ex);
            }

            progress.Report("Conectando con el servidor...");

            CancellationTokenSource? heartbeatCts = null;
            Task? mutationTask = null;

            try
            {
                // Heartbeat
                heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _ = RunHeartbeatAsync(ws, refCounter, heartbeatCts.Token);

                // Join __absinthe__:control
                await PhxSendAsync(ws, "__absinthe__:control", "phx_join", new { }, refCounter.Next(), ct);
                string? joinReply = await PhxReceiveAsync(ws, ct);
                ValidatePhxReply(joinReply, "join al canal de control");

                // Subscribe
                string subscriptionQuery = $"subscription {{ companySeedFileResult(companyId: \"{companyId}\") {{ type status file durationMs erpCompanyId error message }} }}";
                await PhxSendAsync(ws, "__absinthe__:control", "doc",
                    new { query = subscriptionQuery, variables = new { } }, refCounter.Next(), ct);

                string? docReply = await PhxReceiveAsync(ws, ct);
                ValidatePhxReply(docReply, "registro de subscription");

                var docParsed = JObject.Parse(docReply!);
                string? subscriptionId = docParsed["payload"]?["response"]?["subscriptionId"]?.ToString();

                if (string.IsNullOrEmpty(subscriptionId))
                {
                    throw new InvalidOperationException(
                        "No se recibió el identificador de suscripción del servidor. Intenta nuevamente.");
                }

                progress.Report("Ejecutando la configuración de la empresa...");

                // Launch mutation in background
                mutationTask = Task.Run(() => RunMutationAsync(baseUrl, companyId, ct), ct);

                // Listen for subscription events
                bool finished = false;
                while (!finished && ws.State == WebSocketState.Open)
                {
                    using var eventCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    eventCts.CancelAfter(EventTimeout);

                    string? message;
                    try
                    {
                        message = await PhxReceiveAsync(ws, eventCts.Token);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        throw new TimeoutException(
                            "Se agotó el tiempo de espera entre eventos del servidor. Intenta nuevamente.");
                    }

                    if (message == null) break;

                    var parsed = JObject.Parse(message);
                    string? eventName = parsed["event"]?.ToString();
                    string? topic = parsed["topic"]?.ToString();

                    if (eventName == "subscription:data" && topic == subscriptionId)
                    {
                        JToken? evt = parsed["payload"]?["result"]?["data"]?["companySeedFileResult"];
                        if (evt != null)
                        {
                            finished = ProcessEvent(evt, result, progress);
                        }
                    }
                    // Ignore phx_reply, heartbeat, and other messages
                }

                // Wait for mutation result
                MutationResult? mutationResult = null;
                if (mutationTask != null)
                {
                    mutationResult = await (Task<MutationResult?>)mutationTask;
                }

                // Finalize result
                if (mutationResult != null && !mutationResult.Success)
                {
                    result.Success = false;
                    result.Message = mutationResult.Message ?? "La mutation reportó un error.";
                    if (mutationResult.Errors != null)
                    {
                        foreach (var err in mutationResult.Errors)
                        {
                            result.Errors.Add($"{err.Field}: {err.Message}");
                        }
                    }
                }
                else if (result.FilesFailed == 0 && result.Errors.Count == 0)
                {
                    result.Success = true;
                    result.Message = "Configuración completada exitosamente.";
                }
                else
                {
                    result.Success = false;
                    if (string.IsNullOrEmpty(result.Message))
                    {
                        result.Message = $"Configuración finalizada con {result.FilesFailed} error(es).";
                    }
                }
            }
            finally
            {
                heartbeatCts?.Cancel();
                heartbeatCts?.Dispose();

                if (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", closeCts.Token);
                    }
                    catch { /* Best effort close */ }
                }
            }

            return result;
        }

        private static bool ProcessEvent(JToken evt, CompanySeedResultModel result, IProgress<string> progress)
        {
            string type = evt["type"]?.ToString() ?? string.Empty;
            string status = evt["status"]?.ToString() ?? string.Empty;
            string? file = evt["file"]?.Type != JTokenType.Null ? evt["file"]?.ToString() : null;
            int? durationMs = evt["durationMs"]?.Type != JTokenType.Null ? evt["durationMs"]?.Value<int>() : null;
            string? message = evt["message"]?.Type != JTokenType.Null ? evt["message"]?.ToString() : null;
            string? error = evt["error"]?.Type != JTokenType.Null ? evt["error"]?.ToString() : null;

            string fileName = file != null ? System.IO.Path.GetFileName(file) : string.Empty;

            // Detect phase from message
            if (message != null)
            {
                if (message.Contains("phase=starting"))
                {
                    progress.Report("Iniciando la configuración de la empresa...");
                    return false;
                }
                if (message.Contains("phase=resolved"))
                {
                    progress.Report("Archivos de configuración localizados...");
                    return false;
                }
                if (message.Contains("phase=files_discovered"))
                {
                    progress.Report("Archivos de configuración descubiertos...");
                    return false;
                }
                if (message.Contains("phase=file_start"))
                {
                    progress.Report($"Ejecutando: {fileName}...");
                    return false;
                }
                if (message.Contains("phase=finished"))
                {
                    if (status == "ok" || status == "info")
                    {
                        progress.Report("Configuración finalizada.");
                    }
                    else
                    {
                        progress.Report("Configuración finalizada con errores.");
                    }
                    return true; // Signal to exit the loop
                }
            }

            // Handle specific event types
            switch (type)
            {
                case "file_ok":
                    result.FilesExecuted++;
                    string durationText = durationMs.HasValue ? $" ({durationMs}ms)" : string.Empty;
                    progress.Report($"Archivo ejecutado: {fileName}{durationText}");
                    break;

                case "file_error":
                    result.FilesFailed++;
                    result.Errors.Add($"Error en: {fileName}" + (error != null ? $" - {error}" : string.Empty));
                    progress.Report($"Error en: {fileName}");
                    break;

                case "pipeline_warning":
                    result.FilesSkipped++;
                    progress.Report($"Archivo omitido: {fileName}");
                    break;
            }

            return false;
        }

        private static async Task<MutationResult?> RunMutationAsync(
            string baseUrl, int companyId, CancellationToken ct)
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            };
            using var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("x-api-key", SessionInfo.ApiKey);

            var mutationBody = new
            {
                query = "mutation ($companyId: ID!) { runCompanySeeds(companyId: $companyId) { success message errors { field message } } }",
                variables = new { companyId = companyId.ToString() }
            };

            string json = JsonConvert.SerializeObject(mutationBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(baseUrl, content, ct);
            string responseBody = await response.Content.ReadAsStringAsync(ct);

            var parsed = JObject.Parse(responseBody);
            JToken? data = parsed["data"]?["runCompanySeeds"];

            if (data == null) return null;

            return new MutationResult
            {
                Success = data["success"]?.Value<bool>() ?? false,
                Message = data["message"]?.ToString(),
                Errors = data["errors"]?.ToObject<List<MutationFieldError>>()
            };
        }

        private static async Task RunHeartbeatAsync(ClientWebSocket ws, RefCounter refCounter, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(HeartbeatInterval, ct);
                    if (ws.State == WebSocketState.Open)
                    {
                        await PhxSendAsync(ws, "phoenix", "heartbeat", new { }, refCounter.Next(), ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private static async Task PhxSendAsync(
            ClientWebSocket ws, string topic, string eventName, object payload, string msgRef, CancellationToken ct)
        {
            var msg = new { topic, @event = eventName, payload, @ref = msgRef };
            string json = JsonConvert.SerializeObject(msg);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }

        private static async Task<string?> PhxReceiveAsync(ClientWebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();

            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return null;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            return sb.ToString();
        }

        private static void ValidatePhxReply(string? reply, string operationDescription)
        {
            if (string.IsNullOrEmpty(reply))
            {
                throw new InvalidOperationException(
                    $"No se recibió respuesta del servidor para: {operationDescription}.");
            }

            var parsed = JObject.Parse(reply);
            string? status = parsed["payload"]?["status"]?.ToString();

            if (status != "ok")
            {
                string? reason = parsed["payload"]?["response"]?["reason"]?.ToString();
                throw new InvalidOperationException(
                    $"El servidor rechazó la operación '{operationDescription}'" +
                    (reason != null ? $": {reason}" : "."));
            }
        }

        private class RefCounter
        {
            private int _value;
            public string Next() => Interlocked.Increment(ref _value).ToString();
        }

        private class MutationResult
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public List<MutationFieldError>? Errors { get; set; }
        }

        private class MutationFieldError
        {
            public string Field { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
    }
}
