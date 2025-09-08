using Caliburn.Micro;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using NetErp.Helpers.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

namespace NetErp.Helpers.Services
{
    /// <summary>
    /// Interface that defines a data operation that can be queued for background processing.
    /// All operations must provide batch processing information to be processed efficiently.
    /// </summary>
    /// <remarks>
    /// Data operations are automatically batched by type and processed asynchronously to improve performance.
    /// Each operation must have a unique ID to prevent duplicate processing and enable operation replacement.
    /// </remarks>
    public interface IDataOperation
    {
        /// <summary>
        /// Unique identifier for this operation instance.
        /// </summary>
        Guid OperationId { get; }
        
        /// <summary>
        /// Human-readable name for this operation, used for logging and user feedback.
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// The GraphQL model type that this operation will return when processed.
        /// Used to determine the appropriate repository service for processing.
        /// </summary>
        Type ResponseType { get; }
        
        /// <summary>
        /// Variables object containing the data for this specific operation.
        /// This will be extracted and combined with other operations for batch processing.
        /// </summary>
        object Variables { get; }
        
        /// <summary>
        /// Business entity identifier for this operation. Used to prevent duplicate operations
        /// and enable operation replacement (newer operations replace older ones for the same ID).
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the batch processing information for this operation type.
        /// This method is mandatory as all operations are processed in batches for optimal performance.
        /// </summary>
        /// <returns>BatchOperationInfo containing the query, extraction, and variable building logic</returns>
        BatchOperationInfo GetBatchInfo();
    }

    /// <summary>
    /// Contains the necessary information to process operations in batches efficiently.
    /// This class defines how individual operations are combined into batch GraphQL mutations.
    /// </summary>
    /// <remarks>
    /// Batch processing significantly improves performance by reducing the number of network calls
    /// and allowing the server to process multiple related operations in a single transaction.
    /// </remarks>
    public class BatchOperationInfo
    {
        /// <summary>
        /// The GraphQL mutation query to use for batch processing (required).
        /// This query should accept a collection of items and process them in a single operation.
        /// </summary>
        public string BatchQuery { get; set; } = string.Empty;

        /// <summary>
        /// Function to extract the relevant data from an individual operation's variables
        /// that will be included in the batch. This function is called for each operation
        /// in the batch to build the collection of items to process.
        /// </summary>
        public Func<object, object> ExtractBatchItem { get; set; }

        /// <summary>
        /// Function to construct the variables object for the batch operation.
        /// Takes the collection of extracted items and builds the complete variables
        /// object that will be sent with the BatchQuery.
        /// </summary>
        public Func<IEnumerable<object>, object> BuildBatchVariables { get; set; } 
    }

    /// <summary>
    /// Interface for the background queue service that processes data operations asynchronously.
    /// This service provides efficient batch processing, automatic retry logic, and network connectivity handling.
    /// </summary>
    /// <remarks>
    /// The service automatically batches operations by type and processes them efficiently to minimize network calls.
    /// It includes sophisticated retry mechanisms, network connectivity monitoring, and critical error handling.
    /// Operations are deduplicated by ID to prevent processing the same entity multiple times.
    /// 
    /// Key Features:
    /// - Automatic batching by operation type for optimal performance
    /// - Network connectivity monitoring with automatic retry when connection is restored
    /// - Operation deduplication and replacement based on entity ID
    /// - Critical error detection that can pause the service to prevent data corruption
    /// - Comprehensive logging and event notifications for operation status
    /// </remarks>
    /// <example>
    /// <code>
    /// // Inject the service
    /// private readonly IBackgroundQueueService _backgroundQueue;
    /// 
    /// // Enqueue an operation
    /// var operation = new MyDataOperation(data);
    /// var operationId = await _backgroundQueue.EnqueueOperationAsync(operation);
    /// 
    /// // Check for critical errors
    /// if (_backgroundQueue.HasCriticalError())
    /// {
    ///     var errorMessage = _backgroundQueue.GetCriticalErrorMessage();
    ///     // Handle critical error
    /// }
    /// </code>
    /// </example>
    public interface IBackgroundQueueService
    {
        /// <summary>
        /// Enqueues a data operation for background processing.
        /// Operations with the same ID will replace previous operations that haven't been processed yet.
        /// </summary>
        /// <param name="operation">The data operation to enqueue</param>
        /// <returns>The unique operation ID that can be used to track completion</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service has a critical error and cannot accept new operations</exception>
        Task<Guid> EnqueueOperationAsync(IDataOperation operation);
        
