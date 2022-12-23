using Fg.IoTEdgeModule;
using Fg.IoTEdgeModule.Configuration;
using Microsoft.Azure.Devices.Client;
#if BackgroundServices
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#endif
using Microsoft.Extensions.Logging;

namespace FgModule
{
    class Program
    {
        private static ShutdownHandler _shutdownHandler;

        static async Task Main(string[] args)
        {
#if BackgroundServices
            using (var host = CreateHostBuilder(args).Build())
            {
                var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
                ILogger logger = loggerFactory.CreateLogger<Program>();

                _shutdownHandler = ShutdownHandler.Create(TimeSpan.FromSeconds(20), logger);

                logger.LogInformation("Starting FgModule");

                await host.StartAsync(_shutdownHandler.CancellationTokenSource.Token);

                logger.LogInformation("FgModule started");
                Endpoints.ReportEndpoints(logger);
                //BackgroundServiceWaitHandle.SetSignal();

                await host.WaitForShutdownAsync(_shutdownHandler.CancellationTokenSource.Token);

                _shutdownHandler.SignalCleanupComplete();
                logger.LogInformation("FgModule stopped");
            }
#else
            var loggerFactory = CreateLoggerFactory(LogLevel.Information);

            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation("Initializing FgModule ...");

            _shutdownHandler = ShutdownHandler.Create(TimeSpan.FromSeconds(20), loggerFactory.CreateLogger<ShutdownHandler>());

            var moduleClient = await CreateModuleClientAsync();

            var moduleConfiguration = await ModuleConfiguration.CreateFromTwinAsync<FgModuleConfiguration>(moduleClient, loggerFactory.CreateLogger<ModuleConfiguration>());

            ApplyConfiguration(moduleConfiguration);

            loggerFactory = CreateLoggerFactory(moduleConfiguration.MinimumLogLevel);

            await ConfigureDirectMethodHandlersAsync(moduleClient, loggerFactory);

            logger.LogInformation("FgModule initialized and running.");

            Endpoints.ReportEndpoints(logger);

            await WhenCancelled(_shutdownHandler.CancellationTokenSource.Token);

            logger.LogInformation("Shutting down FgModule ...");

            await moduleClient.DisposeAsync();

            _shutdownHandler.SignalCleanupComplete();

            logger.LogInformation("FgModule stopped.");
#endif
        }

#if BackgroundServices
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
                           services.AddHostedService<BackgroundService1>();
                       })
                       .UseConsoleLifetime();
        }
#else
        private static ILoggerFactory CreateLoggerFactory(LogLevel logLevel)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(logLevel);
                builder.AddSystemdConsole(options =>
                {
                    options.UseUtcTimestamp = true;
                    options.TimestampFormat = " yyyy-MM-ddTHH:mm:ss ";
                });

            });
            return loggerFactory;
        }

        private static async Task<ModuleClient> CreateModuleClientAsync()
        {
            var mqttSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();

            return ioTHubModuleClient;
        }

        private static void ApplyConfiguration(FgModuleConfiguration configuration)
        {
            // TODO: retrieve and apply configuration settings here.
        }

        private static async Task ConfigureDirectMethodHandlersAsync(ModuleClient ioTHubModuleClient, ILoggerFactory loggerFactory)
        {
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesChanged, loggerFactory);
        }

        private static Task OnDesiredPropertiesChanged(TwinCollection desiredProperties, object userContext)
        {
            // Force a restart of the module since configuration has changed.
            ModuleClientState state = userContext as ILoggerFactory;

            if (state == null)
            {
                throw new InvalidOperationException("The userContext should contain a ModuleClientState object.");
            }

            var logger = state.LoggerFactory.CreateLogger<Program>();

            logger.LogInformation("Desired properties have changed - initiating a restart of the FgModule module.");

            _shutdownHandler.CancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        private static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }
#endif
    }
}