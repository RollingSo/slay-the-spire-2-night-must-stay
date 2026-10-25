using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

internal static class ReverseTimePerformanceRegression
{
    internal static void Run()
    {
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var player=TrackingArrowRegression.MakePlayer();
        var creature=new Creature(player,70,70);
        typeof(Player).GetField("<Creature>k__BackingField",flags)!.SetValue(player,creature);
        var power=(DuchessMomentPower)ModelDb.Power<DuchessMomentPower>().ToMutable();
        typeof(PowerModel).GetProperty("Owner")!.SetValue(power,creature);
        var powers=(List<PowerModel>)typeof(Creature).GetField("_powers",flags)!.GetValue(creature)!;
        powers.Add(power);
        var concealment=ModelDb.Power<DuchessConcealmentPower>().ToMutable();
        var specProperty=typeof(DuchessCard).GetProperty("Spec",flags)!;
        var activeProperty=typeof(DuchessCard).GetProperty("IsMomentActive",flags)!;
        int cases=0;
        foreach(var type in typeof(DuchessCard).Assembly.GetTypes().Where(t=>!t.IsAbstract && typeof(DuchessCard).IsAssignableFrom(t)))
        foreach(bool upgraded in new[]{false,true})
        {
            var canonical=(CardModel)typeof(ModelDb).GetMethod("Card")!.MakeGenericMethod(type).Invoke(null,null)!;
            var card=(DuchessCard)canonical.ToMutable();
            card.Owner=player;
            if(upgraded && card.IsUpgradable)card.UpgradeInternal();
            var spec=(DuchessCardSpec)specProperty.GetValue(card)!;
            foreach(int moment in Enumerable.Range(0,13))
            foreach(bool concealed in new[]{false,true})
            foreach(decimal cost in new[]{-1m,0m,1m,3m})
            {
                typeof(PowerModel).GetField("_amount",flags)!.SetValue(power,moment+1);
                powers.Remove(concealment);if(concealed)powers.Add(concealment);
                int discount=(bool)activeProperty.GetValue(card)! ? spec.MomentCostReduction : 0;
                if(spec.MomentCostReductionDynamic)discount+=moment;
                if(spec.ConcealedCostReduction>0 && concealed)discount+=spec.ConcealedCostReduction;
                decimal expected=Math.Max(0m,cost-discount);
                bool changed=power.TryModifyEnergyCostInCombat(card,cost,out var actual);
                if(actual!=expected || changed!=(expected!=cost))throw new Exception($"Cost semantics changed: {type.Name} {moment} {cost}");
                cases++;
            }
        }
        var snapshot=typeof(DuchessMomentPower).GetNestedType("CardSnapshot",BindingFlags.NonPublic)!;
        if(!snapshot.IsValueType)throw new Exception("Turn snapshot must not allocate one object per card");
        // Restore membership is materialized after generated-card removal, just
        // as GroupBy buffered the original filter before performing any moves.
        var captured=Enumerable.Range(0,300).Select(i=>(Card:i,Pile:i%5,Index:i/5)).ToArray();
        var live=Enumerable.Range(0,310).Where(i=>i%7!=0).ToArray();
        var expectedOrder=captured.Where(c=>c.Card!=19 && live.Contains(c.Card))
            .GroupBy(c=>c.Pile).SelectMany(g=>g.OrderBy(c=>c.Index)).ToArray();
        var membership=live.ToHashSet();
        var actualOrder=captured.Where(c=>c.Card!=19 && membership.Contains(c.Card))
            .GroupBy(c=>c.Pile).SelectMany(g=>g.OrderBy(c=>c.Index)).ToArray();
        if(!expectedOrder.SequenceEqual(actualOrder))throw new Exception("Restore pile/order/source exclusion changed");
        Console.WriteLine($"PASS: Reverse Time idle-path optimization; {cases} cost cases match baseline; inline turn snapshots.");
    }
}
