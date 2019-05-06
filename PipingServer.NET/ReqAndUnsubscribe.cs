using System;

namespace Piping
{
    public class ReqAndUnsubscribe
    {
        public ReqAndUnsubscribe(Req ReqRes)
           => this.ReqRes = ReqRes;
        public Req ReqRes { get; }
        public event EventHandler unsubscribeClose;
        public void FireUnsubscribeClose() => unsubscribeClose?.Invoke(this, new EventArgs());
    }
}
