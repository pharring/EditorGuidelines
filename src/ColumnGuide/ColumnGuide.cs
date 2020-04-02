// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ColumnGuide
{
    /// <summary>
    /// Adornment class that draws vertical guide lines beneath the text
    /// </summary>
    internal class ColumnGuide
    {
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

        /// <summary>
        /// Creates editor column guidelines
        /// </summary>
        /// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
        /// <param name="settings">The guideline settings.</param>
        /// <param name="guidelineBrush">The guideline brush.</param>
        public ColumnGuide(IWpfTextView view, ITextEditorGuidesSettings settings, GuidelineBrush guidelineBrush)
        {
            _view = view;
            _guidelineBrush = guidelineBrush;
            _guidelineBrush.BrushChanged += GuidelineBrushChanged;
            _strokeParameters = StrokeParameters.FromBrush(_guidelineBrush.Brush);

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
            if (sender is ITextEditorGuidesSettings settings && e.PropertyName == nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars))
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
            var adornmentLayer = _view.GetAdornmentLayer("ColumnGuide");
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

        private bool HaveGuidelinesChanged(IEnumerable<Guideline> newGuidelines)
        {
            if (_lines == null)
            {
                return true;
            }

            var currentGuidelines = from line in _lines select (Guideline)line.DataContext;
            return !currentGuidelines.SequenceEqual(newGuidelines);
        }
    }
}
