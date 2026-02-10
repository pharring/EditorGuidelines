// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Media;
using static System.Globalization.CultureInfo;

namespace EditorGuidelines
{
    #region Adornment Factory
    /// <summary>
    /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
    /// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ColumnGuideAdornmentFactory : IWpfTextViewCreationListener, IPartImportsSatisfiedNotification
    {
        public const string AdornmentLayerName = "ColumnGuide";
        private bool _initialSettingsTracked = false;

        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered 
        /// below the text in the Z-order
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(AdornmentLayerName)]
        [Order(Before = PredefinedAdornmentLayers.Text)]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;

        /// <summary>
        /// Instantiates a ColumnGuide manager when a textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Track initial settings once on the first text view creation (which occurs on UI thread)
            if (!_initialSettingsTracked)
            {
                TrackSettings(Telemetry.CreateInitializeTelemetryItem(nameof(ColumnGuideAdornmentFactory) + " initialized"));
                _initialSettingsTracked = true;
            }

            // Always create the adornment, even if there are no guidelines, since we
            // respond to dynamic changes.
            var _ = new ColumnGuideAdornment(textView, TextEditorGuidesSettings, GuidelineBrush, CodingConventions);
        }

        public void OnImportsSatisfied()
        {
            // Note: This method may be called on a background thread in recent versions of Visual Studio.
            // We must avoid accessing properties that require UI thread affinity.
            // See: https://devblogs.microsoft.com/visualstudio/performance-improvements-to-mef-based-editor-productivity-extensions/

            // Subscribe to events. Event subscriptions themselves are thread-safe.
            GuidelineBrush.BrushChanged += (sender, newBrush) =>
            {
                Telemetry.Client.TrackEvent("GuidelineColorChanged", new Dictionary<string, string> { ["Color"] = newBrush.ToString(InvariantCulture) });
            };

            if (TextEditorGuidesSettings is INotifyPropertyChanged settingsChanged)
            {
                settingsChanged.PropertyChanged += OnSettingsChanged;
            }

            // Note: Initial settings telemetry is deferred to the first TextViewCreated call
            // to avoid accessing GuidelineBrush.Brush and TextEditorGuidesSettings.GuideLinePositionsInChars
            // on a background thread.
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars))
            {
                TrackSettings("SettingsChanged");
            }
        }

        private void TrackSettings(string eventName) => TrackSettings(new EventTelemetry(eventName));

        private void TrackSettings(EventTelemetry telemetry)
        {
            AddBrushColorAndGuidelinePositionsToTelemetry(telemetry, GuidelineBrush.Brush, TextEditorGuidesSettings.GuideLinePositionsInChars);
            Telemetry.Client.TrackEvent(telemetry);
        }

        internal static void AddBrushColorAndGuidelinePositionsToTelemetry(EventTelemetry eventTelemetry, Brush brush, IEnumerable<int> positions)
        {
            var telemetryProperties = eventTelemetry.Properties;

            if (brush != null)
            {
                telemetryProperties.Add("Color", brush.ToString(InvariantCulture) ?? "unknown");

                if (brush.Opacity != 1.0)
                {
                    eventTelemetry.Metrics.Add("Opacity", brush.Opacity);
                }
            }

            var count = 0;
            foreach (var column in positions)
            {
                telemetryProperties.Add("guide" + count.ToString(InvariantCulture), column.ToString(InvariantCulture));
                count++;
            }

            eventTelemetry.Metrics.Add("Count", count);
        }

        internal static void AddGuidelinesToTelemetry(EventTelemetry eventTelemetry, IEnumerable<Guideline> guidelines)
        {
            var telemetryProperties = eventTelemetry.Properties;

            var count = 0;
            foreach (var guideline in guidelines)
            {
                telemetryProperties.Add("guide" + count.ToString(InvariantCulture), guideline.ToString());
                count++;
            }

            eventTelemetry.Metrics.Add("Count", count);
        }

        [Import]
        private ITextEditorGuidesSettings TextEditorGuidesSettings { get; set; }

        [Import]
        private GuidelineBrush GuidelineBrush { get; set; }

        [Import]
        private CodingConventions CodingConventions { get; set; }
    }
#endregion //Adornment Factory
}
