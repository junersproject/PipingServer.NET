using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Piping
{
    public class AsyncMutiPartFormDataEnumerable : IAsyncEnumerable<(IHeaderDictionary Headers, Stream Stream)>
    {
        protected Stream Stream = Stream.Null;
        protected Encoding Encoding;
        protected int BufferSize;
        protected string Boundary = string.Empty;
        
        public AsyncMutiPartFormDataEnumerable(Stream? Stream = null, Encoding? Encoding = null, int BufferSize = 1024, string? Boundary = null)
            => (this.Stream, this.Encoding, this.BufferSize, this.Boundary) = (Stream ?? Stream.Null, Encoding ?? Encoding.UTF8, BufferSize, Boundary ?? string.Empty);
        public async IAsyncEnumerator<(IHeaderDictionary Headers, Stream Stream)> GetAsyncEnumerator(CancellationToken Token = default)
        {
            if (Stream == Stream.Null)
                yield break;
            if (!Stream.CanRead)
                yield break;
            var BufferMemory = new byte[BufferSize].AsMemory();
            Memory<byte> Boundary;
            var count = await Stream.ReadAsync(BufferMemory.Slice(0, 2), Token);
            var StartOrEnd = Encoding.GetBytes("--").AsMemory();
            if (count != 2 || !BufferMemory.Slice(0, 2).Equals(StartOrEnd))
                yield break;
            if (this.Boundary == string.Empty)
            {
                throw new NotImplementedException();
            }
            else
            {
                // TODO -- が追加された形か確認すること
                Boundary = Encoding.GetBytes(this.Boundary).AsMemory();
                count = await this.Stream.ReadAsync(BufferMemory, Token);
                var Memory = BufferMemory.Slice(count);
                // TODO Boundary の比較方法考える

            }
            
            // TODO Content-Type 等のヘッダーの取得
            // TODO Boundary が出るまでbody部の取得
            // TODO Boundary の末尾指定が来たら終了
            //var Stream = new MemoryStream();
            //var Header = new HeaderDictionary();
            //await Task.Delay(TimeSpan.FromMilliseconds(1));
            //yield return (Header, Stream);
            throw new NotImplementedException();
        }
        Task<(Memory<byte> Memory, int ReadCount)> ReadLineAsync()
        {
            throw new NotImplementedException();
        }
    }
}
