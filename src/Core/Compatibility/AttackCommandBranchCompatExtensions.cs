using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands.Builders;

namespace MegaCrit.Sts2.Core.Commands;

/// <summary>
/// Supplies the production one-argument fluent API when the Public Beta's
/// AttackCommand requires the additional CardPlay argument.
/// </summary>
public static class AttackCommandBranchCompatExtensions
{
    public static AttackCommand CompatFromCard(this AttackCommand command, CardModel card) =>
        NightMustStay.Core.Compatibility.Sts2BranchCompat.AttackFromCard(command, card);
}
