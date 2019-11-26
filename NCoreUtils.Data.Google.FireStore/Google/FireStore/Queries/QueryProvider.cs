using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.FireStore.Transformations;
using NCoreUtils.Data.Internal;
using FirestoreQuery = Google.Cloud.Firestore.Query;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public class QueryProvider : QueryProviderBase
    {
        static readonly MethodInfo _gmGetDefaultTransformation;

        static readonly ConcurrentDictionary<Type, object> _initialQueryCache = new ConcurrentDictionary<Type, object>();

        static readonly ConcurrentDictionary<Type, object> _defaultTransformationCache = new ConcurrentDictionary<Type, object>();

        static QueryProvider()
        {
            _gmGetDefaultTransformation = typeof(QueryProvider).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .First(m => m.IsGenericMethodDefinition && m.Name == nameof(GetDefaultTransformation));
        }

        static IQuery<T> Cast<T>(IQueryable<T> source)
        {
            if (source is IQuery<T> query)
            {
                return query;
            }
            throw new InvalidOperationException($"Invalid queryable of type {source.GetType()} supplied.");
        }

        static async IAsyncEnumerable<TElement> ExecuteInternal<TElement>(IQuery<TElement> source, FirestoreQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var items = query.StreamAsync();
            using var itemEnumerator = items.GetEnumerator();
            while (await itemEnumerator.MoveNext(cancellationToken))
            {
                yield return source.Convert(itemEnumerator.Current);
            }
        }

        static async IAsyncEnumerable<TElement> ExecuteKeyInternal<TElement>(
            IQuery<TElement> source,
            IReadOnlyList<Condition> conditions,
            DocumentReference documentReference,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var snapshot = await documentReference.GetSnapshotAsync(cancellationToken);
            if (snapshot.Exists && conditions.All(c => c.Eval(snapshot)))
            {
                yield return source.Convert(snapshot);
            }
        }

        readonly DataRepositoryContext _context;

        readonly FirestoreDb _db;

        public QueryProvider(DataRepositoryContext context, FirestoreDb db)
        {
            _context = context;
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        protected internal virtual ref readonly TypeDescriptor GetTypeDescriptor(Type type)
            => ref _context.Model.GetTypeDescriptor(type);

        protected virtual void ResolvePath(Expression expression, ParameterExpression rootParameter, ref TinyList<string> path)
        {
            if (expression is ParameterExpression pexpr && pexpr == rootParameter)
            {
                return;
            }
            if (expression is MemberExpression mexpr && mexpr.Member is PropertyInfo property)
            {
                ResolvePath(mexpr.Expression, rootParameter, ref path);
                var name = GetTypeDescriptor(property.DeclaringType).GetPropertyName(property);
                path.Add(name);
            }
            throw new NotSupportedException($"Not supported expression {expression} while resolving property path.");
        }

        protected virtual IOrderedQueryable<TElement> ApplyOrderBy<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector, QueryOrdering.OrderingDirection direction)
        {
            TinyList<string> pathParts = new TinyList<string>();
            ResolvePath(selector.Body, selector.Parameters[0], ref pathParts);
            var path = pathParts.Join(".");
            return Cast(source).AddOrdering(path, direction);
        }

        protected virtual DirectQuery<T> DoCreateInitialQuery<T>()
        {
            var transformation = GetDefaultTransformation<T>();
            return new DirectQuery<T>(
                this,
                transformation,
                in TinyList<Condition>.Empty,
                in TinyList<QueryOrdering>.Empty,
                0,
                int.MaxValue);
        }

        protected ITransformation<T> CreateDefaultTransformation<T>()
        {
            ref readonly TypeDescriptor typeDescriptor = ref GetTypeDescriptor(typeof(T));
            if (typeDescriptor.IsPolymorphic)
            {
                return new PolymorphicCtorTransformation<T>(
                    new DocumentPropertySource<string>("$type"),
                    typeDescriptor.Dervied
                        .ToDictionary(
                            ty => GetTypeDescriptor(ty).Name,
                            ty => (ITransformation)_gmGetDefaultTransformation.MakeGenericMethod(ty).Invoke(this, new object[0])
                        )
                );
            }
            var idProperty = typeDescriptor.IdProperty;
            var sources = typeDescriptor.Properties.Map(propertyDescriptor =>
            {
                if (idProperty.HasValue && idProperty.Value == propertyDescriptor)
                {
                    return new DocumentKeySource(propertyDescriptor.Name);
                }
                return (IValueSource)Activator.CreateInstance(typeof(DocumentPropertySource<>).MakeGenericType(propertyDescriptor.Property.PropertyType), new object[]
                {
                    propertyDescriptor.Name
                });
            });
            var mapping = typeDescriptor.Properties
                .Zip(sources, (propertyDescriptor, source) => new KeyValuePair<PropertyInfo, IValueSource>(propertyDescriptor.Property, source))
                .ToImmutableDictionary();
            var bindings = typeDescriptor.Properties.Map(propertyDescriptor => propertyDescriptor.Property);

            return new CtorCompositeTransformation<T>(
                sources.All,
                mapping,
                typeDescriptor.Ctor,
                bindings
            );
        }

        protected internal ITransformation<T> GetDefaultTransformation<T>()
        {
            if (_defaultTransformationCache.TryGetValue(typeof(T), out var boxed))
            {
                return (ITransformation<T>)boxed;
            }
            var transformation = CreateDefaultTransformation<T>();
            _defaultTransformationCache[typeof(T)] = transformation;
            return transformation;
        }

        protected internal virtual IAsyncEnumerable<TElement> ExecuteQuery<TElement>(IQuery<TElement> source, CancellationToken cancellationToken)
        {
            if (source.Limit == 0)
            {
                return AsyncEnumerable.Empty<TElement>();
            }
            // if query contains id = xxx condition than it should be treated differently
            var remain = new TinyList<Condition>();
            var idProperty = GetTypeDescriptor(typeof(TElement)).IdProperty?.Name;
            if (null != idProperty && source.Conditions.TryExtract(c => c.Path == idProperty, out var idCondition, in remain))
            {
                if (idCondition.Operation != Condition.Op.EqualTo)
                {
                    throw new InvalidOperationException("Only equality can be checked on keys.");
                }
                var reference = _db.Collection(GetTypeDescriptor(typeof(TElement)).Name).Document((string)idCondition.Value);
                return ExecuteKeyInternal(source, remain.ToList(), reference, cancellationToken);
            }
            FirestoreQuery query = _db.Collection(GetTypeDescriptor(typeof(TElement)).Name);
            // apply conditions
            for (var i = 0; i < source.Conditions.Count; ++i)
            {
                query = source.Conditions[i].Apply(query);
            }
            // apply ordering
            for (var i = 0; i < source.Ordering.Count; ++i)
            {
                var ordering = source.Ordering[i];
                query = ordering.Direction == QueryOrdering.OrderingDirection.Ascending ? query.OrderBy(ordering.Path) : query.OrderByDescending(ordering.Path);
            }
            // apply fields
            query = query.Select(source.GetUsedFields().Prepend("$type").ToArray());
            // execute
            return ExecuteInternal(source, query, cancellationToken);
        }

        protected override IOrderedQueryable<TElement> ApplyOrderBy<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
            => ApplyOrderBy(source, selector, QueryOrdering.OrderingDirection.Ascending);

        protected override IOrderedQueryable<TElement> ApplyOrderByDescending<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
            => ApplyOrderBy(source, selector, QueryOrdering.OrderingDirection.Descending);

        protected override IQueryable<TResult> ApplySelect<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            throw new NotImplementedException();
        }

        protected override IQueryable<TResult> ApplySelect<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
        {
            throw new NotImplementedException();
        }

        protected override IQueryable<TElement> ApplySkip<TElement>(IQueryable<TElement> source, int count)
            => Cast(source).WithOffset(count);

        protected override IQueryable<TElement> ApplyTake<TElement>(IQueryable<TElement> source, int count)
            => Cast(source).WithLimit(count);

        protected override IOrderedQueryable<TElement> ApplyThenBy<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
            => ApplyOrderBy(source, selector, QueryOrdering.OrderingDirection.Ascending);

        protected override IOrderedQueryable<TElement> ApplyThenByDescending<TElement, TKey>(IQueryable<TElement> source, Expression<Func<TElement, TKey>> selector)
            => ApplyOrderBy(source, selector, QueryOrdering.OrderingDirection.Descending);

        protected override IQueryable<TElement> ApplyWhere<TElement>(IQueryable<TElement> source, Expression<Func<TElement, bool>> predicate)
        {
            var conditions = new TinyList<Condition>();
            predicate.ExtractConditions(GetTypeDescriptor, ref conditions);
            return Cast(source).AddConditions(in conditions);
        }

        protected override IQueryable<TElement> ApplyWhere<TElement>(IQueryable<TElement> source, Expression<Func<TElement, int, bool>> predicate)
        {
            throw new NotSupportedException($"Indexed predicates are not supported.");
        }

        public DirectQuery<T> CreateInitialQuery<T>()
            => (DirectQuery<T>)_initialQueryCache
                .GetOrAdd(typeof(T), _ => DoCreateInitialQuery<T>());

        protected override IQueryable<TResult> ApplyOfType<TElement, TResult>(IQueryable<TElement> source)
        {
            if (source is DirectQuery<TElement> query)
            {
                return new DirectQuery<TResult>(
                    this,
                    GetDefaultTransformation<TResult>(),
                    query.Conditions.CopyAdd(new Condition("$type", Condition.Op.EqualTo, GetTypeDescriptor(typeof(TResult)).Name)),
                    query.Ordering,
                    query.Offset,
                    query.Limit
                );
            }
            throw new NotImplementedException($"OfType should be called prior transformations.");
        }

        protected override Task<bool> ExecuteAll<TElement>(IQueryable<TElement> source, Expression<Func<TElement, bool>> predicate, CancellationToken cancellationToken)
        {
            // FIXME: configure to allow
            throw new InvalidOperationException("All can only be evaluated locally.");
        }

        protected override Task<bool> ExecuteAny<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            // FIXME: configure to allow
            throw new InvalidOperationException("Any can only be evaluated locally.");
        }

        protected override Task<int> ExecuteCount<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            // FIXME: configure to allow
            throw new InvalidOperationException("Count can only be evaluated locally.");
        }

        protected override async Task<TElement> ExecuteFirst<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var items = ExecuteQuery(Cast(source).WithLimit(1));
            await using var enumerator = items.GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync())
            {
                return enumerator.Current;
            }
            throw new InvalidOperationException("Sequence contains no elements.");
        }

        protected override async Task<TElement> ExecuteFirstOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var items = ExecuteQuery(Cast(source).WithLimit(1));
            await using var enumerator = items.GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync())
            {
                return enumerator.Current;
            }
            return default;
        }

        protected override Task<TElement> ExecuteLast<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task<TElement> ExecuteLastOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override async Task<TElement> ExecuteSingle<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var items = ExecuteQuery(Cast(source).WithLimit(2));
            await using var enumerator = items.GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync())
            {
                var result = enumerator.Current;
                if (await enumerator.MoveNextAsync())
                {
                    throw new InvalidOperationException("Sequence contains more than one element.");
                }
                return result;
            }
            throw new InvalidOperationException("Sequence contains no elements.");
        }

        protected override async Task<TElement> ExecuteSingleOrDefault<TElement>(IQueryable<TElement> source, CancellationToken cancellationToken)
        {
            var items = ExecuteQuery(Cast(source).WithLimit(2));
            await using var enumerator = items.GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync())
            {
                var result = enumerator.Current;
                if (await enumerator.MoveNextAsync())
                {
                    throw new InvalidOperationException("Sequence contains more than one element.");
                }
                return result;
            }
            return default;
        }

        protected override IAsyncEnumerable<TElement> ExecuteQuery<TElement>(IQueryable<TElement> source)
            => ExecuteQuery(Cast(source), CancellationToken.None);
    }
}