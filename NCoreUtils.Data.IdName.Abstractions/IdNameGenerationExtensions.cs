using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.IdNameGeneration;

namespace NCoreUtils.Data
{
    public static class IdNameGenerationExtensions
    {
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static Task<string> GenerateAsync<T>(
        //     this IIdNameGenerator generator,
        //     IQueryable<T> query,
        //     string name,
        //     CancellationToken cancellationToken = default(CancellationToken))
        //     where T : class, IHasIdName
        //     => generator.GenerateAsync(query, DummyStringDecomposition.Factory, name, cancellationToken);

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static Task<string> GenerateAsync<T>(
        //     this IIdNameGenerator generator,
        //     IQueryable<T> query,
        //     Func<string, IStringDecomposition> decompose,
        //     T entity,
        //     Func<T, string> nameSelector,
        //     CancellationToken cancellationToken = default(CancellationToken))
        //     where T : class, IHasIdName
        //     => generator.GenerateAsync(query, decompose, nameSelector(entity), cancellationToken);

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static Task<string> GenerateAsync<T>(
        //     this IIdNameGenerator generator,
        //     IQueryable<T> query,
        //     T entity,
        //     Func<T, string> nameSelector,
        //     CancellationToken cancellationToken = default(CancellationToken))
        //     where T : class, IHasIdName
        //     => generator.GenerateAsync(query, DummyStringDecomposition.Factory, nameSelector(entity), cancellationToken);

        // SYNC VERSIONS

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static string Generate<T>(
        //     this IIdNameGenerator generator,
        //     IQueryable<T> query,
        //     Func<string, IStringDecomposition> decompose,
        //     string name)
        //     where T : class, IHasIdName
        //     => generator.GenerateAsync(query, decompose, name).GetAwaiter().GetResult();

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static string Generate<T>(
        //     this IIdNameGenerator generator,
        //     IQueryable<T> query,
        //     string name,
        //     CancellationToken cancellationToken = default(CancellationToken))
        //     where T : class, IHasIdName
        //     => generator.GenerateAsync(query, name).GetAwaiter().GetResult();

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static string Generate<T>(
        //     this IIdNameGenerator generator,
        //     IQueryable<T> query,
        //     Func<string, IStringDecomposition> decompose,
        //     T entity,
        //     Func<T, string> nameSelector,
        //     CancellationToken cancellationToken = default(CancellationToken))
        //     where T : class, IHasIdName
        //     => generator.GenerateAsync(query, decompose, entity, nameSelector).GetAwaiter().GetResult();

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static string Generate<T>(
        //     this IIdNameGenerator generator,
        //     IQueryable<T> query,
        //     T entity,
        //     Func<T, string> nameSelector,
        //     CancellationToken cancellationToken = default(CancellationToken))
        //     where T : class, IHasIdName
        //     => generator.GenerateAsync(query, entity, nameSelector).GetAwaiter().GetResult();
    }
}