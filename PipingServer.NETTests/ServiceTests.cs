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
        [TestMethod]
        public void InstanceTest()
        {
            using (var Host = new SelfHost())
            {
                Host.Open(new Uri("http://localhost:8080/PipingServiceInstanceTest"));
            }
        }
        [TestMethod()]
        public void UploadTest()
        {
            
        }
    }
}