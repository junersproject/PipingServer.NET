using System;
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
                arg.Cancel = Source.Task.IsCompleted;
                Source.TrySetResult(true);
            };
            var BaseAddress = new Uri("http://localhost/Console");
            using var Self = new SelfHost();
            System.Console.Write($"Service Open: ");
            using (SetColor(ConsoleColor.Blue, ConsoleColor.Yellow))
                System.Console.WriteLine($"{BaseAddress}");
            try
            {
                Self.Open(BaseAddress);
                System.Console.WriteLine("Service Console Start.");
                try
                {
                    System.Console.WriteLine("Enter Ctrl + C to stop.");
                    await Source.Task;
                }
                finally
                {
                    System.Console.WriteLine("Service Console Stop.");
                }
            }
            catch (Exception e)
            {
                using var s = SetColor(ConsoleColor.White, ConsoleColor.Red);
                System.Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// コンソールの文字色・背景色を設定する
        /// </summary>
        /// <param name="Foreground"></param>
        /// <param name="Background"></param>
        /// <returns></returns>
        private static IDisposable SetColor(ConsoleColor? Foreground = null, ConsoleColor? Background = null)
        { 
            var _Forground = Foreground is ConsoleColor ? System.Console.ForegroundColor : (ConsoleColor?)null;
            var _Background = Background is ConsoleColor ? System.Console.BackgroundColor : (ConsoleColor?)null;
            try
            {
                if (Foreground is ConsoleColor forground)
                    System.Console.ForegroundColor = forground;
                if (Background is ConsoleColor background)
                    System.Console.BackgroundColor = background;
            }
            catch
            {
                SetBack();
                throw;
            }
            return Disposable.Create(SetBack);
            void SetBack()
            {
                if (_Forground is ConsoleColor forground)
                    System.Console.ForegroundColor = forground;
                if (_Background is ConsoleColor background)
                    System.Console.BackgroundColor = background;
            }
        }
    }
    internal class Disposable : IDisposable
    {
        Action Action;
        Disposable(Action Action) => this.Action = Action;
        public static IDisposable Create(Action Action) => new Disposable(Action ?? throw new ArgumentNullException(nameof(Action)));

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        Action?.Invoke();
                    }
                    catch { }
                    Action = null;
                }
                disposedValue = true;
            }
        }

        ~Disposable()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
