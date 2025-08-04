using Caliburn.Micro;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers.Services
{
    public interface IParallelBatchProcessor
    {
        Task ProcessBatchAsync<T>(
            string query,
            IEnumerable<T> batchData,
            Type responseType,
            int maxBatchSize,
            CancellationToken cancellationToken = default);
    }

    public class ParallelBatchProcessor : IParallelBatchProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ParallelBatchProcessor> _logger;
        private readonly INetworkConnectivityService _networkService;
        private readonly IEventAggregator _eventAggregator;
        private const int MaxRetries = 3;
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        public ParallelBatchProcessor(
            IServiceProvider serviceProvider,
            ILogger<ParallelBatchProcessor> logger,
            INetworkConnectivityService networkService,
            IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _networkService = networkService;
            _eventAggregator = eventAggregator;
        }

        public async Task ProcessBatchAsync<T>(
            string query,
            IEnumerable<T> batchData,
            Type responseType,
            int maxBatchSize,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var dataList = batchData.ToList();
            int totalProcessed = 0;
            int totalFailed = 0;
            var processingId = Guid.NewGuid();

            try
            {
                _logger.LogInformation("Iniciando procesamiento paralelo en lotes. ID: {ProcessingId}, Total elementos: {Count}, Tamaño máximo por lote: {MaxBatchSize}, Tipo de respuesta: {ResponseType}",
                    processingId, dataList.Count, maxBatchSize, responseType.Name);

                // Dividir los datos en lotes
                var batches = CreateBatches(dataList, maxBatchSize);

                // Procesar lotes en paralelo
                var batchTasks = batches.Select(async (batch, index) =>
                {
                    return await ProcessSingleBatchWithRetry(query, batch, responseType, index, cancellationToken);
                });

                var batchResults = await Task.WhenAll(batchTasks);

                // Consolidar resultados para logging
                totalProcessed = batchResults.Sum(r => r.ProcessedCount);
                totalFailed = batchResults.Sum(r => r.FailedCount);

                if (totalFailed == 0)
                {
                    _logger.LogInformation("Procesamiento paralelo completado exitosamente. ID: {ProcessingId}, Procesados: {Processed} elementos en {ElapsedTime}",
                        processingId, totalProcessed, stopwatch.Elapsed);

                    // Publicar mensaje de éxito
                    await _eventAggregator.PublishOnUIThreadAsync(
                        new ParallelBatchCompletedMessage(
                            processingId,
                            true,
                            $"Procesamiento paralelo completado",
                            totalProcessed,
                            totalFailed,
                            stopwatch.Elapsed
                        )
                    );
                }
                else
                {
                    _logger.LogWarning("Procesamiento paralelo completado con errores. ID: {ProcessingId}, Procesados: {Processed}, Fallidos: {Failed}, Tiempo: {ElapsedTime}",
                        processingId, totalProcessed, totalFailed, stopwatch.Elapsed);

                    // Publicar mensaje con errores
                    await _eventAggregator.PublishOnUIThreadAsync(
                        new ParallelBatchCompletedMessage(
                            processingId,
                            false,
                            $"Procesamiento paralelo completado con errores",
                            totalProcessed,
                            totalFailed,
                            stopwatch.Elapsed
                        )
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico en el procesamiento paralelo de lotes. ID: {ProcessingId}, Tiempo transcurrido: {ElapsedTime}",
                    processingId, stopwatch.Elapsed);

                // Publicar mensaje de error crítico
                await _eventAggregator.PublishOnUIThreadAsync(
                    new ParallelBatchCompletedMessage(
                        processingId,
                        false,
                        $"Error crítico en procesamiento paralelo",
                        totalProcessed,
                        dataList.Count - totalProcessed,
                        stopwatch.Elapsed,
                        ex
                    )
                );

                throw;
            }
        }

        private List<List<T>> CreateBatches<T>(List<T> data, int maxBatchSize)
        {
            var batches = new List<List<T>>();

            for (int i = 0; i < data.Count; i += maxBatchSize)
            {
                var batchSize = Math.Min(maxBatchSize, data.Count - i);
                var batch = data.GetRange(i, batchSize);
                batches.Add(batch);
            }

            _logger.LogDebug("Datos divididos en {BatchCount} lotes para procesamiento paralelo", batches.Count);
            return batches;
        }

        private async Task<SingleBatchResult> ProcessSingleBatchWithRetry<T>(
            string query,
            List<T> batch,
            Type responseType,
            int batchIndex,
            CancellationToken cancellationToken,
            int retryCount = 0)
        {
            try
            {
                // Verificar conectividad antes de procesar
                if (!_networkService.IsConnected())
                {
                    _logger.LogWarning("Sin conexión a Internet para el lote {BatchIndex}. Esperando...", batchIndex);

                    // Esperar un poco antes de verificar nuevamente
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                    if (!_networkService.IsConnected())
                    {
                        throw new InvalidOperationException("No hay conexión a Internet disponible");
                    }
                }

                return await ProcessSingleBatch(query, batch, responseType, batchIndex, cancellationToken);
            }
            catch (Exception ex) when (retryCount < MaxRetries)
            {
                _logger.LogWarning(ex, "Error procesando lote {BatchIndex}. Reintento {RetryCount}/{MaxRetries}",
                    batchIndex, retryCount + 1, MaxRetries);

                // Esperar antes de reintentar
                await Task.Delay(RetryDelay, cancellationToken);

                return await ProcessSingleBatchWithRetry(query, batch, responseType, batchIndex, cancellationToken, retryCount + 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lote {BatchIndex} falló después de {MaxRetries} reintentos", batchIndex, MaxRetries);

                return new SingleBatchResult
                {
                    ProcessedCount = 0,
                    FailedCount = batch.Count
                };
            }
        }

        private async Task<SingleBatchResult> ProcessSingleBatch<T>(
            string query,
            List<T> batch,
            Type responseType,
            int batchIndex,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Procesando lote {BatchIndex} con {Count} elementos usando tipo de respuesta {ResponseType}",
                    batchIndex, batch.Count, responseType.Name);

                // Obtener el servicio de acceso a datos genérico usando el responseType proporcionado
                var dataAccessType = typeof(IGenericDataAccess<>).MakeGenericType(responseType);
                var dataAccess = _serviceProvider.GetService(dataAccessType);

                if (dataAccess == null)
                {
                    throw new InvalidOperationException($"No se pudo obtener el servicio de acceso a datos para el tipo {responseType.Name}");
                }

                // Obtener el método SendMutationList
                var sendMutationListMethod = dataAccessType.GetMethod("SendMutationList");
                if (sendMutationListMethod == null)
                {
                    throw new InvalidOperationException($"No se encontró el método SendMutationList en {dataAccessType.Name}");
                }

                // Crear las variables del lote (asumiendo que el batch es directamente utilizable)
                var batchVariables = new { data = batch };

                // Invocar la operación en lote
                var taskObj = sendMutationListMethod.Invoke(
                    dataAccess,
                    new object[] { query, batchVariables }
                );

                // Esperar a que termine la tarea
                await (Task)taskObj;

                _logger.LogDebug("Lote {BatchIndex} procesado exitosamente", batchIndex);

                return new SingleBatchResult
                {
                    ProcessedCount = batch.Count,
                    FailedCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando lote {BatchIndex}", batchIndex);
                throw;
            }
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
        public Exception Exception { get; }

        public ParallelBatchCompletedMessage(
            Guid processingId,
            bool isSuccess,
            string displayName,
            int totalProcessed,
            int totalFailed,
            TimeSpan processingTime,
            Exception exception = null)
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
