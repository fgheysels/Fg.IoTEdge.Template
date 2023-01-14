using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;

namespace FgModule
{
    internal class ModuleState
    {
        public ModuleClient ModuleClient { get; }
        public ILogger Logger { get; }

        public ModuleState(ModuleClient moduleClient, ILogger logger)
        {
            ModuleClient = moduleClient;
            Logger = logger;
        }
    }
}
