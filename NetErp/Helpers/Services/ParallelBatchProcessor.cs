using Caliburn.Micro;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using NetErp.Helpers.Messages;

namespace NetErp.Helpers.Services
{
    /// <summary>
    /// Configuración para el procesamiento paralelo de un batch.
    /// El caller define cómo construir las variables y ejecutar la mutación.
    /// </summary>
    public class ParallelBatchConfig
    {
        /// <summary>
        /// Query GraphQL a ejecutar por cada batch.
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Tamaño máximo de cada sub-batch.
        /// </summary>
        public int MaxBatchSize { get; set; } = 10;

        /// <summary>
        /// Construye las variables a partir de los items del batch.
        /// Recibe la lista de items del sub-batch y retorna el objeto de variables.
        /// </summary>
        public Func<IEnumerable<object>, object> BuildBatchVariables { get; set; } = null!;

        /// <summary>
        /// Ejecuta la mutación batch contra la API.
        /// Recibe (query, variables, cancellationToken).
        /// </summary>
        public Func<string, object, CancellationToken, Task> ExecuteBatchAsync { get; set; } = null!;

        /// <summary>
        /// Tipo de respuesta para identificar qué módulo afecta un error crítico.
        /// </summary>
        public Type ResponseType { get; set; } = typeof(object);
    }

    /// <summary>
    /// Servicio de procesamiento paralelo por lotes.
    /// Divide datasets grandes en sub-batches y los procesa concurrentemente.
    /// </summary>
    public interface IParallelBatchProcessor
    {
        /// <summary>
        /// Procesa una lista de items dividiéndolos en batches paralelos.
        /// </summary>
        Task ProcessBatchAsync<T>(
            IEnumerable<T> items,
            ParallelBatchConfig config,
            CancellationToken cancellationToken = default);

        bool HasCriticalError();
        string GetCriticalErrorMessage();
        void ResetCriticalError();
    }

    public class ParallelBatchProcessor : IParallelBatchProcessor
    {
        private readonly ILogger<ParallelBatchProcessor> _logger;
        private readonly INetworkConnectivityService _networkService;
        private readonly IEventAggregator _eventAggregator;

        private const int MaxRetries = 3;
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
        private volatile bool _serviceHasCriticalError = false;
        private volatile string _criticalErrorMessage = string.Empty;

        public ParallelBatchProcessor(
            IServiceProvider serviceProvider,
            ILogger<ParallelBatchProcessor> logger,
            INetworkConnectivityService networkService,
            IEventAggregator eventAggregator)
        {
            _logger = logger;
            _networkService = networkService;
            _eventAggregator = eventAggregator;
        }

