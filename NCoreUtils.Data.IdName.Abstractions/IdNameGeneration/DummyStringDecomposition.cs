using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data.IdNameGeneration
{
    public sealed class DummyStringDecomposition : IStringDecomposition
    {
        sealed class DummyDecomposer : IStringDecomposer
        {
            IStringDecomposition IStringDecomposer.Decompose(string input) => new DummyStringDecomposition(input);
        }

        public static IStringDecomposer Decomposer { get; } = new DummyDecomposer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DummyStringDecomposition(string input) => new DummyStringDecomposition(input);

        public string MainPart { get; }

        public DummyStringDecomposition(string input) => MainPart = input ?? throw new ArgumentNullException(nameof(input));

        public string Rebuild(string mainPart, string? suffix) => string.IsNullOrEmpty(suffix) ? mainPart : $"{mainPart}-{suffix}";
    }
}