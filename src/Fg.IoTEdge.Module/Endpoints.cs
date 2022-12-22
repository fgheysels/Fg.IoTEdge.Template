﻿using Microsoft.Extensions.Logging;

namespace Fg.IoTEdge.Module
{
    internal static class Endpoints
    {
        internal const string Input1 = "Input1";

        internal static void ReportEndpoints(ILogger logger)
        {
            logger.LogInformation($"This module is accepting messages on inputs/{Input1}");
        }
    }
}
