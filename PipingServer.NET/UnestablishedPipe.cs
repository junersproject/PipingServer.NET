using System.Collections.Generic;

namespace Piping
{
    public class UnestablishedPipe
    {
        public ReqAndUnsubscribe Sender { get; set; }
        public IList<ResAndUnsubscribe> Receivers { get; } = new List<ResAndUnsubscribe>();
        public int ReceiversCount { get; set; }
    }
}
