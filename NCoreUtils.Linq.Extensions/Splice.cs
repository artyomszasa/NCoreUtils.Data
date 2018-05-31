using System;
using System.Linq.Expressions;

namespace NCoreUtils.Linq
{
    public static class Splice
    {
        public static T Value<T>(string name)
            => throw new InvalidOperationException("This method not supposed to be invoked");

        public static TResult Apply<TArg, TResult>(Expression<Func<TArg, TResult>> lambda, TArg arg)
            => throw new InvalidOperationException("This method not supposed to be invoked");
    }
}