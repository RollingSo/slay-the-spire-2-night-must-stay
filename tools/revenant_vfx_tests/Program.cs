using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Revenant;
using NightMustStay.Core.Nodes.Vfx;
using K=NightMustStay.Core.Nodes.Vfx.RevenantAttackVfx.Kind;

typeof(TestMode).GetProperty("IsOn")!.SetValue(null,true);
RevenantChargeVfx.Play(null!); // Headless/test simulations must never create nodes or audio.
void Assert(bool ok,string message) { if(!ok) throw new Exception(message); }
foreach(var (hookName, patchName) in new[] {
    ("BeforeCardPlayed", "BeforeCardMotion"), ("AfterCardPlayed", "AfterCardMotion") })
{
    var original=typeof(MegaCrit.Sts2.Core.Hooks.Hook).GetMethod(hookName)!.GetParameters();
    var parameter=typeof(NightMustStay.Core.Patches.RevenantAnimationPatch).GetMethod(patchName)!.GetParameters().Single();
    int index=int.Parse(parameter.Name![2..]);
    Assert(original[index].ParameterType==parameter.ParameterType,"Animation hook parameter drift: "+hookName);
}
Console.WriteLine("PASS: Revenant card animation hook parameter bindings.");
CardModel Card(Type type)=>(CardModel)RuntimeHelpers.GetUninitializedObject(type);
var groups=new Dictionary<K,Type[]>
{
    [K.HaloOut]=[typeof(Halo),typeof(ThreefoldHalo),typeof(RadagonHalo)],
    [K.LightningRed]=[typeof(AncientDragonLightning),typeof(LansseaxBlade),typeof(AncientDragonSpear),typeof(FlannSaxLightningSpear)],
    [K.LightningYellow]=[typeof(PreciseLightningStrike),typeof(LightningStrike),typeof(LightningSpear),typeof(DeathLightning)],
    [K.LightningBlue]=[typeof(IceLightningSpear)],
    [K.BeastRock]=[typeof(Beaststone),typeof(GurranqsRock)],
    [K.BeastClaw]=[typeof(BeastClaw),typeof(GurranqBeastClaw)],
    [K.Frenzy]=[typeof(SpaceRendingFrenzy),typeof(UnbearableFrenzy),typeof(FrenziedFlame)],
    [K.CursedClaw]=[typeof(StrikeRevenant),typeof(CursedClawCombo),typeof(SoulChargingClaw)]
};
int count=0;
foreach(var (kind,types) in groups)
foreach(var type in types)
{
    var card=Card(type);
    Assert(RevenantAttackEffects.KindFor(card)==kind,"Incorrect mapping: "+type.Name);
    var attack=DamageCmd.Attack(17m).WithHitCount(3).WithHitFx("old","old","old");
    object Field(string name)=>typeof(AttackCommand).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(attack)!;
    Assert(ReferenceEquals(attack,attack.WithRevenantFx(card)),"Replaced attack command.");
    Assert((decimal)Field("_damagePerHit")==17m && (int)Field("_hitCount")==3,"Changed damage or hits.");
    Assert(attack.HitVfx==null && attack.HitSfx==null && attack.TmpHitSfx==null,"Duplicate default hit FX.");
    int hitFactories=((System.Collections.ICollection)Field("_customHitVfxNodes")).Count;
    int castFactories=((System.Collections.ICollection)Field("_customAttackerVfxNodes")).Count;
    Assert(kind==K.HaloOut ? castFactories==1 && hitFactories==0 : hitFactories==1 && castFactories==0,"Wrong cast/hit routing: "+type.Name);
    if (kind == K.HaloOut)
        foreach (var factory in (IEnumerable<Func<Godot.Node2D?>>)Field("_customAttackerVfxNodes"))
            Assert(factory() == null, "Halo damage sampling must not run in TestMode.");
    count++;
}
var unchanged=DamageCmd.Attack(3m).WithHitFx("keep_me");
unchanged.WithRevenantFx(Card(typeof(Recover)));
Assert(unchanged.HitVfx=="keep_me","Unmapped/block cards must not gain an attack VFX.");
foreach(K kind in Enum.GetValues<K>())
{
    Assert(RevenantAttackEffects.Create(null!,kind)==null,"TestMode native node creation.");
    RevenantAttackEffects.Play(null!,kind);
    Assert(kind==K.Heal || RevenantAttackEffects.SoundFor(kind)!=null,"Missing sound mapping.");
}
RevenantAttackEffects.PlayHaloReturn(null!);
Assert(RevenantAttackEffects.SoundFor(K.Heal)==null,"Duplicate native healing sound.");
Assert(RevenantAttackEffects.FamilyKind(RevenantFamilyId.Helen)==K.Helen &&
    RevenantAttackEffects.FamilyKind(RevenantFamilyId.PumpkinHead)==K.Frederick &&
    RevenantAttackEffects.FamilyKind(RevenantFamilyId.Skeleton)==K.Sebastian,"Family identities confused.");

