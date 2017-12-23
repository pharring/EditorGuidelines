using Microsoft.ApplicationInsights;

namespace ColumnGuide
{
    public interface ITelemetry
    {
        /// <summary>
        /// Access the Application Insights telemetry client shared between both components.
        /// </summary>
        TelemetryClient Client { get; }
    }
}
