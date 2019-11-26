using System;
using System.Collections.Generic;
using System.Text;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public static class TinyListExtensions
    {
        public static TinyList<T> CopyAdd<T>(this in TinyList<T> source, T value)
        {
            TinyList<T> result = new TinyList<T>();
            result.Add(in source);
            result.Add(value);
            return result;
        }

        public static TinyList<T> CopyAdd<T>(this in TinyList<T> source, in TinyList<T> other)
        {
            TinyList<T> result = new TinyList<T>();
            result.Add(in source);
            result.Add(in other);
            return result;
        }

        // FIMXE: optimize
        public static string Join(this in TinyList<string> source, string separator)
        {
            var stringBuilder = new StringBuilder();
            if (source.HasFirst)
            {
                stringBuilder.Append(source.First);
                if (source.HasSecond)
                {
                    stringBuilder.Append(separator).Append(source.Second);
                    if (source.HasThird)
                    {
                        stringBuilder.Append(separator).Append(source.Second);
                        if (null != source.List)
                        {
                            foreach (var item in source.List)
                            {
                                stringBuilder.Append(separator).Append(item);
                            }
                        }
                    }
                }
            }
            return stringBuilder.ToString();
        }

        public static bool TryExtract<T>(this in TinyList<T> source, Func<T, bool> predicate, out T match, in TinyList<T> remain)
        {
            if (source.HasFirst)
            {
                if (predicate(source.First))
                {
                    match = source.First;
                    if (source.HasSecond)
                    {
                        remain.Add(source.Second);
                        if (source.HasThird)
                        {
                            remain.Add(source.Third);
                            if (!(source.List is null))
                            {
                                remain.Add(source.List);
                            }
                        }
                    }
                    return true;
                }
                if (source.HasSecond)
                {
                    if (predicate(source.Second))
                    {
                        match = source.Second;
                        remain.Add(source.First);
                        if (source.HasThird)
                        {
                            remain.Add(source.Third);
                            if (!(source.List is null))
                            {
                                remain.Add(source.List);
                            }
                        }
                        return true;
                    }
                    if (source.HasThird)
                    {
                        if (predicate(source.Third))
                        {
                            match = source.Third;
                            remain.Add(source.First);
                            remain.Add(source.Second);
                            if (!(source.List is null))
                            {
                                remain.Add(source.List);
                            }
                            return true;
                        }
                        if (!(source.List is null))
                        {
                            var index = source.List.FindIndex(item => predicate(item));
                            if (-1 != index)
                            {
                                match = source.List[index];
                                remain.Add(source.First);
                                remain.Add(source.Second);
                                remain.Add(source.Third);
                                if (1 != source.List.Count)
                                {
                                    var newlist = new List<T>(source.List);
                                    newlist.RemoveAt(index);
                                    remain.Add(newlist);
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            match = default;
            return false;
        }

        public static List<T> ToList<T>(this in TinyList<T> source)
        {
            var result = new List<T>(Math.Max(source.Count, 4));
            if (source.HasFirst)
            {
                result.Add(source.First);
                if (source.HasSecond)
                {
                    result.Add(source.Second);
                    if (source.HasThird)
                    {
                        result.Add(source.Third);
                        if (null != source.List)
                        {
                            result.AddRange(source.List);
                        }
                    }
                }
            }
            return result;
        }
    }
}