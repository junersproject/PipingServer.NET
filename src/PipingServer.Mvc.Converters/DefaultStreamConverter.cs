using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace PipingServer.Mvc.Converters
{
    /// <summary>
    /// <see cref="Stream"/> と <see cref="IHeaderDictionary"/> を其の儘使用するコンバータ
    /// </summary>
    public class DefaultStreamConverter : IStreamConverter
    {
        /// <summary>
        /// header and body to through.
        /// </summary>
        /// <typeparam name="IHeaderDictionary"></typeparam>
        /// <param name="Headers"></param>
        /// <param name="Body"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">header or body</exception>
        public static Task<(IHeaderDictionary Headers, Stream Stream)> GetStreamAsync<IHeaderDictionary>(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
            where IHeaderDictionary : IDictionary<string, StringValues>
        {
            if (Headers == null)
                throw new ArgumentNullException(nameof(Headers));
            if (Body == null || Body == Stream.Null)
                throw new ArgumentNullException(nameof(Headers));
            if (Token.IsCancellationRequested)
                return Task.FromCanceled<(IHeaderDictionary Headers, Stream Stream)>(Token);
            return Task.FromResult((Headers, Body));
        }
        Task<(IHeaderDictionary Headers, Stream Stream)> IStreamConverter.GetStreamAsync<IHeaderDictionary>(IHeaderDictionary Headers, Stream Body, CancellationToken Token)
            => GetStreamAsync(Headers, Body, Token);

        /// <summary>
        /// default stream converter is <see cref="IsUse{IHeaderDictionary}(IDictionary{string, StringValues})"/> method not implemnented.
        /// </summary>
        /// <typeparam name="IHeaderDictionary"></typeparam>
        /// <param name="Headers"></param>
        /// <returns></returns>
        [Obsolete]
        public bool IsUse<IHeaderDictionary>(IDictionary<string, StringValues> Headers)
            where IHeaderDictionary : IDictionary<string, StringValues>
            => throw new NotImplementedException();

    }
}
