using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Linq;
using NCoreUtils.Text;

namespace NCoreUtils.Data.IdNameGeneration
{
    public class IdNameGenerator : IIdNameGenerator
    {
        class Box<T>
        {
            #pragma warning disable 0649
            public T Value;
            #pragma warning restore 0649
        }

        static Expression BoxedContstant(object value, Type type)
        {
            var boxType = typeof(Box<>).MakeGenericType(type);
            var field = boxType.GetField(nameof(Box<int>.Value));
            var box = Activator.CreateInstance(boxType, true);
            field.SetValue(box, value);
            return Expression.Field(Expression.Constant(box, boxType), field);
        }

        readonly ISimplifier _simplifier;

        public IdNameGenerator(ISimplifier simplifier)
        {
            _simplifier = simplifier ?? throw new ArgumentNullException(nameof(simplifier));
        }

        internal async Task<string> GenerateAsync<T>(
            IQueryable<T> query,
            IdNameDescription idNameDescription,
            string name,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : class, IHasIdName
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            cancellationToken.ThrowIfCancellationRequested();
            var decomposition = idNameDescription.Decomposer.Decompose(name);
            var simplified = _simplifier.Simplify(decomposition.MainPart);
            string result = null;
            for (var index = 0; result == null; ++index)
            {
                var candidate = decomposition.Rebuild(simplified, 0 == index ? null : index.ToString());
                var predicate = LinqExtensions.ReplaceExplicitProperties<Func<T, bool>>(e => e.IdName == candidate);
                if (false == await query.AnyAsync(predicate, cancellationToken))
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
            object indexValues = null,
            CancellationToken cancellationToken = default(CancellationToken))
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
            return GenerateAsync<T>(query, idNameDescription, name, cancellationToken);
        }

        public Task<string> GenerateAsync<T>(
            IQueryable<T> directQuery,
            IdNameDescription idNameDescription,
            T entity,
            CancellationToken cancellationToken = default(CancellationToken))
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
            return GenerateAsync<T>(query, idNameDescription, (string)idNameDescription.NameSourceProperty.GetValue(entity, null), cancellationToken);
        }
    }
}