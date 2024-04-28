namespace NCoreUtils.Data;

public enum MyEnum { A = 0, B = 1 }

public class MyData(int num, string str, MyEnum @enum, IReadOnlyList<int> ints)
{
    public static int StaticNum { get; } = 12;

    public int Num { get; } = num;

    public string Str { get; } = str;

    public MyEnum Enum { get; } = @enum;

    public IReadOnlyList<int> Ints { get; } = ints;
}