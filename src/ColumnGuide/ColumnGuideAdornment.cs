// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.CodingConventions;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using static EditorGuidelines.Parser;

namespace EditorGuidelines
{
    /// <summary>
    /// Adornment class that draws vertical guide lines beneath the text
    /// </summary>
    internal class ColumnGuideAdornment : IDisposable
    {
        /// <summary>
        /// Limits the number of telemetry events for .editorconfig settings.
        /// </summary>
        private static bool s_sentEditorConfigTelemetry;

        /// <summary>
        /// Collection of WPF lines that are drawn into our adornment.
        /// </summary>
        private IEnumerable<Line> _lines;
        
        /// <summary>
        /// The WPF text view associated with this adornment.
        /// </summary>
        private readonly IWpfTextView _view;

        /// <summary>
        /// True when the first layout has completed.
        /// </summary>
        private bool _firstLayoutDone;

        private double _baseIndentation;
        private double _columnWidth;

        private INotifyPropertyChanged _settingsChanged;

        /// <summary>
        /// The brush supplied by Fonts and Colors
        /// </summary>
        private GuidelineBrush _guidelineBrush;

        /// <summary>
        /// The stroke parameters for the <see cref="_guidelineBrush"/>.
        /// </summary>
        private readonly StrokeParameters _strokeParameters;

        private readonly CancellationTokenSource _codingConventionsCancellationTokenSource;
        private bool _isUsingCodingConvention;

        /// <summary>
        /// Creates editor column guidelines
        /// </summary>
        /// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
        /// <param name="settings">The guideline settings.</param>
        /// <param name="guidelineBrush">The guideline brush.</param>
        /// <param name="codingConventionsManager">The coding conventions manager for handling .editorconfig settings.</param>
        /// <param name="telemetry">Telemetry interface.</param>
        public ColumnGuideAdornment(IWpfTextView view, ITextEditorGuidesSettings settings, GuidelineBrush guidelineBrush, ICodingConventionsManager codingConventionsManager)
        {
            _view = view;
            _guidelineBrush = guidelineBrush;
            _guidelineBrush.BrushChanged += GuidelineBrushChanged;
            _strokeParameters = StrokeParameters.FromBrush(_guidelineBrush.Brush);

            if (codingConventionsManager != null && view.TryGetTextDocument(out var textDocument))
            {
                _codingConventionsCancellationTokenSource = new CancellationTokenSource();
                var fireAndForgetTask = LoadGuidelinesFromEditorConfigAsync(codingConventionsManager, textDocument.FilePath);
            }

            InitializeGuidelines(settings.GuideLinePositionsInChars);

            _view.LayoutChanged += OnViewLayoutChanged;
            _settingsChanged = settings as INotifyPropertyChanged;
            if (_settingsChanged != null)
            {
                _settingsChanged.PropertyChanged += SettingsChanged;
            }

            _view.Closed += ViewClosed;
        }

        private void GuidelineBrushChanged(object sender, Brush brush)
        {
            _strokeParameters.Brush = brush;
            if (_lines != null)
            {
                foreach (var line in _lines)
                {
                    line.Stroke = brush;
                }
            }
        }

        private void ViewClosed(object sender, EventArgs e)
        {
            if (_codingConventionsCancellationTokenSource != default(CancellationTokenSource))
            {
                _codingConventionsCancellationTokenSource.Cancel();
            }

            _view.LayoutChanged -= OnViewLayoutChanged;
            _view.Closed -= ViewClosed;
            if (_settingsChanged != null)
            {
                _settingsChanged.PropertyChanged -= SettingsChanged;
                _settingsChanged = null;
            }

            if (_guidelineBrush != null)
            {
                _guidelineBrush.BrushChanged -= GuidelineBrushChanged;
                _guidelineBrush = null;
            }
        }

