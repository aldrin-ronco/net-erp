using Caliburn.Micro;
using Microsoft.Extensions.Logging;
using NetErp.Helpers.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers.Services
{
    public interface INetworkConnectivityService
    {
        bool IsConnected();
        void StartMonitoring();
        void StopMonitoring();
    }

    public class NetworkConnectivityService : INetworkConnectivityService
    {
        private readonly ILogger<NetworkConnectivityService> _logger;
        private readonly IEventAggregator _eventAggregator;
        private bool _isRunning = false;
        private CancellationTokenSource? _cts;
        private Task? _monitoringTask;
        private bool _lastStatus = true;

        public NetworkConnectivityService(
            ILogger<NetworkConnectivityService> logger,
            IEventAggregator eventAggregator)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;
        }

        public bool IsConnected()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://clients3.google.com/generate_204"))
                {
                    return true;
                }
            }
            catch
            {
                _logger.LogWarning("No se detectó conexión a Internet");
                return false;
            }
        }

        public void StartMonitoring()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cts = new CancellationTokenSource();
            _monitoringTask = Task.Run(MonitorNetworkStatusAsync, _cts.Token);
        }

        public void StopMonitoring()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            _isRunning = false;
        }

        private async Task MonitorNetworkStatusAsync()
        {
            while (_isRunning)
            {
                try
                {
                    bool currentStatus = IsConnected();
                    if (currentStatus != _lastStatus)
                    {
                        _lastStatus = currentStatus;
                        await _eventAggregator.PublishOnUIThreadAsync(new NetworkStatusChangedMessage(currentStatus));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al verificar la conectividad");
                }

                // Verificamos cada 10 segundos
                await Task.Delay(10000);
            }
        }
    }
}
