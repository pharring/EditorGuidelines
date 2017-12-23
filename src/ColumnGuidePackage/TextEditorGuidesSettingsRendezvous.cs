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
                    IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                    _instance = componentModel.GetService<ITextEditorGuidesSettingsChanger>();
                }

                return _instance;
            }
        }
    }
}
