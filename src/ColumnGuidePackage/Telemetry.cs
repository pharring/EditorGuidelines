// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.ApplicationInsights;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using ColumnGuide;

namespace Microsoft.ColumnGuidePackage
{
    internal static class Telemetry
    {
        public static readonly TelemetryClient Client = ImportClient();

        private static TelemetryClient ImportClient()
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var telemetry = componentModel.GetService<ITelemetry>();
            return telemetry.Client;
        }
    }
}
