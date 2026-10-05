// Console fixtures cannot call Godot's native logger; errors still fail the test.
internal static class SerializationFixture
{
    public static bool Info(string __0)
    {
        Console.WriteLine(__0);
        return false;
    }
    internal static void Error(string __0) => throw new InvalidOperationException("Native serialization initialization: " + __0);
    internal static bool Prefix(string __0)
    {
        Error(__0);
        return false;
    }
}
