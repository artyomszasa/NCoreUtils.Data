using System;
using System.Text.RegularExpressions;

namespace NCoreUtils.Data.Google.FireStore.Builders
{
    public abstract class NamingConvention
    {
        static readonly char[] _dash = new char[] { '_' };

        static readonly Regex _uppercase = new Regex("[A-Z]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        static string Capitalize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            Span<char> chars = stackalloc char[input.Length];
            input.AsSpan().CopyTo(chars);
            chars[0] = char.ToUpperInvariant(chars[0]);
            return chars.ToString();
        }

        sealed class CamelCaseNamingConvention : NamingConvention
        {
            public override string Consolidate(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Property name must not be empty.", nameof(name));
                }
                var parts = _uppercase.Replace(name, m => "_" + m.Value.ToLowerInvariant()).Split(_dash, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 1; i < parts.Length; ++i)
                {
                    parts[i] = Capitalize(parts[i]);
                }
                return string.Join(string.Empty, parts);
            }
        }

        sealed class PascalCaseNamingConvention : NamingConvention
        {
            public override string Consolidate(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Property name must not be empty.", nameof(name));
                }
                var parts = _uppercase.Replace(name, m => "_" + m.Value.ToLowerInvariant()).Split(_dash, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < parts.Length; ++i)
                {
                    parts[i] = Capitalize(parts[i]);
                }
                return string.Join(string.Empty, parts);
            }
        }

        sealed class SnakeCaseNamingConvention : NamingConvention
        {
            public override string Consolidate(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Property name must not be empty.", nameof(name));
                }
                var parts = _uppercase.Replace(name, m => "_" + m.Value.ToLowerInvariant()).Split(_dash, StringSplitOptions.RemoveEmptyEntries);
                return string.Join("_", parts);
            }
        }

        public static NamingConvention CamelCase { get; } = new CamelCaseNamingConvention();

        public static NamingConvention PascalCase { get; } = new PascalCaseNamingConvention();

        public static NamingConvention SnakeCase { get; } = new SnakeCaseNamingConvention();

        public abstract string Consolidate(string name);
    }
}