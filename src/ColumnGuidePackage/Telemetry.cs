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
            IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var telemetry = componentModel.GetService<ITelemetry>();
            return telemetry.Client;
        }
    }
}
