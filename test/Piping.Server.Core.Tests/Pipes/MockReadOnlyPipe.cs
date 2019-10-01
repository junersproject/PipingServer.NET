﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Piping.Server.Core.Pipes.Tests
{
    public class MockReadOnlyPipe : IReadOnlyPipe, IEquatable<IReadOnlyPipe>, IEquatable<MockReadOnlyPipe>
    {
        public static Comparable Comparer = new Comparable();
        public MockReadOnlyPipe() { }
        public MockReadOnlyPipe(IReadOnlyPipe ReadOnlyPipe)
        {
            Key = ReadOnlyPipe.Key;
            Status = ReadOnlyPipe.Status;
            Required = ReadOnlyPipe.Required;
            IsRemovable = ReadOnlyPipe.IsRemovable;
            ReceiversCount = ReadOnlyPipe.ReceiversCount;
            var Task = ReadOnlyPipe.GetHeadersAsync();
            if (Task.IsCompletedSuccessfully)
                Headers = new HeaderDictionary(Task.Result.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase));
        }
        public RequestKey Key { get; set; }
        public PipeStatus Status { get; set; }

        public PipeType Required { get; set; }


        public bool IsRemovable { get; set; }

        public int ReceiversCount { get; set; }

        event PipeStatusChangeEventHandler? IReadOnlyPipe.OnStatusChanged
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public async ValueTask<IHeaderDictionary> GetHeadersAsync(CancellationToken Token = default)
        {
            await Task.CompletedTask;
            return Headers;
        }

        public IAsyncEnumerable<PipeStatus> OrLaterEventAsync(CancellationToken Token = default)
        {
            throw new NotSupportedException();
        }
        public override string ToString()
            => nameof(MockReadOnlyPipe) + "{"
            + string.Join(", ", new[] {
                $"{nameof(Key)}:{Key}",
                $"{nameof(Status)}:{Status}",
                $"{nameof(Required)}:{Required}",
                $"{nameof(IsRemovable)}:{IsRemovable}",
                $"{nameof(ReceiversCount)}:{ReceiversCount}",
                Headers.Any() ? $"[{string.Join(", ", Headers.Select(v => $"{v.Key}: {v.Value}"))}]" : null,
            }.OfType<string>()) + "}";
        public override bool Equals(object? obj)
        {
            return obj is MockReadOnlyPipe mrop ? Equals(mrop)
                : obj is IReadOnlyPipe rop ? Equals(rop)
                : false;
        }
        public bool Equals([AllowNull] IReadOnlyPipe other)
        {
            return other is MockReadOnlyPipe mrop ? Equals(mrop)
                : other is IReadOnlyPipe _other &&
                Key.Equals(_other.Key) &&
                Status == _other.Status &&
                Required == _other.Required &&
                IsRemovable == _other.IsRemovable &&
                ReceiversCount == _other.ReceiversCount;
        }

        public bool Equals([AllowNull] MockReadOnlyPipe? other)
        {
            return other is MockReadOnlyPipe _other &&
                Key.Equals(_other.Key) &&
                Status == _other.Status &&
                Required == _other.Required &&
                IsRemovable == _other.IsRemovable &&
                ReceiversCount == _other.ReceiversCount &&
                Equals(Headers, _other.Headers);
        }
        private bool Equals(IHeaderDictionary a, IHeaderDictionary b)
            => a.ContentLength == b.ContentLength && 
                a.OrderBy(v => v.Key.ToLower())
                    .Zip(b.OrderBy(v => v.Key.ToLower()))
                    .All(v => string.Equals(v.First.Key, v.Second.Key, StringComparison.OrdinalIgnoreCase)
                        && v.First.Value == v.Second.Value);
        public override int GetHashCode()
            => HashCode.Combine(Key, Status, IsRemovable, ReceiversCount, Headers);
        public static bool operator ==(MockReadOnlyPipe left, MockReadOnlyPipe right)
        {
            return EqualityComparer<MockReadOnlyPipe>.Default.Equals(left, right);
        }

        public static bool operator !=(MockReadOnlyPipe left, MockReadOnlyPipe right)
        {
            return !(left == right);
        }
        public class Comparable : IEqualityComparer<MockReadOnlyPipe>, IComparer<MockReadOnlyPipe>, IComparer
        {
            public bool Equals([AllowNull] MockReadOnlyPipe x, [AllowNull] MockReadOnlyPipe y)
            {
                var XIsNotNull = x is MockReadOnlyPipe;
                var YIsNoNull = y is MockReadOnlyPipe;
                if (!XIsNotNull && !YIsNoNull)
                    return true;
                else if (XIsNotNull != YIsNoNull)
                    return false;
                return x.Equals(y);
            }

            public int GetHashCode([DisallowNull] MockReadOnlyPipe obj)
            {
                throw new NotImplementedException();
            }

            public int Compare(MockReadOnlyPipe x, MockReadOnlyPipe y)
            {
                if (Equals(x, y))
                    return 0;
                else
                    return -1;
            }

            public int Compare(object? x, object? y)
            {
                if (x is MockReadOnlyPipe _x && y is MockReadOnlyPipe _y)
                    return Compare(_x, _y);
                return -1;
            }
        }
    }
}