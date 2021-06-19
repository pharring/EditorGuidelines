// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Windows.Media;
using static System.FormattableString;

namespace EditorGuidelines
{
    /// <summary>
    /// Represents the collection of parameters that define the drawing style of a vertical guideline.
    /// </summary>
    internal sealed class StrokeParameters : IEquatable<StrokeParameters>
    {
        /// <summary>
        /// The default thickness in pixels.
        /// </summary>
        private const double c_defaultThickness = 1.0;

        private static readonly DoubleCollection s_dottedDashArray = new DoubleCollection(new[] { 1.0, 3.0 });
        private static readonly DoubleCollection s_dashedDashArray = new DoubleCollection(new[] { 3.0, 1.0 });
        private static readonly DoubleCollection s_solidDashArray = new DoubleCollection();

        /// <summary>
        /// Create an instance from the given brush. The default thickness and line style are used.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <returns>A new instance.</returns>
        /// <remarks>This is used to create a <see cref="StrokeParameters"/> when all we have is a
        /// brush from Fonts & Colors.</remarks>
        public static StrokeParameters FromBrush(Brush brush) => new StrokeParameters { Brush = brush };

        /// <summary>
        /// The brush. For all practical purposes this is a SolidColorBrush.
        /// </summary>
        public Brush Brush { get; set; }

        /// <summary>
        /// The stroke thickness in pixels.
        /// </summary>
        public double StrokeThickness { get; set; } = c_defaultThickness;

        /// <summary>
        /// The line style.
        /// </summary>
        public LineStyle LineStyle { get; set; } = LineStyle.Dotted;

        /// <summary>
        /// The stroke dash array used to define the drawing style to WPF.
        /// </summary>
        public DoubleCollection StrokeDashArray
        {
            get
            {
                switch (LineStyle)
                {
                    case LineStyle.Dotted:
                        return s_dottedDashArray;

                    case LineStyle.Dashed:
                        return s_dashedDashArray;

                    case LineStyle.Solid:
                        return s_solidDashArray;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Freeze the parameters (especially the Brush). Necessary before passing it across threads.
        /// </summary>
        /// <returns>The frozen object.</returns>
        public StrokeParameters Freeze()
        {
            Brush.Freeze();
            return this;
        }

        /// <summary>
        /// Extract the brush's color.
        /// </summary>
        /// <remarks>Internal for testing.</remarks>
        internal Color BrushColor => (Brush is SolidColorBrush solidColorBrush) ? solidColorBrush.Color : default;

        public bool Equals(StrokeParameters other) => other != null && BrushColor == other.BrushColor && StrokeThickness == other.StrokeThickness && LineStyle == other.LineStyle;

        public override bool Equals(object obj) => obj is StrokeParameters other && Equals(other);

        public override int GetHashCode() => unchecked(Brush.GetHashCode() + StrokeThickness.GetHashCode() + LineStyle.GetHashCode());

        public override string ToString() => Invariant($"{StrokeThickness}px {LineStyle} {BrushColor}");
    }
}
