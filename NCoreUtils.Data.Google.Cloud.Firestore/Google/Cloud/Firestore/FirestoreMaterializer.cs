using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreMaterializer
    {
        protected virtual Func<DocumentSnapshot, T> CompileMaterialization<T>(Expression<Func<DocumentSnapshot, T>> expression)
            => expression.Compile();

        public T Materialize<T>(DocumentSnapshot document, Expression<Func<DocumentSnapshot, T>> selector)
            => CompileMaterialization(selector)(document);

        public IEnumerable<T> Materialize<T>(IEnumerable<DocumentSnapshot> documents, Expression<Func<DocumentSnapshot, T>> selector)
            => documents.Select(CompileMaterialization(selector));

        public IAsyncEnumerable<T> Materialize<T>(IAsyncEnumerable<DocumentSnapshot> documents, Expression<Func<DocumentSnapshot, T>> selector)
            => documents.Select(CompileMaterialization(selector));
    }
}