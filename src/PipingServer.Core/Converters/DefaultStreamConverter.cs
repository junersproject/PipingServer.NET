using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PipingServer.Core.Converters
{
    /// <summary>
    /// <see cref="Stream"/> と <see cref="IHeaderDictionary"/> を其の儘使用するコンバータ
    /// </summary>
    public class DefaultStreamConverter : IStreamConverter
    {
        public static Task<(IHeaderDictionary Headers, Stream Stream)> GetStreamAsync(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
        {
            if (Headers == null)
                throw new ArgumentNullException(nameof(Headers));
            if (Body == null)
                throw new ArgumentNullException(nameof(Headers));
            if (Token.IsCancellationRequested)
                return Task.FromCanceled<(IHeaderDictionary Headers, Stream Stream)>(Token);
            return Task.FromResult((Headers, Body));
        }
        Task<(IHeaderDictionary Headers, Stream Stream)> IStreamConverter.GetStreamAsync(IHeaderDictionary Headers, Stream Body, CancellationToken Token)
            => GetStreamAsync(Headers, Body, Token);


        public bool IsUse(IHeaderDictionary Headers) => true;
    }
}
