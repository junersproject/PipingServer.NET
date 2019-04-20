using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Piping.Tests
{
    [TestClass]
    public class ServiceTest
    {
        static IEnumerable<object[]> GetBaseUriTestData
        {
            get
            {
                yield return new object[]
                {
                    new[]
                    {
                        new Uri("http://localhost/test1"),
                        new Uri("http://localhost/test2"),
                        new Uri("http://localhost/test3"),
                    },
                    new Uri("http://localhost/test2/action"),
                    new Uri("http://localhost/test2"),
                };
                yield return new object[]
                {
                    new[]
                    {
                        new Uri("http://localhost/test1/"),
                        new Uri("http://localhost/test"),
                        new Uri("http://localhost/test3/"),
                    },
                    new Uri("http://localhost/test3/action"),
                    new Uri("http://localhost/test3/"),
                };
            }
        }
        [TestMethod, DynamicData(nameof(GetBaseUriTestData)), TestCategory("ShortTime")]
        public void GetBaseUriTest(IEnumerable<Uri> BaseAddresses, Uri RequestUri, Uri ExpectBaseAddress)
        {
            var ActualBaseAddress = Service.GetBaseUri(BaseAddresses, RequestUri);
            Assert.AreEqual(ExpectBaseAddress, ActualBaseAddress);
        }
        static IEnumerable<object[]> GetRelativeUriTestData
        {
            get
            {
                yield return new object[]
                {
                    new Uri("http://localhost/test1"),
                    new Uri("http://localhost/test1/action"),
                    "/action",
                };
                yield return new object[]
                {
                    new Uri("http://localhost/test2/"),
                    new Uri("http://localhost/test2/action"),
                    "/action",
                };
                yield return new object[]
                {
                    new Uri("http://localhost/test2"),
                    new Uri("http://localhost/test2/"),
                    "/",
                };
                yield return new object[]
                {
                    new Uri("http://localhost/test2"),
                    new Uri("http://localhost/test2"),
                    "/",
                };
            }
        }
        [TestMethod, DynamicData(nameof(GetRelativeUriTestData)), TestCategory("ShortTime")]
        public void GetRelativeUriTest(Uri BaseAddress, Uri RequestUri, string ExpectRelativeUri)
        {
            var ActualRelativeUri = Service.GetRelativeUri(BaseAddress, RequestUri);
            Assert.AreEqual(ExpectRelativeUri, ActualRelativeUri);
        }
    }
}