using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PipingServer.Core.Pipes.Tests
{
    [TestClass]
    public class ReadOnlyPipeExtensionsTests
    {
        static IEnumerable<object[]> ToOptionMethosTestData
        {
            get
            {
                yield return new object[]
                {
                    null!,
                    new []
                    {
                        HttpMethods.Get,
                        HttpMethods.Put,
                        HttpMethods.Post,
                        HttpMethods.Options,
                    }
                };
                yield return new object[]
                {
                    new MockReadOnlyPipe
                    {
                    },
                    new []
                    {
                        HttpMethods.Options,
                        HttpMethods.Get,
                        HttpMethods.Post,
                        HttpMethods.Put,
                    }
                };
                yield return new object[]
                {
                    new MockReadOnlyPipe
                    {
                        Status = PipeStatus.Wait,
                        Required = PipeType.Receiver,
                    },
                    new []
                    {
                        HttpMethods.Options,
                        HttpMethods.Get,
                        HttpMethods.Head,
                    }
                };
                yield return new object[]
                {
                    new MockReadOnlyPipe
                    {
                        Status = PipeStatus.Wait,
                        Required = PipeType.Sender,
                    },
                    new []
                    {
                        HttpMethods.Options,
                        HttpMethods.Post,
                        HttpMethods.Put,
                    }
                };
                yield return new object[]
                {
                    new MockReadOnlyPipe
                    {
                        Status = PipeStatus.Ready,
                    },
                    new []
                    {
                        HttpMethods.Options,
                        HttpMethods.Head,
                    }
                };
                yield return new object[]
                {
                    new MockReadOnlyPipe
                    {
                        Status = PipeStatus.ResponseStart,
                    },
                    new []
                    {
                        HttpMethods.Options,
                        HttpMethods.Head,
                    }
                };
                yield return new object[]
                {
                    new MockReadOnlyPipe
                    {
                        Status = PipeStatus.ResponseEnd,
                    },
                    new []
                    {
                        HttpMethods.Head,
                        HttpMethods.Options,
                    }
                };
                yield return new object[]
                {
                    new MockReadOnlyPipe
                    {
                        Status = PipeStatus.Canceled,
                    },
                    new []
                    {
                        HttpMethods.Options,
                        HttpMethods.Head,
                    }
                };
                yield return new object[]
                {
                    new MockReadOnlyPipe
                    {
                        Status = PipeStatus.Dispose,
                    },
                    new []
                    {
                        HttpMethods.Options,
                    }
                };
            }
        }
        [TestMethod, DynamicData(nameof(ToOptionMethosTestData))]
        public void ToOptionMethodsTest(IReadOnlyPipe? Pipe, IEnumerable<string> ExpectResult)
        {
            var ExpectArray = ExpectResult.OrderBy(v => v).ToArray();
            var ResultArray = Pipe.ToOptionMethods().OrderBy(v => v).ToArray();
            CollectionAssert.AreEqual(ExpectArray, ResultArray, $"[{string.Join(", ", ExpectArray)}] と [{string.Join(", ", ResultArray)}] で違います。");
        }
    }
}
