// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace EditorGuidelines
{
    [Export]
    internal sealed class HostServices
    {
        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider ServiceProvider
        {
            get;
            set;
        }

        public T GetService<T>(Type serviceType) where T : class
            => ServiceProvider.GetService(serviceType) as T;

        // Add services here

        public IVsSettingsManager SettingsManagerService
            => GetService<IVsSettingsManager>(typeof(SVsSettingsManager));
    }
}
