using System;
using System.Collections.Generic;
using System.Text;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public struct TinyList<T>
    {
        static readonly TinyList<T> _empty;

        public static ref readonly TinyList<T> Empty => ref _empty;

        internal bool HasFirst { get; private set; }

        internal bool HasSecond { get; private set; }

        internal bool HasThird { get; private set; }

        internal T First { get; private set; }

        internal T Second { get; private set; }

        internal T Third { get; private set; }

        internal List<T> List { get; private set; }

        public int Count
        {
            get
            {
                if (HasFirst)
                {
                    if (HasSecond)
                    {
                        if (HasThird)
                        {
                            if (null != List)
                            {
                                return 3 + List.Count;
                            }
                            return 3;
                        }
                        return 2;
                    }
                    return 1;
                }
                return 0;
            }
        }

        public T this[int index]
        {
            get => index switch
            {
                0 => HasFirst ? First : throw new IndexOutOfRangeException(),
                1 => HasSecond ? Second : throw new IndexOutOfRangeException(),
                2 => HasThird ? Third : throw new IndexOutOfRangeException(),
                _ => null != List ? List[index - 3] : throw new IndexOutOfRangeException()
            };

            set
            {
                switch (index)
                {
                    case 0:
                        if (!HasFirst)
                        {
                            throw new IndexOutOfRangeException();
                        }
                        First = value;
                        break;
                    case 1:
                        if (!HasSecond)
                        {
                            throw new IndexOutOfRangeException();
                        }
                        Second = value;
                        break;
                    case 2:
                        if (!HasThird)
                        {
                            throw new IndexOutOfRangeException();
                        }
                        Third = value;
                        break;
                    default:
                        if (null == List)
                        {
                            throw new IndexOutOfRangeException();
                        }
                        List[index - 3] = value;
                        break;

                }
            }
        }

        public TinyList(T first)
        {
            HasFirst = true;
            HasSecond = false;
            HasThird = false;
            First = first;
            Second = default;
            Third = default;
            List = null;
        }

        public TinyList(T first, T second)
        {
            HasFirst = true;
            HasSecond = true;
            HasThird = false;
            First = first;
            Second = second;
            Third = default;
            List = null;
        }

        public TinyList(T first, T second, T third)
        {
            HasFirst = true;
            HasSecond = true;
            HasThird = true;
            First = first;
            Second = second;
            Third = third;
            List = null;
        }

        public void Add(T value)
        {
            if (!HasFirst)
            {
                HasFirst = true;
                First = value;
                return;
            }
            if (!HasSecond)
            {
                HasSecond = true;
                Second = value;
                return;
            }
            if (!HasThird)
            {
                HasThird = true;
                Third = value;
                return;
            }
            if (null == List)
            {
                List = new List<T>();
            }
            List.Add(value);
        }

        public void Add(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public void Add(in TinyList<T> values)
        {
            if (values.HasFirst)
            {
                Add(values.First);
                if (values.HasSecond)
                {
                    Add(values.Second);
                    if (values.HasThird)
                    {
                        Add(values.Third);
                        if (!(values.List is null))
                        {
                            Add(values.List);
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            if (HasFirst)
            {
                if (HasSecond)
                {
                    if (HasThird)
                    {
                        if (!(List is null))
                        {
                            return $"[{First}, {Second}, {Third}, ...]";
                        }
                        return $"[{First}, {Second}, {Third}]";
                    }
                    return $"[{First}, {Second}]";
                }
                return $"[{First}]";
            }
            return "[]";
        }
    }
}