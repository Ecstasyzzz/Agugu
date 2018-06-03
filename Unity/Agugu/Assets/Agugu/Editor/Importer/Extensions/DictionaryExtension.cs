using System.Collections.Generic;

namespace Agugu.Editor
{
    public static class DictionaryExtension
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict,
            TKey                                                                           key)
        {
            TValue value;
            return dict.TryGetValue(key, out value) ? value : default(TValue);
        }
    }
}