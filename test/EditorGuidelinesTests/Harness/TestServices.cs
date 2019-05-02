// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Threading;
using Microsoft;
using Microsoft.VisualStudio.Threading;

namespace EditorGuidelinesTests.Harness
{
    public sealed class TestServices
    {
        internal TestServices(JoinableTaskFactory joinableTaskFactory, IServiceProvider serviceProvider)
        {
            JoinableTaskFactory = joinableTaskFactory;
            ServiceProvider = serviceProvider;

            Solution = new SolutionServices(this);
        }

        public JoinableTaskFactory JoinableTaskFactory { get; }
        public IServiceProvider ServiceProvider { get; }

        internal SolutionServices Solution { get; }

        internal void ThrowIfNotOnMainThread()
        {
            Verify.Operation(JoinableTaskFactory.Context.MainThread == Thread.CurrentThread, "This type can only be constructed on the main thread.");
        }
    }
}
