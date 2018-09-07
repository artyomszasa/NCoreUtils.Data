using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace NCoreUtils.Data.IdNameGeneration
{
    public class IdNameGenerationInitialization
    {
        static readonly object _sync = new object();
        // static readonly ConcurrentDictionary<Type, MethodInfo> _initializedFunctions = new ConcurrentDictionary<Type, MethodInfo>();

        static volatile MethodInfo _methodInfo = null;

        readonly DbContext _dbContext;

        readonly IStoredProcedureGenerator _generator;

        public IdNameGenerationInitialization(DbContext dbContext, IStoredProcedureGenerator generator = null)
        {
            _dbContext = dbContext;
            if (null == generator)
            {
                if (!BuiltInStoredProcedureGenerators.TryGetGenerator(dbContext.Database.ProviderName, out generator))
                {
                    throw new InvalidOperationException($"No default generator exists for provider name {dbContext.Database.ProviderName}.");
                }
            }
            _generator = generator;
        }

        public MethodInfo GetGetIdNameSuffixMethod()
        {
            if (null != _methodInfo)
            {
                return _methodInfo;
            }
            lock (_sync)
            {
                if (null != _methodInfo)
                {
                    return _methodInfo;
                }
                var annotation = Annotations.GetIdNameFunctionAnnotation.Unpack(_dbContext.Model.FindAnnotation(Annotations.GetIdNameFunction).Value as string);
                var sql = _generator.Generate(annotation.FunctionSchema, annotation.FunctionName);
                _dbContext.Database.ExecuteSqlCommand(sql);
                _methodInfo = annotation.Method;
                return _methodInfo;
            }
        }

        // public MethodInfo GetGenerationMethod(Type entityType)
        // {
        //     if (_initializedFunctions.TryGetValue(entityType, out var result))
        //     {
        //         return result;
        //     }
        //     lock (_sync)
        //     {
        //         if (_initializedFunctions.TryGetValue(entityType, out result))
        //         {
        //             return result;
        //         }
        //         var model = _dbContext.Model.FindEntityType(entityType) ?? throw new InvalidOperationException($"Type {entityType.FullName} is not an entity type.");
        //         var rel = model.Relational();
        //         var idNameProperty = model.GetProperties().First(p => null != p.FindAnnotation(Annotations.IdNameGenerationMethod));
        //         var methodAnnotation = (Annotations.IdNameGenerationMethodAnnotation)idNameProperty.FindAnnotation(Annotations.IdNameGenerationMethod).Value;
        //         var sql = _generator.Generate(methodAnnotation.FunctionSchema, methodAnnotation.FunctionName, rel.Schema, rel.TableName, idNameProperty.Relational().ColumnName);
        //         _dbContext.Database.ExecuteSqlCommand(sql);
        //         _initializedFunctions[entityType] = methodAnnotation.Method;
        //         return methodAnnotation.Method;
        //     }
        // }
    }
    public class IdNameGenerationInitialization<TDbContext> : IdNameGenerationInitialization
        where TDbContext : DbContext
    {
        public IdNameGenerationInitialization(TDbContext dbContext, IStoredProcedureGenerator generator = null) : base(dbContext, generator) { }
    }
}