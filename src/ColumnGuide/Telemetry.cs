// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Security.Cryptography;

namespace EditorGuidelines
{
    internal static class Telemetry
    {
        private const string c_instrumentationKey = "f8324fcc-eb39-4931-bebc-968aab7d3d7d";

        public static TelemetryClient Client { get; } = CreateClient();

        /// <summary>
        /// Create a telemetry item for the 'initialize' event with additional properties
        /// that only need to be sent once.
        /// </summary>
        /// <param name="name">The name of the initialize event.</param>
        /// <returns>A custom event telemetry with additional context for OS and component version.</returns>
        public static EventTelemetry CreateInitializeTelemetryItem(string name)
        {
            var eventTelemetry = new EventTelemetry(name);
            eventTelemetry.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            eventTelemetry.Context.Component.Version = typeof(Telemetry).Assembly.GetName().Version.ToString();
            return eventTelemetry;
        }

        private static TelemetryClient CreateClient()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope. The TelemetryClient will own it.
            var configuration = new TelemetryConfiguration
            {
                InstrumentationKey = c_instrumentationKey,
                TelemetryChannel = new InMemoryChannel
                {
#if DEBUG
                    DeveloperMode = true
#else
                    DeveloperMode = false
#endif
                }
            };
#pragma warning restore CA2000 // Dispose objects before losing scope

            // Keep this context as small as possible since it's sent with every event.
            var client = new TelemetryClient(configuration);
            client.Context.User.Id = Anonymize(Environment.UserDomainName + "\\" + Environment.UserName);
            client.Context.Session.Id = Convert.ToBase64String(GetRandomBytes(length:6));
            return client;
        }

        private static byte[] GetRandomBytes(int length)
        {
            var buff = new byte[length];
            using (var rnd = RandomNumberGenerator.Create())
            {
                rnd.GetBytes(buff);
            }

            return buff;
        }

        private static string Anonymize(string str)
        {
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
            using (var sha1 = SHA1.Create())
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
            {
                var inputBytes = System.Text.Encoding.Unicode.GetBytes(str);
                var hash = sha1.ComputeHash(inputBytes);
                var base64 = Convert.ToBase64String(hash, 0, 6);
                return base64;
            }
        }
    }
}
