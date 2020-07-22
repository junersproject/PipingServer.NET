using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace PipingServer.Core.Internal
{
    internal class ReadonlyHeaderDictionary : IHeaderDictionary
    {
        readonly IDictionary<string, StringValues> Dict;
        public ReadonlyHeaderDictionary()
            => Dict = new Dictionary<string, StringValues>();
        public ReadonlyHeaderDictionary(IDictionary<string, StringValues> dictionary)
            => Dict = dictionary;
        public ReadonlyHeaderDictionary(IEnumerable<KeyValuePair<string, StringValues>> collection)
            => Dict = new Dictionary<string, StringValues>(collection);
        public ReadonlyHeaderDictionary(IEqualityComparer<string> comparer)
            => Dict = new Dictionary<string, StringValues>(comparer);
        public ReadonlyHeaderDictionary(int capacity)
            => Dict = new Dictionary<string, StringValues>(capacity);
        public ReadonlyHeaderDictionary(IDictionary<string, StringValues> dictionary, IEqualityComparer<string> comparer)
            => Dict = new Dictionary<string, StringValues>(comparer);
        public ReadonlyHeaderDictionary(IEnumerable<KeyValuePair<string, StringValues>> collection, IEqualityComparer<string> comparer)
            => Dict = new Dictionary<string, StringValues>(collection, comparer);
        public ReadonlyHeaderDictionary(int capacity, IEqualityComparer<string> comparer)
            => Dict = new Dictionary<string, StringValues>(capacity, comparer);
        public StringValues this[string key] { get => Dict[key]; set => throw new NotImplementedException(); }

        public long? ContentLength
        {
            get => long.TryParse(Dict["Content-Length"], out var value) ? value : (long?)null;
            set => throw new NotImplementedException();
        }

        public ICollection<string> Keys => Dict.Keys;

        public ICollection<StringValues> Values => Dict.Values;

        public int Count => Dict.Count;

        public bool IsReadOnly => true;

        public void Add(string key, StringValues value) => throw new NotImplementedException();

        public void Add(KeyValuePair<string, StringValues> item) => throw new NotImplementedException();

        public void Clear() => throw new NotImplementedException();

        bool ICollection<KeyValuePair<string, StringValues>>.Contains(KeyValuePair<string, StringValues> item)
            => ((ICollection<KeyValuePair<string, StringValues>>)Dict).Contains(item);

        public bool ContainsKey(string key)
            => Dict.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<string, StringValues>>)Dict).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
            => Dict.GetEnumerator();

        public bool Remove(string key)
            => throw new NotImplementedException();

        public bool Remove(KeyValuePair<string, StringValues> item)
            => throw new NotImplementedException();

        public bool TryGetValue(string key, out StringValues value)
            => Dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator()
            => Dict.GetEnumerator();
    }
}
