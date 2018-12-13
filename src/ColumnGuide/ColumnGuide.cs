// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.VisualStudio.CodingConventions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ColumnGuide
{
    /// <summary>
    /// Adornment class that draws vertical guide lines beneath the text
    /// </summary>
    class ColumnGuide
    {
        private const double _lineThickness = 1.0;
        private static bool _sentEditorConfigTelemetry;

        private IList<Line> _guidelines;
        private readonly IWpfTextView _view;
        private bool _firstLayoutDone;
        private double _baseIndentation;
        private double _columnWidth;
        private INotifyPropertyChanged _settingsChanged;
        private readonly ITelemetry _telemetry;
        private GuidelineBrush _guidelineBrush;
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
            if (_guidelines != null)
            {
                foreach (var guideline in _guidelines)
                {
                    guideline.Stroke = brush;
                }
            }
        }

        void ViewClosed(object sender, EventArgs e)
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
        {
            _guidelines = CreateGuidelines(guidelinePositions);
        }

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

        private void OnViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
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

        private IList<Line> CreateGuidelines(IEnumerable<int> guidelinePositions)
        {
            var lineBrush = _guidelineBrush.Brush;
            var dashArray = new DoubleCollection(new double[] { 1.0, 3.0 });
            var result = new List<Line>();

            foreach (int column in guidelinePositions)
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
            var codingConventionContext = await codingConventionsManager.GetConventionContextAsync(filePath, cancellationToken);

            codingConventionContext.CodingConventionsChangedAsync += OnCodingConventionsChangedAsync;
            cancellationToken.Register(() => codingConventionContext.CodingConventionsChangedAsync -= OnCodingConventionsChangedAsync);

            await UpdateGuidelinesFromCodingConventionAsync(codingConventionContext, cancellationToken);
        }

        private Task OnCodingConventionsChangedAsync(object sender, CodingConventionsChangedEventArgs arg) => UpdateGuidelinesFromCodingConventionAsync((ICodingConventionContext)sender, _codingConventionsCancellationTokenSource.Token);

        private Task UpdateGuidelinesFromCodingConventionAsync(ICodingConventionContext codingConventionContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && codingConventionContext.CurrentConventions.TryGetConventionValue("guidelines", out string convention))
            {
                // Override 'classic' settings.
                _isUsingCodingConvention = true;

                var positions = ParseGuidelinePositionsFromCodingConvention(convention);

                if (!_sentEditorConfigTelemetry)
                {
                    _telemetry.Client.TrackEvent("EditorConfig", new Dictionary<string, string> { ["Convention"] = convention });
                    _sentEditorConfigTelemetry = true;
                }

                // TODO: await JoinableTaskFactory.SwitchToMainThreadAsync();
                _view.VisualElement.Dispatcher.BeginInvoke(new Action<IEnumerable<int>>(PositionsChanged), positions);
            }

            return Task.FromResult(false);
        }

        private static List<int> ParseGuidelinePositionsFromCodingConvention(string codingConvention)
        {
            var positionsAsString = codingConvention.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<int>(positionsAsString.Length);
            foreach (var position in positionsAsString)
            {
                if (int.TryParse(position, out int column) && column >= 0 && column < 10000 && !result.Contains(column))
                {
                    result.Add(column);
                }
            }

            return result;
        }

        private bool HavePositionsChanged(IEnumerable<int> newPositions)
        {
            if (_guidelines == null)
            {
                return true;
            }

            int i = 0;
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
    }
}
