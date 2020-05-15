using System;
using System.Text.RegularExpressions;

namespace NCoreUtils.Data.Build
{
    public class SnakeCaseNaming
    {
        private static readonly Regex _regex = new Regex("[A-Z]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
                Span<char> buffer = stackalloc char[input.Length * 2];
                for (var i = 0; i < input.Length; ++i)
                {
                    var ch = input[i];
                    if (char.IsUpper(ch))
                    {
                        if (position != 0)
                        {
                            buffer[position++] = '_';
                        }
                        buffer[position++] = char.ToLowerInvariant(ch);
                    }
                    else
                    {
                        buffer[position++] = ch;
                    }
                }
                return buffer.Slice(0, position).ToString();
            }
            return _regex.Replace(input, m => $"_{m.Value.ToLowerInvariant()}");
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