using HwHash.Models;
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
            HwHash.HwHash hwHash = new();

            if (!hwHash.Start())
            {
                Assert.Fail();
            }

            List<HwInfoHash> res = [.. hwHash.GetRelevantList()];

            List<HwInfoHash> gol = hwHash.GetOrderedList();

            var ss = gol.GroupBy(x => x.ReadingType);
            var u = gol.Where(x => x.ReadingType == "Temperature");
            var uu = gol.Where(x => x.ReadingType == "Usage");
        }

        [TearDown]
        public void TearDown()
        {

        }
    }
}
