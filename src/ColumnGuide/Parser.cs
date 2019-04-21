// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ColumnGuide
{
    /// <summary>
    /// Parser utilities.
    /// </summary>
    internal static class Parser
    {
        private static readonly char[] s_separators = new[] { ',', ';', ':', ' ' };
        public static HashSet<int> ParseGuidelinePositionsFromCodingConvention(string codingConvention)
        {
            var positionsAsString = codingConvention.Split(s_separators, StringSplitOptions.RemoveEmptyEntries);
            var result = new HashSet<int>();
            foreach (var position in positionsAsString)
            {
                if (TryParsePosition(position, out int column))
                {
                    result.Add(column);
                }
            }

            return result;
        }

        public static bool TryParsePosition(string text, out int column)
            => int.TryParse(text, out column) && column >= 0 && column < 10000;

        /// <summary>
        /// The guideline_style looks like this:
        /// guidelines_style = 1px dotted 80FF0000
        /// Meaning single pixel, dotted style, color red, 50% opaque
        /// 
        /// 1px specifies the width in pixels.
        /// dotted specifies the line style.Simple to support: solid, dotted and dashed
        /// </summary>
        /// <param name="text">The value read from guidelines_style editorconfig.</param>
        /// <returns>New stroke parameters. Null if we couldn't parse it.</returns>
        public static bool TryParseStrokeParametersFromCodingConvention(string text, out StrokeParameters strokeParameters)
        {
            strokeParameters = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var tokens = text.Split(s_separators, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 1)
            {
                return false;
            }

            // Pixel width (stroke thickness)
            if (!tokens[0].EndsWith("px", StringComparison.Ordinal))
            {
                return false;
            }

            if (!double.TryParse(tokens[0].Substring(0, tokens[0].Length - 2), out var strokeThickness))
            {
                return false;
            }

            if (strokeThickness < 0 || strokeThickness > 50)
            {
                return false;
            }

            strokeParameters = new StrokeParameters
            {
                Brush = new SolidColorBrush(Colors.Black),
                StrokeThickness = strokeThickness
            };

            if (tokens.Length < 2)
            {
                return true;
            }

            // Line style
            if (Enum.TryParse<LineStyle>(tokens[1], ignoreCase: true, out var lineStyle))
            {
                strokeParameters.LineStyle = lineStyle;
            }

            if (tokens.Length < 3)
            {
                return true;
            }

            // Color
            if (TryParseColor(tokens[2], out var color))
            {
                strokeParameters.Brush = new SolidColorBrush(color);
            }

            // Ignore trailing tokens.
            return true;
        }

        private static bool IsInRange(char ch, char low, char high)
            => (uint)(ch - low) <= high - low;

        private static bool IsHexDigit(char ch) =>
            IsInRange(ch, '0', '9') ||
            IsInRange(ch, 'A', 'F') ||
            IsInRange(ch, 'a', 'f');

        private static bool IsRGBorARGBValue(string text)
            => (text.Length == 6 || text.Length == 8) && text.All(IsHexDigit);

        private static bool TryParseColor(string text, out Color color)
        {
            if (IsRGBorARGBValue(text))
            {
                // There are no 6 or 8 letter named colors spelled only with the letters A to F.
                text = "#" + text;
            }

            try
            {
                var colorObj = ColorConverter.ConvertFromString(text) as Color?;
                if (colorObj.HasValue)
                {
                    color = colorObj.Value;
                    return true;
                }
            }
            catch (FormatException)
            {
            }

            color = default;
            return false;
        }
    }
}
