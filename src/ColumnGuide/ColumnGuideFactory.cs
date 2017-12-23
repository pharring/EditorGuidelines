using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using System.Collections.Generic;
using System.Windows.Media;
using System.ComponentModel;
using System.Globalization;

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
            var formatMap = EditorFormatMapService.GetEditorFormatMap(textView);
            new ColumnGuide(textView, TextEditorGuidesSettings, formatMap, Telemetry);

            // To reduce the amount of telemetry, only report the color for the first instance.
            if (!_colorReported)
            {
                _colorReported = true;
                var brush = GetGuidelineBrushFromFontsAndColors(formatMap);
                if (brush != null)
                {
                    Telemetry.Client.TrackEvent("CreateGuidelines", new Dictionary<string, string> { ["Color"] = brush.ToString() });
                }
            }
        }

        public void OnImportsSatisfied()
        {
            Telemetry.Client.TrackEvent(nameof(ColumnGuideAdornmentFactory) + " initialized");

            TrackSettings("CreateGuidelines");
            if (TextEditorGuidesSettings is INotifyPropertyChanged settingsChanged)
            {
                settingsChanged.PropertyChanged += OnSettingsChanged;
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars))
            {
                TrackSettings("SettingsChanged");
            }
        }

        private void TrackSettings(string eventName)
        {
            var telemetryProperties = new Dictionary<string, string>();
            foreach (var column in TextEditorGuidesSettings.GuideLinePositionsInChars)
            {
                telemetryProperties.Add("guide" + telemetryProperties.Count.ToString(CultureInfo.InvariantCulture), column.ToString(CultureInfo.InvariantCulture));
            }

            Telemetry.Client.TrackEvent(eventName, telemetryProperties, new Dictionary<string, double> { ["Count"] = telemetryProperties.Count });
        }

        internal static Brush GetGuidelineBrushFromFontsAndColors(IEditorFormatMap formatMap)
        {
            var resourceDictionary = formatMap.GetProperties(GuidelineColorDefinition.Name);
            if (resourceDictionary.Contains(EditorFormatDefinition.BackgroundBrushId))
            {
                return resourceDictionary[EditorFormatDefinition.BackgroundBrushId] as Brush;
            }

            return null;
        }

        [Import]
        private ITextEditorGuidesSettings TextEditorGuidesSettings { get; set; }

        [Import]
        private ITelemetry Telemetry { get; set; }

        [Import]
        private IEditorFormatMapService EditorFormatMapService { get; set; }

        private bool _colorReported;
    }
    #endregion //Adornment Factory
}
