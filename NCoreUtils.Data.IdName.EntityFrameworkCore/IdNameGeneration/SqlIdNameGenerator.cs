using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NCoreUtils.Text;

namespace NCoreUtils.Data.IdNameGeneration
{
    public class SqlIdNameGenerator : IIdNameGenerator
    {
        sealed class Box<T>
        {
            public T Value;

            public Box(T value) => Value = value;
        }

        static Expression BoxedConstant<T>(T value)
        {
            var box = new Box<T>(value);
            return Expression.Field(Expression.Constant(box), nameof(Box<T>.Value));
        }

        static Expression BoxedContstant(object value, Type type)
        {
            var boxType = typeof(Box<>).MakeGenericType(type);
            var field = boxType.GetField(nameof(Box<int>.Value));
            var box = Activator.CreateInstance(boxType, value);
            field.SetValue(box, value);
            return Expression.Field(Expression.Constant(box, boxType), field);
        }

        readonly IdNameGenerationInitialization _initialization;

        readonly DbContext _dbContext;

        readonly ISimplifier _simplifier;

        public SqlIdNameGenerator(IdNameGenerationInitialization initialization, DbContext dbContext, ISimplifier simplifier)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _simplifier = simplifier ?? throw new ArgumentNullException(nameof(simplifier));
            _initialization = initialization ?? throw new ArgumentNullException(nameof(initialization));
        }

        async Task<string> GenerateAsync<T>(
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
            var efAnnotation = _dbContext.Model.FindAnnotation(Annotations.GetIdNameFunction);
            if (null == efAnnotation)
            {
                return await new IdNameGenerator(_simplifier).GenerateAsync(query, idNameDescription, name, cancellationToken);
            }
            var annotation = Annotations.GetIdNameFunctionAnnotation.Unpack(efAnnotation.Value as string);


            cancellationToken.ThrowIfCancellationRequested();
            var decomposition = idNameDescription.Decomposer.Decompose(name);
            var getIdNameSuffix = _initialization.GetGetIdNameSuffixMethod();
            var simplified = _simplifier.Simplify(decomposition.MainPart);
            var regex = "^" + Regex.Escape(decomposition.Rebuild((simplified), "ŁŁŁ")).Replace("-ŁŁŁ", "(-[0-9]+)?") + "$";
            var def = decomposition.Rebuild(simplified, null);

            var eArg = Expression.Parameter(typeof(T));
            var eIfThenElse = Expression.Condition(
                Expression.Equal(
                    Expression.Property(eArg, idNameDescription.IdNameProperty),
                    BoxedConstant(def)
                ),
                BoxedConstant(string.Empty),
                Expression.Call(getIdNameSuffix, Expression.Property(eArg, idNameDescription.IdNameProperty), BoxedConstant(regex))
            );
            var selector = Expression.Lambda<Func<T, string>>(eIfThenElse, eArg);
            var rawSuffixes = await NCoreUtils.Linq.QueryableExtensions.ToListAsync(
                (query ?? _dbContext.Set<T>()).Select(selector).Where(s => s != null),
                cancellationToken);
            string idName;
            if (0 == rawSuffixes.Count)
            {
                idName = def;
            }
            else
            {
                var suffixes = new HashSet<int>(rawSuffixes.Select(raw => string.IsNullOrEmpty(raw)
                    ? 0
                    : int.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture)));
                var suffix = -1;
                for (var i = 0; -1 == suffix; ++i)
                {
                    if (!suffixes.Contains(i))
                    {
                        suffix = i;
                    }
                }
                idName = decomposition.Rebuild(simplified, suffix == 0 ? null : suffix.ToString());
            }
            return idName;
        }

        // FIXME: refactor

        public Task<string> GenerateAsync<T>(
            IQueryable<T> directQuery,
            IdNameDescription idNameDescription,
            string name,
            object indexValues = null,
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
            return GenerateAsync<T>(query, idNameDescription, name, cancellationToken);
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
            return GenerateAsync<T>(query, idNameDescription, (string)idNameDescription.NameSourceProperty.GetValue(entity, null), cancellationToken);
        }
    }

    public class SqlIdNameGenerator<TDbContext> : SqlIdNameGenerator
        where TDbContext : DbContext
    {
        public SqlIdNameGenerator(IdNameGenerationInitialization initialization, TDbContext dbContext, ISimplifier simplifier)
            : base(initialization, dbContext, simplifier)
        { }
    }
}