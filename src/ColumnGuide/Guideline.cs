// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using static System.Globalization.CultureInfo;
using static System.FormattableString;

namespace ColumnGuide
{
    /// <summary>
    /// Represents the position and style of a single guideline.
    /// </summary>
    internal sealed class Guideline : IEquatable<Guideline>
    {
        /// <summary>
        /// The column number of the guideline. The text editor convention is to number the leftmost
        /// column as 1. The guideline is drawn to the right of the given column. That way, when you
        /// put a guideline at column 80, for example, you make room for up to 80 characters to the
        /// left of the guideline. We also allow placing a guideline at column zero, meaning it's to
        /// the left of the first column of text.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// The stroke parameters of the guideline. See <see cref="StrokeParameters"/>.
        /// </summary>
        public StrokeParameters StrokeParameters { get; }

        /// <summary>
        /// Construct a new <see cref="Guideline"/>.
        /// </summary>
        /// <param name="column">The column number. Must be between 0 and 10,000.</param>
        /// <param name="strokeParameters">The stroke parameters for this guideline. If null, then
        /// the default brush from Fonts & Colors is used with default .</param>
        public Guideline(int column, StrokeParameters strokeParameters)
        {
            if (!IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException(nameof(column), Resources.AddGuidelineParameterOutOfRange);
            }

            Column = column;
            StrokeParameters = strokeParameters;
        }

        /// <summary>
        /// Test if this guideline is equivalent to another.
        /// </summary>
        /// <param name="other">The other guideline.</param>
        /// <returns>True if the two guidelines may be considered equal.</returns>
        public bool Equals(Guideline other) =>
            Column == other.Column &&
            (StrokeParameters is null ? other.StrokeParameters is null : StrokeParameters.Equals(other.StrokeParameters));

        public override bool Equals(object obj) => obj is Guideline other && Equals(other);

        public override int GetHashCode() => unchecked(Column.GetHashCode() + (StrokeParameters?.GetHashCode() ?? 0));

        public override string ToString() => StrokeParameters is null ? Column.ToString(InvariantCulture) : Invariant($"{Column} {StrokeParameters}");

        /// <summary>
        /// Check if the given column is valid.
        /// Negative values are not allowed.
        /// Zero is allowed (per user request)
        /// 10000 seems like a sensible upper limit.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns>True if <paramref name="column"/> is valid.</returns>
        internal static bool IsValidColumn(int column) =>
            0 <= column && column <= 10000;
    }
}
