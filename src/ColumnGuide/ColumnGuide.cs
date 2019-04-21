// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.CodingConventions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using static ColumnGuide.Parser;

namespace ColumnGuide
{
    /// <summary>
    /// Adornment class that draws vertical guide lines beneath the text
    /// </summary>
    internal class ColumnGuide : IDisposable
    {
        private static bool s_sentEditorConfigTelemetry;

        private IList<Line> _guidelines;
        private readonly IWpfTextView _view;
        private bool _firstLayoutDone;
        private double _baseIndentation;
        private double _columnWidth;
        private INotifyPropertyChanged _settingsChanged;
        private readonly ITelemetry _telemetry;
        private GuidelineBrush _guidelineBrush; // The brush supplied by fonts and colors
        private StrokeParameters _strokeParameters;
        private readonly CancellationTokenSource _codingConventionsCancellationTokenSource;
        private bool _isUsingCodingConvention;

        /// <summary>
        /// Creates editor column guidelines
        /// </summary>
        /// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
        /// <param name="settings">The guideline settings.</param>
        /// <param name="guidelineBrush">The guideline brush.</param>
        /// <param name="telemetry">Telemetry interface.</param>
        public ColumnGuide(IWpfTextView view, ITextEditorGuidesSettings settings, GuidelineBrush guidelineBrush, ICodingConventionsManager codingConventionsManager, ITelemetry telemetry)
        {
            _view = view;
            _telemetry = telemetry;
            _guidelineBrush = guidelineBrush;
            _guidelineBrush.BrushChanged += GuidelineBrushChanged;
            _strokeParameters = StrokeParameters.FromBrush(_guidelineBrush.Brush);

            if (codingConventionsManager != null && view.TextBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var textDocument))
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
            if (_guidelines != null)
            {
                foreach (var guideline in _guidelines)
                {
                    guideline.Stroke = brush;
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

        private void InitializeGuidelines(IEnumerable<int> guidelinePositions)
            => _guidelines = CreateGuidelines(guidelinePositions);

        private void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_isUsingCodingConvention && sender is ITextEditorGuidesSettings settings && e.PropertyName == nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars))
            {
                PositionsChanged(settings.GuideLinePositionsInChars);
            }
        }

        private void PositionsChanged(IEnumerable<int> newPositions)
        {
            if (HavePositionsChanged(newPositions))
            {
                InitializeGuidelines(newPositions);
                UpdatePositions();
                AddGuidelinesToAdornmentLayer();
            }
        }

