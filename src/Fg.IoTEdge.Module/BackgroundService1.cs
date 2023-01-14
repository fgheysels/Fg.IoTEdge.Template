using System.Text;
using Fg.IoTEdgeModule;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FgModule
{
    internal class BackgroundService1 : BackgroundService
    {
        private readonly ModuleClient _moduleClient;
        private readonly IHostApplicationLifetime _hostLifetime;
        private readonly ILogger<BackgroundService1> _logger;

        public BackgroundService1(ModuleClient moduleClient, IHostApplicationLifetime hostLifetime, ILogger<BackgroundService1> logger)
        {
            _moduleClient = moduleClient;
            _hostLifetime = hostLifetime;
            _logger = logger;

            _hostLifetime.ApplicationStopping.Register(() => _logger.LogInformation("stopping host"));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await _moduleClient.SetInputMessageHandlerAsync(Endpoints.Input1, OnMessageReceived, _moduleClient, cancellationToken);

            await BackgroundServiceWaitHandle.WaitForSignalAsync();

            while (!cancellationToken.IsCancellationRequested)
            {
                // TODO: write some long running logic here.
            }
        }

        private Task<MessageResponse> OnMessageReceived(Message message, object state)
        {
            string messageContent = Encoding.UTF8.GetString(message.GetBytes());

            _logger.LogInformation($"Message received: {messageContent}");

            return Task.FromResult(MessageResponse.Completed);
        }
    }
}
