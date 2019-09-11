using Microsoft.AspNetCore.Mvc;
using Piping.Server.Core;
using Piping.Server.Mvc.Binder;

namespace Piping.Server.Mvc.Models
{
    [ModelBinder(typeof(ReceiveBinder))]
    public class ReceiveData
    {
        public RequestKey Key { get; }
        public ReceiveData(RequestKey Key) => this.Key = Key;
    }
}
