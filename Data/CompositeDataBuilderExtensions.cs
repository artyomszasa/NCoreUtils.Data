using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Contains extensions for <see cref="NCoreUtils.Data.ICompositeDataBuilder" />.
    /// </summary>
    public static class CompositeDataBuilderExtensions
    {
        /// <summary>
        /// Default attribute handlers. Handles the following attributes:
        /// <para>
        /// <see cref="T:System.ComponentModel.DataAnnotations.RequiredAttribute" /> - when present adds
        /// <see cref="T:NCoreUtils.Data.RequiredFlag" /> to the composite data.
        /// </para>
        /// </summary>
        public static ImmutableArray<Action<Attribute, ICompositeDataBuilder, Action>> DefaultAttributeHandlers { get; private set; }
        static CompositeDataBuilderExtensions()
        {
            var hs = new List<Action<Attribute, ICompositeDataBuilder, Action>>();
            hs.Add(MatchAttribute<RequiredAttribute>((_, builder) => builder.Required()));
        }
        static Func<ICompositeData, IPartialData> CreateFactory<TPartialData>() where TPartialData : IPartialData
        {
            return compositeData => {
                var services = new ServiceCollection();
                services.AddSingleton<ICompositeData>(compositeData);
                services.AddSingleton(compositeData.GetType(), compositeData);
                var provider = services.BuildServiceProvider();
                return ActivatorUtilities.CreateInstance<TPartialData>(provider);
            };
        }
        static Action<Attribute, ICompositeDataBuilder, Action> MatchAttribute<T>(Action<T, ICompositeDataBuilder> action) where T : Attribute
        {
            return (attr, builder, next) => {
                if (attr is T match)
                {
                    action(match, builder);
                }
                else
                {
                    next();
                }
            };
        }
        /// <summary>
        /// Adds partial data instance to the builder if not present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        public static bool TryAdd(this ICompositeDataBuilder builder, Type dataType, IPartialData instance)
            => builder.TryAdd(dataType, _ => instance);
        /// <summary>
        /// Adds partial data factory to the builder if not present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        public static bool TryAdd<TPartialData>(this ICompositeDataBuilder builder, Func<ICompositeData, TPartialData> factory) where TPartialData : IPartialData
            => builder.TryAdd(typeof(TPartialData), compositeData => factory(compositeData));
        /// <summary>
        /// Adds partial data instance to the builder if not present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        public static bool TryAdd<TPartialData>(this ICompositeDataBuilder builder, TPartialData instance) where TPartialData : IPartialData
            => builder.TryAdd(typeof(TPartialData), instance);
        /// <summary>
        /// Adds partial data factory to the builder if not present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        public static bool TryAdd<TPartialData>(this ICompositeDataBuilder builder) where TPartialData : IPartialData
            => builder.TryAdd(typeof(TPartialData), CreateFactory<TPartialData>());
        /// <summary>
        /// Replaces partial data instance to the builder if present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        public static bool TryReplace(this ICompositeDataBuilder builder, Type dataType, IPartialData instance)
            => builder.TryReplace(dataType, _ => instance);
        /// <summary>
        /// Replaces partial data factory to the builder if present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        public static bool TryReplace<TPartialData>(this ICompositeDataBuilder builder, Func<ICompositeData, TPartialData> factory) where TPartialData : IPartialData
            => builder.TryReplace(typeof(TPartialData), compositeData => factory(compositeData));
        /// <summary>
        /// Replaces partial data instance to the builder if present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        public static bool TryReplace<TPartialData>(this ICompositeDataBuilder builder, TPartialData instance) where TPartialData : IPartialData
            => builder.TryReplace(typeof(TPartialData), instance);
        /// <summary>
        /// Replaces partial data factory to the builder if present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        public static bool TryReplace<TPartialData>(this ICompositeDataBuilder builder) where TPartialData : IPartialData
            => builder.TryReplace(typeof(TPartialData), CreateFactory<TPartialData>());
        /// <summary>
        /// Adds partial data factory to the builder if not present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <param name="throwIfPresent">Whether to throw exception if partial data has already been added.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// Thrown if partial data has already been added.
        /// </exception>
        public static ICompositeDataBuilder Add(this ICompositeDataBuilder builder, Type dataType, Func<ICompositeData, IPartialData> factory, bool throwIfPresent = false)
        {
            if (!builder.TryAdd(dataType, factory) && throwIfPresent)
            {
                throw new InvalidOperationException($"Partial data of type {dataType} has already been registered.");
            }
            return builder;
        }
        /// <summary>
        /// Adds partial data instance to the builder if not present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <param name="throwIfPresent">Whether to throw exception if partial data has already been added.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// Thrown if partial data has already been added.
        /// </exception>
        public static ICompositeDataBuilder Add(this ICompositeDataBuilder builder, Type dataType, IPartialData instance, bool throwIfPresent = false)
            => builder.Add(dataType, _ => instance, throwIfPresent);
        /// <summary>
        /// Adds partial data factory to the builder if not present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <param name="throwIfPresent">Whether to throw exception if partial data has already been added.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// Thrown if partial data has already been added and <paramref name="throwIfPresent" /> is <c>true</c>.
        /// </exception>
        public static ICompositeDataBuilder Add<TPartialData>(this ICompositeDataBuilder builder, Func<ICompositeData, TPartialData> factory, bool throwIfPresent = false) where TPartialData : IPartialData
            => builder.Add(typeof(TPartialData), compositeData => factory(compositeData), throwIfPresent);
        /// <summary>
        /// Adds partial data instance to the builder if not present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <param name="throwIfPresent">Whether to throw exception if partial data has already been added.</param>
        /// <returns>
        /// <c>true</c> if partial data has been added, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// Thrown if partial data has already been added and <paramref name="throwIfPresent" /> is <c>true</c>.
        /// </exception>
        public static ICompositeDataBuilder Add<TPartialData>(this ICompositeDataBuilder builder, TPartialData instance, bool throwIfPresent = false) where TPartialData : IPartialData
            => builder.Add<TPartialData>(_ => instance, throwIfPresent);
        /// <summary>
        /// Replaces partial data factory to the builder if present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <param name="throwIfNotPresent">
        /// <c>true</c> if exception should be thrown if partial data is not present.
        /// </param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        public static ICompositeDataBuilder Replace(this ICompositeDataBuilder builder, Type dataType, Func<ICompositeData, IPartialData> factory, bool throwIfNotPresent = false)
        {
            if (!builder.TryReplace(dataType, factory) && throwIfNotPresent)
            {
                throw new InvalidOperationException($"Partial data of type {dataType} has not been registered.");
            }
            return builder;
        }
        /// <summary>
        /// Replaces partial data instance to the builder if present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <param name="throwIfNotPresent">
        /// <c>true</c> if exception should be thrown if partial data is not present.
        /// </param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        public static ICompositeDataBuilder Replace(this ICompositeDataBuilder builder, Type dataType, IPartialData instance, bool throwIfNotPresent = false)
            => builder.Replace(dataType, _ => instance, throwIfNotPresent);
        /// <summary>
        /// Replaces partial data factory to the builder if present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <param name="throwIfNotPresent">
        /// <c>true</c> if exception should be thrown if partial data is not present.
        /// </param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        public static ICompositeDataBuilder Replace<TPartialData>(this ICompositeDataBuilder builder, Func<ICompositeData, TPartialData> factory, bool throwIfNotPresent = false) where TPartialData : IPartialData
            => builder.Replace(typeof(TPartialData), compositeData => factory(compositeData), throwIfNotPresent);
        /// <summary>
        /// Replaces partial data instance to the builder if present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <param name="throwIfNotPresent">
        /// <c>true</c> if exception should be thrown if partial data is not present.
        /// </param>
        /// <returns>
        /// <c>true</c> if partial data has been replaced, <c>false</c> otherwise.
        /// </returns>
        public static ICompositeDataBuilder Replace<TPartialData>(this ICompositeDataBuilder builder, TPartialData instance, bool throwIfNotPresent = false) where TPartialData : IPartialData
            => builder.Replace<TPartialData>(_ => instance, throwIfNotPresent);
        /// <summary>
        /// Adds partial data factory to the builder if not present and replaces already registered partial data is
        /// present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <returns>The target builder.</returns>
        public static ICompositeDataBuilder AddOrReplace(this ICompositeDataBuilder builder, Type dataType, Func<ICompositeData, IPartialData> factory)
        {
            if (!builder.TryAdd(dataType, factory))
            {
                builder.Replace(dataType, factory);
            }
            return builder;
        }
        /// <summary>
        /// Adds partial data instance to the builder if not present and replaces already registered partial data is
        /// present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="dataType">Partial data type.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <returns>The target builder.</returns>
        public static ICompositeDataBuilder AddOrReplace(this ICompositeDataBuilder builder, Type dataType, IPartialData instance)
            => builder.AddOrReplace(dataType, _ => instance);
        /// <summary>
        /// Adds partial data factory to the builder if not present and replaces already registered partial data is
        /// present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="factory">Partial data factory.</param>
        /// <returns>The target builder.</returns>
        public static ICompositeDataBuilder AddOrReplace<TPartialData>(this ICompositeDataBuilder builder, Func<ICompositeData, TPartialData> factory) where TPartialData : IPartialData
            => builder.AddOrReplace(typeof(TPartialData), compositeData => factory(compositeData));
        /// <summary>
        /// Adds partial data instance to the builder if not present and replaces already registered partial data is
        /// present.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="instance">Partial data instance.</param>
        /// <returns>The target builder.</returns>
        public static ICompositeDataBuilder AddOrReplace<TPartialData>(this ICompositeDataBuilder builder, TPartialData instance) where TPartialData : IPartialData
            => builder.AddOrReplace<TPartialData>(_ => instance);
        /// <summary>
        /// Applies attributes handled by the <paramref name="handlers" /> to the builder.
        /// </summary>
        /// <param name="builder">Target builder.</param>
        /// <param name="source">Attribute source.</param>
        /// <param name="handlers">Handler collection.</param>
        /// <returns>Target builder.</returns>
        public static ICompositeDataBuilder ApplyAttributes(this ICompositeDataBuilder builder, ICustomAttributeProvider source, IEnumerable<Action<Attribute, ICompositeDataBuilder, Action>> handlers)
        {
            var attributes = source.GetCustomAttributes(true).Select(boxed => boxed as Attribute).Where(attr => null != attr).ToList();
            foreach (var handler in handlers)
            {
                attributes = attributes.Where(attribute => {
                    var unhandled = false;
                    handler(attribute, builder, () => unhandled = true);
                    return unhandled;
                }).ToList();
            }
            return builder;
        }
    }
}