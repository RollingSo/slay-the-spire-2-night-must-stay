using System.Reflection;
using System.Runtime.Loader;

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine(
        "Usage: ModelIdCollisionValidator <sts2.dll> <mod.dll> [dependency-directory]");
    return 2;
}

string gameAssemblyPath = Path.GetFullPath(args[0]);
string modAssemblyPath = Path.GetFullPath(args[1]);
if (!File.Exists(gameAssemblyPath) || !File.Exists(modAssemblyPath))
{
    Console.Error.WriteLine("The game or mod assembly does not exist.");
    return 3;
}

string[] assemblyDirectories = args.Length == 3
    ? [
        Path.GetDirectoryName(gameAssemblyPath)!,
        Path.GetDirectoryName(modAssemblyPath)!,
        Path.GetFullPath(args[2]),
    ]
    : [Path.GetDirectoryName(gameAssemblyPath)!, Path.GetDirectoryName(modAssemblyPath)!];
using BranchLoadContext context = new(assemblyDirectories);
Assembly gameAssembly = context.LoadFromAssemblyPath(gameAssemblyPath);
Assembly modAssembly = context.LoadFromAssemblyPath(modAssemblyPath);

Type abstractModel = gameAssembly.GetType(
    "MegaCrit.Sts2.Core.Models.AbstractModel",
    throwOnError: true)!;
Type stringHelper = gameAssembly.GetType(
    "MegaCrit.Sts2.Core.Helpers.StringHelper",
    throwOnError: true)!;
MethodInfo slugify = stringHelper.GetMethod(
    "Slugify",
    BindingFlags.Public | BindingFlags.Static,
    binder: null,
    types: [typeof(string)],
    modifiers: null)
    ?? throw new MissingMethodException(stringHelper.FullName, "Slugify(string)");

Dictionary<string, string> gameIds = GetModelTypes(gameAssembly, abstractModel)
    .ToDictionary(type => GetModelId(type, abstractModel, slugify), type => type.FullName!);

var collisions = GetModelTypes(modAssembly, abstractModel)
    .Select(type => new
    {
        Id = GetModelId(type, abstractModel, slugify),
        ModType = type.FullName!,
    })
    .Where(candidate => gameIds.ContainsKey(candidate.Id))
    .Select(candidate => new
    {
        candidate.Id,
        candidate.ModType,
        GameType = gameIds[candidate.Id],
    })
    .OrderBy(candidate => candidate.Id)
    .ToArray();

if (collisions.Length > 0)
{
    Console.Error.WriteLine("Model ID collisions detected:");
    foreach (var collision in collisions)
    {
        Console.Error.WriteLine(
            $"{collision.Id}: {collision.ModType} conflicts with {collision.GameType}");
    }
    return 1;
}

Console.WriteLine(
    $"Model ID collision scan passed ({GetModelTypes(modAssembly, abstractModel).Count()} mod models)." );
return 0;

static IEnumerable<Type> GetModelTypes(Assembly assembly, Type abstractModel) =>
    assembly.GetTypes().Where(type => !type.IsAbstract && abstractModel.IsAssignableFrom(type));

static string GetModelId(Type type, Type abstractModel, MethodInfo slugify)
{
    Type categoryType = type;
    while (categoryType.BaseType is not null && categoryType.BaseType != abstractModel)
        categoryType = categoryType.BaseType;

    string category = (string)slugify.Invoke(null, [categoryType.Name])!;
    const string modelSuffix = "_MODEL";
    if (category.EndsWith(modelSuffix, StringComparison.Ordinal))
        category = category[..^modelSuffix.Length];

    string entry = (string)slugify.Invoke(null, [type.Name])!;
    return $"{category}.{entry}";
}

internal sealed class BranchLoadContext(params string[] assemblyDirectories)
    : AssemblyLoadContext(isCollectible: true), IDisposable
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        foreach (string directory in assemblyDirectories)
        {
            string candidate = Path.Combine(directory, $"{assemblyName.Name}.dll");
            if (File.Exists(candidate))
                return LoadFromAssemblyPath(candidate);
        }
        return null;
    }

    public void Dispose() => Unload();
}
