// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using Microsoft.VisualStudio.Threading;

namespace EditorGuidelinesTests.Harness
{
    internal abstract class AbstractServices
    {
        protected AbstractServices(TestServices testServices)
        {
            TestServices = testServices;
        }

        protected TestServices TestServices { get; }
        protected JoinableTaskFactory JoinableTaskFactory => TestServices.JoinableTaskFactory;
        protected IServiceProvider ServiceProvider => TestServices.ServiceProvider;
    }
}
