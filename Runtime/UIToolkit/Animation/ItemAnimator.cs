using System;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView
{
    /// <summary>
    /// Pluggable expression layer for item change effects.
    /// </summary>
    /// <remarks>
    /// The view owns the mechanics (event routing by cause, deferred recycling
    /// of disappearing holders, interruption bookkeeping); the animator owns
    /// everything temporal — duration, easing, interpolation. The virtualizer's
    /// rect is always the single final truth; the animator only draws the
    /// frames converging toward it.
    ///
    /// Contract:
    /// - While an animation is active, the animator owns the holder's visual
    ///   box (transform, size, opacity). The view does not snap it directly.
    /// - Starting a new animation on a holder replaces any active one; the
    ///   replaced animation's completion is discarded. Move animations must
    ///   depart from the holder's *current visual state* (re-targeting), not
    ///   from the reported previous rect, so interruptions stay smooth.
    /// - <see cref="Cancel"/> jumps the holder to its final state, invokes the
    ///   pending completion, and neutralizes animation-owned styles (e.g.
    ///   opacity back to 1) so the holder is safe to reuse. It must be a no-op
    ///   beyond neutralization when nothing is animating.
    /// - <see cref="AnimateDisappear"/> MUST eventually invoke
    ///   <paramref name="onComplete"/> (directly or via Cancel): the view
    ///   recycles the holder only then.
    /// </remarks>
    public interface IItemAnimator<T>
    {
        /// <summary>Item appeared because it was inserted. The final rect is already applied.</summary>
        void AnimateAppear(CollectionItemViewHolder<T> holder, ItemRect target, Action onComplete);

        /// <summary>Realized item's rect changed (data change, relayout, layout swap).</summary>
        void AnimateMove(CollectionItemViewHolder<T> holder, ItemRect from, ItemRect to, Action onComplete);

        /// <summary>Item was removed while visible. Recycle happens in <paramref name="onComplete"/>.</summary>
        void AnimateDisappear(CollectionItemViewHolder<T> holder, Action onComplete);

        /// <summary>Jump to final state, fire pending completion, neutralize styles.</summary>
        void Cancel(CollectionItemViewHolder<T> holder);
    }
}
