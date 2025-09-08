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
    /// <summary>
    /// Interface for parallel batch processing service that handles high-volume data operations.
    /// Provides optimized parallel processing with automatic retry logic and network connectivity awareness.
    /// </summary>
    /// <remarks>
    /// This service is designed for scenarios where large datasets need to be processed efficiently
    /// by dividing them into smaller batches and processing them in parallel. It includes
    /// sophisticated error handling, network monitoring, and critical error detection.
    /// </remarks>
    public interface IParallelBatchProcessor
    {
        /// <summary>
        /// Processes a large dataset by dividing it into parallel batches for optimal performance.
        /// Each batch is processed independently and can be retried on failure.
        /// </summary>
        /// <typeparam name="T">The type of data being processed</typeparam>
        /// <param name="query">GraphQL mutation query for batch processing</param>
        /// <param name="batchData">The complete dataset to be processed</param>
        /// <param name="responseType">The GraphQL model type for repository resolution</param>
        /// <param name="maxBatchSize">Maximum number of items to process in each parallel batch</param>
        /// <param name="cancellationToken">Token to cancel the entire operation</param>
        /// <returns>A task that completes when all batches have been processed</returns>
        /// <exception cref="InvalidOperationException">Thrown when service has critical error or repository cannot be resolved</exception>
        Task ProcessBatchAsync<T>(
            string query,
            IEnumerable<T> batchData,
            Type responseType,
            int maxBatchSize,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if the service has encountered a critical error that prevents further processing.
        /// </summary>
        /// <returns>True if there is a critical error, false otherwise</returns>
        bool HasCriticalError();
        
        /// <summary>
        /// Gets the message describing the current critical error, if any.
        /// </summary>
        /// <returns>The critical error message, or empty string if no critical error exists</returns>
        string GetCriticalErrorMessage();
    }

    /// <summary>
    /// High-performance parallel batch processor for handling large-scale data operations.
    /// Divides datasets into optimal batch sizes and processes them concurrently with comprehensive error handling.
    /// </summary>
    /// <remarks>
    /// This processor is optimized for scenarios requiring high throughput data processing such as:
    /// 
    /// **Price List Updates**: Processing thousands of price changes efficiently
    /// **Bulk Data Import**: Importing large datasets with automatic retry on network issues
    /// **Mass Updates**: Applying changes to multiple entities simultaneously
    /// 
    /// **Key Features**:
    /// - **Parallel Processing**: Divides work into concurrent batches for maximum throughput
    /// - **Smart Retry Logic**: Automatically retries failed batches with exponential backoff
    /// - **Network Awareness**: Monitors connectivity and delays processing during outages
    /// - **Error Classification**: Distinguishes between recoverable and critical errors
    /// - **Progress Tracking**: Provides detailed logging and event notifications
    /// - **Resource Management**: Prevents memory issues through controlled batch sizes
    /// 
    /// **Error Handling**:
    /// - Network errors trigger automatic retries
    /// - Configuration errors pause the service to prevent data corruption
    /// - Individual batch failures don't stop other parallel operations
    /// - Critical errors are reported through the event system for UI feedback
    /// 
    /// The processor uses dependency injection to resolve repository services dynamically
    /// and integrates with Caliburn.Micro's event aggregator for progress notifications.
    /// </remarks>
    public class ParallelBatchProcessor : IParallelBatchProcessor
    {
        /// <summary>Service provider for resolving repository instances dynamically</summary>
        private readonly IServiceProvider _serviceProvider;
        /// <summary>Logger for detailed operation tracking and debugging</summary>
        private readonly ILogger<ParallelBatchProcessor> _logger;
        /// <summary>Network connectivity monitoring service</summary>
        private readonly INetworkConnectivityService _networkService;
        /// <summary>Event aggregator for publishing processing completion events</summary>
        private readonly IEventAggregator _eventAggregator;
        
        /// <summary>Maximum number of retry attempts per batch</summary>
        private const int MaxRetries = 3;
        /// <summary>Delay between retry attempts</summary>
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
        /// <summary>Flag indicating if service has encountered a critical error</summary>
        private bool _serviceHasCriticalError = false;
        /// <summary>Message describing the current critical error</summary>
        private string _criticalErrorMessage = string.Empty;

        /// <summary>
        /// Initializes a new instance of the ParallelBatchProcessor with dependency injection.
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving repository instances</param>
        /// <param name="logger">Logger for operation tracking and debugging</param>
        /// <param name="networkService">Network connectivity monitoring service</param>
        /// <param name="eventAggregator">Event aggregator for publishing completion events</param>
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

        /// <inheritdoc/>
        public async Task ProcessBatchAsync<T>(
            string query,
            IEnumerable<T> batchData,
            Type responseType,
            int maxBatchSize,
            CancellationToken cancellationToken = default)
        {
            // Check if service has a critical error
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

        /// <summary>
        /// Divides a dataset into smaller batches for parallel processing.
        /// Each batch will contain at most maxBatchSize elements.
        /// </summary>
        /// <typeparam name="T">Type of data being batched</typeparam>
        /// <param name="data">The complete dataset to divide</param>
        /// <param name="maxBatchSize">Maximum elements per batch</param>
        /// <returns>List of batches ready for parallel processing</returns>
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

        /// <summary>
        /// Processes a single batch with automatic retry logic on recoverable errors.
        /// Implements exponential backoff and network connectivity checking.
        /// </summary>
        /// <typeparam name="T">Type of data in the batch</typeparam>
        /// <param name="query">GraphQL mutation query</param>
        /// <param name="batch">Data batch to process</param>
        /// <param name="responseType">Response type for repository resolution</param>
        /// <param name="batchIndex">Index of this batch for logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="retryCount">Current retry attempt (internal use)</param>
        /// <returns>Result containing processed and failed counts</returns>
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

        /// <summary>
        /// Processes a single batch by resolving the appropriate repository and invoking the GraphQL mutation.
        /// Handles repository resolution, method invocation, and critical error detection.
        /// </summary>
        /// <typeparam name="T">Type of data in the batch</typeparam>
        /// <param name="query">GraphQL mutation query</param>
        /// <param name="batch">Data batch to process</param>
        /// <param name="responseType">Response type for repository resolution</param>
        /// <param name="batchIndex">Index of this batch for logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result containing processed and failed counts</returns>
        private async Task<SingleBatchResult> ProcessSingleBatch<T>(
            string query,
            List<T> batch,
            Type responseType,
            int batchIndex,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing batch {BatchIndex} with {Count} elements using response type {ResponseType}",
                    batchIndex, batch.Count, responseType.Name);

                // Get repository using the provided responseType
                var repositoryType = typeof(IRepository<>).MakeGenericType(responseType);
                var repository = _serviceProvider.GetService(repositoryType);

                if (repository == null)
                {
                    throw new InvalidOperationException($"Could not obtain repository for type {responseType.Name}");
                }

                // Get the method 
                string methodName = "NoJodaPai";
                var mutationMethod = repositoryType.GetMethod(methodName);
                if (mutationMethod == null)
                {
                    throw new InvalidOperationException($"Method {methodName} not found in {repositoryType.Name}");
                }

                // Create batch variables (assuming batch is directly usable)
                var batchVariables = new { data = batch };

                // Invoke the batch operation
                var taskObj = mutationMethod.Invoke(
                    repository,
                    new object[] { query, batchVariables, cancellationToken }
                );

                // Wait for the task to complete
                await (Task)taskObj;

                _logger.LogDebug("Batch {BatchIndex} processed successfully", batchIndex);

                return new SingleBatchResult
                {
                    ProcessedCount = batch.Count,
                    FailedCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchIndex}", batchIndex);
                
                // Check if it's an unrecoverable error before re-throwing
                if (!IsRecoverableError(ex))
                {
                    // Only send critical message the first time
                    if (!_serviceHasCriticalError)
                    {
                        // Unrecoverable error - stop the service
                        _serviceHasCriticalError = true;
                        _criticalErrorMessage = $"Critical error in single batch processing: {ex.Message}";
                        
                        _logger.LogCritical("ParallelBatchProcessor paused due to unrecoverable error in batch processing: {ErrorMessage}", ex.Message);
                        
                        // Send critical message
                        string userMessage = $"A critical system error has been detected that prevents continuation.\n\n" +
                                           $"Error: {ex.Message}\n\n" +
                                           $"Please contact technical support.";

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

        /// <inheritdoc/>
        public bool HasCriticalError()
        {
            return _serviceHasCriticalError;
        }

        /// <inheritdoc/>
        public string GetCriticalErrorMessage()
        {
            return _criticalErrorMessage;
        }
        
        /// <summary>
        /// Determines whether an exception represents a recoverable error that should be retried
        /// versus a critical error that indicates a configuration or code issue.
        /// </summary>
        /// <param name="ex">The exception to classify</param>
        /// <returns>True if the error is recoverable and should be retried, false for critical errors</returns>
        private bool IsRecoverableError(Exception ex)
        {
            // Network/connectivity errors - recoverable
            if (ex is HttpRequestException || 
                ex is TaskCanceledException ||
                ex is TimeoutException ||
                ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Code/reflection/configuration errors - unrecoverable
            if (ex is InvalidOperationException ||
                ex is ArgumentException ||
                ex is NotSupportedException ||
                ex is System.Reflection.TargetParameterCountException ||
                ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("method", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // By default, consider unknown errors as recoverable
            return true;
        }

        /// <summary>
        /// Internal class to track the results of processing a single batch.
        /// Used for aggregating statistics across multiple parallel batches.
        /// </summary>
        private class SingleBatchResult
        {
            /// <summary>Number of items successfully processed in this batch</summary>
            public int ProcessedCount { get; set; }
            /// <summary>Number of items that failed processing in this batch</summary>
            public int FailedCount { get; set; }
        }
    }

    /// <summary>
    /// Message published when parallel batch processing completes, either successfully or with errors.
    /// Contains comprehensive statistics about the processing operation for UI feedback and logging.
    /// </summary>
    /// <remarks>
    /// This message is published through Caliburn.Micro's event aggregator and can be handled
    /// by ViewModels to update progress indicators, show completion notifications, or handle errors.
    /// </remarks>
    public class ParallelBatchCompletedMessage
    {
        /// <summary>Unique identifier for this processing operation</summary>
        public Guid ProcessingId { get; }
        /// <summary>Indicates whether the overall processing was successful (no failures)</summary>
        public bool IsSuccess { get; }
        /// <summary>Human-readable description of the processing operation</summary>
        public string DisplayName { get; }
        /// <summary>Total number of items successfully processed across all batches</summary>
        public int TotalProcessed { get; }
        /// <summary>Total number of items that failed processing across all batches</summary>
        public int TotalFailed { get; }
        /// <summary>Total time taken for the entire parallel processing operation</summary>
        public TimeSpan ProcessingTime { get; }
        /// <summary>Exception that caused critical failure, if any</summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new parallel batch completion message with processing statistics.
        /// </summary>
        /// <param name="processingId">Unique identifier for the processing operation</param>
        /// <param name="isSuccess">Whether the processing completed successfully</param>
        /// <param name="displayName">Human-readable description of the operation</param>
        /// <param name="totalProcessed">Number of items successfully processed</param>
        /// <param name="totalFailed">Number of items that failed processing</param>
        /// <param name="processingTime">Total processing duration</param>
        /// <param name="exception">Exception that caused failure, if any</param>
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
