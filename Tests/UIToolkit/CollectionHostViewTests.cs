using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Headwind.VirtualCollectionView.Editor;

namespace Headwind.VirtualCollectionView.Tests.UIToolkit
{
    /// <summary>Config stub with a single matching preview builder.</summary>
    internal sealed class StubConfig : CollectionViewConfig
    {
    }

    internal sealed class StubConfigPreviewBuilder : CollectionViewPreviewBuilder<StubConfig>
    {
        protected override VisualElement Build(StubConfig config) =>
            new VisualElement { name = "stub-preview" };
    }

    /// <summary>A second config type, for mismatch and ambiguity scenarios.</summary>
    internal sealed class OtherConfig : CollectionViewConfig
    {
    }

    internal sealed class OtherConfigPreviewBuilderA : CollectionViewPreviewBuilder<OtherConfig>
    {
        protected override VisualElement Build(OtherConfig config) => new VisualElement();
    }

    internal sealed class OtherConfigPreviewBuilderB : CollectionViewPreviewBuilder<OtherConfig>
    {
        protected override VisualElement Build(OtherConfig config) => new VisualElement();
    }

    /// <summary>Config with no preview builder at all.</summary>
    internal sealed class OrphanConfig : CollectionViewConfig
    {
    }

    public class CollectionHostViewTests
    {
        private StubConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<StubConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Host_FillsSlot()
        {
            var host = new CollectionHostView();
            var view = new VisualElement();

            host.Host(view);

            Assert.That(host.HostedView, Is.SameAs(view));
            Assert.That(view.parent, Is.SameAs(host));
            Assert.That(view.style.position.value, Is.EqualTo(Position.Absolute));
            Assert.That(view.style.left.value.value, Is.EqualTo(0f));
            Assert.That(view.style.top.value.value, Is.EqualTo(0f));
            Assert.That(view.style.right.value.value, Is.EqualTo(0f));
            Assert.That(view.style.bottom.value.value, Is.EqualTo(0f));
        }

        [Test]
        public void Host_ReplacesPreviousView()
        {
            var host = new CollectionHostView();
            var first = new VisualElement();
            var second = new VisualElement();

            host.Host(first);
            host.Host(second);

            Assert.That(first.parent, Is.Null);
            Assert.That(host.HostedView, Is.SameAs(second));
        }

        [Test]
        public void Unhost_ReturnsViewAndClears()
        {
            var host = new CollectionHostView();
            var view = new VisualElement();
            host.Host(view);

            var unhosted = host.Unhost();

            Assert.That(unhosted, Is.SameAs(view));
            Assert.That(view.parent, Is.Null);
            Assert.That(host.HostedView, Is.Null);
            Assert.That(host.Unhost(), Is.Null);
        }

        [Test]
        public void GetConfig_ReturnsTypedConfig()
        {
            var host = new CollectionHostView { Config = _config };

            Assert.That(host.GetConfig<StubConfig>(), Is.SameAs(_config));
            Assert.That(host.GetConfig<CollectionViewConfig>(), Is.SameAs(_config));
        }

        [Test]
        public void GetConfig_MissingConfig_LogsErrorAndReturnsNull()
        {
            var host = new CollectionHostView();

            LogAssert.Expect(LogType.Error, new Regex("no CollectionViewConfig assigned.*StubConfig"));
            Assert.That(host.GetConfig<StubConfig>(), Is.Null);
        }

        [Test]
        public void GetConfig_MismatchedConfig_LogsErrorAndReturnsNull()
        {
            var host = new CollectionHostView { Config = _config };

            LogAssert.Expect(LogType.Error, new Regex("has StubConfig assigned.*expected OtherConfig"));
            Assert.That(host.GetConfig<OtherConfig>(), Is.Null);
        }

        [Test]
        public void PreviewFactory_ResolvesBuilderByConfigType()
        {
            Assert.That(CollectionHostView.EditorPreviewFactory, Is.Not.Null);
            Assert.That(CollectionHostView.EditorPreviewFactory(_config).name, Is.EqualTo("stub-preview"));
        }

        [Test]
        public void PreviewFactory_MissingConfig_YieldsPlaceholder()
        {
            Assert.That(CollectionHostView.EditorPreviewFactory(null), Is.Not.Null);
        }

        [Test]
        public void PreviewFactory_NoBuilder_YieldsPlaceholder()
        {
            var orphan = ScriptableObject.CreateInstance<OrphanConfig>();
            try
            {
                var preview = CollectionHostView.EditorPreviewFactory(orphan);

                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.name, Is.Not.EqualTo("stub-preview"));
            }
            finally
            {
                Object.DestroyImmediate(orphan);
            }
        }

        [Test]
        public void PreviewFactory_AmbiguousBuilders_LogsErrorAndYieldsPlaceholder()
        {
            var other = ScriptableObject.CreateInstance<OtherConfig>();
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("Ambiguous preview builders for OtherConfig"));
                Assert.That(CollectionHostView.EditorPreviewFactory(other), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }
    }
}
