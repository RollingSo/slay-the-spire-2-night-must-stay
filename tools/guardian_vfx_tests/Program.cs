using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.TestSupport;
using NightMustStay.Core.Nodes.Vfx;

typeof(TestMode).GetProperty("IsOn")!.SetValue(null, true);
void Assert(bool value, string message) { if (!value) throw new Exception(message); }

foreach (var kind in Enum.GetValues<GuardianAttackVfx.Kind>())
{
    Assert(GuardianAttackEffects.Create(null!, kind) == null, "Headless factory must not enter Godot.");
    GuardianAttackEffects.Play(null!, kind);
}
Assert(NightreignHitVfx.CreateGuardianCounter(null!) == null, "Counter facade must retain test-mode safety.");
Assert(NightreignHitVfx.CreateGuardianWhirlwind(null!) == null, "Wind facade must retain test-mode safety.");
Assert(NightreignHitVfx.CreateGuardianShieldPoke(null!) == null, "Shield-poke facade must retain test-mode safety.");
foreach (bool wind in new[] { false, true })
{
    AttackCommand attack = DamageCmd.Attack(17m).WithHitCount(3).WithHitFx("old", "old_sfx");
    object Field(string name) => typeof(AttackCommand).GetField(name,
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(attack)!;
    var originalDamage = Field("_damagePerHit");
    var updated = wind ? attack.WithGuardianWhirlwindFx() : attack.WithGuardianWeaponFx();
    Assert(ReferenceEquals(updated, attack), "Effects must decorate the same attack.");
    Assert(Equals(Field("_damagePerHit"), originalDamage) && (int)Field("_hitCount") == 3, "Effects changed damage/hit count.");
    Assert(attack.HitVfx == null && attack.HitSfx == null, "Legacy effect was not cleared.");
    Assert(((System.Collections.ICollection)Field("_customHitVfxNodes")).Count == 1, "Expected one VFX factory.");
}
Console.WriteLine("PASS: three test-mode factories/facades; weapon/wind preserve damage and hit count and clear legacy VFX/SFX.");
