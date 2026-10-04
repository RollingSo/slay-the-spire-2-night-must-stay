using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: inspect_sts2_api <sts2.dll> <type-name> [method-name-filter] [dependency-directory] [mod.dll for prepare-duchess]");
    return 2;
}

string assemblyPath = Path.GetFullPath(args[0]);
string assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
string typeName = args[1];
string methodFilter = args.Length > 2 ? args[2] : string.Empty;

using InspectionLoadContext context = new(assemblyDirectory, args.Length > 3 ? args[3] : null);
Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);
if (methodFilter == "prepare-duchess")
{
    Assembly mod = context.LoadFromAssemblyPath(Path.GetFullPath(args[4]));
    Type card = mod.GetType("NightMustStay.Core.Models.Cards.DuchessCard", true)!;
    MethodInfo onPlay = card.GetMethod("OnPlay", BindingFlags.Instance | BindingFlags.NonPublic)!;
    Type state = onPlay.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
    RuntimeHelpers.PrepareMethod(state.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.MethodHandle);
    RuntimeHelpers.RunClassConstructor(mod.GetType("NightMustStay.Core.Compatibility.Sts2BranchCompat", true)!.TypeHandle);
    Console.WriteLine("PASS: Duchess OnPlay async body JIT compiles against " + assemblyPath);
    return 0;
}
Type? type = assembly.GetType(typeName) ?? assembly.GetTypes().FirstOrDefault(candidate =>
    candidate.FullName == typeName || candidate.Name == typeName);
if (type is null)
{
    Console.Error.WriteLine($"Type not found: {typeName}");
    return 3;
}

Console.WriteLine(type.FullName);
if (methodFilter == "constructors")
{
    foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        Console.WriteLine(constructor);
    return 0;
}
if (methodFilter == "fields")
{
    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        Console.WriteLine(field);
    return 0;
}
foreach (MethodInfo method in type
             .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
             .Where(method => string.IsNullOrEmpty(methodFilter) || method.Name.Contains(methodFilter, StringComparison.OrdinalIgnoreCase))
             .OrderBy(method => method.Name))
{
    Console.WriteLine($"{(method.IsStatic ? "static" : "instance")} {method}");
}

return 0;

internal sealed class InspectionLoadContext(string assemblyDirectory, string? fallbackDirectory)
    : AssemblyLoadContext(isCollectible: true), IDisposable
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string candidate = Path.Combine(assemblyDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(candidate)) return LoadFromAssemblyPath(candidate);
        candidate = Path.Combine(fallbackDirectory ?? assemblyDirectory, $"{assemblyName.Name}.dll");
        return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
    }

    public void Dispose() => Unload();
}