        /// <summary>
        /// Signals the service to complete processing all queued operations and stop accepting new ones.
        /// This method should be called during application shutdown.
        /// </summary>
        /// <returns>A task that completes when all queued operations have been processed</returns>
        Task CompleteAsync();
        
        /// <summary>
        /// Checks if the service has encountered a critical error that prevents further operation.
        /// When true, the service will reject new operations until the error is resolved.
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
    /// High-performance background queue service for processing data operations in batches.
    /// Implements sophisticated queueing, batching, retry logic, and network monitoring capabilities.
    /// </summary>
    /// <remarks>
    /// This service is designed to handle high-volume data operations efficiently by:
    /// 
    /// **Batch Processing**: Operations are automatically grouped by type and processed in batches
    /// to minimize network overhead and improve throughput.
    /// 
    /// **Smart Deduplication**: Operations with the same ID replace earlier operations, ensuring
    /// only the most recent version is processed.
    /// 
    /// **Network Resilience**: Monitors network connectivity and automatically retries operations
    /// when connection is restored.
    /// 
    /// **Error Classification**: Distinguishes between recoverable errors (network issues) and
    /// critical errors (configuration/code issues) to prevent infinite retry loops.
    /// 
    /// **Memory Management**: Includes automatic cleanup of completed operations to prevent
    /// memory leaks during long-running sessions.
    /// 
    /// **Event Integration**: Publishes operation completion events through Caliburn.Micro's
    /// event aggregator for UI updates and logging.
    /// 
    /// The service uses bounded channels for thread-safe queuing and supports graceful shutdown
    /// with proper resource cleanup.
    /// </remarks>
    public class BackgroundQueueService : IBackgroundQueueService, IDisposable
    {
        /// <summary>Primary queue for new operations</summary>
        private readonly Channel<IDataOperation> _queue;
        /// <summary>Secondary queue for operations that need to be retried</summary>
        private readonly Channel<IDataOperation> _retryQueue;
        /// <summary>Service provider for resolving repository instances dynamically</summary>
        private readonly IServiceProvider _serviceProvider;
        /// <summary>Logger for operation tracking and debugging</summary>
        private readonly ILogger<BackgroundQueueService> _logger;
        /// <summary>Network connectivity monitoring service</summary>
        private readonly INetworkConnectivityService _networkService;
        /// <summary>Event aggregator for publishing operation completion events</summary>
        private readonly IEventAggregator _eventAggregator;
        /// <summary>Cancellation token source for graceful shutdown</summary>
        private readonly CancellationTokenSource _cts = new();
        /// <summary>Background processing task</summary>
        private Task? _processingTask;
        
        /// <summary>Maximum number of operations to process in a single batch</summary>
        private const int MaxBatchSize = 10;
        /// <summary>Timeout for batch collection in milliseconds</summary>
        private const int BatchTimeoutMs = 10000;
        /// <summary>Maximum number of retry attempts for failed operations</summary>
        private const int MaxRetries = 3;
        /// <summary>Delay between retry attempts</summary>
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
        
        /// <summary>Flag indicating if the service has encountered a critical error</summary>
        private bool _serviceHasCriticalError = false;
        /// <summary>Message describing the current critical error</summary>
        private string _criticalErrorMessage = string.Empty;

        /// <summary>Storage for pending operations indexed by entity ID for deduplication</summary>
        private readonly Dictionary<int, IDataOperation> _pendingOperationsById = new();
        /// <summary>Timestamps of completed operations for cleanup purposes</summary>
        private readonly Dictionary<int, DateTime> _completedOperationsTimestamp = new();
        /// <summary>Lock object for thread-safe access to operation dictionaries</summary>
        private readonly object _lockObject = new();
        /// <summary>Interval for cleaning up old completed operation records</summary>
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        /// <summary>Timestamp of last cleanup operation</summary>
        private DateTime _lastCleanup = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the BackgroundQueueService with dependency injection.
        /// Automatically starts background processing and network monitoring.
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving repository instances</param>
        /// <param name="logger">Logger for operation tracking and debugging</param>
        /// <param name="networkService">Network connectivity monitoring service</param>
        /// <param name="eventAggregator">Event aggregator for publishing completion events</param>
        public BackgroundQueueService(
            IServiceProvider serviceProvider,
            ILogger<BackgroundQueueService> logger,
            INetworkConnectivityService networkService,
            IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _networkService = networkService;
            _eventAggregator = eventAggregator;

            // Configure bounded channels for thread-safe queuing
            _queue = Channel.CreateBounded<IDataOperation>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true
            });