        private void InitializeGuidelines(IEnumerable<int> guideLinePositions)
        {
            var initialGuidelines = GuidelinesFromSettings(guideLinePositions);
            CreateVisualLines(initialGuidelines);
        }

        private void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_isUsingCodingConvention && sender is ITextEditorGuidesSettings settings && e.PropertyName == nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars))
            {
                var guidelines = GuidelinesFromSettings(settings.GuideLinePositionsInChars);
                GuidelinesChanged(guidelines);
            }
        }

        /// <summary>
        /// Given a collection of column numbers, create a <see cref="Guideline"/> collection.
        /// The guidelines will all have the default (null) brush.
        /// </summary>
        /// <param name="guideLinePositions">The position of each guideline in characters from the left edge.</param>
        /// <returns>A <see cref="Guideline"/> collection.</returns>
        private static IEnumerable<Guideline> GuidelinesFromSettings(IEnumerable<int> guideLinePositions)
        {
            var guidelines = from column in guideLinePositions select new Guideline(column, null);
            return guidelines;
        }

        private void GuidelinesChanged(IEnumerable<Guideline> newGuidelines)
        {
            if (HaveGuidelinesChanged(newGuidelines))
            {
                CreateVisualLines(newGuidelines);
                UpdatePositions();
                AddGuidelinesToAdornmentLayer();
            }
        }

        private void OnViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            var fUpdatePositions = false;

            var lineSource = _view.FormattedLineSource;
            if (lineSource == null)
            {
                return;
            }

            if (_columnWidth != lineSource.ColumnWidth)
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

        /// <summary>
        /// Create the vertical lines.
        /// </summary>
        /// <param name="guidelines">The collection of guidelines with position and style information.</param>
        private void CreateVisualLines(IEnumerable<Guideline> guidelines)
        {
            _lines = (from guideline in guidelines select CreateLine(guideline)).ToArray();
        }

        /// <summary>
        /// Create a single vertical column guide with the current stroke parameters for
        /// the given column in the current viewport.
        /// </summary>
        /// <param name="column">The columnar position of the new guideline.</param>
        /// <returns>The new vertical column guide.</returns>
        private Line CreateLine(Guideline guideline)
        {
            var strokeParameters = guideline.StrokeParameters ?? _strokeParameters;
            var line = new Line
            {
                DataContext = guideline,
                Stroke = strokeParameters.Brush,
                StrokeThickness = strokeParameters.StrokeThickness,
                StrokeDashArray = strokeParameters.StrokeDashArray
            };

            return line;
        }

        /// <summary>
        /// Update the rendering position of the given line to the current viewport.
        /// </summary>
        /// <param name="line">The line to update.</param>
        private void UpdatePosition(Line line)
        {
            var guideline = (Guideline)line.DataContext;

            line.X1 = line.X2 = _baseIndentation + 0.5 + (guideline.Column * _columnWidth);
            line.Y1 = _view.ViewportTop;
            line.Y2 = _view.ViewportBottom;
        }

        /// <summary>
        /// Update all line positions when the viewport changes.
        /// </summary>
        private void UpdatePositions()
        {
            foreach (var line in _lines)
            {
                UpdatePosition(line);
            }
        }

        private void AddGuidelinesToAdornmentLayer()
        {
            // Get a reference to our adornment layer.
            var adornmentLayer = _view.GetAdornmentLayer(ColumnGuideAdornmentFactory.AdornmentLayerName);
            if (adornmentLayer == null)
            {
                return;
            }

            adornmentLayer.RemoveAllAdornments();

            // Add the guidelines to the adornment layer and make them relative to the viewport.
            foreach (UIElement element in _lines)
            {
                adornmentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, element, null);
            }
        }

        /// <summary>
        /// Try to load guideline positions from .editorconfig
        /// </summary>
        /// <param name="codingConventionsManager">The coding conventions (.editorconfig) manager.</param>
        /// <param name="filePath">Path to the document being edited.</param>
        /// <returns>A task which completes when the convention has been loaded and applied.</returns>
        private async Task LoadGuidelinesFromEditorConfigAsync(ICodingConventionsManager codingConventionsManager, string filePath)
        {
            var cancellationToken = _codingConventionsCancellationTokenSource.Token;
            var codingConventionContext = await codingConventionsManager.GetConventionContextAsync(filePath, cancellationToken).ConfigureAwait(false);

            codingConventionContext.CodingConventionsChangedAsync += OnCodingConventionsChangedAsync;
            cancellationToken.Register(() => codingConventionContext.CodingConventionsChangedAsync -= OnCodingConventionsChangedAsync);

            await UpdateGuidelinesFromCodingConventionAsync(codingConventionContext, cancellationToken).ConfigureAwait(false);
        }

        private Task OnCodingConventionsChangedAsync(object sender, CodingConventionsChangedEventArgs arg) => UpdateGuidelinesFromCodingConventionAsync((ICodingConventionContext)sender, _codingConventionsCancellationTokenSource.Token);

        private Task UpdateGuidelinesFromCodingConventionAsync(ICodingConventionContext codingConventionContext, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            StrokeParameters strokeParameters = null;

            if (codingConventionContext.CurrentConventions.TryGetConventionValue("guidelines_style", out string guidelines_style))
            {
                if (TryParseStrokeParametersFromCodingConvention(guidelines_style, out strokeParameters))
                {
                    _isUsingCodingConvention = true;
                    strokeParameters.Freeze();
                }
            }

            ICollection<Guideline> guidelines = null;

            if (codingConventionContext.CurrentConventions.TryGetConventionValue("guidelines", out string guidelinesConventionValue))
            {
                guidelines = ParseGuidelinesFromCodingConvention(guidelinesConventionValue, strokeParameters);
            }

            // Also support max_line_length: https://github.com/editorconfig/editorconfig/wiki/EditorConfig-Properties#max_line_length
            if (codingConventionContext.CurrentConventions.TryGetConventionValue("max_line_length", out string max_line_length) && TryParsePosition(max_line_length, out int maxLineLengthValue))
            {
                (guidelines ?? (guidelines = new List<Guideline>())).Add(new Guideline(maxLineLengthValue, strokeParameters));
            }

            if (guidelines != null)
            {
                // Override 'classic' settings.
                _isUsingCodingConvention = true;

                // TODO: await JoinableTaskFactory.SwitchToMainThreadAsync();
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                _view.VisualElement.Dispatcher.BeginInvoke(new Action<IEnumerable<Guideline>>(GuidelinesChanged), guidelines);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
            }

            if (_isUsingCodingConvention && !s_sentEditorConfigTelemetry)
            {
                var eventTelemetry = new EventTelemetry("EditorConfig");
                if (!string.IsNullOrEmpty(guidelinesConventionValue))
                {
                    eventTelemetry.Properties.Add("Convention", guidelinesConventionValue);
                }

                if (!string.IsNullOrEmpty(max_line_length))
                {
                    eventTelemetry.Properties.Add(nameof(max_line_length), max_line_length);
                }

                if (!string.IsNullOrEmpty(guidelines_style))
                {
                    eventTelemetry.Properties.Add(nameof(guidelines_style), guidelines_style);
                }

                ColumnGuideAdornmentFactory.AddGuidelinesToTelemetry(eventTelemetry, guidelines);
                Telemetry.Client.TrackEvent(eventTelemetry);
                s_sentEditorConfigTelemetry = true;
            }

            return Task.CompletedTask;
        }

        private bool HaveGuidelinesChanged(IEnumerable<Guideline> newGuidelines)
        {
            if (_lines == null)
            {
                return true;
            }

            var currentGuidelines = from line in _lines select (Guideline)line.DataContext;
            return !currentGuidelines.SequenceEqual(newGuidelines);
        }

        public void Dispose() => _codingConventionsCancellationTokenSource.Dispose();
    }
}
