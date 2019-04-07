// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using ColumnGuide;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.ColumnGuidePackage
{
    static class TextEditorGuidesSettingsRendezvous
    {
        private static ITextEditorGuidesSettingsChanger s_instance;
        public static ITextEditorGuidesSettingsChanger Instance => s_instance ?? (s_instance = GetGlobalInstance());

        private static ITextEditorGuidesSettingsChanger GetGlobalInstance()
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            return componentModel.GetService<ITextEditorGuidesSettingsChanger>();
        }
    }
}
