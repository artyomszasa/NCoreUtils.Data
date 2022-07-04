using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Model;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public abstract class FirestoreDataRepository : IDataRepository
    {
        IDataRepositoryContext IDataRepository.Context => Context;

        protected FirestoreConverter Converter { get; }

        protected FirestoreModel Model { get; }

        protected abstract DataEntity Entity { get; }

        public abstract Type ElementType { get; }

        public FirestoreDataRepositoryContext Context { get; }

        public FirestoreQueryProvider QueryProvider { get; }

        public FirestoreDataRepository(
            FirestoreDataRepositoryContext context,
            FirestoreQueryProvider queryProvider,
            FirestoreModel model)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            QueryProvider = queryProvider ?? throw new ArgumentNullException(nameof(queryProvider));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Converter = model.Converter;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "All members of data entity has preserved types.")]
        protected virtual Dictionary<string, object> PopulateDTO(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
            object data)
        {
            if (!Model.TryGetDataEntity(type, out var entity))
            {
                throw new InvalidOperationException($"Unable to populate data for {type} as it has not been registered.");
            }
            var d = new Dictionary<string, object>();
            var key = entity.Key;
            foreach (var prop in entity.Properties)
            {
                if (key != null && key.Count == 1 && key[0].Property.Equals(prop.Property))
                {
                    continue;
                }
                var value = Converter.ConvertToValue(prop.Property.GetValue(data, null), prop.Property.PropertyType);
                d[prop.Name] = value;
            }
            return d;
        }
    }

    public class FirestoreDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TData>
        : FirestoreDataRepository, IDataRepository<TData, string>
        where TData : IHasId<string>
    {
        IDataRepositoryContext IDataRepository.Context => Context;

        protected override DataEntity Entity { get; }

        public virtual IQueryable<TData> Items => QueryProvider.CreateQueryable<TData>();

        public override Type ElementType => typeof(TData);

        public FirestoreDataRepository(
            FirestoreDataRepositoryContext context,
            FirestoreQueryProvider queryProvider,
            FirestoreModel model)
            : base(context, queryProvider, model)
        {
            if (!model.TryGetDataEntity(typeof(TData), out var entity))
            {
                throw new InvalidOperationException($"Unable to create firestore data respository for {typeof(TData)} as it has not been registered.");
            }
            Entity = entity;
        }

        protected virtual async Task<string> InsertAsync(TData item, CancellationToken cancellationToken = default)
        {
            var tx = Context.CurrentTransaction;
            if (tx is null)
            {
                // ID may be user-defined
                var docref = string.IsNullOrEmpty(item.Id)
                    ? Context.Db.Collection(Entity.Name).Document()
                    : Context.Db.Collection(Entity.Name).Document(item.Id);
                var data = PopulateDTO(typeof(TData), item);
                var res = await docref.CreateAsync(data, cancellationToken);
                return docref.Id;
            }
            else
            {
                return await tx.ExecuteAsync(tx =>
                {
                    // ID may be user-defined
                    var docref = string.IsNullOrEmpty(item.Id)
                        ? tx.Database.Collection(Entity.Name).Document()
                        : tx.Database.Collection(Entity.Name).Document(item.Id);
                    var data = PopulateDTO(typeof(TData), item);
                    tx.Create(docref, data);
                    return Task.FromResult(docref.Id);
                });
            }
        }

        protected virtual async Task<string> UpdateAsync(TData item, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                throw new InvalidOperationException($"Trying to update entity without valid id.");
            }
            var tx = Context.CurrentTransaction;
            if (tx is null)
            {
                var docref = Context.Db.Collection(Entity.Name).Document(item.Id);
                var data = PopulateDTO(typeof(TData), item);
                await docref.SetAsync(data, SetOptions.Overwrite, cancellationToken);
                return docref.Id;
            }
            else
            {
                return await tx.ExecuteAsync(tx =>
                {
                    var docref = tx.Database.Collection(Entity.Name).Document();
                    var data = PopulateDTO(typeof(TData), item);
                    tx.Set(docref, data, SetOptions.Overwrite);
                    return Task.FromResult(docref.Id);
                });
            }
        }

        /// <summary>
        /// By default insert is performed if entity has no valid ID. Overriding this method allows replacing this
        /// behaviour.
        /// </summary>
        /// <param name="item">Item being persisted.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>
        /// <c>true</c> if insert operation shpuld be performed, <c>false</c> otherwise.
        /// </returns>
        protected virtual ValueTask<bool> ShouldInsert(TData item, CancellationToken cancellationToken)
            => new(string.IsNullOrEmpty(item.Id));

        public virtual Task<TData?> LookupAsync(string id, CancellationToken cancellationToken = default)
            => ((FirestoreQuery<TData>)Items)
                .AddCondition(new FirestoreCondition(
                    FieldPath.DocumentId,
                    FirestoreCondition.Op.EqualTo,
                    Context.Db.Collection(Entity.Name).Document(id)
                ))
                .FirstOrDefaultAsync(cancellationToken)!;

        public virtual async Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default)
        {
            string id;
            if (await ShouldInsert(item, cancellationToken))
            {
                id = await InsertAsync(item, cancellationToken);
            }
            else
            {
                id = await UpdateAsync(item, cancellationToken);
            }
            return (await LookupAsync(id, cancellationToken))!;
        }

        public virtual Task RemoveAsync(TData item, bool force = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                throw new InvalidOperationException("Unable to remove entity without id.");
            }
            var tx = Context.CurrentTransaction;
            if (tx is null)
            {
                var docref = Context.Db.Collection(Entity.Name).Document(item.Id);
                return docref.DeleteAsync(cancellationToken: cancellationToken);
            }
            return tx.ExecuteAsync(tx =>
            {
                var docref = tx.Database.Collection(Entity.Name).Document(item.Id);
                tx.Delete(docref);
                return Task.CompletedTask;
            });
        }
    }
}