        private void StrokeParametersChanged(StrokeParameters strokeParameters)
        {
            if (!_strokeParameters.Equals(strokeParameters))
            {
                _strokeParameters = strokeParameters;
                UpdateLineStrokes();
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
        /// Create a single vertical column guide with the current stroke parameters for
        /// the given column in the current viewport.
        /// </summary>
        /// <param name="column">The columnar position of the new guideline.</param>
        /// <returns>The new vertical column guide.</returns>
        private Line CreateLine(int column)
        {
            var line = new Line { DataContext = column };
            AddStrokeParameters(line);
            return line;
        }

        /// <summary>
        /// Create all the guidelines for the given positions.
        /// </summary>
        /// <param name="guidelinePositions">The columnar positions of the new guidelines.</param>
        /// <returns>The list of created lines.</returns>
        private IList<Line> CreateGuidelines(IEnumerable<int> guidelinePositions)
        {
            var result = new List<Line>();

            foreach (var column in guidelinePositions)
            {
                var line = CreateLine(column);
                result.Add(line);
            }

            return result;
        }

        /// <summary>
        /// Update the rendering position of the given line to the current viewport.
        /// </summary>
        /// <param name="line">The line to update.</param>
        private void UpdatePosition(Line line)
        {
            var column = (int)line.DataContext;

            line.X1 = line.X2 = _baseIndentation + 0.5 + (column * _columnWidth);
            line.Y1 = _view.ViewportTop;
            line.Y2 = _view.ViewportBottom;
        }

        /// <summary>
        /// Update all line positions when the viewport changes.
        /// </summary>
        private void UpdatePositions()
        {
            foreach (var line in _guidelines)
            {
                UpdatePosition(line);
            }
        }

        /// <summary>
        /// Update the given line with the current stroke parameters.
        /// </summary>
        /// <param name="line">The line to update.</param>
        private void AddStrokeParameters(Line line)
        {
            line.Stroke = _strokeParameters.Brush;
            line.StrokeThickness = _strokeParameters.StrokeThickness;
            line.StrokeDashArray = _strokeParameters.StrokeDashArray;
            line.Stroke = _strokeParameters.Brush;
        }

        /// <summary>
        /// Update all guidelines with the current stroke parameters.
        /// </summary>
        private void UpdateLineStrokes()
        {
            if (_guidelines != null)
            {
                foreach (var line in _guidelines)
                {
                    AddStrokeParameters(line);
                }
            }
        }

        private void AddGuidelinesToAdornmentLayer()
        {
            //Grab a reference to the adornment layer that this adornment should be added to
            var adornmentLayer = _view.GetAdornmentLayer("ColumnGuide");
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

        /// <summary>
        /// Try to load guideline positions from .editorconfig
        /// The 'guidelines' setting is parsed as a list of integer column values.
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

            if (codingConventionContext.CurrentConventions.TryGetConventionValue("guidelines_style", out string guidelines_style))
            {
                if (TryParseStrokeParametersFromCodingConvention(guidelines_style, out var strokeParameters))
                {
                    _isUsingCodingConvention = true;
                    _view.VisualElement.Dispatcher.BeginInvoke(new Action<StrokeParameters>(StrokeParametersChanged), strokeParameters.Freeze());
                }
            }

            ICollection<int> positions = null;

            if (codingConventionContext.CurrentConventions.TryGetConventionValue("guidelines", out string guidelines))
            {
                positions = ParseGuidelinePositionsFromCodingConvention(guidelines);
            }

            // Also support max_line_length: https://github.com/editorconfig/editorconfig/wiki/EditorConfig-Properties#max_line_length
            if (codingConventionContext.CurrentConventions.TryGetConventionValue("max_line_length", out string max_line_length) && TryParsePosition(max_line_length, out int maxLineLengthValue))
            {
                (positions ?? (positions = new List<int>())).Add(maxLineLengthValue);
            }

            if (positions != null)
            {
                // Override 'classic' settings.
                _isUsingCodingConvention = true;

                // TODO: await JoinableTaskFactory.SwitchToMainThreadAsync();
                _view.VisualElement.Dispatcher.BeginInvoke(new Action<IEnumerable<int>>(PositionsChanged), positions);
            }

            if (_isUsingCodingConvention && !s_sentEditorConfigTelemetry)
            {
                var eventTelemetry = new EventTelemetry("EditorConfig");
                if (!string.IsNullOrEmpty(guidelines))
                {
                    eventTelemetry.Properties.Add("Convention", guidelines);
                }

                if (!string.IsNullOrEmpty(max_line_length))
                {
                    eventTelemetry.Properties.Add(nameof(max_line_length), max_line_length);
                }

                if (!string.IsNullOrEmpty(guidelines_style))
                {
                    eventTelemetry.Properties.Add(nameof(guidelines_style), guidelines_style);
                }

                ColumnGuideAdornmentFactory.AddStrokeParametersAndPositionsToTelemetry(eventTelemetry, _strokeParameters, positions);
                _telemetry.Client.TrackEvent(eventTelemetry);
                s_sentEditorConfigTelemetry = true;
            }

            return Task.CompletedTask;
        }

        private bool HavePositionsChanged(IEnumerable<int> newPositions)
        {
            if (_guidelines == null)
            {
                return true;
            }

            var i = 0;
            foreach (var newPosition in newPositions)
            {
                if (i >= _guidelines.Count)
                {
                    return true;
                }

                var oldPosition = (int)_guidelines[i].DataContext;

                if (newPosition != oldPosition)
                {
                    return true;
                }

                i++;
            }

            return i != _guidelines.Count;
        }

        public void Dispose() => _codingConventionsCancellationTokenSource.Dispose();
    }
}
