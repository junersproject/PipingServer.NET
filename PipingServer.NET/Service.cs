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
using System.Linq;
using System.Threading.Tasks;
using HttpMultipartParser;

namespace Piping
{
    [AspNetCompatibilityRequirements(RequirementsMode =AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class Service : IService
    {
        readonly bool EnableLog = false;
        readonly string Location;
        readonly string BasePath;
        readonly FileVersionInfo VERSION;
        readonly Encoding Encoding = new UTF8Encoding(false);
        /// <summary>
        /// デフォルト設定の反映
        /// </summary>
        /// <param name="config"></param>
        public static void Configure(ServiceConfiguration config)
        {
            var TransferMode = System.ServiceModel.TransferMode.Streamed;
            var SendTimeout = TimeSpan.FromHours(1);
            var OpenTimeout = TimeSpan.FromHours(1);
            var CloseTimeout = TimeSpan.FromHours(1);
            var MaxBufferSize = int.MaxValue;
            var MaxReceivedMessageSize = int.MaxValue;
            config.AddServiceEndpoint(typeof(IService), new WebHttpBinding
            {
                TransferMode = TransferMode,
                SendTimeout = SendTimeout,
                OpenTimeout = OpenTimeout,
                CloseTimeout = CloseTimeout,
                MaxBufferSize = MaxBufferSize,
                MaxReceivedMessageSize = MaxReceivedMessageSize,
            }, "").EndpointBehaviors.Add(new WebHttpBehavior());
            var sdb = config.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (sdb != null)
                sdb.HttpHelpPageEnabled = false;
        }
        readonly Dictionary<string, SenderResponseWaiters> pathToUnestablishedPipe = new Dictionary<string, SenderResponseWaiters>();
        public Service()
        {
            Location = Assembly.GetExecutingAssembly().Location;
            BasePath = Path.GetDirectoryName(Location);
            VERSION = FileVersionInfo.GetVersionInfo(Location);
            NAME_TO_RESERVED_PATH = new Dictionary<string, Func<Task<Stream>>>
            {
                {DefaultPath.Root, GetDefaultPageAsync },
                {DefaultPath.Version, GetVersionAsync},
                {DefaultPath.Help, GetHelpAsync},
                {DefaultPath.Favicon, GetFaviconAsync},
                {DefaultPath.Robots, GetRobotsAsync},
            };
        }
        internal Dictionary<string, Func<Task<Stream>>> NAME_TO_RESERVED_PATH;
        
        internal static Uri GetBaseUri(IEnumerable<Uri> BaseAddresses, Uri RequestUri)
        {
            var RequestUriString = RequestUri.ToString();
            return BaseAddresses.FirstOrDefault(IsFind);
            bool IsFind(Uri a)
            {
                var _a = a.ToString();
                if (_a.Last() != '/')
                    _a += '/';
                return RequestUriString.IndexOf(_a) == 0;
            }
        }
        internal Uri GetBaseUri()
            => GetBaseUri(OperationContext.Current.Host.BaseAddresses, WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri);
        internal static string GetRelativeUri(IEnumerable<Uri> BaseAddresses, Uri RequestUri)
            => GetRelativeUri(GetBaseUri(BaseAddresses, RequestUri), RequestUri);
        internal static string GetRelativeUri(Uri BaseAddress, Uri RequestUri)
        {
            var b = BaseAddress.ToString();
            if (b.Last() != '/')
                b += '/';
            var r = RequestUri.ToString();
            var result = r.Substring(b.Length -1);
            if (!result.Any())
                result = "/";
            else if (result.FirstOrDefault() != '/')
                result = '/' + result;
            return result;
        }
        internal string GetRelativeUri()
            => GetRelativeUri(OperationContext.Current.Host.BaseAddresses, WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri);
        /// <summary>
        /// エントリーポイント
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        [OperationBehavior(ReleaseInstanceMode = ReleaseInstanceMode.None)]
        public Task<Stream> DefaultAsync(Stream inputStream)
        {
            var Current = WebOperationContext.Current;
            var Request = Current.IncomingRequest;
            var Response = Current.OutgoingResponse;
            var Method = Request.Method;
            switch (Method)
            {
                case "POST":
                case "PUT":
                    return UploadAsync(inputStream, GetRelativeUri(), Request, Response);
                case "GET":
                    return DownloadAsync(GetRelativeUri(), Response);
                case "OPTIONS":
                    return Task.FromResult(OptionsResponseGenerator(Response));
                default:
                    return Task.FromResult(NotImplemented(Response));
            }
        }
        protected Stream BadRequest(OutgoingWebResponseContext Response, string AndMessage = null)
        {
            Response.StatusCode = HttpStatusCode.BadRequest;
            Response.StatusDescription = AndMessage;
            Response.ContentLength = 0;
            return new MemoryStream(new byte[0]);
        }

        public async Task<Stream> UploadAsync(Stream InputStream, string RelativeUri, IncomingWebRequestContext Request = null, OutgoingWebResponseContext Response = null)
        {
            var output = new MemoryStream();
            Request ??= WebOperationContext.Current.IncomingRequest;
            Response ??= WebOperationContext.Current.OutgoingResponse;
            if (NAME_TO_RESERVED_PATH.TryGetValue(RelativeUri, out _))
                return BadRequest(Response, $"[ERROR] Cannot send to a reserved path '{RelativeUri}'. (e.g. '/mypath123')\n");
            var Key = new RequestKey(RelativeUri);
            // If the number of receivers is invalid
            if (Key.Receivers <= 0)
                return BadRequest(Response, $"[ERROR] n should > 0, but n = ${Key.Receivers}.\n");
            if (EnableLog)
                Console.WriteLine(pathToUnestablishedPipe.Select(v => $"{v.Key}:{v.Value}"));

            // If the path connection is connecting
            // Get unestablished pipe
            if (!pathToUnestablishedPipe.TryGetValue(Key.LocalPath, out var waiter))
            {
                waiter = new SenderResponseWaiters(Key.Receivers);
                pathToUnestablishedPipe[Key.LocalPath] = waiter;
            }
            if (waiter.IsEstablished)
                return BadRequest(Response, $"[ERROR] Connection on '{RelativeUri}' has been established already.\n");
            try
            {
                return await waiter.AddSenderAsync(Key, new ReqRes
                {
                    Request = Request,
                    RequestStream = InputStream,
                    Response = Response,
                    ResponseStream = output,
                }, Encoding, 1024);
            }catch(InvalidOperationException e)
            {
                return BadRequest(Response, e.Message);
            }
        }
        Task<Stream> IService.PostUploadAsync(Stream InputStream) => UploadAsync(InputStream, GetRelativeUri(), WebOperationContext.Current.IncomingRequest, WebOperationContext.Current.OutgoingResponse);
        Task<Stream> IService.PutUploadAsync(Stream InputStream) => UploadAsync(InputStream, GetRelativeUri(), WebOperationContext.Current.IncomingRequest, WebOperationContext.Current.OutgoingResponse);
        public async Task<Stream> DownloadAsync(string RelativeUri, OutgoingWebResponseContext Response = null)
        {
            if (NAME_TO_RESERVED_PATH.TryGetValue(RelativeUri, out var Generator))
                return await Generator();
            Response ??= WebOperationContext.Current.OutgoingResponse;
            var ResponseStream = new MemoryStream();
            var Key = new RequestKey(RelativeUri);
            if (Key.Receivers <= 0)
                return BadRequest(Response, $"[ERROR] n should > 0, but n = {Key.Receivers}.\n");
            if (!pathToUnestablishedPipe.TryGetValue(Key.LocalPath, out var waiter))
            {
                waiter = new SenderResponseWaiters(Key.Receivers);
                pathToUnestablishedPipe[Key.LocalPath] = waiter;
            }
            if (waiter.IsEstablished)
                return BadRequest(Response, $"[ERROR] Connection on '{RelativeUri}' has been established already.\n");
            var rs = new ReqRes
            {
                Response = Response,
            };
            try
            {
                var stream = new CloseRegisterStream(await waiter.AddReceiverAsync(rs));
                stream.Closed += (obj, args) =>
                {
                    waiter.UnRegisterReceiver(rs);
                    if (waiter.ReceiversIsEmpty)
                    {
                        pathToUnestablishedPipe.Remove(Key.LocalPath);
                        waiter.Dispose();
                    }
                };
                return stream;
            }
            catch(Exception e)
            {
                waiter.UnRegisterReceiver(rs);
                return BadRequest(Response, e.Message);
            }
        }
        Task<Stream> IService.GetDownloadAsync() => DownloadAsync(GetRelativeUri(), WebOperationContext.Current.OutgoingResponse);
        public Task<Stream> GetDefaultPageAsync()
            => Task.FromResult(DefaultPageResponseGenerator(WebOperationContext.Current.OutgoingResponse));
        public Task<Stream> GetVersionAsync()
            => Task.FromResult(VersionResponseGenerator(WebOperationContext.Current.OutgoingResponse));
        public Task<Stream> GetHelpAsync()
            => Task.FromResult(HelpPageResponseGenerator(WebOperationContext.Current.OutgoingResponse));
        public Task<Stream> GetFaviconAsync()
            => Task.FromResult(FileGetGenerator(DefaultPath.Favicon, WebOperationContext.Current.OutgoingResponse));
        public Task<Stream> GetRobotsAsync()
            => Task.FromResult(FileGetGenerator(DefaultPath.Robots, WebOperationContext.Current.OutgoingResponse));
        public Task<Stream> GetOptionsAsync()
            => Task.FromResult(OptionsResponseGenerator(WebOperationContext.Current.OutgoingResponse));
        protected Stream DefaultPageResponseGenerator(OutgoingWebResponseContext Response)
        {
            var Encoding = Response.BindingWriteEncoding;
            var Bytes = Encoding.GetBytes(GetDefaultPage());
            Response.ContentLength = Bytes.Length;
            Response.ContentType = $"text/html;charset={Encoding.WebName}";
            return new MemoryStream(Bytes);
        }
        internal static string GetDefaultPage() => Properties.Resource.DefaultPage;
        protected Stream HelpPageResponseGenerator(OutgoingWebResponseContext Response)
        {
            var url = GetBaseUri();
            var Encoding = Response.BindingWriteEncoding;
            var Bytes = Encoding.GetBytes(GetHelpPageText(url, VERSION));
            Response.ContentLength = Bytes.Length;
            Response.ContentType = $"text/plain;charset={Encoding.WebName}";
            return new MemoryStream(Bytes);
        }
        internal static string GetHelpPageText(Uri url, FileVersionInfo version)
        {
            return $@"Help for piping - server {version}
(Repository: https://github.com/nwtgck/piping-server)

======= Get  =======
curl {url}/mypath

======= Send =======
# Send a file
curl -T myfile {url}/mypath

# Send a text
echo 'hello!' | curl -T - {url}/mypath

# Send a directory (zip)
zip -q -r - ./mydir | curl -T - {url}/mypath

# Send a directory (tar.gz)
tar zfcp - ./mydir | curl -T - {url}/mypath

# Encryption
## Send
cat myfile | openssl aes-256-cbc | curl -T - {url}/mypath
## Get
curl {url}/mypath | openssl aes-256-cbc -d";
        }
        protected Stream VersionResponseGenerator(OutgoingWebResponseContext Response)
        {
            var Encoding = Response.BindingWriteEncoding;
            var Bytes = Encoding.GetBytes($"{VERSION.FileVersion}\n");
            Response.ContentLength = Bytes.Length;
            Response.ContentType = $"text/plain;charset={Encoding.WebName}";
            return new MemoryStream(Bytes);
        }
        protected Stream OptionsResponseGenerator(OutgoingWebResponseContext Response)
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
        protected Stream FileGetGenerator(string FileName, OutgoingWebResponseContext Response)
        {
            var FilePath = Path.Combine(BasePath, FileName.TrimStart('/'));
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
        }
    }
}
