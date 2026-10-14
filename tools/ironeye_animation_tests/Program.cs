using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using NightMustStay.Core.Patches;

static class Production
{
    internal static readonly Type Context = typeof(IroneyeAnimationPatch).Assembly.GetType("NightMustStay.Core.Nodes.IroneyeAnimationContext")!;
    internal static object? Call(string method, params object?[] args) => Context.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
    internal static object? Patch(string method, object?[] args) => typeof(IroneyeAnimationPatch).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
    internal static string Kind(Creature actor) => Call("For",actor)!.ToString()!;
    internal static AttackCommand Command(Creature actor, string? kind)
    {
        var command = new AttackCommand(1m);
        typeof(AttackCommand).GetProperty("Attacker")!.SetValue(command,actor);
        if (kind != null) Call("Register",command,Enum.Parse(Context.GetNestedType("AttackKind",BindingFlags.NonPublic)!,kind));
        return command;
    }
}
sealed class Harness(AttackCommand command)
{
    internal AttackCommand Command => command;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string[]> Execute(Task gate, Harness? nested = null, bool fail = false)
    {
        string before = Production.Kind(command.Attacker!);
        await gate.ConfigureAwait(false);
        string after = Production.Kind(command.Attacker!);
        if (nested != null) await nested.Execute(Task.CompletedTask).ConfigureAwait(false);
        if (fail) throw new InvalidOperationException("expected test failure");
        return [before,after,Production.Kind(command.Attacker!)];
    }
}
[HarmonyPatch(typeof(Harness),nameof(Harness.Execute))]
static class HarnessPatch
{
    static void Prefix(Harness __instance, out object? __state)
    {
        object?[] args = [__instance.Command,null];
        Production.Patch("EnterAttack",args);
        __state = args[1];
    }
    static void Postfix(object? __state) => Production.Patch("LeaveAttack",[__state]);
    static Exception? Finalizer(Exception? __exception, object? __state) => (Exception?)Production.Patch("FinalizeAttack",[__exception,__state]);
}
static class Program
{
    static readonly List<object> Checks=[];
    static void Check(string name,bool passed)
    {
        Checks.Add(new {name,passed});
        Console.WriteLine($"{(passed?"PASS":"FAIL")} {name}");
        if (!passed) throw new Exception(name);
    }
    public static async Task Main()
    {
        var harmony = new Harmony("nightmuststay.animation.async-tests");
        Check("production_harmony_targets_bind",harmony.CreateClassProcessor(typeof(IroneyeAnimationPatch)).Patch().Count == 6);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        var actor=(Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        var other=(Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        Check("default_shot",Production.Kind(actor)=="Shot");
        var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var knife=new Harness(Production.Command(actor,"Knife")).Execute(gate.Task);
        Check("caller_scope_restored_before_task_completion",Production.Kind(actor)=="Shot");
        var shot=new Harness(Production.Command(actor,"Shot")).Execute(gate.Task);
        gate.SetResult();
        Check("knife_scope_survives_await",(await knife).All(k=>k=="Knife"));
        Check("parallel_same_actor_shot_isolated",(await shot).All(k=>k=="Shot"));
        Check("caller_scope_clean",Production.Kind(actor)=="Shot");
        var parent=new Harness(Production.Command(actor,"Knife"));
        var nested=new Harness(Production.Command(actor,null));
        Check("nested_unclassified_restores_parent",(await parent.Execute(Task.CompletedTask,nested)).All(k=>k=="Knife"));
        Check("other_actor_does_not_inherit_knife",Production.Kind(other)=="Shot");
        try { await parent.Execute(Task.CompletedTask,fail:true); throw new Exception("missing exception"); }
        catch (InvalidOperationException) { Check("async_exception_preserved",true); }
        Check("scope_clean_after_failure",Production.Kind(actor)=="Shot");
        var weak=Production.Command(actor,"Shot");
        var context=Production.Context.GetNestedType("AttackKind",BindingFlags.NonPublic)!;
        Production.Call("Register",weak,Enum.Parse(context,"Knife"));
        Check("latest_builder_metadata_wins",(await new Harness(weak).Execute(Task.CompletedTask)).All(k=>k=="Knife"));
        Directory.CreateDirectory("build");
        File.WriteAllText("build/ironeye_animation_context_checks.json",System.Text.Json.JsonSerializer.Serialize(Checks));
    }
}
