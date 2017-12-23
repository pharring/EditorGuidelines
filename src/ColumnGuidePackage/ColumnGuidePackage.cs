using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;

namespace Microsoft.ColumnGuidePackage
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 2)]
    [Guid(GuidList.guidColumnGuidePkgString)]
    public sealed class ColumnGuidePackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public ColumnGuidePackage()
        {
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            Telemetry.Client.TrackEvent(nameof(ColumnGuidePackage) + "." + nameof(Initialize), new Dictionary<string, string>() { ["VSVersion"] = GetShellVersion() });

            // Add our command handlers for menu (commands must exist in the .vsct file)

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                _addGuidelineCommand = new OleMenuCommand(AddColumnGuideExecuted, null, AddColumnGuideBeforeQueryStatus, new CommandID(GuidList.guidColumnGuideCmdSet, (int)PkgCmdIDList.cmdidAddColumnGuideline))
                {
                    ParametersDescription = "<column>"
                };
                mcs.AddCommand(_addGuidelineCommand);

                _removeGuidelineCommand = new OleMenuCommand(RemoveColumnGuideExecuted, null, RemoveColumnGuideBeforeChangeQueryStatus, new CommandID(GuidList.guidColumnGuideCmdSet, (int)PkgCmdIDList.cmdidRemoveColumnGuideline))
                {
                    ParametersDescription = "<column>"
                };
                mcs.AddCommand(_removeGuidelineCommand);

                mcs.AddCommand(new MenuCommand(RemoveAllGuidelinesExecuted, new CommandID(GuidList.guidColumnGuideCmdSet, (int)PkgCmdIDList.cmdidRemoveAllColumnGuidelines)));
            }
        }

        #endregion

        private string GetShellVersion()
        {
            var shell = GetService(typeof(SVsShell)) as IVsShell;
            if (shell != null)
            {
                object obj;
                if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out obj)) && obj != null)
                {
                    return obj.ToString();
                }
            }

            return "Unknown";
        }

        OleMenuCommand _addGuidelineCommand;
        OleMenuCommand _removeGuidelineCommand;

        private void AddColumnGuideBeforeQueryStatus(object sender, EventArgs e)
        {
            int currentColumn = GetCurrentEditorColumn();
            _addGuidelineCommand.Enabled = TextEditorGuidesSettingsRendezvous.Instance.CanAddGuideline(currentColumn);
        }

        private void RemoveColumnGuideBeforeChangeQueryStatus(object sender, EventArgs e)
        {
            int currentColumn = GetCurrentEditorColumn();
            _removeGuidelineCommand.Enabled = TextEditorGuidesSettingsRendezvous.Instance.CanRemoveGuideline(currentColumn);
        }

        /// <summary>
        /// Determine the applicable column number for an add or remove command.
        /// The column is parsed from command arguments, if present. Otherwise
        /// the current position of the caret is used to determine the column.
        /// </summary>
        /// <param name="e">Event args passed to the command handler.</param>
        /// <returns>The column number. May be negative to indicate the column number is unavailable.</returns>
        /// <exception cref="ArgumentException">The column number parsed from event args was not a valid integer.</exception>
        private int GetApplicableColumn(EventArgs e)
        {
            var inValue = ((OleMenuCmdEventArgs)e).InValue as string;
            if (!string.IsNullOrEmpty(inValue))
            {
                int column;
                if (!int.TryParse(inValue, out column) || column < 0)
                    throw new ArgumentException("Invalid column");

                Telemetry.Client.TrackEvent("Command parameter used");
                return column;
            }

            return GetCurrentEditorColumn();
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void AddColumnGuideExecuted(object sender, EventArgs e)
        {
            int column = GetApplicableColumn(e);
            if (column >= 0)
            {
                Telemetry.Client.TrackEvent(nameof(AddColumnGuideExecuted), new Dictionary<string, string>() { ["Column"] = column.ToString() });
                TextEditorGuidesSettingsRendezvous.Instance.AddGuideline(column);
            }
        }

        private void RemoveColumnGuideExecuted(object sender, EventArgs e)
        {
            int column = GetApplicableColumn(e);
            if (column >= 0)
            {
                Telemetry.Client.TrackEvent(nameof(RemoveColumnGuideExecuted), new Dictionary<string, string>() { ["Column"] = column.ToString() });
                TextEditorGuidesSettingsRendezvous.Instance.RemoveGuideline(column);
            }
        }

        private void RemoveAllGuidelinesExecuted(object sender, EventArgs e)
        {
            Telemetry.Client.TrackEvent(nameof(RemoveAllGuidelinesExecuted));
            TextEditorGuidesSettingsRendezvous.Instance.RemoveAllGuidelines();
        }

        /// <summary>
        /// Find the active text view (if any) in the active document.
        /// </summary>
        /// <returns>The IVsTextView of the active view, or null if there is no active document or the
        /// active view in the active document is not a text view.</returns>
        private IVsTextView GetActiveTextView()
        {
            IVsMonitorSelection selection = GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;
            object frameObj = null;
            ErrorHandler.ThrowOnFailure(selection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out frameObj));

            IVsWindowFrame frame = frameObj as IVsWindowFrame;
            if (frame == null)
            {
                return null;
            }

            return GetActiveView(frame);
        }

        private static IVsTextView GetActiveView(IVsWindowFrame windowFrame)
        {
            if (windowFrame == null)
            {
                throw new ArgumentException("windowFrame");
            }

            object pvar;
            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));

            IVsTextView textView = pvar as IVsTextView;
            if (textView == null)
            {
                IVsCodeWindow codeWin = pvar as IVsCodeWindow;
                if (codeWin != null)
                {
                    ErrorHandler.ThrowOnFailure(codeWin.GetLastActiveView(out textView));
                }
            }
            return textView;
        }

        private static IWpfTextView GetTextViewFromVsTextView(IVsTextView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            IVsUserData userData = view as IVsUserData;
            if (userData == null)
            {
                throw new InvalidOperationException();
            }

            object objTextViewHost;
            if (VSConstants.S_OK != userData.GetData(Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost, out objTextViewHost))
            {
                throw new InvalidOperationException();
            }

            IWpfTextViewHost textViewHost = objTextViewHost as IWpfTextViewHost;
            if (textViewHost == null)
            {
                throw new InvalidOperationException();
            }

            return textViewHost.TextView;
        }

        /// <summary>
        /// Given an IWpfTextView, find the position of the caret and report its column number
        /// The column number is 0-based
        /// </summary>
        /// <param name="textView">The text view containing the caret</param>
        /// <returns>The column number of the caret's position. When the caret is at the leftmost column, the return value is zero.</returns>
        private static int GetCaretColumn(IWpfTextView textView)
        {
            // This is the code the editor uses to populate the status bar. Thanks, Jack!
            Microsoft.VisualStudio.Text.Formatting.ITextViewLine caretViewLine = textView.Caret.ContainingTextViewLine;
            double columnWidth = textView.FormattedLineSource.ColumnWidth;
            return (int)(Math.Round((textView.Caret.Left - caretViewLine.Left) / columnWidth));
        }

        private int GetCurrentEditorColumn()
        {
            IVsTextView view = GetActiveTextView();
            if (view == null)
            {
                return -1;
            }

            try
            {
                IWpfTextView textView = GetTextViewFromVsTextView(view);
                int column = GetCaretColumn(textView);

                // Note: GetCaretColumn returns 0-based positions. Guidelines are 1-based positions.
                // However, do not subtract one here since the caret is positioned to the left of
                // the given column and the guidelines are positioned to the right. We want the
                // guideline to line up with the current caret position. e.g. When the caret is
                // at position 1 (zero-based), the status bar says column 2. We want to add a
                // guideline for column 1 since that will place the guideline where the caret is.
                return column;
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
        }
    }
}
