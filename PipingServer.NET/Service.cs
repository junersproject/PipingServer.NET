using System;
using System.IO;

namespace Piping
{
    public class Service : IService
    {
        public string PostUpload(string per, Stream inputStream)
        {
            throw new NotImplementedException();
        }
        public string PutUpload(string per, Stream inputStream)
        {
            throw new NotImplementedException();
        }
        public Stream Download(string per)
        {
            throw new NotImplementedException();
        }
    }
}
