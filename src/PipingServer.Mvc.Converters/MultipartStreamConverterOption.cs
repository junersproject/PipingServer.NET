using Microsoft.AspNetCore.WebUtilities;

namespace PipingServer.Mvc.Converters
{
    public class MultipartStreamConverterOption
    {
        /// <summary>
        /// The limit for the number of headers to read.
        /// </summary>
        public int HeadersCountLimit { get; set; } = MultipartReader.DefaultHeadersCountLimit;
        /// <summary>
        /// The combined size limit for headers per multipart section.
        /// </summary>
        public int HeadersLengthLimit { get; set; } = MultipartReader.DefaultHeadersLengthLimit;
        /// <summary>
        /// multipart parsing default buffer size.
        /// </summary>
        public int BufferSize { get; set; } = 1024 * 4;
        /// <summary>
        /// The optional limit for the total response body length.
        /// </summary>
        public long? BodyLengthLimit { get; set; }
    }
}
