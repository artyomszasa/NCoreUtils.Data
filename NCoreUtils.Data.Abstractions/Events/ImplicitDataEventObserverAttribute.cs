using System;

namespace NCoreUtils.Data.Events
{
    /// <summary>
    /// Marks data event observer as implicit. Implicit observers can be loaded with single call.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ImplicitDataEventObserverAttribute : Attribute { }
}