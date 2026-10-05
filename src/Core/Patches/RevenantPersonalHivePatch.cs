using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(PersonalHivePower), nameof(PersonalHivePower.AfterDamageReceived))]
public static class RevenantPersonalHivePatch
{
    [HarmonyPrefix]
    public static void ResolveNecroCardRecipient(ref Creature dealer)
    {
        // The native hook resolves Osty's owner, but other pets have no Player.
        // Resolve ownership only inside Hive's retaliation; the attack itself
        // must still use the Necro as dealer for its Strength and other powers.
        if (RevenantSummonManager.IsRegisteredNecroCreature(dealer)
            && dealer.PetOwner?.Creature is { } owner)
            dealer = owner;
    }
}
