using UnityEngine;
using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView.Editor
{
    /// <summary>
    /// Builder of on-canvas placeholder boxes shown instead of a host preview.
    /// </summary>
    /// <remarks>
    /// Problems must be visible on the canvas, not only in the console —
    /// designers work in UI Builder, not in logs.
    /// </remarks>
    internal static class PreviewPlaceholderFactory
    {
        public static VisualElement MissingConfig() =>
            Build($"Assign a {nameof(CollectionViewConfig)}", isError: false);

        public static VisualElement Error(string message) =>
            Build(message, isError: true);

        private static VisualElement Build(string message, bool isError)
        {
            var box = new VisualElement();
            var accent = isError
                ? new Color(0.85f, 0.15f, 0.85f)
                : new Color(0.55f, 0.55f, 0.60f);
            box.style.backgroundColor = new Color(accent.r, accent.g, accent.b, 0.08f);
            box.style.borderLeftColor = accent;
            box.style.borderRightColor = accent;
            box.style.borderTopColor = accent;
            box.style.borderBottomColor = accent;
            box.style.borderLeftWidth = 1f;
            box.style.borderRightWidth = 1f;
            box.style.borderTopWidth = 1f;
            box.style.borderBottomWidth = 1f;
            box.style.justifyContent = Justify.Center;

            var label = new Label(message);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = accent;
            box.Add(label);

            return box;
        }
    }
}
