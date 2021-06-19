// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorGuidelines
{
    internal static class IWpfTextViewExtensions
    {
        public static bool TryGetTextDocument(this IWpfTextView view, out ITextDocument textDocument)
        {
            if (view == null)
            {
                textDocument = null;
                return false;
            }

            // Try the TextBuffer first. If that fails, try the DocumentBuffer on the TextDataModel.
            return view.TextBuffer.TryGetTextDocument(out textDocument)
                || view.TextDataModel.DocumentBuffer.TryGetTextDocument(out textDocument);
        }
    }
}
