using Caliburn.Micro;
using Common.Interfaces;
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
    public interface IParallelBatchProcessor
    {
        Task ProcessBatchAsync<T>(
            string query,
            IEnumerable<T> batchData,
            Type responseType,
            int maxBatchSize,
            CancellationToken cancellationToken = default);
        
        bool HasCriticalError();
        string GetCriticalErrorMessage();
    }

    public class ParallelBatchProcessor : IParallelBatchProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ParallelBatchProcessor> _logger;
        private readonly INetworkConnectivityService _networkService;
        private readonly IEventAggregator _eventAggregator;
        private const int MaxRetries = 3;
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
        private bool _serviceHasCriticalError = false;
        private string _criticalErrorMessage = string.Empty;

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
            // Verificar si el servicio tiene un error crítico
            if (_serviceHasCriticalError)
            {
                _logger.LogError("Rechazando procesamiento debido a error crítico del servicio: {ErrorMessage}", _criticalErrorMessage);
                throw new InvalidOperationException($"ParallelBatchProcessor has critical error: {_criticalErrorMessage}");
            }
            
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
                if (!await _networkService.IsConnectedAsync())
                {
                    _logger.LogWarning("Sin conexión a Internet para el lote {BatchIndex}", batchIndex);
                    throw new InvalidOperationException("No hay conexión a Internet disponible");
                }

                return await ProcessSingleBatch(query, batch, responseType, batchIndex, cancellationToken);
            }
            catch (Exception ex) when (retryCount < MaxRetries && IsRecoverableError(ex))
            {
                _logger.LogWarning(ex, "Error procesando lote {BatchIndex}. Reintento {RetryCount}/{MaxRetries}",
                    batchIndex, retryCount + 1, MaxRetries);

                // Esperar antes de reintentar
                await Task.Delay(RetryDelay, cancellationToken);

                return await ProcessSingleBatchWithRetry(query, batch, responseType, batchIndex, cancellationToken, retryCount + 1);
            }
            catch (Exception ex)
            {
                // Verificar si es un error irrecuperable
                bool isRecoverable = IsRecoverableError(ex);
                
                if (!isRecoverable)
                {
                    // Error irrecuperable - parar el servicio
                    _serviceHasCriticalError = true;
                    _criticalErrorMessage = $"Critical error in parallel batch processing: {ex.Message}";
                    
                    _logger.LogCritical("ParallelBatchProcessor pausado debido a error irrecuperable: {ErrorMessage}", ex.Message);
                    
                    // El mensaje crítico se envía desde ProcessSingleBatch
                    throw; // Re-lanzar para que el caller maneje el error crítico
                }
                
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

                // Obtener el repositorio usando el responseType proporcionado
                var repositoryType = typeof(IRepository<>).MakeGenericType(responseType);
                var repository = _serviceProvider.GetService(repositoryType);

                if (repository == null)
                {
                    throw new InvalidOperationException($"No se pudo obtener el repositorio para el tipo {responseType.Name}");
                }

                // Obtener el método 
                string methodName = "NoJodaPai";
                var mutationMethod = repositoryType.GetMethod(methodName);
                if (mutationMethod == null)
                {
                    throw new InvalidOperationException($"No se encontró el método {methodName} en {repositoryType.Name}");
                }

                // Crear las variables del lote (asumiendo que el batch es directamente utilizable)
                var batchVariables = new { data = batch };

                // Invocar la operación en lote
                var taskObj = mutationMethod.Invoke(
                    repository,
                    new object[] { query, batchVariables, cancellationToken }
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
                
                // Verificar si es un error irrecuperable antes de re-lanzar
                if (!IsRecoverableError(ex))
                {
                    // Solo enviar mensaje crítico la primera vez
                    if (!_serviceHasCriticalError)
                    {
                        // Error irrecuperable - parar el servicio
                        _serviceHasCriticalError = true;
                        _criticalErrorMessage = $"Critical error in single batch processing: {ex.Message}";
                        
                        _logger.LogCritical("ParallelBatchProcessor pausado debido a error irrecuperable en procesamiento de lote: {ErrorMessage}", ex.Message);
                        
                        // Enviar mensaje crítico
                        string userMessage = $"Se ha detectado un error crítico en el sistema que impide continuar.\n\n" +
                                           $"Error: {ex.Message}\n\n" +
                                           $"Por favor, comuníquese con el área de soporte técnico.";

                        await _eventAggregator.PublishOnUIThreadAsync(
                            new CriticalSystemErrorMessage(
                                responseType,
                                nameof(ParallelBatchProcessor),
                                ex.Message, 
                                userMessage
                            )
                        );
                    }
                }
                
                throw;
            }
        }

        public bool HasCriticalError()
        {
            return _serviceHasCriticalError;
        }

        public string GetCriticalErrorMessage()
        {
            return _criticalErrorMessage;
        }
        
        private bool IsRecoverableError(Exception ex)
        {
            // Errores de red/conectividad - recuperables
            if (ex is HttpRequestException || 
                ex is TaskCanceledException ||
                ex is TimeoutException ||
                ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Errores de código/reflexión/configuración - irrecuperables
            if (ex is InvalidOperationException ||
                ex is ArgumentException ||
                ex is NotSupportedException ||
                ex is System.Reflection.TargetParameterCountException ||
                ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("method", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Por defecto, consideramos recuperables los errores desconocidos
            return true;
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
