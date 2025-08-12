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

    // Mensaje para estado de red
    public class NetworkStatusChangedMessage
    {
        public bool IsConnected { get; }

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
