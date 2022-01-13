using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.IdNameGeneration
{
    public class IdNameGenerator : IIdNameGenerator
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private class Box<T>
        {
            public T Value;

            public Box(T value)
                => Value = value;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Used internally.")]
        private static Expression BoxedContstant(
            object? value,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        {
            var boxType = typeof(Box<>).MakeGenericType(type);
            var field = boxType.GetField(nameof(Box<int>.Value))
                ?? throw new InvalidOperationException($"Could not get Value field for Box<{type}>. Consider preserving types explicitly.");
            var ctor = boxType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new [] { type }, null)
                ?? throw new InvalidOperationException($"Could not get ctor for Box<{type}>. Consider preserving types explicitly.");
            var box = ctor.Invoke(new object?[] { value });
            return Expression.Field(Expression.Constant(box, boxType), field);
        }

        private IStringSimplifier Simplifier { get; }

        public IdNameGenerator(IStringSimplifier simplifier)
        {
            Simplifier = simplifier ?? throw new ArgumentNullException(nameof(simplifier));
        }

        internal async Task<string> GenerateAsync<T>(
            IQueryable<T> query,
            IdNameDescription idNameDescription,
            string name,
            CancellationToken cancellationToken = default)
            where T : class, IHasIdName
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            cancellationToken.ThrowIfCancellationRequested();
            var decomposition = idNameDescription.Decomposer.Decompose(name);
            var simplified = Simplifier.Simplify(decomposition.MainPart);
            string? result = default;
            for (var index = 0; result is null; ++index)
            {
                var candidate = decomposition.Rebuild(simplified, 0 == index ? default : index.ToString());
                var predicate = LinqExtensions.ReplaceExplicitProperties<Func<T, bool>>(e => e.IdName == candidate);
                if (!await query.AnyAsync(predicate, cancellationToken))
                {
                    result = candidate;
                }
            }
            return result;
        }

        public Task<string> GenerateAsync<T>(
            IQueryable<T> directQuery,
            IdNameDescription idNameDescription,
            string name,
            object? indexValues = default,
            CancellationToken cancellationToken = default)
            where T : class, IHasIdName
        {
            cancellationToken.ThrowIfCancellationRequested();
            IQueryable<T> query;
            // collect constraints
            if (0 == idNameDescription.AdditionalIndexProperties.Length)
            {
                query = directQuery;
            }
            else
            {
                if (null == indexValues)
                {
                    throw new ArgumentNullException(
                        nameof(indexValues),
                        $"Following index values should be specified: {String.Join(",", idNameDescription.AdditionalIndexProperties.Select(p => p.Name))}"
                    );
                }
                var eArg = Expression.Parameter(typeof(T));
                var predicates = new List<Expression>(idNameDescription.AdditionalIndexProperties.Length);
                var props = indexValues.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                foreach (var indexProperty in idNameDescription.AdditionalIndexProperties)
                {
                    var prop = props.FirstOrDefault(p => p.Name == indexProperty.Name);
                    if (null == prop)
                    {
                        throw new InvalidOperationException($"Required index property {indexProperty.Name} was not specified.");
                    }
                    predicates.Add(Expression.Equal(
                        Expression.Property(eArg, indexProperty),
                        BoxedContstant(prop.GetValue(indexValues), indexProperty.PropertyType)
                    ));
                }
                var compositePredicate = Expression.Lambda<Func<T, bool>>(predicates.Aggregate((a, b) => Expression.AndAlso(a, b)), eArg);
                query = directQuery.Where(compositePredicate);
            }
            return GenerateAsync(query, idNameDescription, name, cancellationToken);
        }

        public Task<string> GenerateAsync<T>(
            IQueryable<T> directQuery,
            IdNameDescription idNameDescription,
            T entity,
            CancellationToken cancellationToken = default)
            where T : class, IHasIdName
        {
            cancellationToken.ThrowIfCancellationRequested();
            IQueryable<T> query;
            // collect constraints
            if (0 == idNameDescription.AdditionalIndexProperties.Length)
            {
                query = directQuery;
            }
            else
            {
                var eArg = Expression.Parameter(typeof(T));

                var allPredicates = idNameDescription.AdditionalIndexProperties
                    .Select(p => Expression.Equal(
                        Expression.Property(eArg, p),
                        Expression.Property(Expression.Constant(entity), p)
                    ))
                    .Aggregate((a, b) => Expression.AndAlso(a, b));
                var predicate = Expression.Lambda<Func<T, bool>>(allPredicates, eArg);
                query = directQuery.Where(predicate);
            }
            var nameSource = idNameDescription.NameSourceProperty.GetValue(entity, null) as string
                ?? throw new InvalidOperationException($"Unable to get source name for {typeof(T).Name}.");
            return GenerateAsync(query, idNameDescription, nameSource, cancellationToken);
        }
    }
}