// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.CodingConventions;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorGuidelines
{
    /// <summary>
    /// Coding conventions support via Microsoft.VisualStudio.CodingConventions assembly.
    /// </summary>
    [Export]
    internal class CodingConventions
    {
        /// <summary>
        /// Try to import from Microsoft.VisualStudio.CodingConventions.
        /// </summary>
        [Import(AllowDefault = true)]
        private ICodingConventionsManager CodingConventionsManager { get; set; }

        public async Task<Context> CreateContextAsync(IWpfTextView view, CancellationToken cancellationToken)
        {
            if (CodingConventionsManager is null)
            {
                // Coding Conventions not available in this SKU.
                return null;
            }

            if (!view.TryGetTextDocument(out var textDocument))
            {
                return null;
            }

            string filePath = textDocument.FilePath;
            ICodingConventionContext codingConventionContext = await CodingConventionsManager.GetConventionContextAsync(filePath, cancellationToken).ConfigureAwait(false);
            return new Context(codingConventionContext, cancellationToken);
        }

        /// <summary>
        /// Coding conventions narrowed to a single file.
        /// </summary>
        public class Context
        {
            private readonly ICodingConventionContext _innerContext;

            public Context(ICodingConventionContext innerContext, CancellationToken cancellationToken)
            {
                _innerContext = innerContext;
                _innerContext.CodingConventionsChangedAsync += OnCodingConventionsChangedAsync;
                cancellationToken.Register(() => _innerContext.CodingConventionsChangedAsync -= OnCodingConventionsChangedAsync);
            }

            private Task OnCodingConventionsChangedAsync(object sender, CodingConventionsChangedEventArgs arg)
            {
                ConventionsChanged?.Invoke(this);
                return Task.CompletedTask;
            }

            public delegate void ConventionsChangedEventHandler(Context sender);
            
            public event ConventionsChangedEventHandler ConventionsChanged;

            public bool TryGetCurrentSetting(string key, out string value)
            {
                return _innerContext.CurrentConventions.TryGetConventionValue(key, out value);
            }
        }
    }
}
