using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.UIElements;
#endif

namespace Headwind.VirtualCollectionView
{
    /// <summary>
    /// Designer-owned specification asset for one collection view placement.
    /// </summary>
    /// <remarks>
    /// The library only ever sees this base type: a
    /// <see cref="CollectionHostView"/> carries the reference from UXML to
    /// runtime code, and the editor preview pipeline calls
    /// <see cref="CreatePreviewView"/>. Everything else — templates, sizes,
    /// layout parameters, preview data — lives on concrete subclasses, which
    /// controllers resolve via
    /// <see cref="CollectionHostView.GetConfig{TConfig}"/>.
    /// </remarks>
    public abstract class CollectionViewConfig : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>
        /// Editor-only factory for a live, self-contained preview of this config.
        /// </summary>
        /// <remarks>
        /// Runs inside editor panels (the UI Builder canvas, catalog tooling).
        /// Implementations close the generic seam here: build the concrete
        /// VirtualCollectionView&lt;T&gt; with a sample-data adapter and the
        /// same layout parameters the runtime path uses, and return it
        /// untyped. The host parents the result and stretches it to fill the
        /// slot, so the slot's resolved size becomes the preview's viewport.
        /// </remarks>
        public abstract VisualElement CreatePreviewView();
#endif
    }
}
