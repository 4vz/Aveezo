using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public static class DictionaryExtensions
    {
        public static List<TKey> ToList<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection keys)
        {
            List<TKey> list = new List<TKey>();

            foreach (TKey key in keys)
            {
                list.Add(key);
            }

            return list;
        }

        public static TKey[] ToArray<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection keys)
        {
            return keys.ToList<TKey, TValue>().ToArray();
        }

        public static List<TValue> ToList<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection values)
        {
            List<TValue> list = new List<TValue>();

            foreach (TValue value in values)
            {
                list.Add(value);
            }

            return list;
        }

        public static TValue[] ToArray<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection values)
        {
            return values.ToList<TKey, TValue>().ToArray();
        }

        public static (TKey, TValue) Get<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, int index)
        {
            int i = 0;
            foreach (var (key, value) in dictionary)
            {
                if (i == index)
                {
                    return (key, value);
                }
                i++;
            }

            return (default, default);
        }
   
        public static List<T> ToList<T, TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, T> cast)
        {
            var list = new List<T>();

            foreach (var pair in dictionary)
            {
                list.Add(cast(pair));
            }

            return list;
        }

        public static (TKey, TValue)[] ToTupleArray<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) => dictionary.ToList(o => (o.Key, o.Value)).ToArray();

        public static bool Contains<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> item)
        {
            foreach (var (key, value) in dictionary)
            {
                if (Equals(key, item.Key) && Equals(value, item.Value))
                    return true;
            }
            return false;
        }

        public static bool Diff<TKey, TValue1, TValue2>(this Dictionary<TKey, TValue1> left, Dictionary<TKey, TValue2> right, Action<TKey, TValue1> onlyLeft, Action<TKey, TValue2> onlyRight, Action<TKey, TValue1, TValue2> both)
        {
            if (left == null) left = new();
            if (right == null) right = new();

            var same = true;

            foreach (var (leftKey, leftValue) in left)
            {
                if (!right.ContainsKey(leftKey))
                {
                    same = false;
                    onlyLeft?.Invoke(leftKey, leftValue);
                }
            }
            foreach (var (rightKey, rightValue) in right)
            {
                if (!left.ContainsKey(rightKey))
                {
                    same = false;
                    onlyRight?.Invoke(rightKey, rightValue);
                }
                else
                    both?.Invoke(rightKey, left[rightKey], rightValue);
            }

            return same;
        }

    }

    public static class SortedDictionaryExtensions
    {
        public static List<TKey> ToList<TKey, TValue>(this SortedDictionary<TKey, TValue>.KeyCollection keys)
        {
            List<TKey> list = new List<TKey>();

            foreach (TKey key in keys)
            {
                list.Add(key);
            }

            return list;
        }

        public static TKey[] ToArray<TKey, TValue>(this SortedDictionary<TKey, TValue>.KeyCollection keys)
        {
            return keys.ToList<TKey, TValue>().ToArray();
        }

        public static List<TValue> ToList<TKey, TValue>(this SortedDictionary<TKey, TValue>.ValueCollection values)
        {
            List<TValue> list = new List<TValue>();

            foreach (TValue value in values)
            {
                list.Add(value);
            }

            return list;
        }

        public static TValue[] ToArray<TKey, TValue>(this SortedDictionary<TKey, TValue>.ValueCollection values)
        {
            return values.ToList<TKey, TValue>().ToArray();
        }

        public static (TKey, TValue) Get<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, int index)
        {
            int i = 0;
            foreach (var (key, value) in dictionary)
            {
                if (i == index)
                {
                    return (key, value);
                }
                i++;
            }

            return (default, default);
        }
    }
}