string Read(string file)=>File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(),file));
string cards=Read("src/Core/Models/Cards/RevenantAdvancedCards.cs");
Assert(cards.Contains(".CompatFromCard(card).WithRevenantFx(card)"),"Random-per-hit helper lacks FX.");
Assert(cards.Contains("Sts2BranchCompat.Damage(\n".Replace("\n",Environment.NewLine)) || cards.Contains("Sts2BranchCompat.Damage(\n"),"Family sacrifice must remain a native damage call.");
string manager=Read("src/Core/Models/Revenant/RevenantSummonManager.cs");
string animationPatch=Read("src/Core/Patches/RevenantAnimationPatch.cs");
Assert(animationPatch.Contains("nameof(NCreature._Ready)") &&
    animationPatch.Contains("RevenantSummonManager.NotifyCreatureNodeReady(__instance)"),
    "Family visuals are not retried when the backing Osty node becomes ready.");
Assert(manager.Contains("_familyVisual.GetParent() != petNode"),
    "Family visuals are not rebound after the backing combat node is rebuilt.");
string completion=manager[manager.IndexOf("public async Task NotifyChargeCompleted(")..manager.IndexOf("public async Task NotifyChargedCardPlayed(")];
Assert(Regex.Matches(completion,"RevenantChargeVfx.Play").Count==1,"Charge completion cue must use the shared completion notification exactly once.");
int chargeTypes=0;
foreach(var type in typeof(BeastClaw).Assembly.GetTypes().Where(t=>typeof(IRevenantChargeCard).IsAssignableFrom(t)&&!t.IsInterface))
{
    string source=Directory.GetFiles("src/Core/Models/Cards","*.cs").Select(File.ReadAllText).First(s=>s.Contains("class "+type.Name+" :"));
    int start=source.IndexOf("class "+type.Name+" :");
    int next=source.IndexOf("public sealed class ",start+10);
    string body=source[start..(next<0?source.Length:next)];
    int transition=body.IndexOf("public async Task CompleteCharge(");
    string charge=body[transition..];
    Assert(charge.IndexOf("if (IsChargeComplete)")<charge.IndexOf("ChargeComplete = true"),"Repeated completion is not guarded: "+type.Name);
    Assert(charge.IndexOf("NotifyChargeCompleted(this)")>charge.IndexOf("ChargeComplete = true"),"Cue precedes actual charge completion: "+type.Name);
    chargeTypes++;
}
Assert(chargeTypes==8,"Audit new charge-card paths when adding a charge type.");
Assert(Read("src/Core/Models/Cards/RevenantTextTableCards.cs").Contains("await chargeCard.CompleteCharge(context)"),"Preparation Ritual bypasses completion.");
Assert(Read("src/Core/Models/Cards/RevenantNecroExpansionCards.cs").Contains("await chargeCard.CompleteCharge(context)"),"Harmony bypasses completion.");
Console.WriteLine("PASS: eight charge transitions, shared completion cue, direct/assisted paths and TestMode safety.");
string actions=manager[manager.IndexOf("private async Task PerformFamilyAction(")..manager.IndexOf("public IReadOnlyList<RevenantNecro> GetNecros()")];
Assert(Regex.Matches(actions,"RevenantAttackEffects.FamilyDamage").Count==6,"Not all 6 family actions connected.");
Assert(!actions.Contains("Sts2BranchCompat.Damage"),"Family action bypasses FX routing.");
string familyHelpers=Read("src/Core/Models/Cards/RevenantTextTableCards.cs");
Assert(Regex.Matches(familyHelpers,"RevenantAttackEffects.FamilyDamage").Count==2,"Family card followups lack FX.");
string halo=Read("src/Core/Models/Power/RevenantFreezePower.cs");
int returnIndex=halo.IndexOf("RevenantAttackEffects.PlayHaloReturn(card)");
Assert(returnIndex>halo.IndexOf("await CardPileCmd.Add(card, PileType.Hand);"),"Halo returns before actual recovery.");
Assert(halo[..returnIndex].TrimEnd().EndsWith("NightMustStay.Core.Nodes.Vfx."),"Unexpected return call site.");
Assert(Regex.Matches(halo,"PlayHaloReturn").Count==1,"Duplicate return animation.");
foreach(string path in new[]{"src/Core/Models/Cards/RevenantAdvancedCards.cs","src/Core/Models/Cards/RevenantExpansionCards.cs","src/Core/Models/Power/RevenantAdvancedPowers.cs"})
    Assert(!Read(path).Contains("CreatureCmd.Heal("),"Healing bypasses gold FX: "+path);
Console.WriteLine($"PASS: {count} prayer/claw card mappings, 6 family actions + 2 family followup routes, halo cast/return separation, gold heal hooks, TestMode safety, unchanged attack damage/hit counts.");
