using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestFixture]
    public class CommonTests
    {
        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public void StartTest()
        {
            if (!HwHash.HwHash.Launch())
            {
                Assert.Fail();
            }

            List<HwHash.HwInfoHash> res = HwHash.HwHash.GetRelevantList();
            List<HwHash.HwInfoHash> gol = HwHash.HwHash.GetOrderedList();

            var u = gol.Where(x => x.ReadingType == "Temperature");
        }

        [TearDown]
        public void TearDown()
        {

        }
    }
}
