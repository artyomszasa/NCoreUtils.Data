using System;
using System.Collections;
using System.Collections.Generic;

namespace NCoreUtils.Linq
{
    public static class Enumerator
    {
        sealed class SelectEnumerator<TSource, TTarget> : IEnumerator<TTarget>
        {
            readonly IEnumerator<TSource> _source;

            readonly Func<TSource, TTarget> _selector;

            public TTarget Current { get; private set; } = default(TTarget);

            object IEnumerator.Current => Current;

            public SelectEnumerator(IEnumerator<TSource> source, Func<TSource, TTarget> selector)
            {
                _source = source;
                _selector = selector;
            }

            public void Dispose() => _source.Dispose();

            public bool MoveNext()
            {
                if (_source.MoveNext())
                {
                    Current = _selector(_source.Current);
                    return true;
                }
                Current = default(TTarget);
                return false;
            }

            public void Reset()
            {
                _source.Reset();
                Current = default(TTarget);
            }
        }

        sealed class WhereEnumerator<T> : IEnumerator<T>
        {
            readonly IEnumerator<T> _source;

            readonly Func<T, bool> _predicate;

            public T Current { get; private set; } = default(T);

            object IEnumerator.Current => Current;

            public WhereEnumerator(IEnumerator<T> source, Func<T, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            public void Dispose() => _source.Dispose();

            public bool MoveNext()
            {
                var found = false;
                var done = false;
                do
                {
                    if (!_source.MoveNext())
                    {
                        done = true;
                        Current = default(T);
                    }
                    else
                    {
                        var current = _source.Current;
                        if (_predicate(current))
                        {
                            found = true;
                            done = true;
                            Current = current;
                        }
                    }
                }
                while (!done);
                return found;
            }

            public void Reset()
            {
                _source.Reset();
                Current = default(T);
            }
        }

        public static IEnumerator<TTarget> Select<TSource, TTarget>(this IEnumerator<TSource> source, Func<TSource, TTarget> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }
            return new SelectEnumerator<TSource, TTarget>(source, selector);
        }

        public static IEnumerator<T> Where<T>(this IEnumerator<T> source, Func<T, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return new WhereEnumerator<T>(source, predicate);
        }
    }
}