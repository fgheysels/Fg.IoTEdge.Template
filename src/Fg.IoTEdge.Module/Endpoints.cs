using Microsoft.Extensions.Logging;

namespace FgModule
{
    internal static class Endpoints
    {
        internal const string Input1 = "input1";

        internal static void ReportEndpoints(ILogger logger)
        {
            logger.LogInformation($"This module is accepting messages on inputs/{Input1}");
        }
    }
}
