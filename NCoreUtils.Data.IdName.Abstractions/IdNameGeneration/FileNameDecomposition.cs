using System;

namespace NCoreUtils.Data.IdNameGeneration
{
    public class FileNameDecomposition : IStringDecomposition
    {
        sealed class FileNameDecomposer : IStringDecomposer
        {
            IStringDecomposition IStringDecomposer.Decompose(string input) => new FileNameDecomposition(input);
        }

        public static IStringDecomposer Decomposer { get; } = new FileNameDecomposer();

        public string MainPart { get; }

        public string Extension { get; }

        public FileNameDecomposition(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            var extensionIndex = input.LastIndexOf('.');
            if (-1 == extensionIndex)
            {
                MainPart = input;
                Extension = null;
            }
            else
            {
                MainPart = input.Substring(0, extensionIndex);
                Extension = input.Substring(extensionIndex);
            }
        }

        public virtual string Rebuild(string mainPart, string suffix)
        {
            if (null == Extension)
            {
                return string.IsNullOrEmpty(suffix) ? mainPart : $"{mainPart}-{suffix}";
            }
            return string.IsNullOrEmpty(suffix) ? (mainPart + Extension) : $"{mainPart}-{suffix}{Extension}";
        }
    }
}