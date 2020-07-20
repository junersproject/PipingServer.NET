using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;

namespace PipingServer.Client
{
    public class PipingServerClientOptions
    {
        public string DefaultName { get; set; } = Options.DefaultName;
    }
}
