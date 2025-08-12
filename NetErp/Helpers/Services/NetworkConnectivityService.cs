using Caliburn.Micro;
using Microsoft.Extensions.Logging;
using NetErp.Helpers.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers.Services
{
    public interface INetworkConnectivityService
    {
        Task<bool> IsConnectedAsync();
        void StartMonitoring();
        void StopMonitoring();
    }

    public class NetworkConnectivityService : INetworkConnectivityService, IDisposable
    {
        private readonly ILogger<NetworkConnectivityService> _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly object _lockObject = new();
        private volatile bool _isRunning = false;
        private CancellationTokenSource? _cts;
        private Task? _monitoringTask;
        private bool _lastStatus = true;
        
        // Múltiples endpoints para fallback
        private readonly string[] _connectivityUrls = 
        {
            "https://www.google.com/generate_204",
            "https://www.microsoft.com/favicon.ico", 
            "https://www.cloudflare.com/"
        };

        public NetworkConnectivityService(
            ILogger<NetworkConnectivityService> logger,
            IEventAggregator eventAggregator)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        public async Task<bool> IsConnectedAsync()
        {
            // Intentar conectar a múltiples endpoints para mayor fiabilidad
            foreach (var url in _connectivityUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url, _cts?.Token ?? CancellationToken.None);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch (TaskCanceledException)
                {
                    // Operación cancelada, salir inmediatamente
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Falló conexión a {Url}: {Error}", url, ex.Message);
                    // Continuar con el siguiente endpoint
                }
            }
            
            _logger.LogWarning("No se detectó conexión a Internet en ningún endpoint");
            return false;
        }

        public void StartMonitoring()
        {
            lock (_lockObject)
            {
                if (_isRunning) return;

                _isRunning = true;
                _cts = new CancellationTokenSource();
                _monitoringTask = Task.Run(MonitorNetworkStatusAsync, _cts.Token);
                _logger.LogDebug("Network monitoring started");
            }
        }

        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isRunning) return;

                _isRunning = false;
                _cts?.Cancel();
                _logger.LogDebug("Network monitoring stopped");
            }
        }

        private async Task MonitorNetworkStatusAsync()
        {
            while (_isRunning && !(_cts?.Token.IsCancellationRequested ?? true))
            {
                try
                {
                    bool currentStatus = await IsConnectedAsync();
                    if (currentStatus != _lastStatus)
                    {
                        _lastStatus = currentStatus;
                        var statusMessage = currentStatus ? "Conexión a Internet establecida" : "Se perdió la conexión a Internet";
                        _logger.LogInformation(statusMessage);
                        
                        await _eventAggregator.PublishOnUIThreadAsync(new NetworkStatusChangedMessage(currentStatus));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Salida controlada por cancellation
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al verificar la conectividad");
                }

                try
                {
                    // Verificamos cada 10 segundos con cancellation support
                    await Task.Delay(10000, _cts?.Token ?? CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    // Salida controlada por cancellation
                    break;
                }
            }
            
            _logger.LogDebug("Network monitoring loop ended");
        }

        public void Dispose()
        {
            try
            {
                StopMonitoring();
                
                // Esperar un momento para que el monitoring termine
                _monitoringTask?.Wait(TimeSpan.FromSeconds(2));
                
                _cts?.Dispose();
                _httpClient?.Dispose();
                
                _logger.LogDebug("NetworkConnectivityService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during NetworkConnectivityService disposal");
            }
        }
    }
}
