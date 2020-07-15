using System;
using System.Collections.Generic;
using System.Linq;
internal class DisposableList<T> : List<T>, IDisposable
    where T : IDisposable
{
    public DisposableList() : base() { }
    public DisposableList(IEnumerable<T> collections) : base(collections) { }
    #region IDisposable Support
    private bool disposedValue = false; // 重複する呼び出しを検出するには

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var d in ((IEnumerable<T>)this).Reverse())
                    d?.Dispose();
                this.Clear();
            }
            disposedValue = true;
        }
    }
    // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
    public void Dispose()
    {
        // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        Dispose(true);
        // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
        // GC.SuppressFinalize(this);
    }
    #endregion

}
internal class DisposableList : DisposableList<IDisposable>
{
    public DisposableList() : base() { }
    public DisposableList(IEnumerable<IDisposable> collections) : base(collections) { }
}
