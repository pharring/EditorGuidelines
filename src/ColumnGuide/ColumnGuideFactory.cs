// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.CodingConventions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Globalization.CultureInfo;

namespace ColumnGuide
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
        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered 
        /// below the text in the Z-order
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("ColumnGuide")]
        [Order(Before = PredefinedAdornmentLayers.Text)]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;

        /// <summary>
        /// Instantiates a ColumnGuide manager when a textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            // Always create the adornment, even if there are no guidelines, since we
            // respond to dynamic changes.
#pragma warning disable IDE0067 // Dispose objects before losing scope
#pragma warning disable CA2000 // Dispose objects before losing scope
            var _ = new ColumnGuide(textView, TextEditorGuidesSettings, GuidelineBrush, CodingConventionsManager, Telemetry);
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning restore IDE0067 // Dispose objects before losing scope
        }

        public void OnImportsSatisfied()
        {
            TrackSettings(global::ColumnGuide.Telemetry.CreateInitializeTelemetryItem(nameof(ColumnGuideAdornmentFactory) + " initialized"));

            GuidelineBrush.BrushChanged += (sender, newBrush) =>
            {
                Telemetry.Client.TrackEvent("GuidelineColorChanged", new Dictionary<string, string> { ["Color"] = newBrush.ToString(InvariantCulture) });
            };

            if (TextEditorGuidesSettings is INotifyPropertyChanged settingsChanged)
            {
                settingsChanged.PropertyChanged += OnSettingsChanged;
            }

            // Show a warning dialog if running in an old version of VS
            if (IsRunningInOldVsVersion() && !TextEditorGuidesSettings.DontShowVsVersionWarning)
            {
                ThreadHelper.Generic.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    var dlg = new OldVsVersionDialog();
                    if (dlg.ShowModal() == true && dlg.DontShowAgain)
                    {
                        TextEditorGuidesSettings.DontShowVsVersionWarning = true;
                    }
                });
            }
        }

        private bool IsRunningInOldVsVersion()
        {
            // Check VS Version
            var vsShell = HostServices.GetService<IVsShell>(typeof(SVsShell));
            if (0 == vsShell.GetProperty(-9068, out var obj) && obj != null)
            {
                var vsVersion = obj.ToString();
                if (vsVersion.Length >= 3 && int.TryParse(vsVersion.Substring(0, 2), out var majorVersion))
                {
                    return majorVersion < 14;
                }
            }

            return false;
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
            => AddStrokeParametersAndPositionsToTelemetry(eventTelemetry, StrokeParameters.FromBrush(brush), positions);

        internal static void AddStrokeParametersAndPositionsToTelemetry(EventTelemetry eventTelemetry, StrokeParameters strokeParameters, IEnumerable<int> positions)
        {
            var telemetryProperties = eventTelemetry.Properties;

            if (strokeParameters.Brush != null)
            {
                telemetryProperties.Add("Color", strokeParameters.Brush.ToString(InvariantCulture) ?? "unknown");

                if (strokeParameters.Brush.Opacity != 1.0)
                {
                    eventTelemetry.Metrics.Add("Opacity", strokeParameters.Brush.Opacity);
                }
            }

            telemetryProperties.Add("Style", strokeParameters.LineStyle.ToString());

            var count = 0;
            foreach (var column in positions)
            {
                telemetryProperties.Add("guide" + count.ToString(InvariantCulture), column.ToString(InvariantCulture));
                count++;
            }

            eventTelemetry.Metrics.Add("Thickness", strokeParameters.StrokeThickness);
            eventTelemetry.Metrics.Add("Count", count);
        }

        [Import]
        private ITextEditorGuidesSettings TextEditorGuidesSettings { get; set; }

        [Import]
        private ITelemetry Telemetry { get; set; }

        [Import]
        private GuidelineBrush GuidelineBrush { get; set; }

        [Import(AllowDefault = true)]
        private ICodingConventionsManager CodingConventionsManager { get; set; }

        [Import]
        private HostServices HostServices { get; set; }
    }
    #endregion //Adornment Factory
}