            _retryQueue = Channel.CreateBounded<IDataOperation>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true
            });

            // Start background processing and network monitoring
            _processingTask = Task.Run(ProcessOperationsAsync);
            _networkService.StartMonitoring();
        }

        /// <inheritdoc/>
        public async Task<Guid> EnqueueOperationAsync(IDataOperation operation)
        {
            // Check if service has a critical error
            if (_serviceHasCriticalError)
            {
                _logger.LogError("Rechazando operación debido a error crítico del servicio: {ErrorMessage}", _criticalErrorMessage);
                throw new InvalidOperationException($"Background service has critical error: {_criticalErrorMessage}");
            }

            // Reemplazar o agregar operación por el Id definido en la operación
            lock (_lockObject)
            {
                _pendingOperationsById[operation.Id] = operation;
            }

            await _queue.Writer.WriteAsync(operation, _cts.Token);
            _logger.LogDebug("Operación encolada/reemplazada: {OperationId} - {DisplayName} - ID:{Id}",
                operation.OperationId, operation.DisplayName, operation.Id);

            return operation.OperationId;
        }

        /// <summary>
        /// Main background processing loop that handles batching, retry logic, and network monitoring.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        private async Task ProcessOperationsAsync()
        {
            var batch = new List<IDataOperation>();
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(BatchTimeoutMs));

            try
            {
                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    // Verificamos la conectividad antes de procesar
                    if (!await _networkService.IsConnectedAsync())
                    {
                        _logger.LogWarning("No hay conexión a Internet. Esperando...");
                        continue;
                    }

                    // Primero procesamos reintentos pendientes
                    while (_retryQueue.Reader.TryRead(out var retryOperation))
                    {
                        batch.Add(retryOperation);
                        if (batch.Count >= MaxBatchSize)
                        {
                            await ProcessBatchWithRetry(batch);
                            batch.Clear();
                        }
                    }

                    // Luego procesamos operaciones nuevas
                    while (_queue.Reader.TryRead(out var operation))
                    {
                        // Verificar si esta es la versión más reciente de la operación para este ID
                        bool addOperation;
                        lock (_lockObject)
                        {
                            addOperation = !_pendingOperationsById.TryGetValue(operation.Id, out var latestOperation) ||
                                          ReferenceEquals(latestOperation, operation);
                        }

                        if (addOperation)
                        {
                            batch.Add(operation);
                            if (batch.Count >= MaxBatchSize)
                            {
                                await ProcessBatchWithRetry(batch);
                                batch.Clear();
                            }
                        }
                    }

                    if (batch.Count > 0)
                    {
                        await ProcessBatchWithRetry(batch);
                        batch.Clear();
                    }
                    
                    // Limpieza periódica de operaciones completadas
                    if (DateTime.UtcNow - _lastCleanup > _cleanupInterval)
                    {
                        CleanupCompletedOperations();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Salida controlada
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el procesamiento de operaciones");
            }
        }

        /// <summary>
        /// Processes a batch of operations with retry logic and error classification.
        /// Handles both recoverable errors (network issues) and critical errors (configuration issues).
        /// </summary>
        /// <param name="batch">The batch of operations to process</param>
        /// <param name="retryCount">Current retry attempt (0-based)</param>
        private async Task ProcessBatchWithRetry(List<IDataOperation> batch, int retryCount = 0)
        {
            try
            {
                // Si no hay conectividad, enviamos todo a la cola de reintentos
                if (!await _networkService.IsConnectedAsync())
                {
                    _logger.LogWarning("Sin conexión a Internet. Moviendo batch a cola de reintentos.");
                    foreach (var op in batch)
                    {
                        await _retryQueue.Writer.WriteAsync(op, _cts.Token);
                    }
                    return;
                }

                await ProcessBatch(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando batch. Reintento {RetryCount}/{MaxRetries}", retryCount, MaxRetries);

                // Verificar si es un error irrecuperable
                bool isRecoverable = IsRecoverableError(ex);
                
                if (!isRecoverable)
                {
                    // Error irrecuperable - parar el servicio
                    _serviceHasCriticalError = true;
                    _criticalErrorMessage = $"Critical error in batch processing: {ex.Message}";
                    
                    _logger.LogCritical("Servicio pausado debido a error irrecuperable: {ErrorMessage}", ex.Message);
                    
                    // El mensaje crítico ya fue enviado desde ProcessOperationBatch
                    return; // No continuar con reintentos
                }

                // Error recuperable - lógica normal de reintentos
                if (retryCount < MaxRetries)
                {
                    // Esperamos antes de reintentar
                    await Task.Delay(RetryDelay);
                    await ProcessBatchWithRetry(batch, retryCount + 1);
                }
                else
                {
                    _logger.LogWarning("Batch fallido después de {MaxRetries} reintentos (error recuperable). Moviendo a cola de reintentos.", MaxRetries);

                    // Solo para errores recuperables, movemos a la cola de reintentos
                    foreach (var op in batch)
                    {
                        // Notificamos el fallo temporal
                        await _eventAggregator.PublishOnUIThreadAsync(
                            new OperationCompletedMessage(
                                op.OperationId,
                                false,
                                op.DisplayName,
                                ex
                            )
                        );

                        await _retryQueue.Writer.WriteAsync(op, _cts.Token);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a batch of operations by grouping them by response type and operation type
        /// for optimal batching and parallel processing.
        /// </summary>
        /// <param name="batch">The batch of operations to process</param>
        private async Task ProcessBatch(List<IDataOperation> batch)
        {
            try
            {
                // Remove duplicates within the same batch based on operation ID
                var distinctBatch = batch.GroupBy(op => op.Id).Select(g => g.Last()).ToList();

                // Group operations by response type for repository resolution
                var groupedOperations = distinctBatch.GroupBy(op => op.ResponseType);

                // Create parallel processing tasks for each response type group
                var groupTasks = groupedOperations.Select(async group =>
                {
                    var operations = group.ToList();
                    var responseType = group.Key;

                    // Resolve the repository service for this response type
                    var repositoryType = typeof(IRepository<>).MakeGenericType(responseType);
                    var repository = _serviceProvider.GetService(repositoryType);
                    
                    if (repository == null)
                    {
                        _logger.LogError("Could not obtain repository for type {ResponseTypeName}", responseType.Name);
                        throw new InvalidOperationException($"Repository for type {responseType.Name} not found in service provider");
                    }

                    // Group by operation type for optimal batch processing
                    var typeGroups = operations.GroupBy(op => op.GetType());

                    // Process each operation type group in parallel
                    var typeGroupTasks = typeGroups.Select(async typeGroup =>
                    {
                        var opsOfSameType = typeGroup.ToList();

                        // All operations of the same type share the same batch configuration
                        var batchInfo = opsOfSameType.First().GetBatchInfo();

                        // Split large groups into manageable batch sizes
                        for (int i = 0; i < opsOfSameType.Count; i += MaxBatchSize)
                        {
                            var batchSize = Math.Min(MaxBatchSize, opsOfSameType.Count - i);
                            var batchToProcess = opsOfSameType.GetRange(i, batchSize);

                            await ProcessOperationBatch(batchToProcess, batchInfo, repositoryType, repository, responseType);
                        }
                    });

                    await Task.WhenAll(typeGroupTasks);
                });

                // Wait for all response type groups to complete processing
                await Task.WhenAll(groupTasks);
            }
            catch
            {
                // Re-throw so retry mechanism can handle it
                throw;
            }
        }

        /// <summary>
        /// Processes a specific batch of operations of the same type using the provided repository.
        /// Handles GraphQL mutation execution, success/failure notifications, and operation cleanup.
        /// </summary>
        /// <param name="batch">Operations to process in this batch</param>
        /// <param name="batchInfo">Batch processing configuration</param>
        /// <param name="repositoryType">Type of the repository service</param>
        /// <param name="repository">Repository instance for processing</param>
        /// <param name="responseType">Expected response type for error handling</param>
        private async Task ProcessOperationBatch(
            List<IDataOperation> batch,
            BatchOperationInfo batchInfo,
            Type repositoryType,
            object repository,
            Type responseType)
        {
            try
            {
                // Extract and format elements for the batch
                var batchItems = batch.Select(op => batchInfo.ExtractBatchItem(op.Variables)).ToList();

                // Build variables for the complete batch
                var batchVariables = batchInfo.BuildBatchVariables(batchItems);

                // Get the batch query
                var batchQuery = batchInfo.BatchQuery;

                string methodName = "SendMutationListAsync";
                // Method is SendMutationListAsync for processing batches in IRepository
                var mutationMethod = repositoryType.GetMethod(methodName);
                if (mutationMethod == null)
                {
                    _logger.LogError("Method {MethodName} not found in {repositoryTypeName}", methodName, repositoryType.Name);
                    throw new InvalidOperationException($"Method {methodName} not found on {repositoryType.Name}");
                }

                // Invoke the batch operation
                var taskObj = mutationMethod.Invoke(
                    repository,
                    [batchQuery, batchVariables, _cts.Token]
                );
                
                if (taskObj == null)
                {
                    _logger.LogError("Method invocation {MethodName} returned null", methodName);
                    throw new InvalidOperationException("Method invocation returned null");
                }

                // Wait for the task to complete
                await (Task)taskObj;

                // Notify success to all elements in the batch
                foreach (var op in batch)
                {
                    await _eventAggregator.PublishOnUIThreadAsync(
                        new OperationCompletedMessage(
                            op.OperationId,
                            true,
                            op.DisplayName
                        )
                    );

                    // Remove from pending operations and mark as completed
                    lock (_lockObject)
                    {
                        _pendingOperationsById.Remove(op.Id);
                        _completedOperationsTimestamp[op.Id] = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing operation batch");

                // Check if it's an unrecoverable error and we have type information
                bool isIrrecoverable = !IsRecoverableError(ex);
                if (isIrrecoverable && responseType != null)
                {
                    // Send critical message to block the affected module
                    string userMessage = $"A critical system error has been detected that prevents continuation.\n\n" +
                                       $"Error: {ex.Message}\n\n" +
                                       $"Please contact technical support.";
                    
                    await _eventAggregator.PublishOnUIThreadAsync(
                        new CriticalSystemErrorMessage(
                            responseType,
                            nameof(BackgroundQueueService),
                            ex.Message, 
                            userMessage
                        )
                    );
                }

                // Notify error to all elements in the batch
                foreach (var op in batch)
                {
                    await _eventAggregator.PublishOnUIThreadAsync(
                        new OperationCompletedMessage(
                            op.OperationId,
                            false,
                            op.DisplayName,
                            ex
                        )
                    );
                }

                throw;
            }
        }

        /// <summary>
        /// Performs periodic cleanup of completed operation timestamps to prevent memory leaks.
        /// Removes records of operations completed more than 30 minutes ago.
        /// </summary>
        private void CleanupCompletedOperations()
        {
            lock (_lockObject)
            {
                try
                {
                    // Clean operations completed more than 30 minutes ago
                    var cutoffTime = DateTime.UtcNow.AddMinutes(-30);
                    var keysToRemove = _completedOperationsTimestamp
                        .Where(kvp => kvp.Value < cutoffTime)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in keysToRemove)
                    {
                        _completedOperationsTimestamp.Remove(key);
                    }

                    _lastCleanup = DateTime.UtcNow;

                    if (keysToRemove.Count > 0)
                    {
                        _logger.LogDebug("Cleanup completed: {Count} operations removed", keysToRemove.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during cleanup of completed operations");
                }
            }
        }

        /// <summary>
        /// Determines if an exception represents a recoverable error that should be retried
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

        /// <inheritdoc/>
        public async Task CompleteAsync()
        {
            _queue.Writer.Complete();
            await (_processingTask ?? Task.CompletedTask);
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
        /// Performs cleanup of resources including stopping network monitoring and canceling background tasks.
        /// </summary>
        public void Dispose()
        {
            _networkService.StopMonitoring();
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
