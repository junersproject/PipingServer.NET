using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piping.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var Source = new TaskCompletionSource<bool>();
            System.Console.CancelKeyPress += (o, arg) =>
            {
                arg.Cancel = false;
                Source.TrySetResult(true);
            };
            var BaseAddress = new Uri("http://localhost/Console");
            using (var Self = new SelfHost())
            {
                Self.Open(BaseAddress);
                System.Console.WriteLine("Service Console Start.");
                try
                {
                    await Source.Task;
                }
                finally
                {
                    System.Console.WriteLine("Service Console Stop.");
                }
            }
        }
    }
}
