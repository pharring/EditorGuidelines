// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using ColumnGuide;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.ColumnGuidePackage
{
    static class TextEditorGuidesSettingsRendezvous
    {
        private static ITextEditorGuidesSettingsChanger _instance;
        public static ITextEditorGuidesSettingsChanger Instance
        {
            get
            {
                if (_instance == null)
                {
                    var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                    _instance = componentModel.GetService<ITextEditorGuidesSettingsChanger>();
                }

                return _instance;
            }
        }
    }
}