        public async Task ProcessBatchAsync<T>(
            IEnumerable<T> items,
            ParallelBatchConfig config,
            CancellationToken cancellationToken = default)
        {
            if (_serviceHasCriticalError)
            {
                _logger.LogError("Rechazando procesamiento debido a error crítico: {ErrorMessage}", _criticalErrorMessage);
                throw new InvalidOperationException($"ParallelBatchProcessor tiene un error crítico: {_criticalErrorMessage}");
            }

            var stopwatch = Stopwatch.StartNew();
            var dataList = items.Cast<object>().ToList();
            int totalProcessed = 0;
            int totalFailed = 0;
            var processingId = Guid.NewGuid();

            try
            {
                _logger.LogInformation("Iniciando procesamiento paralelo. ID: {ProcessingId}, Total: {Count}, BatchSize: {MaxBatchSize}",
                    processingId, dataList.Count, config.MaxBatchSize);

                var batches = CreateBatches(dataList, config.MaxBatchSize);

                var batchTasks = batches.Select(async (batch, index) =>
                {
                    return await ProcessSingleBatchWithRetry(batch, config, index, cancellationToken);
                });

                var batchResults = await Task.WhenAll(batchTasks);

                totalProcessed = batchResults.Sum(r => r.ProcessedCount);
                totalFailed = batchResults.Sum(r => r.FailedCount);

                stopwatch.Stop();

                await _eventAggregator.PublishOnUIThreadAsync(
                    new ParallelBatchCompletedMessage(
                        processingId,
                        totalFailed == 0,
                        totalFailed == 0 ? "Procesamiento completado" : "Procesamiento completado con errores",
                        totalProcessed,
                        totalFailed,
                        stopwatch.Elapsed
                    )
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error crítico en procesamiento paralelo. ID: {ProcessingId}", processingId);

                await _eventAggregator.PublishOnUIThreadAsync(
                    new ParallelBatchCompletedMessage(
                        processingId,
                        false,
                        "Error crítico en procesamiento paralelo",
                        totalProcessed,
                        dataList.Count - totalProcessed,
                        stopwatch.Elapsed,
                        ex
                    )
                );

                throw;
            }
        }

        private List<List<object>> CreateBatches(List<object> data, int maxBatchSize)
        {
            var batches = new List<List<object>>();
            for (int i = 0; i < data.Count; i += maxBatchSize)
            {
                var batchSize = Math.Min(maxBatchSize, data.Count - i);
                batches.Add(data.GetRange(i, batchSize));
            }
            return batches;
        }

        private async Task<SingleBatchResult> ProcessSingleBatchWithRetry(
            List<object> batch,
            ParallelBatchConfig config,
            int batchIndex,
            CancellationToken cancellationToken)
        {
            for (int retryCount = 0; retryCount <= MaxRetries; retryCount++)
            {
                try
                {
                    if (!await _networkService.IsConnectedAsync())
                    {
                        _logger.LogWarning("Sin conexión a Internet para el lote {BatchIndex}", batchIndex);
                        throw new HttpRequestException("No hay conexión a Internet disponible");
                    }

                    var batchVariables = config.BuildBatchVariables(batch);
                    await config.ExecuteBatchAsync(config.Query, batchVariables, cancellationToken);

                    _logger.LogDebug("Lote {BatchIndex} procesado exitosamente ({Count} items)", batchIndex, batch.Count);
                    return new SingleBatchResult { ProcessedCount = batch.Count, FailedCount = 0 };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en lote {BatchIndex}. Intento {RetryCount}/{MaxRetries}", batchIndex, retryCount + 1, MaxRetries);

                    if (!IsRecoverableError(ex))
                    {
                        _serviceHasCriticalError = true;
                        _criticalErrorMessage = $"Error crítico en procesamiento: {ex.Message}";
                        _logger.LogCritical("Servicio pausado: {ErrorMessage}", ex.Message);

                        await _eventAggregator.PublishOnUIThreadAsync(
                            new CriticalSystemErrorMessage(
                                config.ResponseType,
                                nameof(ParallelBatchProcessor),
                                ex.Message,
                                $"El servicio de procesamiento paralelo ha detectado un error crítico.\n\n" +
                                $"Detalle: {ex.Message}\n\n" +
                                $"Comuníquese con soporte técnico."
                            )
                        );

                        throw;
                    }

                    if (retryCount < MaxRetries)
                    {
                        await Task.Delay(RetryDelay, cancellationToken);
                        continue;
                    }

                    _logger.LogWarning("Lote {BatchIndex} fallido después de {MaxRetries} reintentos", batchIndex, MaxRetries);
                    return new SingleBatchResult { ProcessedCount = 0, FailedCount = batch.Count };
                }
            }

            return new SingleBatchResult { ProcessedCount = 0, FailedCount = batch.Count };
        }

        /// <summary>
        /// Clasifica errores por tipo de excepción.
        /// Solo errores de infraestructura son irrecuperables.
        /// </summary>
        private bool IsRecoverableError(Exception ex)
        {
            if (ex is System.Reflection.TargetInvocationException ||
                ex is System.Reflection.TargetParameterCountException ||
                ex is MissingMethodException ||
                ex is MissingMemberException ||
                ex is NotSupportedException)
            {
                return false;
            }

            if (ex is InvalidOperationException && ex.Source == typeof(ParallelBatchProcessor).Assembly.GetName().Name)
            {
                return false;
            }

            return true;
        }

        public bool HasCriticalError() => _serviceHasCriticalError;
        public string GetCriticalErrorMessage() => _criticalErrorMessage;

        public void ResetCriticalError()
        {
            _serviceHasCriticalError = false;
            _criticalErrorMessage = string.Empty;
            _logger.LogInformation("Estado de error crítico reseteado.");
        }

        private class SingleBatchResult
        {
            public int ProcessedCount { get; set; }
            public int FailedCount { get; set; }
        }
    }

    public class ParallelBatchCompletedMessage
    {
        public Guid ProcessingId { get; }
        public bool IsSuccess { get; }
        public string DisplayName { get; }
        public int TotalProcessed { get; }
        public int TotalFailed { get; }
        public TimeSpan ProcessingTime { get; }
        public Exception? Exception { get; }

        public ParallelBatchCompletedMessage(
            Guid processingId,
            bool isSuccess,
            string displayName,
            int totalProcessed,
            int totalFailed,
            TimeSpan processingTime,
            Exception? exception = null)
        {
            ProcessingId = processingId;
            IsSuccess = isSuccess;
            DisplayName = displayName;
            TotalProcessed = totalProcessed;
            TotalFailed = totalFailed;
            ProcessingTime = processingTime;
            Exception = exception;
        }
    }
}
