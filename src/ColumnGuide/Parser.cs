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
        public static HashSet<int> ParseGuidelinePositionsFromCodingConvention(string codingConvention)
        {
            var positionsAsString = codingConvention.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

            // Pixel width (stroke thickness)
            int tokenStart = GetNextToken(text, start: 0, out var tokenLength);
            if (tokenStart < 0)
            {
                return false;
            }

            var token = text.Substring(tokenStart, tokenLength);
            if (!token.EndsWith("px", StringComparison.Ordinal))
            {
                return false;
            }

            if (!double.TryParse(token.Substring(0, tokenLength - 2), out var strokeThickness))
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

            // Line style
            tokenStart = GetNextToken(text, tokenStart + tokenLength, out tokenLength);
            if (tokenStart < 0)
            {
                return true;
            }

            token = text.Substring(tokenStart, tokenLength);
            if (Enum.TryParse<LineStyle>(token, ignoreCase: true, out var lineStyle))
            {
                strokeParameters.LineStyle = lineStyle;
            }

            // Color
            tokenStart = GetNextToken(text, tokenStart + tokenLength, out tokenLength);
            if (tokenStart < 0)
            {
                return true;
            }

            token = text.Substring(tokenStart, tokenLength);
            if (TryParseColor(token, out var color))
            {
                strokeParameters.Brush = new SolidColorBrush(color);
            }

            return true;
        }

        /// <summary>
        /// Extract the next token from a string of whitespace-separated tokens.
        /// </summary>
        /// <param name="text">The complete text.</param>
        /// <param name="start">The starting index.</param>
        /// <param name="tokenLength">The length of the token found.</param>
        /// <returns>The starting index of the next token.</returns>
        private static int GetNextToken(string text, int start, out int tokenLength)
        {
            // Find the next token separated by whitespace or punctuation
            if (start < 0 || start > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            // Skip leading whitespace
            int i;
            for (i = start; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    break;
                }
            }

            if (i == text.Length)
            {
                // No more tokens
                tokenLength = 0;
                return -1;
            }

            start = i;

            for (; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    break;
                }
            }

            tokenLength = i - start;
            return start;
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
