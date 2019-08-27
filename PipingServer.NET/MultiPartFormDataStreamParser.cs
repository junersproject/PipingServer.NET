using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileUploadSample;
using Microsoft.AspNetCore.Http;

namespace Piping
{
    public class AsyncMutiPartFormDataEnumerable : IAsyncEnumerable<(IHeaderDictionary Headers, Stream Stream)>
    {
        protected IHeaderDictionary Headers;
        protected Stream Stream = Stream.Null;
        protected int MultipartBoundaryLengthLimit;


        public AsyncMutiPartFormDataEnumerable(IHeaderDictionary? Headers = null, Stream? Stream = null, int MultipartBoundaryLengthLimit = MultipartRequestHelper.MultipartBoundaryLengthLimit)
            => (this.Headers, this.Stream, this.MultipartBoundaryLengthLimit) = (Headers ?? new HeaderDictionary(), Stream ?? Stream.Null, MultipartBoundaryLengthLimit);
        public async IAsyncEnumerator<(IHeaderDictionary Headers, Stream Stream)> GetAsyncEnumerator(CancellationToken Token = default)
        {
            if (string.IsNullOrEmpty(Headers["ContentType"]))
                yield break;
            if (Stream == Stream.Null || !Stream.CanRead)
                yield break;
            await foreach (var section in MultipartRequestHelper.GetMutipartSectionAsync(Headers, Stream, MultipartBoundaryLengthLimit, Token))
            {
                yield return (new HeaderDictionary(section.Headers), section.Body);
            }
        }
    }
}
