// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System.Threading;
using System.Windows.Threading;
using Microsoft;
using Microsoft.VisualStudio.Threading;

namespace EditorGuidelinesTests.Threading
{
    // JoinableTaskFactory.WithPriority is available in later releases of vs-threading, but we reference 1.2.0.0 for
    // compatibility with Visual Studio 2013.
    // https://github.com/Microsoft/vs-threading/pull/142
    internal static class JoinableTaskFactoryExtensions
    {
        internal static JoinableTaskFactory WithPriority(this JoinableTaskFactory joinableTaskFactory, Dispatcher dispatcher, DispatcherPriority priority)
        {
            Requires.NotNull(joinableTaskFactory, nameof(joinableTaskFactory));
            Requires.NotNull(dispatcher, nameof(dispatcher));

            return new DispatcherJoinableTaskFactory(joinableTaskFactory, dispatcher, priority);
        }

        private class DispatcherJoinableTaskFactory : DelegatingJoinableTaskFactory
        {
            private readonly Dispatcher _dispatcher;
            private readonly DispatcherPriority _priority;

            public DispatcherJoinableTaskFactory(JoinableTaskFactory innerFactory, Dispatcher dispatcher, DispatcherPriority priority)
                : base(innerFactory)
            {
                _dispatcher = dispatcher;
                _priority = priority;
            }

            protected override void PostToUnderlyingSynchronizationContext(SendOrPostCallback callback, object state)
            {
                _dispatcher.BeginInvoke(_priority, callback, state);
            }
        }
    }
}
