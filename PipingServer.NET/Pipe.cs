using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Piping
{
    public class Pipe
    {
        public Req Sender { get; }
        public IReadOnlyCollection<Res> Receivers { get; }
        public Pipe(Req Sender, IList<Res> Receivers)
            => (this.Sender, this.Receivers) = (Sender, new ReadOnlyCollection<Res>(Receivers));
        public Pipe(Req Sender, IEnumerable<Res> Receivers) : this(Sender, Receivers.ToList()) { }
        public void Deconstruct(out Req Sender, out IReadOnlyCollection<Res> Receivers)
            => (Sender, Receivers) = (this.Sender, this.Receivers);
    }
}
