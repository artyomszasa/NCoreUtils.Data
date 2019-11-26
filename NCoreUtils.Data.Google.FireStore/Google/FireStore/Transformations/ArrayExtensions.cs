namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    static class ArrayExtensions
    {
        public static T[] Slice<T>(this T[] source, int start, int end)
        {
            #if NETSTANDARD2_1
            return source[start .. end];
            #else
            var count = end - start;
            var result = new T[count];
            for (var i = 0; i < count; ++i)
            {
                result[i] = source[start + i];
            }
            return result;
            #endif
        }
    }
}