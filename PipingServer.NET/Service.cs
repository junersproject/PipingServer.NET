using System;
using System.Net;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Security;
using System.Diagnostics;
using System.Collections.Generic;

namespace Piping
{
    [AspNetCompatibilityRequirements(RequirementsMode =AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Service : IService
    {
        string Location;
        string BasePath;
        FileVersionInfo VERSION;
        /// <summary>
        /// デフォルト設定の反映
        /// </summary>
        /// <param name="config"></param>
        public static void Configure(ServiceConfiguration config)
        {
            var HttpGetEnabled = false;
            var HttpsGetEnabled = false;
            var TransferMode = System.ServiceModel.TransferMode.Streamed;
            var SendTimeout = TimeSpan.FromHours(1);
            var OpenTimeout = TimeSpan.FromHours(1);
            var CloseTimeout = TimeSpan.FromHours(1);
            var MaxBufferSize = int.MaxValue;
            var MaxReceivedMessageSize = int.MaxValue;
            foreach (var address in config.BaseAddresses)
                if (address.Scheme == "http")
                    HttpGetEnabled = true;
                else if (address.Scheme == "https")
                    HttpsGetEnabled = true;
            config.Description.Behaviors.Add(new ServiceMetadataBehavior
            {
                HttpGetEnabled = HttpGetEnabled,
                HttpsGetEnabled = HttpsGetEnabled,
            });
            config.AddServiceEndpoint(typeof(IService), new WebHttpBinding
            {
                TransferMode = TransferMode,
                SendTimeout = SendTimeout,
                OpenTimeout = OpenTimeout,
                CloseTimeout = CloseTimeout,
                MaxBufferSize = MaxBufferSize,
                MaxReceivedMessageSize = MaxReceivedMessageSize,
            }, "").EndpointBehaviors.Add(new WebHttpBehavior());
        }
        public Service()
        {
            Location = Assembly.GetExecutingAssembly().Location;
            BasePath = Path.GetDirectoryName(Location);
            VERSION = FileVersionInfo.GetVersionInfo(Location);
            NAME_TO_RESERVED_PATH = new Dictionary<string, Func<OutgoingWebResponseContext, Stream>>
            {
                {"/", DefaultPage },
                {"/version",  VersionPage},
                {"/help", HelpPage },
                {"/favicon.ico", FileGetGenerator("/favicon.ico") },
                {"/robots.txt", FileGetGenerator("/robots.txt") },
            };
        }
        private Encoding Encoding = new UTF8Encoding(false);
        private Dictionary<string, Func<OutgoingWebResponseContext, Stream>> NAME_TO_RESERVED_PATH;

        /// <summary>
        /// エントリーポイント
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        [OperationBehavior(ReleaseInstanceMode = ReleaseInstanceMode.None)]
        public Stream Default(Stream inputStream)
        {
            var Current = WebOperationContext.Current;
            var Request = Current.IncomingRequest;
            var Response = Current.OutgoingResponse;
            var Method = Request.Method;
            var RequestUri = Request.UriTemplateMatch.RequestUri;
            var reqPath = RequestUri.LocalPath.TrimEnd('/');
            if (reqPath == string.Empty)
                reqPath = "/";
            switch (Method)
            {
                case "POST":
                case "PUT":
                    if (NAME_TO_RESERVED_PATH.TryGetValue(reqPath, out _))
                    {
                        Response.StatusCode = HttpStatusCode.BadRequest;
                        var Encoding = Response.BindingWriteEncoding;
                        var Bytes = Encoding.GetBytes($"[ERROR] Cannot send to a reserved path '{reqPath}'. (e.g. '/mypath123')\n");
                        Response.ContentLength = Bytes.Length;
                        Response.ContentType = $"text/plain;charset={Encoding.WebName}";
                        return new MemoryStream(Bytes);
                    }
                    throw new NotImplementedException();
                case "GET":
                    if (NAME_TO_RESERVED_PATH.TryGetValue(reqPath, out var Generator))
                        return Generator(Response);
                    throw new NotImplementedException();
                case "OPTIONS":
                    return Options(Response);
                default:
                    return NotImplemented(Response);
            }
            throw new NotImplementedException();
        }
        protected Stream DefaultPage(OutgoingWebResponseContext Response)
        {
            var Encoding = Response.BindingWriteEncoding;
            var Bytes = Encoding.GetBytes(Properties.Resource.DefaultPage);
            Response.ContentLength = Bytes.Length;
            Response.ContentType = $"text/html;charset={Encoding.WebName}";
            return new MemoryStream(Bytes);
        }
        protected Stream HelpPage(OutgoingWebResponseContext Response)
        {
            var url = HttpContext.Current.Server.MapPath(".");
            var Encoding = Response.BindingWriteEncoding;
            var Bytes = Encoding.GetBytes(@$"Help for piping - server { VERSION}
(Repository: https://github.com/nwtgck/piping-server)

======= Get  =======
curl ${url}/mypath

======= Send =======
# Send a file
curl -T myfile ${url}/mypath

# Send a text
echo 'hello!' | curl -T - ${url}/mypath

# Send a directory (zip)
zip -q -r - ./mydir | curl -T - ${url}/mypath

# Send a directory (tar.gz)
tar zfcp - ./mydir | curl -T - ${url}/mypath

# Encryption
## Send
cat myfile | openssl aes-256-cbc | curl -T - ${url}/mypath
## Get
curl ${url}/mypath | openssl aes-256-cbc -d");
            Response.ContentLength = Bytes.Length;
            Response.ContentType = $"text/plain;charset={Encoding.WebName}";
            return new MemoryStream(Bytes);
        }
        protected Stream VersionPage(OutgoingWebResponseContext Response)
        {
            var Encoding = Response.BindingWriteEncoding;
            var Bytes = Encoding.GetBytes($"{VERSION}\n");
            Response.ContentLength = Bytes.Length;
            Response.ContentType = $"text/plain;charset={Encoding.WebName}";
            return new MemoryStream(Bytes);
        }
        protected Stream Options(OutgoingWebResponseContext Response)
        {
            Response.StatusCode = HttpStatusCode.OK;
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET, HEAD, POST, PUT, OPTIONS");
            Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Content-Disposition");
            Response.Headers.Add("Access-Control-Max-Age", "86400");
            Response.ContentLength = 0;
            return new MemoryStream(new byte[0]);
        }
        protected Stream NotImplemented(OutgoingWebResponseContext Response)
        {
            Response.StatusCode = HttpStatusCode.NotImplemented;
            Response.ContentLength = 0;
            return new MemoryStream(new byte[0]);
        }
        protected bool ExistsFile(string FileName)
        {
            var FilePath = Path.Combine(BasePath, FileName.TrimStart('/'));
            return File.Exists(FilePath);
        }
        protected Func<OutgoingWebResponseContext, Stream> FileGetGenerator(string FileName)
        {
            var FilePath = Path.Combine(BasePath, FileName.TrimStart('/'));
            return (Response) =>
            {
                try
                {
                    var Bytes = File.ReadAllBytes(FilePath);
                    Response.StatusCode = HttpStatusCode.OK;
                    Response.ContentType = MimeMapping.GetMimeMapping(FilePath);
                    Response.ContentLength = Bytes.Length;
                    return new MemoryStream(Bytes);
                }
                catch (FileNotFoundException)
                {
                    Response.StatusCode = HttpStatusCode.NotFound;
                }
                catch (SecurityException)
                {
                    Response.StatusCode = HttpStatusCode.NotFound;
                }
                catch (Exception)
                {
                    Response.StatusCode = HttpStatusCode.InternalServerError;
                }
                Response.ContentLength = 0;
                return new MemoryStream(new byte[0]);
            };
        }
    }
}
