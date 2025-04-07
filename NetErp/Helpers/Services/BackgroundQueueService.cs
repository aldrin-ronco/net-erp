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

namespace NetErp.Helpers.Services
{
    public interface IDataOperation
    {
        string Query { get; }
        object Variables { get; }
        Type ResponseType { get; }
        string GenericDataAccessMethod { get; }
        Guid OperationId { get; }
        string DisplayName { get; }
        int Id { get; }

        // Método para obtener la información de lote para esta operación
        // Este método es obligatorio, ya que todo se procesará en lotes
        BatchOperationInfo GetBatchInfo();
    }

    // Clase que contiene la información necesaria para procesar operaciones en lote
    public class BatchOperationInfo
    {
        // Query a usar para la operación en lote (obligatorio)
        public string BatchQuery { get; set; }

        // Método para extraer el valor a incluir en el lote desde las variables de la operación
        public Func<object, object> ExtractBatchItem { get; set; }

        // Método para construir el objeto de variables del lote
        public Func<IEnumerable<object>, object> BuildBatchVariables { get; set; }
    }

    public interface IBackgroundQueueService
    {
        Task<Guid> EnqueueOperationAsync(IDataOperation operation);
        Task CompleteAsync();
    }

    public class BackgroundQueueService : IBackgroundQueueService, IDisposable
    {
        private readonly Channel<IDataOperation> _queue;
        private readonly Channel<IDataOperation> _retryQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundQueueService> _logger;
        private readonly INetworkConnectivityService _networkService;
        private readonly IEventAggregator _eventAggregator;
        private readonly CancellationTokenSource _cts = new();
        private Task? _processingTask;
        private const int MaxBatchSize = 10;
        private const int BatchTimeoutMs = 10000; // 20 segundos
        private const int MaxRetries = 3;
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        // Almacenamiento de operaciones por ID de producto
        private readonly Dictionary<int, IDataOperation> _pendingOperationsById = new();
        private readonly object _lockObject = new();

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

            // Configuración de colas
            _queue = Channel.CreateBounded<IDataOperation>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true
            });

            _retryQueue = Channel.CreateBounded<IDataOperation>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = true
            });

            // Iniciar procesamiento y monitoreo de red
            _processingTask = Task.Run(ProcessOperationsAsync);
            _networkService.StartMonitoring();
        }

        public async Task<Guid> EnqueueOperationAsync(IDataOperation operation)
        {
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

        private async Task ProcessOperationsAsync()
        {
            var batch = new List<IDataOperation>();
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(BatchTimeoutMs));

            try
            {
                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    // Verificamos la conectividad antes de procesar
                    if (!_networkService.IsConnected())
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

        private async Task ProcessBatchWithRetry(List<IDataOperation> batch, int retryCount = 0)
        {
            try
            {
                // Si no hay conectividad, enviamos todo a la cola de reintentos
                if (!_networkService.IsConnected())
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

                if (retryCount < MaxRetries)
                {
                    // Esperamos antes de reintentar
                    await Task.Delay(RetryDelay);
                    await ProcessBatchWithRetry(batch, retryCount + 1);
                }
                else
                {
                    _logger.LogError("Batch fallido después de {MaxRetries} reintentos. Moviendo a cola de reintentos para posterior procesamiento.", MaxRetries);

                    // Movemos a la cola de reintentos para procesar más tarde
                    foreach (var op in batch)
                    {
                        // Notificamos el fallo
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

        private async Task ProcessBatch(List<IDataOperation> batch)
        {
            try
            {
                // Elimina duplicados dentro del mismo batch basado en el Id de la operación
                var distinctBatch = batch.GroupBy(op => op.Id).Select(g => g.Last()).ToList();

                // Group operations by response type
                var groupedOperations = distinctBatch.GroupBy(op => op.ResponseType);

                // Create tasks for each response type group
                var groupTasks = groupedOperations.Select(async group =>
                {
                    var operations = group.ToList();
                    var responseType = group.Key;

                    // Get the generic data access service
                    var dataAccessType = typeof(IGenericDataAccess<>).MakeGenericType(responseType);
                    var dataAccess = _serviceProvider.GetService(dataAccessType);

                    // Group by operation type for optimal batching
                    var typeGroups = operations.GroupBy(op => op.GetType());

                    // Process each type group in parallel
                    var typeGroupTasks = typeGroups.Select(async typeGroup =>
                    {
                        var opsOfSameType = typeGroup.ToList();

                        // All operations of the same type use the same batch info
                        var batchInfo = opsOfSameType.First().GetBatchInfo();

                        // Split into manageable batches
                        for (int i = 0; i < opsOfSameType.Count; i += MaxBatchSize)
                        {
                            var batchSize = Math.Min(MaxBatchSize, opsOfSameType.Count - i);
                            var batchToProcess = opsOfSameType.GetRange(i, batchSize);

                            await ProcessOperationBatch(batchToProcess, batchInfo, dataAccessType, dataAccess);
                        }
                    });

                    await Task.WhenAll(typeGroupTasks);
                });

                // Wait for all response type groups to complete
                await Task.WhenAll(groupTasks);
            }
            catch
            {
                // Relanzamos para que el mecanismo de reintentos lo capture
                throw;
            }
        }

        private async Task ProcessOperationBatch(
            List<IDataOperation> batch,
            BatchOperationInfo batchInfo,
            Type dataAccessType,
            object dataAccess)
        {
            try
            {
                // Extraer y formatear elementos para el lote
                var batchItems = batch.Select(op => batchInfo.ExtractBatchItem(op.Variables)).ToList();

                // Construir variables para el lote completo
                var batchVariables = batchInfo.BuildBatchVariables(batchItems);

                // Obtener la query del lote
                var batchQuery = batchInfo.BatchQuery;

                // Método siempre es SendMutationList para procesar lotes
                var sendMutationListMethod = dataAccessType.GetMethod("SendMutationList");

                // Invocar la operación en lote
                var taskObj = sendMutationListMethod.Invoke(
                    dataAccess,
                    new object[] { batchQuery, batchVariables }
                );

                // Esperar a que termine la tarea
                await (Task)taskObj;

                // Notificar éxito a todos los elementos del lote
                foreach (var op in batch)
                {
                    await _eventAggregator.PublishOnUIThreadAsync(
                        new OperationCompletedMessage(
                            op.OperationId,
                            true,
                            op.DisplayName
                        )
                    );

                    // Eliminar de operaciones pendientes
                    lock (_lockObject)
                    {
                        _pendingOperationsById.Remove(op.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando lote de operaciones");

                // Notificar error a todos los elementos del lote
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

        public async Task CompleteAsync()
        {
            _queue.Writer.Complete();
            await (_processingTask ?? Task.CompletedTask);
        }

        public void Dispose()
        {
            _networkService.StopMonitoring();
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
