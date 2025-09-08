using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Helpers.Messages
{
    public class OperationCompletedMessage
    {
        public Guid OperationId { get; }
        public bool Success { get; }
        public Exception? Exception { get; }
        public string DisplayName { get; }

        public OperationCompletedMessage(Guid operationId, bool success, string displayName, Exception? exception = null)
        {
            OperationId = operationId;
            Success = success;
            Exception = exception;
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// Message published when network connectivity status changes.
    /// Used by NetworkConnectivityService to notify other components about connection state changes.
    /// </summary>
    /// <remarks>
    /// This message is published through Caliburn.Micro's event aggregator whenever
    /// the network connectivity status changes from connected to disconnected or vice versa.
    /// Components like BackgroundQueueService and ParallelBatchProcessor subscribe to
    /// these events to adjust their behavior based on connectivity status.
    /// </remarks>
    public class NetworkStatusChangedMessage
    {
        /// <summary>
        /// Indicates the current network connectivity status.
        /// True if internet connection is available, false if disconnected.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Initializes a new network status change message.
        /// </summary>
        /// <param name="isConnected">The current connectivity status</param>
        public NetworkStatusChangedMessage(bool isConnected)
        {
            IsConnected = isConnected;
        }
    }

    // Mensaje para errores críticos del sistema que requieren bloqueo de módulo
    public class CriticalSystemErrorMessage
    {
        public Type ResponseType { get; }
        public string ServiceName { get; }
        public string ErrorMessage { get; }
        public string UserMessage { get; }
        public DateTime Timestamp { get; }

        public CriticalSystemErrorMessage(Type responseType, string serviceName, string errorMessage, string userMessage)
        {
            ResponseType = responseType;
            ServiceName = serviceName;
            ErrorMessage = errorMessage;
            UserMessage = userMessage;
            Timestamp = DateTime.UtcNow;
        }
    }
}
