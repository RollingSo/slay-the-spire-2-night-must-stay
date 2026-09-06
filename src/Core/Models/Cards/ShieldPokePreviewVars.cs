using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Compatibility;

namespace NightMustStay.Core.Models.Cards;

/// <summary>Preserves the Damage key/upgrades while previewing the same raw amount as OnPlay.</summary>
internal sealed class ShieldPokeDamageVar(decimal damage) : DamageVar(damage, ValueProp.Move)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature target, bool runGlobalHooks)
    {
        // Preserve native enchantment/upgrade preview bookkeeping on the unchanged base value.
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks: false);
        if (card is not ShieldPoke poke) return;
        decimal raw = poke.GetDamageBeforeHooks();
        if (runGlobalHooks)
        {
            PreviewValue = Sts2BranchCompat.ModifyDamage(
                card.Owner.RunState, card.CombatState, target, card.Owner.Creature,
                raw, Props, card, ModifyDamageHookType.All, previewMode,
                out IEnumerable<AbstractModel> _);
        }
        else
        {
            if (card.Enchantment is { } enchantment)
            {
                raw += enchantment.EnchantDamageAdditive(raw, Props);
                raw *= enchantment.EnchantDamageMultiplicative(raw, Props);
            }
            PreviewValue = raw;
        }
    }
}

/// <summary>Fearless skips GainBlock entirely, so even Dexterity cannot turn its preview above zero.</summary>
internal sealed class ShieldPokeBlockVar(decimal block) : BlockVar(block, ValueProp.Move)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature target, bool runGlobalHooks)
    {
        if (Fearless.GetShieldPokeDamageBonus(card) > 0m)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks: false);
            PreviewValue = 0m;
            return;
        }
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
    }
}
