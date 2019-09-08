namespace Piping.Server.Core.Converters
{
    public class MultipartStreamConverterOption
    {
        public int MultipartBoundaryLengthLimit { get; set; } = 1024;
        public int DefaultBufferSize { get; set; } = 1024 * 4;
    }
}
