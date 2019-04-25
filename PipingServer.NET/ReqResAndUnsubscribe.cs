using System;

namespace Piping
{
    public class ReqResAndUnsubscribe
    {
        public ReqResAndUnsubscribe(ReqRes ReqRes)
           => this.ReqRes = ReqRes;
        public ReqRes ReqRes { get; }
        public event EventHandler unsubscribeClose;
        public void FireUnsubscribeClose() => unsubscribeClose?.Invoke(this, new EventArgs());
    }
}
