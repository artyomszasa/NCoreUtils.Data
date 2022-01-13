using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NCoreUtils.Data.IdNameGeneration;

namespace NCoreUtils.Data
{
    public static class ModelBuilderIdNameGenerationExtensions
    {
        static readonly object _sync = new();
        static readonly string _assemblyName;
        static long _valueSupply;
        static readonly ConstructorInfo _dbFuntionAttributeCtor;
        static AssemblyBuilder? _assemblyBuilder = null;
        static ModuleBuilder? _moduleBuilder = null;

        static ModelBuilderIdNameGenerationExtensions()
        {
            _assemblyName = typeof(ModelBuilderIdNameGenerationExtensions).Namespace + "IdNameGeneration.Generated";
            _dbFuntionAttributeCtor = typeof(DbFunctionAttribute).GetConstructors().First(m => m.GetParameters().Length == 2);
        }

        // static (string functionName, MethodInfo method) GenerateIdNameGenerationMethod(string schema)
        // {
        //     if (null == _assemblyBuilder)
        //     {
        //         lock (_sync)
        //         {
        //             if (null == _assemblyBuilder)
        //             {
        //                 var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(_assemblyName), AssemblyBuilderAccess.Run);
        //                 _moduleBuilder = assemblyBuilder.DefineDynamicModule(_assemblyName);
        //                 _assemblyBuilder = assemblyBuilder;
        //             }
        //         }
        //     }
        //     var uid = Interlocked.Increment(ref _valueSupply);
        //     var typeBuilder = _moduleBuilder.DefineType($"DbFunctions_{uid}");
        //     var methodBuilder = typeBuilder.DefineMethod("GenerateIdName", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(string), new [] { typeof(string) });
        //     var functionName = $"generate_idname_for_{uid}";
        //     methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(_dbFuntionAttributeCtor, new object[] { functionName, schema }));
        //     {
        //         var il = methodBuilder.GetILGenerator();
        //         il.Emit(OpCodes.Ldstr, $"{methodBuilder.Name} may not be called at runtime.");
        //         il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new [] { typeof(string) }));
        //         il.Emit(OpCodes.Throw);
        //     }
        //     return (functionName, typeBuilder.CreateTypeInfo().AsType().GetMethod(methodBuilder.Name));
        // }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(InvalidOperationException))]
        private static (string functionName, MethodInfo method) GenerateGetIdNameSuffixMethod(string? schema)
        {
            if (null == _assemblyBuilder)
            {
                lock (_sync)
                {
                    if (null == _assemblyBuilder)
                    {
                        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(_assemblyName), AssemblyBuilderAccess.Run);
                        _moduleBuilder = assemblyBuilder.DefineDynamicModule(_assemblyName);
                        Annotations._generatedAssembly = assemblyBuilder;

                        _assemblyBuilder = assemblyBuilder;
                    }
                }
            }
            var uid = Interlocked.Increment(ref _valueSupply);
            var typeBuilder = _moduleBuilder!.DefineType($"DbFunctions_{uid}");
            var methodBuilder = typeBuilder.DefineMethod("GetIdNameSuffix", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(string), new [] { typeof(string), typeof(string) });
            methodBuilder.DefineParameter(1, ParameterAttributes.None, "source");
            methodBuilder.DefineParameter(2, ParameterAttributes.None, "pattern");
            var functionName = $"get_idname_suffix_{uid}";
            methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(_dbFuntionAttributeCtor, new object[] { functionName, schema! }));
            {
                var il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldstr, $"{methodBuilder.Name} may not be called at runtime.");
                il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new [] { typeof(string) })!);
                il.Emit(OpCodes.Throw);
            }
            return (functionName, typeBuilder.CreateTypeInfo()!.AsType().GetMethod(methodBuilder.Name)!);
        }

        public static ModelBuilder HasGetIdNameSuffixFunction(this ModelBuilder builder, string? schema = default)
        {
            var (functionName, method) = GenerateGetIdNameSuffixMethod(schema);
            builder
                .HasAnnotation(Annotations.GetIdNameFunction, new Annotations.GetIdNameFunctionAnnotation(method, schema!, functionName).Pack())
                .HasDbFunction(method);
            return builder;
        }

        /// <summary>
        /// <para>NOTE: must be called after setting table schema/name and column names.</para>
        /// </summary>
        /// <param name="builder">Entity type builder.</param>
        /// <param name="idNameSelector">Id name selector.</param>
        /// <param name="nameSelector">Source name selector.</param>
        /// <param name="additionalIndexPropertySelector">Additional index property selector.</param>
        /// <param name="maxLength">Maximum length of the id name field.</param>
        /// <typeparam name="T">Related entity type.</typeparam>
        /// <returns>Entity type builder passed to the method.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "All related types should be preserved though entities.")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Tuple<,>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Tuple<,,>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Tuple<,,,>))]
        public static EntityTypeBuilder<T> HasIdName<T>(
            this EntityTypeBuilder<T> builder,
            Expression<Func<T, string>> idNameSelector,
            Expression<Func<T, string>> nameSelector,
            Expression<Func<T, object>>? additionalIndexPropertySelector = default,
            int maxLength = 320)
            where T : class
        {
            // var rel = builder.Metadata.Relational();
            var idNameBuilder = builder.Property(idNameSelector).HasMaxLength(maxLength).IsRequired(true).IsUnicode(false);

            var descBuilder = new IdNameDescriptionBuilder<T>()
                .SetIdNameProperty(idNameBuilder.Metadata.PropertyInfo ?? throw new InvalidOperationException($"Could not gete id name property."))
                .SetNameSourceProperty(nameSelector)
                .SetDecompose(DummyStringDecomposition.Decomposer);
            if (null != additionalIndexPropertySelector)
            {
                descBuilder.SetAdditionalIndexProperties(additionalIndexPropertySelector);
            }
            var desc = descBuilder.Build();

            var nameProperty = nameSelector.ExtractProperty();
            var annotation = new Annotations.IdNameSourcePropertyAnnotation(
                sourceNameProperty: desc.NameSourceProperty,
                additionalIndexProperties: desc.AdditionalIndexProperties);
            idNameBuilder
                .HasAnnotation(Annotations.IdNameSourceProperty, annotation.Pack());
            var eArg = Expression.Parameter(typeof(T));
            if (desc.AdditionalIndexProperties.Length > 0)
            {
                var properties = new List<PropertyInfo>{ desc.IdNameProperty };
                properties.AddRange(desc.AdditionalIndexProperties);
                var tupleType = properties.Count switch
                {
                    2 => typeof(Tuple<,>).MakeGenericType(properties.MapToArray(p => p.PropertyType)),
                    3 => typeof(Tuple<,,>).MakeGenericType(properties.MapToArray(p => p.PropertyType)),
                    4 => typeof(Tuple<,,,>).MakeGenericType(properties.MapToArray(p => p.PropertyType)),
                    5 => typeof(Tuple<,,,,>).MakeGenericType(properties.MapToArray(p => p.PropertyType)),
                    _ => throw new InvalidOperationException($"Not supported index property count = {properties.Count}."),
                };
                var eArgs = properties.MapToArray(p => Expression.Property(eArg, p));
                var members = properties.Select((_, i) => (MemberInfo)tupleType.GetProperty($"Item{i + 1}")!).ToArray();
                var selector = Expression.Lambda<Func<T, object?>>(
                    Expression.New(
                        tupleType.GetConstructor(properties.MapToArray(p => p.PropertyType))!,
                        eArgs,
                        members
                    ),
                    eArg
                );
                builder.HasIndex(selector).IsUnique(true);
            }
            else
            {
                builder.HasIndex(Expression.Lambda<Func<T, object?>>(
                    Expression.Property(eArg, idNameBuilder.Metadata.PropertyInfo),
                    eArg
                )).IsUnique(true);
            }
            return builder;
        }

        public static EntityTypeBuilder<T> HasIdName<T>(
            this EntityTypeBuilder<T> builder,
            Expression<Func<T, string>> nameSelector,
            Expression<Func<T, object>>? additionalIndexPropertySelector = default,
            int maxLength = 320)
            where T : class, IHasIdName
        {
            var selector = LinqExtensions.ReplaceExplicitProperties<Func<T, string>>(e => e.IdName);
            return builder.HasIdName(
                selector,
                nameSelector,
                additionalIndexPropertySelector,
                maxLength);
        }

    }
}