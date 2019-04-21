using System.Collections.Generic;

namespace Piping
{
    public class UnestablishedPipe
    {
        public ReqResAndUnsubscribe Sender { get; set; }
        public List<ReqResAndUnsubscribe> Receivers { get; } = new List<ReqResAndUnsubscribe>();
        public int ReceiversCount { get; set; }
    }
}
