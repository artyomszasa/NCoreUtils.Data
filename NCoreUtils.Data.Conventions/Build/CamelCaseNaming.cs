using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NCoreUtils.Data.Build
{
    public class CamelCaseNaming
    {
        private static readonly Regex _regex = new Regex("([^a-zA-Z0-9]+)([a-zA-Z0-9])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAlpha(char ch)
            => ('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAlphaNum(char ch)
            => IsAlpha(ch) || ('0' <= ch && ch <= '9');

        // FIMXE: handle acronyms
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            if (input.Length < 4096)
            {
                var position = 0;
                Span<char> buffer = stackalloc char[input.Length]; // output will be shorter or of equal size.
                var nonAlphanum = false;
                for (var i = 0; i < input.Length; ++i)
                {
                    var ch = input[i];
                    if (IsAlphaNum(ch))
                    {
                        if (position == 0)
                        {
                            buffer[position++] = char.ToLowerInvariant(ch);
                        }
                        else if (nonAlphanum)
                        {
                            buffer[position++] = char.ToUpperInvariant(ch);
                        }
                        else
                        {
                            buffer[position++] = ch;
                        }
                        nonAlphanum = false;
                    }
                    else
                    {
                        nonAlphanum = true;
                    }
                }
                return buffer.Slice(0, position).ToString();
            }
            return _regex.Replace(input, m => m.Index == 0 ? m.Groups[1].Value.ToLowerInvariant() : m.Groups[1].Value.ToUpperInvariant());
        }

        public void Apply(DataPropertyBuilder propertyBuilder)
        {
            propertyBuilder.SetName(Apply(propertyBuilder.Property.Name));
        }

        public void Apply(DataEntityBuilder entityBuilder, bool applyToProperties = true)
        {
            entityBuilder.SetName(Apply(entityBuilder.EntityType.Name));
            if (applyToProperties)
            {
                foreach (var property in entityBuilder.Properties.Values)
                {
                    Apply(property);
                }
            }
        }
    }
}