using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Aveezo
{
    public static class TupleExtensions
    {
        public static ITuple[] Cast<T1, T2>(this List<(T1, T2)> valueTupleList) => valueTupleList.ToArray().Cast();
        public static ITuple[] Cast<T1, T2, T3>(this List<(T1, T2, T3)> valueTupleList) => valueTupleList.ToArray().Cast();
        public static ITuple[] Cast<T1, T2, T3, T4>(this List<(T1, T2, T3, T4)> valueTupleList) => valueTupleList.ToArray().Cast();
        public static ITuple[] Cast<T1, T2, T3, T4, T5>(this List<(T1, T2, T3, T4, T5)> valueTupleList) => valueTupleList.ToArray().Cast();
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6>(this List<(T1, T2, T3, T4, T5, T6)> valueTupleList) => valueTupleList.ToArray().Cast();
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6, T7>(this List<(T1, T2, T3, T4, T5, T6, T7)> valueTupleList) => valueTupleList.ToArray().Cast();
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6, T7, T8>(this List<(T1, T2, T3, T4, T5, T6, T7, T8)> valueTupleList) => valueTupleList.ToArray().Cast();
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this List<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> valueTupleList) => valueTupleList.ToArray().Cast();
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this List<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> valueTupleList) => valueTupleList.ToArray().Cast();

        public static ITuple[] Cast<T1, T2>(this (T1, T2)[] valueTupleArray) => valueTupleArray.Cast(a => (ITuple)a);
        public static ITuple[] Cast<T1, T2, T3>(this (T1, T2, T3)[] valueTupleArray) => valueTupleArray.Cast(a => (ITuple)a);
        public static ITuple[] Cast<T1, T2, T3, T4>(this (T1, T2, T3, T4)[] valueTupleArray) => valueTupleArray.Cast(a => (ITuple)a);
        public static ITuple[] Cast<T1, T2, T3, T4, T5>(this (T1, T2, T3, T4, T5)[] valueTupleArray) => valueTupleArray.Cast(a => (ITuple)a);
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6>(this (T1, T2, T3, T4, T5, T6)[] valueTupleArray) => valueTupleArray.Cast(a => (ITuple)a);
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6, T7>(this (T1, T2, T3, T4, T5, T6, T7)[] valueTupleArray) => valueTupleArray.Cast(a => (ITuple)a);
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6, T7, T8>(this (T1, T2, T3, T4, T5, T6, T7, T8)[] valueTupleArray) => valueTupleArray.Cast(a => (ITuple)a);
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this (T1, T2, T3, T4, T5, T6, T7, T8, T9)[] valueTupleArray) => valueTupleArray.Cast(a => (ITuple)a);
        public static ITuple[] Cast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)[] valueTupleArray) => valueTupleArray.Cast(a => (ITuple)a);

        public static T[] ToArray<T>(this ITuple item)
        {
            var list = new List<T>(item.Length);

            for (var i = 0; i < item.Length; i++)
            {
                list.Add((T)item[i]);
            }

            return list.ToArray();
        }

        public static object[] ToArray(this ITuple item) => item.ToArray<object>();

        public static T[] ToArray<T>(this ITuple[] array, int index)
        {
            var list = new List<T>(array.Length);

            foreach (var item in array)
            {
                if (index < item.Length)
                    list.Add((T)item[index]);
                else
                    return null;
            }

            return list.ToArray();
        }

        public static int IndexOf<T>(this ITuple[] array, T item, int tupleIndex)
        {
            var iteration = 0;
            var found = -1;

            foreach (var i in array)
            {
                if (tupleIndex < i.Length)
                {
                    var v = i[tupleIndex];

                    if (Equals(v, item))
                    {
                        found = iteration;
                        break;
                    }
                }
                else break;

                iteration++;
            }

            return found;
        }

        public static bool Contains<T>(this ITuple[] array, T item, int tupleIndex) => array.IndexOf(item, tupleIndex) > -1;

        public static int IndexOf<T>(this List<ITuple> list, T item, int tupleIndex) => list.ToArray().IndexOf(item, tupleIndex);

        public static bool Contains<T>(this List<ITuple> list, T item, int tupleIndex) => list.IndexOf(item, tupleIndex) > -1;

        public static bool Diff<T>(this ITuple[] array, T[] referenceArray, int tupleIndex, out ITuple[] notExists, out T[] newItems)
        {
            notExists = null;
            newItems = null;

            // check for not exists
            var notExistsList = new List<ITuple>();

            foreach (var tuple in array)
            {
                if (tupleIndex < tuple.Length)
                {
                    var item = (T)tuple[tupleIndex];

                    if (!Array.Exists(referenceArray, t => Equals(t, item)))
                    {
                        notExistsList.Add(tuple);
                    }
                }
                else
                    return false;
            }

            // check for new items
            var newItemsList = new List<T>();
            var itemArray = array.ToArray<T>(tupleIndex);

            if (itemArray == null)
                return false;

            foreach (var item in referenceArray)
            {
                if (!Array.Exists(itemArray, t => Equals(t, item)))
                {
                    newItemsList.Add(item);
                }
            }

            notExists = notExistsList.ToArray();
            newItems = newItemsList.ToArray();

            return true;
        }

        public static bool Diff<T>(this ITuple[] array, T[] referenceArray, int tupleIndex, Action<ITuple> deleted, Action<T> added)
        {
            var ret = Diff(array, referenceArray, tupleIndex, out ITuple[] notExists, out T[] newItems);

            foreach (var ituple in notExists) deleted(ituple);
            foreach (var item in newItems) added(item);

            return ret;
        }

    }
}
