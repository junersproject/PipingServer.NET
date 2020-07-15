namespace PipingServer.Core.Converters
{
    public class MultipartStreamConverterOption
    {
        /// <summary>
        /// multipart boundary length limit size.
        /// </summary>
        public int MultipartBoundaryLengthLimit { get; set; } = 1024;
        /// <summary>
        /// multipart parsing default buffer size.
        /// </summary>
        public int BufferSize { get; set; } = 1024 * 4;
    }
}
