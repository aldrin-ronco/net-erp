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
    /// <summary>
    /// Interface for monitoring network connectivity status in the application.
    /// Provides methods to check internet connectivity and monitor connection state changes.
    /// </summary>
    /// <remarks>
    /// This service is essential for applications that rely heavily on network operations,
    /// such as GraphQL API calls, background data synchronization, and real-time updates.
    /// It helps prevent unnecessary network operation attempts during offline periods
    /// and provides user feedback about connection status.
    /// </remarks>
    public interface INetworkConnectivityService
    {
        /// <summary>
        /// Asynchronously checks if the device has internet connectivity.
        /// Tests multiple endpoints to ensure reliable connectivity detection.
        /// </summary>
        /// <returns>True if internet connection is available, false otherwise</returns>
        /// <remarks>
        /// This method attempts to connect to multiple well-known endpoints for redundancy.
        /// It's designed to be fast and reliable, with appropriate timeouts to prevent hanging.
        /// </remarks>
        Task<bool> IsConnectedAsync();
        
        /// <summary>
        /// Starts continuous monitoring of network connectivity status.
        /// Publishes events when connection state changes (connected/disconnected).
        /// </summary>
        /// <remarks>
        /// Once started, the service will check connectivity at regular intervals
        /// and publish NetworkStatusChangedMessage events through the event aggregator
        /// whenever the connection status changes.
        /// </remarks>
        void StartMonitoring();
        
        /// <summary>
        /// Stops the continuous network connectivity monitoring.
        /// </summary>
        /// <remarks>
        /// This method gracefully cancels the monitoring loop and cleans up resources.
        /// It's safe to call multiple times and will have no effect if monitoring is not active.
        /// </remarks>
        void StopMonitoring();
    }

    /// <summary>
    /// Implementation of network connectivity monitoring service for NetERP application.
    /// Provides robust internet connectivity detection and continuous monitoring capabilities.
    /// </summary>
    /// <remarks>
    /// This service implements a multi-endpoint connectivity testing approach for maximum reliability.
    /// It uses well-known, lightweight endpoints from major internet providers (Google, Microsoft, Cloudflare)
    /// to ensure accurate connectivity detection even when some services might be blocked or unavailable.
    /// 
    /// **Key Features:**
    /// - **Multi-Endpoint Testing**: Tests multiple endpoints for redundancy
    /// - **Continuous Monitoring**: Background thread monitors connectivity changes
    /// - **Event Publishing**: Integrates with Caliburn.Micro event system
    /// - **Timeout Protection**: Prevents hanging on slow or unresponsive networks
    /// - **Thread-Safe Operations**: Safe for concurrent use across the application
    /// - **Graceful Resource Management**: Proper disposal of HTTP clients and cancellation tokens
    /// 
    /// **Usage Pattern:**
    /// The service is typically started during application initialization and runs continuously
    /// throughout the application lifecycle, providing real-time connectivity status to other
    /// services like BackgroundQueueService and ParallelBatchProcessor.
    /// </remarks>
    public class NetworkConnectivityService : INetworkConnectivityService, IDisposable
    {
        /// <summary>Logger for connectivity events and debugging</summary>
        private readonly ILogger<NetworkConnectivityService> _logger;
        /// <summary>Event aggregator for publishing connectivity status changes</summary>
        private readonly IEventAggregator _eventAggregator;
        /// <summary>HTTP client for connectivity testing with configured timeout</summary>
        private readonly HttpClient _httpClient;
        /// <summary>Lock object for thread-safe state management</summary>
        private readonly object _lockObject = new();
        /// <summary>Flag indicating if monitoring is currently active</summary>
        private volatile bool _isRunning = false;
        /// <summary>Cancellation token source for graceful monitoring shutdown</summary>
        private CancellationTokenSource? _cts;
        /// <summary>Background monitoring task</summary>
        private Task? _monitoringTask;
        /// <summary>Last known connectivity status for change detection</summary>
        private bool _lastStatus = true;
        
        /// <summary>
        /// Multiple connectivity test endpoints for fallback reliability.
        /// Uses lightweight endpoints from major providers to ensure broad compatibility.
        /// </summary>
        private readonly string[] _connectivityUrls = 
        {
            "https://www.google.com/generate_204",
            "https://www.microsoft.com/favicon.ico", 
            "https://www.cloudflare.com/"
        };

        /// <summary>
        /// Initializes a new instance of the NetworkConnectivityService with dependency injection.
        /// Configures HTTP client with appropriate timeout for connectivity testing.
        /// </summary>
        /// <param name="logger">Logger for connectivity events and debugging</param>
        /// <param name="eventAggregator">Event aggregator for publishing status change events</param>
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

        /// <inheritdoc/>
        public async Task<bool> IsConnectedAsync()
        {
            // Attempt to connect to multiple endpoints for greater reliability
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
                    // Operation canceled, exit immediately
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Connection failed to {Url}: {Error}", url, ex.Message);
                    // Continue with next endpoint
                }
            }
            
            _logger.LogWarning("No internet connection detected on any endpoint");
            return false;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <summary>
        /// Background monitoring loop that continuously checks network connectivity status.
        /// Publishes events when connectivity status changes and handles graceful shutdown.
        /// </summary>
        /// <remarks>
        /// This method runs on a background thread and checks connectivity every 10 seconds.
        /// It publishes NetworkStatusChangedMessage events only when the status actually changes
        /// to avoid flooding the event system with redundant notifications.
        /// </remarks>
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
                        var statusMessage = currentStatus ? "Internet connection established" : "Internet connection lost";
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
                    _logger.LogError(ex, "Error checking connectivity");
                }

                try
                {
                    // Check every 10 seconds with cancellation support
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

        /// <summary>
        /// Performs cleanup of resources including stopping monitoring, disposing HTTP client,
        /// and canceling background tasks with appropriate timeout.
        /// </summary>
        /// <remarks>
        /// This method implements proper resource disposal pattern with timeout protection
        /// to prevent hanging during application shutdown. It safely handles disposal
        /// even if monitoring was not started or already disposed.
        /// </remarks>
        public void Dispose()
        {
            try
            {
                StopMonitoring();
                
                // Wait a moment for monitoring to finish
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
