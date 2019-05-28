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
using System.ServiceModel.Channels;

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
            var HasSecure = false;
            var HasNotSecure = false;
            foreach (var address in config.BaseAddresses) {
                if (address.Scheme == "https")
                    HasSecure = true;
                else if (address.Scheme == "http")
                    HasNotSecure = true;
            }
            Binding binding = null;
            if (HasSecure)
            {
                var Mode = WebHttpSecurityMode.Transport;
                var ClientCredentialType =HttpClientCredentialType.Basic;
                var whb = new WebHttpBinding
                {
                    TransferMode = TransferMode,
                    SendTimeout = SendTimeout,
                    OpenTimeout = OpenTimeout,
                    CloseTimeout = CloseTimeout,
                    MaxBufferSize = MaxBufferSize,
                    MaxReceivedMessageSize = MaxReceivedMessageSize,
                    Security =
                    {
                        Mode = Mode,
                        Transport =
                        {
                            ClientCredentialType = ClientCredentialType,
                        }
                    }
                };
                binding = whb;
            }
            else if (HasNotSecure)
            {
                var whb = new WebHttpBinding
                {
                    TransferMode = TransferMode,
                    SendTimeout = SendTimeout,
                    OpenTimeout = OpenTimeout,
                    CloseTimeout = CloseTimeout,
                    MaxBufferSize = MaxBufferSize,
                    MaxReceivedMessageSize = MaxReceivedMessageSize,
                };
                binding = whb;
            }
            if (!(binding is Binding _binding))
                return;
            var endpoint = config.AddServiceEndpoint(typeof(IService), _binding, "");
            endpoint.EndpointBehaviors.Add(new WebHttpBehavior
            {
                AutomaticFormatSelectionEnabled = false,
                HelpEnabled = false,
                DefaultBodyStyle = WebMessageBodyStyle.Bare,
                DefaultOutgoingRequestFormat = WebMessageFormat.Json,
                DefaultOutgoingResponseFormat = WebMessageFormat.Json,
                FaultExceptionEnabled = true,
            });
        }
        /// <summary>
        /// 待ち合わせ用 Dictionary
        /// </summary>
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
        /// <summary>
        /// 固定API
        /// </summary>
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
            var Context = WebOperationContext.Current;
            var Method = Context.IncomingRequest.Method;
            switch (Method)
            {
                case "POST":
                case "PUT":
                    return UploadAsync(inputStream, GetRelativeUri(), Context);
                case "GET":
                    return DownloadAsync(GetRelativeUri(), Context);
                case "OPTIONS":
                    return Task.FromResult(OptionsResponseGenerator(Context));
                default:
                    return Task.FromResult(NotImplemented(Context));
            }
        }
        protected Stream BadRequest(WebOperationContext Context, string AndMessage = null)
        {
            Context.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
            Context.OutgoingResponse.StatusDescription = AndMessage;
            Context.OutgoingResponse.ContentLength = 0;
            return new MemoryStream(new byte[0]);
        }

        public async Task<Stream> UploadAsync(Stream InputStream, string RelativeUri, WebOperationContext Context = null)
        {
            Context ??= WebOperationContext.Current;
            if (NAME_TO_RESERVED_PATH.TryGetValue(RelativeUri, out _))
                return BadRequest(Context, $"[ERROR] Cannot send to a reserved path '{RelativeUri}'. (e.g. '/mypath123')\n");
            var Key = new RequestKey(RelativeUri);
            // If the number of receivers is invalid
            if (Key.Receivers <= 0)
                return BadRequest(Context, $"[ERROR] n should > 0, but n = ${Key.Receivers}.\n");
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
                return BadRequest(Context, $"[ERROR] Connection on '{RelativeUri}' has been established already.\n");
            try
            {
                return waiter.AddSender(Key, new ReqRes
                {
                    Context = Context,
                    RequestStream = InputStream,
                }, Encoding, 1024);
            }catch(InvalidOperationException e)
            {
                return BadRequest(Context, e.Message);
            }
        }
        Task<Stream> IService.PostUploadAsync(Stream InputStream) => UploadAsync(InputStream, GetRelativeUri(), WebOperationContext.Current);
        Task<Stream> IService.PutUploadAsync(Stream InputStream) => UploadAsync(InputStream, GetRelativeUri(), WebOperationContext.Current);
        public async Task<Stream> DownloadAsync(string RelativeUri, WebOperationContext Context = null)
        {
            if (NAME_TO_RESERVED_PATH.TryGetValue(RelativeUri, out var Generator))
                return await Generator();
            Context ??= WebOperationContext.Current;
            var ResponseStream = new MemoryStream();
            var Key = new RequestKey(RelativeUri);
            if (Key.Receivers <= 0)
                return BadRequest(Context, $"[ERROR] n should > 0, but n = {Key.Receivers}.\n");
            if (!pathToUnestablishedPipe.TryGetValue(Key.LocalPath, out var waiter))
            {
                waiter = new SenderResponseWaiters(Key.Receivers);
                pathToUnestablishedPipe[Key.LocalPath] = waiter;
            }
            if (waiter.IsEstablished)
                return BadRequest(Context, $"[ERROR] Connection on '{RelativeUri}' has been established already.\n");
            var rs = new ReqRes
            {
                Context = Context,
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
                return BadRequest(Context, e.Message);
            }
        }
        Task<Stream> IService.GetDownloadAsync() => DownloadAsync(GetRelativeUri(), WebOperationContext.Current);
        public Task<Stream> GetDefaultPageAsync()
            => Task.FromResult(DefaultPageResponseGenerator(WebOperationContext.Current));
        public Task<Stream> GetVersionAsync()
            => Task.FromResult(VersionResponseGenerator(WebOperationContext.Current));
        public Task<Stream> GetHelpAsync()
            => Task.FromResult(HelpPageResponseGenerator(WebOperationContext.Current));
        public Task<Stream> GetFaviconAsync()
            => Task.FromResult(FileGetGenerator(DefaultPath.Favicon, WebOperationContext.Current));
        public Task<Stream> GetRobotsAsync()
            => Task.FromResult(FileGetGenerator(DefaultPath.Robots, WebOperationContext.Current));
        public Task<Stream> GetOptionsAsync()
            => Task.FromResult(OptionsResponseGenerator(WebOperationContext.Current));
        protected Stream DefaultPageResponseGenerator(WebOperationContext Context)
        {
            var Encoding = Context.OutgoingResponse.BindingWriteEncoding;
            var Bytes = Encoding.GetBytes(GetDefaultPage());
            Context.OutgoingResponse.ContentLength = Bytes.Length;
            Context.OutgoingResponse.ContentType = $"text/html; charset={Encoding.WebName}";
            return new MemoryStream(Bytes);
        }
        internal static string GetDefaultPage() => Properties.Resource.DefaultPage;
        protected Stream HelpPageResponseGenerator(WebOperationContext Context)
        {
            var url = GetBaseUri();
            var Bytes = Encoding.GetBytes(GetHelpPageText(url, VERSION));
            Context.OutgoingResponse.ContentLength = Bytes.Length;
            Context.OutgoingResponse.ContentType = $"text/plain;charset={Encoding.WebName}";
            return new MemoryStream(Bytes);
        }
        internal static string GetHelpPageText(Uri url, FileVersionInfo version)
        {
            return $@"Help for piping-server {version.ProductVersion}
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
        protected Stream VersionResponseGenerator(WebOperationContext Context)
        {
            var Encoding = Context.OutgoingResponse.BindingWriteEncoding;
            var Bytes = Encoding.GetBytes($"{VERSION.FileVersion}\n");
            Context.OutgoingResponse.ContentLength = Bytes.Length;
            Context.OutgoingResponse.ContentType = $"text/plain;charset={Encoding.WebName}";
            return new MemoryStream(Bytes);
        }
        protected Stream OptionsResponseGenerator(WebOperationContext Context)
        {
            Context.OutgoingResponse.StatusCode = HttpStatusCode.OK;
            Context.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            Context.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, HEAD, POST, PUT, OPTIONS");
            Context.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Content-Disposition");
            Context.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "86400");
            Context.OutgoingResponse.ContentLength = 0;
            return new MemoryStream(new byte[0]);
        }
        protected Stream NotImplemented(WebOperationContext Context)
        {
            Context.OutgoingResponse.StatusCode = HttpStatusCode.NotImplemented;
            Context.OutgoingResponse.ContentLength = 0;
            return new MemoryStream(new byte[0]);
        }
        protected bool ExistsFile(string FileName)
        {
            var FilePath = Path.Combine(BasePath, FileName.TrimStart('/'));
            return File.Exists(FilePath);
        }
        protected Stream FileGetGenerator(string FileName, WebOperationContext Context)
        {
            var FilePath = Path.Combine(BasePath, FileName.TrimStart('/'));
            try
            {
                var Bytes = File.ReadAllBytes(FilePath);
                Context.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                Context.OutgoingResponse.ContentType = MimeMapping.GetMimeMapping(FilePath);
                Context.OutgoingResponse.ContentLength = Bytes.Length;
                return new MemoryStream(Bytes);
            }
            catch (FileNotFoundException)
            {
                Context.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
            }
            catch (SecurityException)
            {
                Context.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
            }
            catch (Exception)
            {
                Context.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            }
            Context.OutgoingResponse.ContentLength = 0;
            return new MemoryStream(new byte[0]);
        }
    }
}
