using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Piping
{
    public class Pipe
    {
        public ReqRes Sender { get; }
        public IReadOnlyCollection<ReqRes> Receivers { get; }
        public Pipe(ReqRes Sender, IList<ReqRes> Receivers)
            => (this.Sender, this.Receivers) = (Sender, new ReadOnlyCollection<ReqRes>(Receivers));
        public Pipe(ReqRes Sender, IEnumerable<ReqRes> Receivers) : this(Sender, Receivers.ToList()) { }
        public void Deconstruct(out ReqRes Sender, out IReadOnlyCollection<ReqRes> Receivers)
            => (Sender, Receivers) = (this.Sender, this.Receivers);
    }
}
