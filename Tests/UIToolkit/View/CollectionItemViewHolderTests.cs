using System;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView.Tests.UIToolkit
{
    public class CollectionItemViewHolderTests
    {
        [Test]
        public void Constructor_NullItemView_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new NullableHolder(null));
        }

        [Test]
        public void Constructor_PreparesViewForTransformPlacement()
        {
            var view = new VisualElement();
            var holder = new NullableHolder(view);

            Assert.That(holder.ItemView, Is.SameAs(view));
            Assert.That(view.style.position.value, Is.EqualTo(Position.Absolute));
            Assert.That(view.style.left.value.value, Is.EqualTo(0f));
            Assert.That(view.style.top.value.value, Is.EqualTo(0f));
            Assert.That(view.usageHints & UsageHints.DynamicTransform,
                Is.EqualTo(UsageHints.DynamicTransform));
        }

        [Test]
        public void BoundIndex_StartsUnbound()
        {
            var holder = new NullableHolder(new VisualElement());
            Assert.That(holder.BoundIndex, Is.EqualTo(-1));
        }

        [Test]
        public void SubtypeHolder_CastsBaseModelToSubtype()
        {
            var holder = new SubtypeHolder();
            object boxed = "hello";

            holder.Bind(boxed);

            Assert.That(holder.LastBound, Is.EqualTo("hello"));
        }

        private sealed class NullableHolder : CollectionItemViewHolder<string>
        {
            public NullableHolder(VisualElement itemView) : base(itemView)
            {
            }

            public override void Bind(string item)
            {
            }
        }

        private sealed class SubtypeHolder : CollectionItemViewHolder<object, string>
        {
            public string LastBound;

            public SubtypeHolder() : base(new VisualElement())
            {
            }

            protected override void Bind(string item) => LastBound = item;
        }
    }
}
