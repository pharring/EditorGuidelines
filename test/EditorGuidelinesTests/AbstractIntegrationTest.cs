// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using EditorGuidelinesTests.Harness;
using EditorGuidelinesTests.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace EditorGuidelinesTests
{
    public abstract class AbstractIntegrationTest : IAsyncLifetime, IDisposable
    {
        private JoinableTaskContext _joinableTaskContext;
        private JoinableTaskCollection _joinableTaskCollection;
        private JoinableTaskFactory _joinableTaskFactory;

        private TestServices _testServices;

        protected AbstractIntegrationTest()
        {
            Assert.True(Application.Current.Dispatcher.CheckAccess());

            if (ServiceProvider.GetService(typeof(SVsTaskSchedulerService)) is IVsTaskSchedulerService2 taskSchedulerService)
            {
                JoinableTaskContext = (JoinableTaskContext)taskSchedulerService.GetAsyncTaskContext();
            }
            else
            {
                JoinableTaskContext = new JoinableTaskContext();
            }
        }

        protected static IServiceProvider ServiceProvider => Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider;

        public JoinableTaskContext JoinableTaskContext
        {
            get
            {
                return _joinableTaskContext ?? throw new InvalidOperationException();
            }

            private set
            {
                if (value == _joinableTaskContext)
                {
                    return;
                }

                if (value is null)
                {
                    _joinableTaskContext = null;
                    _joinableTaskCollection = null;
                    _joinableTaskFactory = null;
                }
                else
                {
                    _joinableTaskContext = value;
                    _joinableTaskCollection = value.CreateCollection();
                    _joinableTaskFactory = value.CreateFactory(_joinableTaskCollection).WithPriority(Application.Current.Dispatcher, DispatcherPriority.Background);
                }
            }
        }

        protected JoinableTaskFactory JoinableTaskFactory => _joinableTaskFactory ?? throw new InvalidOperationException();

        protected TestServices TestServices => _testServices ?? throw new InvalidOperationException();

        public virtual Task InitializeAsync()
        {
            _testServices = new TestServices(JoinableTaskFactory, ServiceProvider);
            return Task.CompletedTask;
        }

        public virtual async Task DisposeAsync()
        {
            await _joinableTaskCollection.JoinTillEmptyAsync();
            _testServices = null;
            JoinableTaskContext = null;
        }

        public virtual void Dispose()
        {
        }
    }
}
