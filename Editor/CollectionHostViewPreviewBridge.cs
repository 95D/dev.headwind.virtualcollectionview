using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView.Editor
{
    /// <summary>
    /// Installer of the host-view preview hook at editor load.
    /// </summary>
    /// <remarks>
    /// The runtime assembly declares the hook but cannot populate it; this
    /// bridge closes the loop so a <see cref="CollectionHostView"/> in any
    /// editor panel (the UI Builder canvas included) renders its config's live
    /// preview. Placeholder policy lives here: a missing config or a throwing
    /// preview yields an on-canvas placeholder instead of an empty slot.
    /// </remarks>
    [InitializeOnLoad]
    internal static class CollectionHostViewPreviewBridge
    {
        static CollectionHostViewPreviewBridge()
        {
            CollectionHostView.EditorPreviewFactory = CreatePreview;
        }

        private static VisualElement CreatePreview(CollectionViewConfig config)
        {
            if (config == null)
                return PreviewPlaceholderFactory.MissingConfig();

            try
            {
                return config.CreatePreviewView();
            }
            catch (Exception e)
            {
                Debug.LogException(e, config);
                return PreviewPlaceholderFactory.Error(
                    $"{config.GetType().Name} preview threw {e.GetType().Name} (see console)");
            }
        }
    }
}
