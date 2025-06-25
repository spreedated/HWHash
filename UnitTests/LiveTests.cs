using HwHash.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HwHash;

namespace UnitTests
{
    [TestFixture]
    //[Ignore("Live tests, run it manually")]
    public class LiveTests
    {
        HwHash.HwHash hwHash;

        [SetUp]
        public void SetUp()
        {
            this.hwHash = new();
        }

        [Test]
        [Description("Have HwInfo running with sensors on")]
        [TestCase(true)]
        [TestCase(false)]
        public void StartTest(bool highPrecision)
        {
            this.hwHash = new(new() { HighPrecision = highPrecision });

            Assert.That(this.hwHash.IsRunning, Is.False);

            if (!this.hwHash.Start())
            {
                Assert.Fail();
            }

            Assert.That(this.hwHash.IsRunning, Is.True);

            this.hwHash.Stop();

            Assert.That(this.hwHash.IsRunning, Is.False);
        }

        [TearDown]
        public void TearDown()
        {

        }
    }
}
