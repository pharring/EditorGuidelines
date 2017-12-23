using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.ComponentModel.Composition;

namespace ColumnGuide
{
    [Export(typeof(ITelemetry))]
    internal class Telemetry : ITelemetry
    {
        private const string c_InstrumentationKey = "f8324fcc-eb39-4931-bebc-968aab7d3d7d";

        private readonly TelemetryClient _telemetryClient = CreateClient();

        public TelemetryClient Client => _telemetryClient;

        private Telemetry()
        {
        }

        private static TelemetryClient CreateClient()
        {
            var configuration = new TelemetryConfiguration
            {
                InstrumentationKey = c_InstrumentationKey,
                TelemetryChannel = new InMemoryChannel
                {
#if DEBUG
                    DeveloperMode = true
#else
                    DeveloperMode = false
#endif
                }
            };

            var client = new TelemetryClient(configuration);
            client.Context.User.Id = Anonymize(Environment.UserDomainName + "\\" + Environment.UserName);
            client.Context.Session.Id = Guid.NewGuid().ToString();
            client.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            client.Context.Component.Version = typeof(Telemetry).Assembly.GetName().Version.ToString();

            return client;
        }

        private static string Anonymize(string str)
        {
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] inputBytes = System.Text.Encoding.Unicode.GetBytes(str);
                byte[] hash = sha1.ComputeHash(inputBytes);
                string base64 = System.Convert.ToBase64String(hash);
                return base64;
            }
        }
    }
}
