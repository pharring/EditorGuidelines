// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Xunit;
using ColumnGuide;
using System.Windows.Media;

namespace ColumnGuideTests
{
    public class UnitTest1
    {
        [Theory]
        [InlineData("")]
        [InlineData("0", 0)]
        [InlineData("0 1", 0, 1)]
        [InlineData("0,1", 0, 1)]
        [InlineData("0;1,2", 0, 1, 2)]
        [InlineData("0 1,2    3", 0, 1, 2, 3)]
        [InlineData("132:80, 40,50,60 4 8", 4, 8, 40, 50, 60, 80, 132)]
        [InlineData("80,80,80", 80)]
        [InlineData("-1, 99999, 80", 80)]
        [InlineData("ABC, 3.14, 80", 80)]
        public void ParseGuidelinePositionsTest(string codingConvention, params int[] expected)
        {
            var actual = Parser.ParseGuidelinePositionsFromCodingConvention(codingConvention);
            Assert.True(actual.SetEquals(expected));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(",", false)]
        [InlineData("2", false)] // No 'px' suffix
        [InlineData("-2px", false)] // -ve
        [InlineData("51px", false)] // Too big
        [InlineData("abpx", false)] // Not a number
        [InlineData("5px, unknown-style", true, 5)] // Unrecognized style, but otherwise valid
        [InlineData("5px, unknown-style, green", true, 5, default(LineStyle), 0xFF, 0x00, 0x80, 0x00)] // Unrecognized style, but color valid
        [InlineData("1px", true, 1.0)]
        [InlineData("0.5px", true, 0.5)]
        [InlineData("3px dashed", true, 3, LineStyle.Dashed)]
        [InlineData("1.9px solid red", true, 1.9, LineStyle.Solid, 0xFF, 0xFF, 0x00, 0x00)]
        [InlineData("2.00px dashed A0553201", true, 2, LineStyle.Dashed, 0xA0, 0x55, 0x32, 0x01)]
        [InlineData("1px solid FEDCBA", true, 1, LineStyle.Solid, 0xFF, 0xFE, 0xDC, 0xBA)]
        [InlineData("1px;solid;Not-a-real-color", true, 1, LineStyle.Solid)]
        [InlineData("4px:dotted:blue:ignored", true, 4, LineStyle.Dotted, 0xFF, 0x00, 0x00, 0xFF)]
        internal void ParseStyleTest(string text, bool expected, double expectedThickness = default, LineStyle expectedLineStyle = default, byte expectedA = 0xFF, byte expectedR = 0, byte expectedG = 0, byte expectedB = 0)
        {
            var actual = Parser.TryParseStrokeParametersFromCodingConvention(text, out var strokeParameters);
            Assert.Equal(expected, actual);

            if (expected)
            {
                Assert.Equal(expectedThickness, strokeParameters.StrokeThickness);
                Assert.Equal(expectedLineStyle, strokeParameters.LineStyle);
                var expectedColor = Color.FromArgb(expectedA, expectedR, expectedG, expectedB);
                Assert.Equal(expectedColor, strokeParameters.BrushColor);
            }
        }
    }
}
