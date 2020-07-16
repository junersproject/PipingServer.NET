using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 解放可能なリスト
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IDisposableList<T> : IList<T>, IDisposable { }
/// <summary>
/// 解放可能なリスト（任意型対応）
/// </summary>
/// <typeparam name="T"></typeparam>
internal class DisposableList<T> : List<T>, IDisposableList<T>
    where T : IDisposable
{
    protected DisposableList() : base() { }
    protected DisposableList(IEnumerable<T> collections) : base(collections) { }
    public static IDisposableList<Disposable> Create<Disposable>(IEnumerable<Disposable> collections) where Disposable : IDisposable
        => new DisposableList<Disposable>(collections);
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
/// <summary>
/// 解放可能なリスト（任意引数対応）
/// </summary>
internal class DisposableList : DisposableList<IDisposable>
{
    protected DisposableList() : base() { }
    protected DisposableList(IEnumerable<IDisposable> collections) : base(collections) { }
    public static IDisposableList<IDisposable> Create(params IDisposable[] collections) => new DisposableList(collections);
    public static new IDisposableList<Disposable> Create<Disposable>(IEnumerable<Disposable> collections) where Disposable : IDisposable
        => DisposableList<Disposable>.Create(collections);
}
