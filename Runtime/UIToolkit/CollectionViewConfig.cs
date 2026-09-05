using UnityEngine;

namespace Headwind.VirtualCollectionView
{
    /// <summary>
    /// Designer-owned specification asset for one collection view placement.
    /// </summary>
    /// <remarks>
    /// Pure data: templates, sizes, layout parameters, preview data all live
    /// on concrete subclasses, which controllers resolve via
    /// <see cref="CollectionHostView.GetConfig{TConfig}"/>. View assembly
    /// stays outside the asset — controllers build the runtime view, and
    /// editor-assembly preview builders (CollectionViewPreviewBuilder&lt;TConfig&gt;)
    /// build the UI Builder preview from the same asset.
    /// </remarks>
    public abstract class CollectionViewConfig : ScriptableObject
    {
    }
}
