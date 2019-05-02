// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

namespace EditorGuidelinesTests.Harness
{
    internal sealed class SolutionServices : AbstractServices
    {
        public SolutionServices(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task OpenSolutionAsync(string path, bool saveExistingSolutionIfExists = false)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await CloseSolutionAsync(saveExistingSolutionIfExists);

            var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));
            ErrorHandler.ThrowOnFailure(solution.OpenSolutionFile((uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, path));
            await Task.Yield();
        }

        public async Task SaveSolutionAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!await IsSolutionOpenAsync())
            {
                throw new InvalidOperationException("Cannot save solution when no solution is open.");
            }

            var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));

            // Make sure the director exists so the Save dialog does not appear
            ErrorHandler.ThrowOnFailure(solution.GetSolutionInfo(out var solutionDirectory, out _, out _));
            Directory.CreateDirectory(solutionDirectory);

            ErrorHandler.ThrowOnFailure(solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave, pHier: null, docCookie: 0));
        }

        public async Task<bool> IsSolutionOpenAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));
            ErrorHandler.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpen));
            return (bool)isOpen;
        }

        public async Task CloseSolutionAsync(bool saveIfExists = false)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));
            if (!await IsSolutionOpenAsync())
            {
                return;
            }

            if (saveIfExists)
            {
                await SaveSolutionAsync();
            }

            using (var semaphore = new SemaphoreSlim(1))
            using (var solutionEvents = new SolutionEvents(TestServices, solution))
            {
                await semaphore.WaitAsync();
                solutionEvents.AfterCloseSolution += HandleAfterCloseSolution;

                try
                {
                    ErrorHandler.ThrowOnFailure(solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject | (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave, pHier: null, docCookie: 0));
                    await semaphore.WaitAsync();
                }
                finally
                {
                    solutionEvents.AfterCloseSolution -= HandleAfterCloseSolution;
                }

                // Local functions
                void HandleAfterCloseSolution(object sender, EventArgs e) => semaphore.Release();
            }
        }

        public async Task OpenFileAsync(string projectName, string relativeFilePath)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var filePath = GetAbsolutePathForProjectRelativeFilePath(projectName, relativeFilePath);
            if (!IsDocumentOpen(filePath, VSConstants.LOGVIEWID.Code_guid, out _, out _, out var windowFrame))
            {
                var uiShellOpenDocument = (IVsUIShellOpenDocument)ServiceProvider.GetService(typeof(SVsUIShellOpenDocument));
                ErrorHandler.ThrowOnFailure(uiShellOpenDocument.OpenDocumentViaProject(filePath, VSConstants.LOGVIEWID.Code_guid, out _, out _, out _, out windowFrame));
            }
            else if (windowFrame is object)
            {
                var uiShellOpenDocument = (IVsUIShellOpenDocument3)ServiceProvider.GetService(typeof(SVsUIShellOpenDocument));
                if (((__VSNEWDOCUMENTSTATE)uiShellOpenDocument.NewDocumentState).HasFlag(__VSNEWDOCUMENTSTATE.NDS_Permanent))
                {
                    windowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_IsProvisional, false);
                }
            }

            if (windowFrame is object)
            {
                ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }

            var view = GetTextView(windowFrame);

            // Reliably set focus using NavigateToLineAndColumn
            var textManager = (IVsTextManager)ServiceProvider.GetService(typeof(SVsTextManager));
            ErrorHandler.ThrowOnFailure(view.GetBuffer(out var textLines));
            ErrorHandler.ThrowOnFailure(view.GetCaretPos(out var line, out var column));
            ErrorHandler.ThrowOnFailure(textManager.NavigateToLineAndColumn(textLines, VSConstants.LOGVIEWID.Code_guid, line, column, line, column));
        }

        public async Task<bool> IsDocumentOpenAsync(string projectName, string relativeFilePath, Guid logicalView)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var filePath = GetAbsolutePathForProjectRelativeFilePath(projectName, relativeFilePath);
            return IsDocumentOpen(filePath, logicalView, out _, out _, out _);
        }

        private string GetAbsolutePathForProjectRelativeFilePath(string projectName, string relativeFilePath)
        {
            TestServices.ThrowIfNotOnMainThread();

            var dte = (EnvDTE.DTE)ServiceProvider.GetService(typeof(EnvDTE.DTE));
            var project = dte.Solution.Projects.Cast<EnvDTE.Project>().First(x => x.Name == projectName);
            var projectPath = Path.GetDirectoryName(project.FullName);
            return Path.Combine(projectPath, relativeFilePath);
        }

        private IVsTextView GetTextView(IVsWindowFrame windowFrame)
        {
            TestServices.ThrowIfNotOnMainThread();

            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var docView));
            var textView = docView as IVsTextView;
            if (textView is null)
            {
                var codeWindow = docView as IVsCodeWindow;
                if (codeWindow is object)
                {
                    ErrorHandler.ThrowOnFailure(codeWindow.GetPrimaryView(out textView));
                }
            }

            return textView;
        }

        private bool IsDocumentOpen(string fullPath, Guid logicalView, out IVsUIHierarchy hierarchy, out uint itemID, out IVsWindowFrame windowFrame)
        {
            TestServices.ThrowIfNotOnMainThread();

            var uiShellOpenDocument = (IVsUIShellOpenDocument)ServiceProvider.GetService(typeof(SVsUIShellOpenDocument));
            var runningDocumentTable = (IVsRunningDocumentTable)ServiceProvider.GetService(typeof(SVsRunningDocumentTable));

            var docData = IntPtr.Zero;
            try
            {
                var itemidOpen = new uint[1];
                ErrorHandler.ThrowOnFailure(runningDocumentTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, fullPath, out var hier, out itemidOpen[0], out docData, out var cookie));

                var flags = logicalView == Guid.Empty ? (uint)__VSIDOFLAGS.IDO_IgnoreLogicalView : 0;
                ErrorHandler.ThrowOnFailure(uiShellOpenDocument.IsDocumentOpen((IVsUIHierarchy)hier, itemidOpen[0], fullPath, logicalView, flags, out hierarchy, itemidOpen, out windowFrame, out var open));
                if (windowFrame is object)
                {
                    itemID = itemidOpen[0];
                    return open == 1;
                }
            }
            finally
            {
                if (docData != IntPtr.Zero)
                {
                    Marshal.Release(docData);
                }
            }

            itemID = (uint)VSConstants.VSITEMID.Nil;
            return false;
        }

        private sealed class SolutionEvents : IVsSolutionEvents, IDisposable
        {
            private readonly JoinableTaskFactory _joinableTaskFactory;
            private readonly IVsSolution _solution;
            private readonly uint _cookie;

            public SolutionEvents(TestServices testServices, IVsSolution solution)
            {
                testServices.ThrowIfNotOnMainThread();

                _joinableTaskFactory = testServices.JoinableTaskFactory;
                _solution = solution;
                ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _cookie));
            }

            public event EventHandler AfterCloseSolution;

            public void Dispose()
            {
                _joinableTaskFactory.Run(async () =>
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ErrorHandler.ThrowOnFailure(_solution.UnadviseSolutionEvents(_cookie));
                });
            }

            public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.S_OK;
            public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;
            public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;
            public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;
            public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;
            public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;
            public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => VSConstants.S_OK;
            public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;
            public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;

            public int OnAfterCloseSolution(object pUnkReserved)
            {
                AfterCloseSolution?.Invoke(this, EventArgs.Empty);
                return VSConstants.S_OK;
            }
        }
    }
}
