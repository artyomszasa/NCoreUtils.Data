using System;
using System.Linq.Expressions;

namespace NCoreUtils.Linq
{
    public static class Splice
    {
        public static T Value<T>(Expression value)
            => throw new InvalidOperationException("This method not supposed to be invoked at runtime.");

        public static TResult Apply<TArg, TResult>(Expression<Func<TArg, TResult>> lambda, TArg arg)
            => throw new InvalidOperationException("This method not supposed to be invoked at runtime.");

        public static TResult Apply<TArg1, TArg2, TResult>(Expression<Func<TArg1, TArg2, TResult>> lambda, TArg1 arg1, TArg2 arg2)
            => throw new InvalidOperationException("This method not supposed to be invoked at runtime.");

        public static TResult Apply<TArg1, TArg2, TArg3, TResult>(Expression<Func<TArg1, TArg2, TArg3, TResult>> lambda, TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => throw new InvalidOperationException("This method not supposed to be invoked at runtime.");

        public static TResult Apply<TArg1, TArg2, TArg3, TArg4, TResult>(Expression<Func<TArg1, TArg2, TResult>> lambda, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
            => throw new InvalidOperationException("This method not supposed to be invoked at runtime.");

        public static TResult Apply<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Expression<Func<TArg1, TArg2, TResult>> lambda, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
            => throw new InvalidOperationException("This method not supposed to be invoked at runtime.");
    }
}