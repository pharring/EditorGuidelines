using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.ComponentModel.Composition;
using System.Security.Cryptography;

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
            client.Context.Session.Id = Convert.ToBase64String(GetRandomBytes(length:6));
            client.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            client.Context.Component.Version = typeof(Telemetry).Assembly.GetName().Version.ToString();

            return client;
        }

        private static byte[] GetRandomBytes(int length)
        {
            var buff = new byte[length];
            RandomNumberGenerator.Create().GetBytes(buff);
            return buff;
        }

        private static string Anonymize(string str)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] inputBytes = System.Text.Encoding.Unicode.GetBytes(str);
                byte[] hash = sha1.ComputeHash(inputBytes);
                string base64 = Convert.ToBase64String(hash, 0, 6);
                return base64;
            }
        }
    }
}
