using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;

namespace EditorGuidelines
{
    /// <summary>
    /// Dev 17 support for Coding Conventions. Uses the dictionary supplied via <see cref="IEditorOptions"/>.
    /// </summary>
    [Export]
    internal sealed class CodingConventions
    {
        public Task<Context> CreateContextAsync(IWpfTextView view, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Context(view.Options, cancellationToken));
        }

        /// <summary>
        /// Coding conventions narrowed to a single file.
        /// </summary>
        public class Context
        {
            private readonly IEditorOptions _editorOptions;
            private readonly CancellationToken _cancellationToken;
            private IReadOnlyDictionary<string, object> _currentConventions;

            public Context(IEditorOptions editorOptions, CancellationToken cancellationToken)
            {
                _editorOptions = editorOptions;
                _cancellationToken = cancellationToken;

                _editorOptions.OptionChanged += OnEditorOptionChanged;
                _cancellationToken.Register(() => _editorOptions.OptionChanged -= OnEditorOptionChanged);
            }

            private void OnEditorOptionChanged(object sender, EditorOptionChangedEventArgs e)
            {
                if (e.OptionId == DefaultOptions.RawCodingConventionsSnapshotOptionName)
                {
                    _currentConventions = _editorOptions.GetOptionValue(DefaultOptions.RawCodingConventionsSnapshotOptionId);
                    ConventionsChanged?.Invoke(this);
                }
            }

            public delegate void ConventionsChangedEventHandler(Context sender);

            public event ConventionsChangedEventHandler ConventionsChanged;

            public bool TryGetCurrentSetting(string key, out string value)
            {
                if (_currentConventions is null || !_currentConventions.TryGetValue(key, out object obj) || obj is null)
                { 
                    value = null;
                    return false;
                }

                value = obj.ToString();
                return !string.IsNullOrEmpty(value);
            }
        }

    }
}
