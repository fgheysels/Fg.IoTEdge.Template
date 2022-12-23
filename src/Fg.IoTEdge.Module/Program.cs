using System.Net;
using Fg.IoTEdgeModule;
using Fg.IoTEdgeModule.Configuration;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FgModule
{
    class Program
    {
        private static ShutdownHandler _shutdownHandler;

        static async Task Main(string[] args)
        {
            using (var host = CreateHostBuilder(args).Build())
            {
                var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
                ILogger logger = loggerFactory.CreateLogger<Program>();

                _shutdownHandler = ShutdownHandler.Create(TimeSpan.FromSeconds(20), logger);

                logger.LogInformation("Starting IoTEdgeModule");

                await host.StartAsync(_shutdownHandler.CancellationTokenSource.Token);

                logger.LogInformation("IoTEdgeModule started");
                Endpoints.ReportEndpoints(logger);
                //BackgroundServiceWaitHandle.SetSignal();

                await host.WaitForShutdownAsync(_shutdownHandler.CancellationTokenSource.Token);

                _shutdownHandler.SignalCleanupComplete();
                logger.LogInformation("IoTEdgeModule stopped");
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureIoTEdgeModuleClient(TransportType.Amqp_Tcp_Only, configureModuleClient =>
                       {
                           configureModuleClient.SetDesiredPropertyUpdateCallbackAsync((twin, context) =>
                           {
                               // When the desired properties have changed, force a restart of the module.
                               Console.WriteLine("Desired properties have changed, forcing a restart of the module");

                               _shutdownHandler.CancellationTokenSource.Cancel();
                               return Task.CompletedTask;

                           }, configureModuleClient).Wait();
                       })
                       .ConfigureServices(services =>
                       {
                           services.AddSingleton<FgModuleConfiguration>(sp =>
                           {
                               var moduleClient = sp.GetService<ModuleClient>();

                               return ModuleConfiguration.CreateFromTwinAsync<FgModuleConfiguration>(
                                                            moduleClient,
                                                            sp.GetRequiredService<ILogger<FgModuleConfiguration>>())
                                                         .GetAwaiter().GetResult();
                           });
                       })
                       .ConfigureLogging(logging =>
                       {
                           var configuration = logging.Services.BuildServiceProvider().GetService<FgModuleConfiguration>();

                           LogLevel logLevel = LogLevel.Information;

                           if (configuration != null)
                           {
                               logLevel = configuration.MinimumLogLevel;
                           }

                           logging.SetMinimumLevel(logLevel);

                           logging.AddSystemdConsole(consoleLogging =>
                           {
                               consoleLogging.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz ";
                           });
                       })
                       .ConfigureServices(services =>
                       {
                           ////services.AddSingleton(_ => VesselIdentifier.GetVesselIdentifier());

                           ////services.AddSingleton<IEnumerable<IMetricCreator>>(sp => GetImplementationsOf<IMetricCreator>(sp));
                           ////services.AddSingleton<IEnumerable<IMetricQualityInspector>>(sp => GetImplementationsOf<IMetricQualityInspector>(sp));

                           ////services.AddSingleton(sp => VesselTelemetryTransformer.Create(sp.GetService<IEnumerable<IMetricCreator>>(), sp.GetService<IEnumerable<IMetricQualityInspector>>(), sp.GetService<ILogger<VesselTelemetryTransformer>>()));

                           ////services.AddHostedService<App>();
                       })
                       .UseConsoleLifetime();
        }
    }
}