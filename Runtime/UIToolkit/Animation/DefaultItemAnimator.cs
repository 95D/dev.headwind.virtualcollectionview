using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView
{
    /// <summary>
    /// Default expression: fade-in on insert, fade-out on remove, ease-out slide on move.
    /// </summary>
    /// <remarks>
    /// Moves depart from the holder's current visual state (not the reported
    /// previous rect). One active animation per holder; re-targeting replaces
    /// it smoothly.
    /// </remarks>
    public class DefaultItemAnimator<T> : IItemAnimator<T>
    {
        /// <summary>Animation length in milliseconds.</summary>
        public long DurationMs { get; set; } = 150;

        private sealed class Anim
        {
            public IVisualElementScheduledItem Ticker;
            public long LastTime;
            public float Elapsed;
            public ItemRect From;
            public ItemRect To;
            public float FromOpacity;
            public float ToOpacity;
            public Action OnComplete;
        }

        private readonly Dictionary<CollectionItemViewHolder<T>, Anim> _anims =
            new Dictionary<CollectionItemViewHolder<T>, Anim>();

        public void AnimateAppear(CollectionItemViewHolder<T> holder, ItemRect target, Action onComplete)
        {
            // The view already applied the final rect; fade in at that spot.
            Start(holder, target, target, 0f, 1f, onComplete);
        }

        public void AnimateMove(CollectionItemViewHolder<T> holder, ItemRect from, ItemRect to, Action onComplete)
        {
            // Depart from the current visual state (not the reported `from`)
            // so an interrupted slide re-targets without a jump.
            Start(holder, ReadVisual(holder.ItemView), to, ReadOpacity(holder.ItemView), 1f, onComplete);
        }

        public void AnimateDisappear(CollectionItemViewHolder<T> holder, Action onComplete)
        {
            var current = ReadVisual(holder.ItemView);
            Start(holder, current, current, ReadOpacity(holder.ItemView), 0f, onComplete);
        }

        public void Cancel(CollectionItemViewHolder<T> holder)
        {
            if (_anims.TryGetValue(holder, out var anim))
                Finish(holder, anim);
            // Neutralize animation-owned styles for safe reuse (a completed
            // disappear leaves opacity at 0).
            holder.ItemView.style.opacity = 1f;
        }

        private void Start(
            CollectionItemViewHolder<T> holder, ItemRect from, ItemRect to,
            float fromOpacity, float toOpacity, Action onComplete)
        {
            if (!_anims.TryGetValue(holder, out var anim))
            {
                anim = new Anim();
                _anims.Add(holder, anim);
                anim.Ticker = holder.ItemView.schedule
                    .Execute(timer => Step(holder, timer))
                    .Every(16);
            }
            else
            {
                anim.Ticker.Resume();
            }

            anim.LastTime = -1;
            anim.Elapsed = 0f;
            anim.From = from;
            anim.To = to;
            anim.FromOpacity = fromOpacity;
            anim.ToOpacity = toOpacity;
            anim.OnComplete = onComplete;
            Apply(holder.ItemView, anim, 0f);
        }

        private void Step(CollectionItemViewHolder<T> holder, TimerState timer)
        {
            if (!_anims.TryGetValue(holder, out var anim))
                return;

            if (anim.LastTime < 0)
            {
                anim.LastTime = timer.now;
                return;
            }

            anim.Elapsed += timer.now - anim.LastTime;
            anim.LastTime = timer.now;

            var t = DurationMs <= 0 ? 1f : Mathf.Clamp01(anim.Elapsed / DurationMs);
            Apply(holder.ItemView, anim, 1f - (1f - t) * (1f - t)); // ease-out quad

            if (t >= 1f)
                Finish(holder, anim);
        }

        private void Finish(CollectionItemViewHolder<T> holder, Anim anim)
        {
            anim.Ticker.Pause();
            _anims.Remove(holder);
            Apply(holder.ItemView, anim, 1f);
            anim.OnComplete?.Invoke();
        }

        private static void Apply(VisualElement view, Anim anim, float t)
        {
            view.style.translate = new Translate(
                Mathf.LerpUnclamped(anim.From.X, anim.To.X, t),
                Mathf.LerpUnclamped(anim.From.Y, anim.To.Y, t));
            view.style.width = Mathf.LerpUnclamped(anim.From.Width, anim.To.Width, t);
            view.style.height = Mathf.LerpUnclamped(anim.From.Height, anim.To.Height, t);
            view.style.opacity = Mathf.LerpUnclamped(anim.FromOpacity, anim.ToOpacity, t);
        }

        /// <summary>Current visual box from inline styles (the view always sets them before animating).</summary>
        private static ItemRect ReadVisual(VisualElement view)
        {
            var translate = view.style.translate.value;
            return new ItemRect(
                translate.x.value,
                translate.y.value,
                view.style.width.value.value,
                view.style.height.value.value);
        }

        private static float ReadOpacity(VisualElement view)
        {
            var opacity = view.style.opacity;
            return opacity.keyword == StyleKeyword.Undefined ? opacity.value : 1f;
        }
    }
}
