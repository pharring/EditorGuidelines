// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.ApplicationInsights;

namespace EditorGuidelines
{
    public interface ITelemetry
    {
        /// <summary>
        /// Access the Application Insights telemetry client shared between both components.
        /// </summary>
        TelemetryClient Client { get; }
    }
}
