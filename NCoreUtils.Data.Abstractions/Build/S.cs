namespace NCoreUtils.Data.Build;

internal static class S
{
    public const string CtorUsesReflection = "This contructor populates metadata using reflection. In trimmable/AOT context either generator should be used or all affected classes, interfaces and members should be preserved manually.";

    public const string MethodUsesReflection = "This method populates metadata using reflection. In trimmable/AOT context either generator should be used or all affected classes, interfaces and members should be preserved manually.";
}