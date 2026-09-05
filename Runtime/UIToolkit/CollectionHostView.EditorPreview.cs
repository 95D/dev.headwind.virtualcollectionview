#if UNITY_EDITOR
using System;
using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView
{
    // Edit-time half of the host: the live preview shown in editor panels
    // (the UI Builder canvas) while no view is hosted. Same assembly as the
    // runtime half — a partial split, not an editor assembly — because the
    // preview reacts to runtime members (_hosted, Config, panel events).
    public partial class CollectionHostView
    {
        /// <summary>
        /// Preview factory installed by the editor assembly.
        /// </summary>
        /// <remarks>
        /// Declared here because the runtime assembly cannot reference editor
        /// code; the editor assembly assigns it at load. Receives the host's
        /// current config, which may be null — placeholder policy (missing
        /// config, throwing preview) is the installer's responsibility. A null
        /// return shows nothing.
        /// </remarks>
        public static Func<CollectionViewConfig, VisualElement> EditorPreviewFactory;

        private VisualElement _preview;

        private void RebuildPreview()
        {
            ClearPreview();

            // Edit-time only: never over hosted content, never in player
            // panels (the game view), never before panel attach.
            if (_hosted != null || panel?.contextType != ContextType.Editor)
                return;

            _preview = EditorPreviewFactory?.Invoke(_config);
            if (_preview == null)
                return;
            Fill(_preview);
            Add(_preview);
        }

        private void ClearPreview()
        {
            _preview?.RemoveFromHierarchy();
            _preview = null;
        }
    }
}
#endif
