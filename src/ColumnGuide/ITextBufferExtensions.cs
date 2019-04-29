// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace ColumnGuide
{
    internal static class ITextBufferExtensions
    {
        public static bool TryGetTextDocument(this ITextBuffer textBuffer, out ITextDocument textDocument)
        {
            if (textBuffer == null)
            {
                textDocument = null;
                return false;
            }

            return textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out textDocument);
        }
    }
}
