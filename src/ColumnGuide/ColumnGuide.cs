using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System.ComponentModel;
using Microsoft.VisualStudio.Text.Classification;

namespace ColumnGuide
{
    /// <summary>
    /// Adornment class that draws vertical guide lines beneath the text
    /// </summary>
    class ColumnGuide
    {
        private const double _lineThickness = 1.0;

        private IList<Line> _guidelines;
        private IWpfTextView _view;
        private bool _firstLayoutDone;
        private double _baseIndentation;
        private double _columnWidth;
        private INotifyPropertyChanged _settingsChanged;
        private IEditorFormatMap _formatMap;
        private ITelemetry _telemetry;
        private Brush _guidelineBrush;

        /// <summary>
        /// Creates editor column guidelines
        /// </summary>
        /// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
        /// <param name="settings">The guideline settings.</param>
        /// <param name="editorFormatMap">The editor format map used to discover formatting options (guideline color).</param>
        public ColumnGuide(IWpfTextView view, ITextEditorGuidesSettings settings, IEditorFormatMap editorFormatMap, ITelemetry telemetry)
        {
            _view = view;
            _formatMap = editorFormatMap;
            _telemetry = telemetry;

            InitializeGuidelines(settings);

            _view.LayoutChanged += OnViewLayoutChanged;
            _settingsChanged = settings as INotifyPropertyChanged;
            if (_settingsChanged != null)
            {
                _settingsChanged.PropertyChanged += SettingsChanged;
            }

            _formatMap.FormatMappingChanged += FormatMappingChanged;
            _view.Closed += ViewClosed;
        }

        private void FormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            GuidelineBrush = GetGuidelineBrushFromFontsAndColors();
        }

        void ViewClosed(object sender, EventArgs e)
        {
            _view.LayoutChanged -= OnViewLayoutChanged;
            _view.Closed -= ViewClosed;
            if (_settingsChanged != null)
            {
                _settingsChanged.PropertyChanged -= SettingsChanged;
                _settingsChanged = null;
            }

            if (_formatMap != null)
            {
                _formatMap.FormatMappingChanged -= FormatMappingChanged;
                _formatMap = null;
            }
        }

        private void InitializeGuidelines(ITextEditorGuidesSettings settings)
        {
            _guidelines = CreateGuidelines(settings);
        }

        void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            ITextEditorGuidesSettings settings = sender as ITextEditorGuidesSettings;
            if (settings != null && e.PropertyName == nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars))
            {
                InitializeGuidelines(settings);
                UpdatePositions();
                AddGuidelinesToAdornmentLayer();
            }
        }

        private Brush GetGuidelineBrushFromFontsAndColors()
        {
            return ColumnGuideAdornmentFactory.GetGuidelineBrushFromFontsAndColors(_formatMap);
        }

        private Brush GuidelineBrush
        {
            get => _guidelineBrush ?? (_guidelineBrush = GetGuidelineBrushFromFontsAndColors());

            set
            {
                if (value != _guidelineBrush)
                {
                    _guidelineBrush = value;
                    _telemetry.Client.TrackEvent("GuidelineColorChanged", new Dictionary<string, string> { ["Color"] = value.ToString() });
                    if (_guidelines != null)
                    {
                        foreach (var guideline in _guidelines)
                        {
                            guideline.Stroke = value;
                        }
                    }
                }
            }
        }

        void OnViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            bool fUpdatePositions = false;

            IFormattedLineSource lineSource = _view.FormattedLineSource;
            if (lineSource == null)
            {
                return;
            }

            if(_columnWidth != lineSource.ColumnWidth )
            {
                _columnWidth = lineSource.ColumnWidth;
                fUpdatePositions = true;
            }

            if (_baseIndentation != lineSource.BaseIndentation)
            {
                _baseIndentation = lineSource.BaseIndentation;
                fUpdatePositions = true;
            }

            if (fUpdatePositions ||
                e.VerticalTranslation ||
                e.NewViewState.ViewportTop != e.OldViewState.ViewportTop ||
                e.NewViewState.ViewportBottom != e.OldViewState.ViewportBottom)
            {
                UpdatePositions();
            }

            if (!_firstLayoutDone)
            {
                AddGuidelinesToAdornmentLayer();
                _firstLayoutDone = true;
            }
        }

        private IList<Line> CreateGuidelines(ITextEditorGuidesSettings settings)
        {
            var lineBrush = GuidelineBrush;
            var dashArray = new DoubleCollection(new double[] { 1.0, 3.0 });
            var result = new List<Line>();

            foreach (int column in settings.GuideLinePositionsInChars)
            {
                var line = new Line()
                {
                    DataContext = column,
                    Stroke = lineBrush,
                    StrokeThickness = _lineThickness,
                    StrokeDashArray = dashArray
                };

                result.Add(line);
            }

            return result;
        }

        void UpdatePosition(Line line)
        {
            int column = (int)line.DataContext;

            line.X1 = line.X2 = _baseIndentation + 0.5 + column * _columnWidth;
            line.Y1 = _view.ViewportTop;
            line.Y2 = _view.ViewportBottom;
        }

        void UpdatePositions()
        {
            foreach (Line line in _guidelines)
            {
                UpdatePosition(line);
            }
        }

        void AddGuidelinesToAdornmentLayer()
        {
            //Grab a reference to the adornment layer that this adornment should be added to
            IAdornmentLayer adornmentLayer = _view.GetAdornmentLayer("ColumnGuide");
            if (adornmentLayer == null)
            {
                return;
            }

            adornmentLayer.RemoveAllAdornments();

            // Add the guidelines to the adornment layer and make them relative to the viewport
            foreach (UIElement element in _guidelines)
            {
                adornmentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, element, null);
            }
        }
    }
}
