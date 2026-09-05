using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView
{
    /// <summary>
    /// Non-generic, UXML-placeable slot that carries a <see cref="CollectionViewConfig"/> and hosts one collection view.
    /// </summary>
    /// <remarks>
    /// The designer/controller boundary: designers place and size the host in
    /// UXML and assign the config; controllers query the host, read the config
    /// through <see cref="GetConfig{TConfig}"/> (the single dynamic seam),
    /// assemble the generic adapter and view, and <see cref="Host"/> the
    /// result. Editor panels (the UI Builder canvas) render a live preview
    /// instead; that half of the class lives in
    /// CollectionHostView.EditorPreview.cs and compiles away in players.
    /// </remarks>
    [UxmlElement]
    public partial class CollectionHostView : VisualElement
    {
        private CollectionViewConfig _config;
        private VisualElement _hosted;

        public CollectionHostView()
        {
#if UNITY_EDITOR
            RegisterCallback<AttachToPanelEvent>(_ => RebuildPreview());
            RegisterCallback<DetachFromPanelEvent>(_ => ClearPreview());
#endif
        }

        /// <summary>
        /// Specification asset for the collection view this slot hosts.
        /// </summary>
        /// <remarks>
        /// Inert data at runtime until a controller reads it; editor panels
        /// re-render the preview when it changes.
        /// </remarks>
        [UxmlAttribute]
        public CollectionViewConfig Config
        {
            get => _config;
            set
            {
                if (_config == value)
                    return;
                _config = value;
#if UNITY_EDITOR
                RebuildPreview();
#endif
            }
        }

        /// <summary>The view passed to <see cref="Host"/>, or null.</summary>
        public VisualElement HostedView => _hosted;

        /// <summary>
        /// Config downcast with the fail-fast policy: null plus an error log on
        /// a missing or mismatched asset, so callers can no-op gracefully.
        /// </summary>
        public TConfig GetConfig<TConfig>() where TConfig : CollectionViewConfig
        {
            if (_config is TConfig typed)
                return typed;

            Debug.LogError(_config == null
                ? $"[{nameof(CollectionHostView)}] '{name}' has no {nameof(CollectionViewConfig)} assigned; expected {typeof(TConfig).Name}."
                : $"[{nameof(CollectionHostView)}] '{name}' has {_config.GetType().Name} assigned; expected {typeof(TConfig).Name}.",
                _config);
            return null;
        }

        /// <summary>
        /// Hosts the assembled collection view, stretched to fill this slot.
        /// </summary>
        /// <remarks>
        /// Replaces a previously hosted view. The slot's resolved size becomes
        /// the view's viewport.
        /// </remarks>
        public void Host(VisualElement view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            _hosted?.RemoveFromHierarchy();
            _hosted = view;
            Fill(view);
            Add(view);
        }

        /// <summary>Removes and returns the hosted view, or null when nothing is hosted.</summary>
        public VisualElement Unhost()
        {
            var view = _hosted;
            _hosted = null;
            view?.RemoveFromHierarchy();
            return view;
        }

        private static void Fill(VisualElement view)
        {
            view.style.position = Position.Absolute;
            view.style.left = 0f;
            view.style.top = 0f;
            view.style.right = 0f;
            view.style.bottom = 0f;
        }
    }
}
