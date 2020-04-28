using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Model;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public abstract class FirestoreDataRespository : IDataRepository
    {
        IDataRepositoryContext IDataRepository.Context => Context;

        protected FirestoreModel Model { get; }

        protected abstract DataEntity Entity { get; }

        public abstract Type ElementType { get; }

        public FirestoreDataRepositoryContext Context { get; }

        public FirestoreQueryProvider QueryProvider { get; }

        public FirestoreDataRespository(
            FirestoreDataRepositoryContext context,
            FirestoreQueryProvider queryProvider,
            FirestoreModel model)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            QueryProvider = queryProvider ?? throw new ArgumentNullException(nameof(queryProvider));
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        protected virtual Dictionary<string, object> PopulateDTO(Type type, object data)
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
                if (FirestoreModel._primitiveTypes.Contains(prop.Property.PropertyType))
                {
                    d[prop.Name] = prop.Property.GetValue(data, null);
                }
                // FIMXE: collections
                else
                {
                    // fallback to nested entity
                    d[prop.Name] = PopulateDTO(prop.Property.PropertyType, prop.Property.GetValue(data, null));
                }
            }
            return d;
        }
    }

    public class FirestoreDataRepository<TData> : FirestoreDataRespository, IDataRepository<TData, string>
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
                var docref = Context.Db.Collection(Entity.Name).Document();
                var data = PopulateDTO(typeof(TData), item);
                var res = await docref.CreateAsync(data, cancellationToken);
                return docref.Id;
            }
            else
            {
                return await tx.ExecuteAsync(tx =>
                {
                    var docref = tx.Database.Collection(Entity.Name).Document();
                    var data = PopulateDTO(typeof(TData), item);
                    tx.Create(docref, data);
                    return Task.FromResult(docref.Id);
                });
            }
        }

        protected virtual async Task<string> UpdateAsync(TData item, CancellationToken cancellationToken = default)
        {
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

        public virtual Task<TData> LookupAsync(string id, CancellationToken cancellationToken = default)
            => ((FirestoreQuery<TData>)Items)
                .AddCondition(new FirestoreCondition(FieldPath.DocumentId, FirestoreCondition.Op.EqualTo, id))
                .FirstOrDefaultAsync(cancellationToken);

        public virtual async Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                var id = await InsertAsync(item, cancellationToken);
                return await LookupAsync(id, cancellationToken);
            }
            else
            {
                await UpdateAsync(item, cancellationToken);
                return item;
            }
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