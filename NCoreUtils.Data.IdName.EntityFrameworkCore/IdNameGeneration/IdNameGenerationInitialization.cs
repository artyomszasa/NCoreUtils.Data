using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace NCoreUtils.Data.IdNameGeneration
{
    public class IdNameGenerationInitialization
    {
        static readonly object _sync = new object();

        static readonly ConcurrentDictionary<string, MethodInfo> _initializedFunctions = new ConcurrentDictionary<string, MethodInfo>();

        readonly DbContext _dbContext;

        readonly IStoredProcedureGenerator _generator;

        public IdNameGenerationInitialization(DbContext dbContext, IStoredProcedureGenerator generator = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            if (null == generator)
            {
                if (!BuiltInStoredProcedureGenerators.TryGetGenerator(dbContext.Database.ProviderName, out generator))
                {
                    throw new InvalidOperationException($"No default generator exists for provider name {dbContext.Database.ProviderName}.");
                }
            }
            _generator = generator;
        }

        [ExcludeFromCodeCoverage]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfNoAnnotation(string packedAnnotation)
        {
            if (null == packedAnnotation)
            {
                throw new InvalidOperationException("GetIdFunction annotation not defined on the context.");
            }
        }

        public MethodInfo GetGetIdNameSuffixMethod()
        {
            var packedAnnotation = _dbContext.Model.FindAnnotation(Annotations.GetIdNameFunction).Value as string;
            ThrowIfNoAnnotation(packedAnnotation);
            if (_initializedFunctions.TryGetValue(packedAnnotation, out var method))
            {
                return method;
            }
            lock (_sync)
            {
                return _initializedFunctions.GetOrAdd(packedAnnotation, raw =>
                {
                    var annotation = Annotations.GetIdNameFunctionAnnotation.Unpack(raw);
                    var sql = _generator.Generate(annotation.FunctionSchema, annotation.FunctionName);
                    _dbContext.Database.ExecuteSqlCommand(sql);
                    return annotation.Method;
                });
            }
        }
    }

    public class IdNameGenerationInitialization<TDbContext> : IdNameGenerationInitialization
        where TDbContext : DbContext
    {
        public IdNameGenerationInitialization(TDbContext dbContext, IStoredProcedureGenerator generator = null) : base(dbContext, generator) { }
    }
}