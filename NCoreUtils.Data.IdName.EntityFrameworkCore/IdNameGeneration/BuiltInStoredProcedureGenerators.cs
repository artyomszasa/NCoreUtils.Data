using System;
using System.Collections.Concurrent;

namespace NCoreUtils.Data.IdNameGeneration
{
    static class BuiltInStoredProcedureGenerators
    {
        static readonly ConcurrentDictionary<Type, IStoredProcedureGenerator> _cache = new ConcurrentDictionary<Type, IStoredProcedureGenerator>();

        static IStoredProcedureGenerator CreateGenerator(Type type)
        {
            if (_cache.TryGetValue(type, out var generator))
            {
                return generator;
            }
            return _cache.GetOrAdd(type, ty => (IStoredProcedureGenerator)Activator.CreateInstance(ty, true));
        }

        public static bool TryGetGenerator(string providerName, out IStoredProcedureGenerator generator)
        {
            switch (providerName)
            {
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    generator = CreateGenerator(typeof(PostgresStoredProcedureGenerator));
                    return true;
            }
            generator = default(IStoredProcedureGenerator);
            return false;
        }
    }
}