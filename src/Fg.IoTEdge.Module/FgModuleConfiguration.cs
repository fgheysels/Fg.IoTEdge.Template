using Fg.IoTEdgeModule.Configuration;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;

namespace FgModule
{
    internal class FgModuleConfiguration : ModuleConfiguration
    {
        private const string MinimumLogLevelProperty = "LogLevel";

        /// <summary>
        /// Gets the LogLevel that must be used while logging.
        /// </summary>
        public LogLevel MinimumLogLevel { get; private set; }

        public FgModuleConfiguration(ILogger logger) : base(logger)
        {
        }

        protected override void InitializeFromTwin(TwinCollection desiredProperties)
        {
            MinimumLogLevel = LogLevel.Information;

            if (desiredProperties.Contains(MinimumLogLevelProperty))
            {
                string desiredLogLevel = desiredProperties[MinimumLogLevelProperty].ToString();

                if (Enum.TryParse(desiredLogLevel, out LogLevel logLevel))
                {
                    MinimumLogLevel = logLevel;
                }
                else
                {
                    Console.WriteLine($"Unable to parse desired LogLevel {desiredLogLevel} - falling back to Information");
                    Console.WriteLine("Possible LogLevels are: None, Critical, Error, Warning, Debug, Trace");

                    MinimumLogLevel = LogLevel.Information;
                }
            }
        }

        protected override void SetReportedProperties(TwinCollection reportedProperties)
        {
            reportedProperties[MinimumLogLevelProperty] = MinimumLogLevel.ToString();
        }

        protected override string ModuleName => nameof(FgModule);
    }
}
