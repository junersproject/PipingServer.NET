using System.Collections.Generic;

namespace Piping
{
    public class WaiterDictionary : Dictionary<string, IWaiters>, IWaiterDictionary
    {
        public WaiterDictionary() : base() { }
    }
}
