using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView.Editor
{
    /// <summary>
    /// Edit-time counterpart of a runtime controller: builds a live preview view from a config.
    /// </summary>
    /// <remarks>
    /// Deriving from <see cref="CollectionViewPreviewBuilder{TConfig}"/> IS
    /// the registration: the generic argument maps the builder to its config
    /// type, discovered via TypeCache on each preview request. Builders are
    /// instantiated per request (parameterless constructor required), so they
    /// may keep per-build state.
    /// </remarks>
    public abstract class CollectionViewPreviewBuilder
    {
        public abstract VisualElement Build(CollectionViewConfig config);
    }

    /// <summary>
    /// Typed convenience base that closes the generic seam.
    /// </summary>
    /// <remarks>
    /// The bridge only dispatches configs of <typeparamref name="TConfig"/>
    /// (or a subtype without a more specific builder) to this builder, so the
    /// single cast here is safe.
    /// </remarks>
    public abstract class CollectionViewPreviewBuilder<TConfig> : CollectionViewPreviewBuilder
        where TConfig : CollectionViewConfig
    {
        public sealed override VisualElement Build(CollectionViewConfig config) => Build((TConfig)config);

        protected abstract VisualElement Build(TConfig config);
    }
}
