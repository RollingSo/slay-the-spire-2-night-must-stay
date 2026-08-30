using System.Reflection;
using System.Runtime.Loader;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: inspect_sts2_api <sts2.dll> <type-name> [method-name-filter]");
    return 2;
}

string assemblyPath = Path.GetFullPath(args[0]);
string assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
string typeName = args[1];
string methodFilter = args.Length > 2 ? args[2] : string.Empty;

using InspectionLoadContext context = new(assemblyDirectory);
Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);
Type? type = assembly.GetTypes().FirstOrDefault(candidate =>
    candidate.FullName == typeName || candidate.Name == typeName);
if (type is null)
{
    Console.Error.WriteLine($"Type not found: {typeName}");
    return 3;
}

Console.WriteLine(type.FullName);
foreach (MethodInfo method in type
             .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
             .Where(method => string.IsNullOrEmpty(methodFilter) || method.Name.Contains(methodFilter, StringComparison.OrdinalIgnoreCase))
             .OrderBy(method => method.Name))
{
    Console.WriteLine($"{(method.IsStatic ? "static" : "instance")} {method}");
}

return 0;

internal sealed class InspectionLoadContext(string assemblyDirectory)
    : AssemblyLoadContext(isCollectible: true), IDisposable
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string candidate = Path.Combine(assemblyDirectory, $"{assemblyName.Name}.dll");
        return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
    }

    public void Dispose() => Unload();
}
