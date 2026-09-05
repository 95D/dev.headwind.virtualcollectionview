using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView.Tests.UIToolkit
{
    /// <summary>Config stub whose preview is a recognizable sentinel element.</summary>
    internal sealed class StubConfig : CollectionViewConfig
    {
        public override VisualElement CreatePreviewView() => new VisualElement { name = "stub-preview" };
    }

    /// <summary>A second config type, for mismatch scenarios.</summary>
    internal sealed class OtherConfig : CollectionViewConfig
    {
        public override VisualElement CreatePreviewView() => new VisualElement();
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
        public void EditorBridge_InstallsPreviewFactory()
        {
            Assert.That(CollectionHostView.EditorPreviewFactory, Is.Not.Null);
            Assert.That(CollectionHostView.EditorPreviewFactory(_config).name, Is.EqualTo("stub-preview"));
        }

        [Test]
        public void EditorBridge_MissingConfig_YieldsPlaceholder()
        {
            Assert.That(CollectionHostView.EditorPreviewFactory(null), Is.Not.Null);
        }
    }
}
