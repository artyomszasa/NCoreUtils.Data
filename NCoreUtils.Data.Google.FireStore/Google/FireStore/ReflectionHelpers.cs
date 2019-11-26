using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore
{
    static class ReflectionHelpers
    {
        public static bool IsSameOrOverridden(this PropertyInfo a, PropertyInfo b)
        {
            if (a is null)
            {
                return b is null;
            }
            if (b is null)
            {
                return false;
            }
            if (ReferenceEquals(a, b) || a.Equals(b))
            {
                return true;
            }
            if (!(a.GetMethod is null && b.GetMethod is null))
            {
                return a.GetMethod.GetBaseDefinition() == b.GetMethod
                    || b.GetMethod.GetBaseDefinition() == a.GetMethod;
            }
            return false;
        }
    }
}