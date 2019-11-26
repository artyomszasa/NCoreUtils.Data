using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.FireStore.Transformations;

namespace NCoreUtils.Data.Google.FireStore
{
    public class DataRepository<TData> : IDataRepository<TData, string>
        where TData : IHasId<string>
    {
        readonly string _collectionName;

        IDataRepositoryContext IDataRepository.Context => Context;

        public IQueryable<TData> Items => Context.Provider.CreateInitialQuery<TData>();

        public Type ElementType => typeof(TData);

        public DataRepositoryContext Context { get; }

        public DataRepository(DataRepositoryContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _collectionName = context.Provider.GetTypeDescriptor(typeof(TData)).Name;
        }

        public async Task<TData> LookupAsync(string id, CancellationToken cancellationToken = default)
        {
            switch (Context.CurrentTransaction)
            {
                case null:
                    var snapshot = await Context.Database.Collection(_collectionName).Document(id).GetSnapshotAsync(cancellationToken);
                    return snapshot.Exists
                        ? Context.Provider.GetDefaultTransformation<TData>().GetValue(snapshot)
                        : default;
                case DataTransaction tx:
                    return await tx.ExecuteAsync(async transaction =>
                    {
                        var snapshot = await transaction.Database.Collection(_collectionName).Document(id).GetSnapshotAsync(cancellationToken);
                        return snapshot.Exists
                            ? Context.Provider.GetDefaultTransformation<TData>().GetValue(snapshot)
                            : default;
                    });
            }
        }

        public Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default)
        {
            ref readonly TypeDescriptor typeDescriptor = ref Context.Provider.GetTypeDescriptor(typeof(TData));
            var collection = Context.Database.Collection(typeDescriptor.Name);
            var documentReference = string.IsNullOrEmpty(item.Id) ? collection.Document() : collection.Document(item.Id);
            var key = documentReference.Id;
            var idProperty = typeDescriptor.IdProperty;
            var data = new DocumentConverter(Context.Provider.GetTypeDescriptor).PopulateValue(item);
            var transformation = Context.Provider.GetDefaultTransformation<TData>();
            return PersistAndReconvert(transformation, Context.CurrentTransaction, data, documentReference, idProperty, key, cancellationToken);

            static async Task<TData> PersistAndReconvert(
                ITransformation<TData> transformation,
                DataTransaction tx,
                object data,
                DocumentReference documentReference,
                PropertyDescriptor? idProperty,
                string key,
                CancellationToken cancellationToken)
            {
                if (tx is null)
                {
                    await documentReference.SetAsync(data, cancellationToken: cancellationToken);
                }
                else
                {
                    await tx.ExecuteAsync(transaction =>
                    {
                        transaction.Set(documentReference, data);
                        return Task.CompletedTask;
                    });
                }
                if (idProperty.HasValue && data is IDictionary<string, object> ddata)
                {
                    ddata[idProperty.Value.Name] = key;
                }
                return transformation.GetValue(data);
            }
        }

        public async Task RemoveAsync(TData item, bool force = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                throw new InvalidOperationException($"Unable to remove entity without id.");
            }
            // FIMXE: state remove (force arg)
            switch (Context.CurrentTransaction)
            {
                case null:
                    await Context.Database.Collection(_collectionName).Document(item.Id).DeleteAsync();
                    break;
                case DataTransaction tx:
                    await tx.ExecuteAsync(transaction =>
                    {
                        transaction.Delete(transaction.Database.Collection(_collectionName).Document(item.Id));
                        return Task.CompletedTask;
                    });
                    break;
            }
        }
    }
}