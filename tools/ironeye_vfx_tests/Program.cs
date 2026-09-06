using System.Reflection;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.TestSupport;
using NightMustStay.Core.Nodes.Vfx;

typeof(TestMode).GetProperty("IsOn")!.SetValue(null, true);
void Assert(bool value, string message) { if (!value) throw new Exception(message); }
foreach (var kind in Enum.GetValues<IroneyeAttackVfx.Kind>())
{
    Assert(IroneyeAttackEffects.Create(null!, kind) == null, "TestMode must not create a native node.");
    IroneyeAttackEffects.Play(null!, kind);
}
Assert(NightreignHitVfx.CreateIroneyeKnife(null!) == null, "Knife compatibility safety.");
Assert(NightreignHitVfx.CreateIroneyeShot(null!, null!) == null, "Arrow compatibility safety.");
Assert(NightreignHitVfx.CreateIroneyeMarkTrigger(null!) == null, "Mark compatibility safety.");
foreach (bool shot in new[] { false, true })
foreach (bool poisoned in new[] { false, true })
{
    AttackCommand attack = DamageCmd.Attack(17m).WithHitCount(3).WithHitFx("old", "old_sfx", "old_tmp");
    object Field(string name) => typeof(AttackCommand).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(attack)!;
    var updated = shot ? attack.WithIroneyeShotFx(null!, poisoned) : attack.WithIroneyeKnifeFx(poisoned);
    Assert(ReferenceEquals(updated, attack), "Must decorate the same attack.");
    Assert((decimal)Field("_damagePerHit") == 17m && (int)Field("_hitCount") == 3, "Damage/hit count changed.");
    Assert(attack.HitVfx == null && attack.HitSfx == null && attack.TmpHitSfx == null, "Legacy effects were not cleared.");
    Assert(((System.Collections.ICollection)Field("_customHitVfxNodes")).Count == 1, "Duplicate VFX factory.");
}
string root = Directory.GetCurrentDirectory();
int knife = 0, shotCount = 0, attacks = 0;
foreach (string file in Directory.GetFiles(Path.Combine(root, "src/Core/Models/Cards"), "*Ironeye*.cs"))
{
    string source = File.ReadAllText(file);
    attacks += Regex.Matches(source, @"DamageCmd\.Attack\(").Count;
    knife += Regex.Matches(source, @"\.WithIroneyeKnifeFx\((?:poisoned: true)?\)").Count;
    shotCount += Regex.Matches(source, @"\.WithIroneyeShotFx\(Owner\.Creature(?:, poisoned: true)?\)").Count;
    Assert(!source.Contains("CreateIroneyeMarkTrigger"), "A card is faking mark consumption: " + file);
    Assert(!source.Contains("CreateIroneyeShot") && !source.Contains("CreateIroneyeKnife"), "A card bypasses explicit routing: " + file);
}
Assert(attacks == knife + shotCount && attacks > 0, "Some Ironeye AttackCommand lacks typed VFX routing.");
string powers = File.ReadAllText(Path.Combine(root, "src/Core/Models/Power/IroneyePowers.cs"));
Assert(Regex.Matches(powers, "PlayIroneyeMarkTrigger").Count == 1, "Mark fracture must have one real consumption entry.");
int triggerOne = powers.IndexOf("public async Task TriggerOne(", StringComparison.Ordinal);
Assert(powers.IndexOf("PlayIroneyeMarkTrigger", StringComparison.Ordinal) > triggerOne, "Mark VFX not routed through actual TriggerOne.");
string burst = powers[powers.IndexOf("public sealed class PoisonBurstPower")..powers.IndexOf("public sealed class HiddenPoisonPower")];
Assert(burst.IndexOf("Kind.PoisonBurst") > burst.IndexOf("!target.IsAlive") &&
    burst.IndexOf("Kind.PoisonBurst") < burst.IndexOf("await hiddenPoison.Trigger"), "Burst must follow eligibility checks and precede damage, including lethal hits.");
Assert(Regex.Matches(powers,"Kind.PoisonBurst").Count==1,"DOT or Mark is faking an active poison burst.");
var expectedPoison = new HashSet<string> { "VenomDagger","WitheringCut","TwinKissPoisonMoth","PoisonBurst","IroneyePoisonArrow","PoisonMistArrowArray","CorrodeAll" };
var actualPoison = new HashSet<string>();
foreach (string file in Directory.GetFiles(Path.Combine(root,"src/Core/Models/Cards"),"*Ironeye*.cs"))
{
    string current="";
    foreach (string line in File.ReadLines(file))
    {
        var match=Regex.Match(line,@"public (?:sealed )?class (\w+)");
        if(match.Success) current=match.Groups[1].Value;
        if(line.Contains("poisoned: true")) actualPoison.Add(current);
    }
}
Assert(expectedPoison.SetEquals(actualPoison),"Incorrect toxic card routing: "+string.Join(",",actualPoison));
Console.WriteLine("PASS: seven toxic card routes, both normal/poison factories, six effect types, active-burst-only entry, no changed damage/hits.");
Console.WriteLine($"PASS: {attacks} card attack routes ({knife} knife, {shotCount} shot); only actual mark consumption has fracture VFX; TestMode/factories/damage/hit count preserved.");
