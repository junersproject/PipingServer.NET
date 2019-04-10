using Microsoft.VisualStudio.TestTools.UnitTesting;
using Piping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piping.Tests
{
    [TestClass()]
    public class ServiceTests
    {
        [TestMethod,TestCategory("ShortTime")]
        public void InstanceTest()
        {
            using (var Host = new SelfHost {
                HttpGetEnabled = true,
            })
            {
                Host.Open(new Uri("http://localhost:8080/PipingServiceInstanceTest"));
            }
        }
        [TestMethod,TestCategory("ShortTime")]
        public void UploadTest()
        {
            Assert.Fail();
        }
    }
}