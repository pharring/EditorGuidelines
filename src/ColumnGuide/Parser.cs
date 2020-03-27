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
        /// <summary>
        /// Note: Semicolon is not a valid separator because it's treated as a comment in .editorconfig
        /// </summary>
        private static readonly char[] s_separators = { ',', ':', ' ' };

        private static readonly char[] s_space= { ' ' };

        private static readonly char[] s_comma = { ',' };

        public static HashSet<Guideline> ParseGuidelinesFromCodingConvention(string codingConvention, StrokeParameters fallbackStrokeParameters)
        {
            // First try parsing as a sequence of columns and styles separated by commas.
            var result = ParseGuidelines(codingConvention, fallbackStrokeParameters);
            if (result != null)
            {
                return result;
            }

            // Fall back to parsing as just a set of column positions, ignoring any unparsable values.
            result = new HashSet<Guideline>();
            foreach (var position in GetTokens(codingConvention, s_separators))
            {
                if (TryParsePosition(position, out int column))
                {
                    result.Add(new Guideline(column, fallbackStrokeParameters));
                }
            }

            return result;
        }

        /// <summary>
        /// Try to parse as a sequence of columns and styles separated by commas.
        /// e.g. 40 1px solid red, 80 2px dashed blue
        /// The style part is optional but, if present, it must be well-formed.
        /// </summary>
        /// <param name="codingConvention">The coding convention.</param>
        /// <param name="fallbackStrokeParameters">Stroke parameters to use when the style is not specified.</param>
        /// <returns>The set of guidelines if successful, or null if not.</returns>
        private static HashSet<Guideline> ParseGuidelines(string codingConvention, StrokeParameters fallbackStrokeParameters)
        {
            var set = new HashSet<Guideline>();
            foreach (var token in GetTokens(codingConvention, s_comma))
            {
                var partEnumerator = GetTokens(token, s_space);
                if (!partEnumerator.MoveNext())
                {
                    // Empty token. Ignore and continue.
                    continue;
                }

                if (!TryParsePosition(partEnumerator.Current, out var column))
                {
                    return null;
                }

                var strokeParameters = fallbackStrokeParameters;
                if (partEnumerator.MoveNext() && !TryParseStrokeParameters(partEnumerator, out strokeParameters))
                {
                    return null;
                }

                set.Add(new Guideline(column, strokeParameters?.Freeze()));
            }

            return set;
        }

        public static bool TryParsePosition(string text, out int column)
            => int.TryParse(text, out column) && Guideline.IsValidColumn(column);

        /// <summary>
        /// The guideline_style looks like this:
        /// guidelines_style = 1px dotted 80FF0000
        /// Meaning single pixel, dotted style, color red, 50% opaque
        /// 
        /// 1px specifies the width in pixels.
        /// dotted specifies the line style.Simple to support: solid, dotted and dashed
        /// </summary>
        /// <param name="text">The value read from guidelines_style editorconfig.</param>
        /// <param name="strokeParameters">The parsed stroke parameters.</param>
        /// <returns>True if parameters were parsed. False otherwise.</returns>
        public static bool TryParseStrokeParametersFromCodingConvention(string text, out StrokeParameters strokeParameters)
        {
            var tokensEnumerator = GetTokens(text, s_separators).GetEnumerator();
            if (!tokensEnumerator.MoveNext())
            {
                strokeParameters = null;
                return false;
            }

            return TryParseStrokeParameters(tokensEnumerator, out strokeParameters);
        }

        private static bool TryParseStrokeParameters(TokenEnumerator tokensEnumerator, out StrokeParameters strokeParameters)
        {
            strokeParameters = null;

            // Pixel width (stroke thickness)
            var token = tokensEnumerator.Current;
            if (!token.EndsWith("px", StringComparison.Ordinal))
            {
                return false;
            }

            if (!double.TryParse(token.Substring(0, token.Length - 2), out var strokeThickness))
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

            if (!tokensEnumerator.MoveNext())
            {
                return true;
            }

            // Line style
            token = tokensEnumerator.Current;
            if (Enum.TryParse<LineStyle>(token, ignoreCase: true, out var lineStyle))
            {
                strokeParameters.LineStyle = lineStyle;
            }

            if (!tokensEnumerator.MoveNext())
            {
                return true;
            }

            // Color
            token = tokensEnumerator.Current;
            if (TryParseColor(token, out var color))
            {
                strokeParameters.Brush = new SolidColorBrush(color);
            }

            // Ignore trailing tokens.
            return true;
        }

        private struct TokenEnumerator
        {
            private readonly string _text;
            private readonly char[] _separators;
            private int _iStart;

            public TokenEnumerator(string text, char[] separators)
            {
                _text = text;
                _separators = separators;
                _iStart = 0;
                Current = null;
            }

            public TokenEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (_text == null)
                {
                    return false;
                }

                // Equivalent of text.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                while (_iStart < _text.Length)
                {
                    var iNextSeparator = _text.IndexOfAny(_separators, _iStart);
                    if (iNextSeparator < 0)
                    {
                        iNextSeparator = _text.Length;
                    }

                    var tokenLength = iNextSeparator - _iStart;
                    if (tokenLength > 0)
                    {
                        Current = _text.Substring(_iStart, tokenLength);
                        _iStart = iNextSeparator + 1;
                        return true;
                    }

                    _iStart = iNextSeparator + 1;
                }

                return false;
            }

            public string Current { get; private set; }
        }

        private static TokenEnumerator GetTokens(string text, char[] separators) => new TokenEnumerator(text, separators);

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
