using System;

namespace Piping
{
    public class ResAndUnsubscribe
    {
        public ResAndUnsubscribe(Res ReqRes)
           => this.ReqRes = ReqRes;
        public Res ReqRes { get; }
        public event EventHandler unsubscribeClose;
        public void FireUnsubscribeClose() => unsubscribeClose?.Invoke(this, new EventArgs());
    }
}
