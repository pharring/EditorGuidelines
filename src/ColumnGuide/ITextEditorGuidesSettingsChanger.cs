// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;

namespace EditorGuidelines
{
    public interface ITextEditorGuidesSettingsChanger
    {
        bool AddGuideline(int column);
        bool RemoveGuideline(int column);
        bool CanAddGuideline(int column);
        bool CanRemoveGuideline(int column);
        void RemoveAllGuidelines();
    }
}
