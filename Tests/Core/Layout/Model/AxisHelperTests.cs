using Headwind.VirtualCollectionView.Core;
using NUnit.Framework;

namespace Headwind.VirtualCollectionView.Tests
{
    public class AxisHelperTests
    {
        [Test]
        public void Vertical_MainIsY_CrossIsX()
        {
            var axis = new AxisHelper(Orientation.Vertical);
            var v = new LayoutVector2(3f, 7f);

            Assert.That(axis.Main(v), Is.EqualTo(7f));
            Assert.That(axis.Cross(v), Is.EqualTo(3f));
        }

        [Test]
        public void Horizontal_MainIsX_CrossIsY()
        {
            var axis = new AxisHelper(Orientation.Horizontal);
            var v = new LayoutVector2(3f, 7f);

            Assert.That(axis.Main(v), Is.EqualTo(3f));
            Assert.That(axis.Cross(v), Is.EqualTo(7f));
        }

        [Test]
        public void MakeVec_RoundTripsWithMainAndCross()
        {
            foreach (var orientation in new[] { Orientation.Vertical, Orientation.Horizontal })
            {
                var axis = new AxisHelper(orientation);
                var v = axis.MakeVec(11f, 22f);

                Assert.That(axis.Main(v), Is.EqualTo(11f));
                Assert.That(axis.Cross(v), Is.EqualTo(22f));
            }
        }

        [Test]
        public void MakeRect_Vertical_MapsMainToY()
        {
            var axis = new AxisHelper(Orientation.Vertical);
            var rect = axis.MakeRect(mainPos: 10f, crossPos: 20f, mainSize: 30f, crossSize: 40f);

            Assert.That(rect, Is.EqualTo(new ItemRect(20f, 10f, 40f, 30f)));
            Assert.That(axis.MainMin(rect), Is.EqualTo(10f));
            Assert.That(axis.MainMax(rect), Is.EqualTo(40f));
        }

        [Test]
        public void MakeRect_Horizontal_MapsMainToX()
        {
            var axis = new AxisHelper(Orientation.Horizontal);
            var rect = axis.MakeRect(mainPos: 10f, crossPos: 20f, mainSize: 30f, crossSize: 40f);

            Assert.That(rect, Is.EqualTo(new ItemRect(10f, 20f, 30f, 40f)));
            Assert.That(axis.MainMin(rect), Is.EqualTo(10f));
            Assert.That(axis.MainMax(rect), Is.EqualTo(40f));
        }
    }
}
