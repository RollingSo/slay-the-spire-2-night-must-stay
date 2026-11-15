using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.CardPools;
using NightMustStay.Core.Patches;
internal static class WylderNewCardsRegression
{
    static void Check(bool ok,string label) { if(!ok) throw new Exception(label); }
    public static void Run()
    {
        var shot=ModelDb.Card<WylderGrappleShot>().ToMutable();
        Check(shot.EnergyCost.GetResolved()==1 && shot.Type==CardType.Skill && shot.TargetType==TargetType.AnyEnemy && shot.Rarity==CardRarity.Uncommon && shot.DynamicVars["Vulnerable"].BaseValue==2,"Grapple Shot base");
        shot.UpgradeInternal(); Check(shot.EnergyCost.GetResolved()==0 && shot.DynamicVars["Vulnerable"].BaseValue==2,"Grapple Shot upgrade");
        var aerial=ModelDb.Card<WylderAerialStake>().ToMutable();
        Check(aerial.EnergyCost.GetResolved()==2 && aerial.DynamicVars.Damage.BaseValue==20 && aerial.DynamicVars["Imbalance"].BaseValue==3,"Aerial base");
        aerial.UpgradeInternal(); Check(aerial.EnergyCost.GetResolved()==1 && aerial.DynamicVars.Damage.BaseValue==20 && aerial.DynamicVars["Imbalance"].BaseValue==3,"Aerial cost-only upgrade");
        var leap=ModelDb.Card<WylderSlidingLeap>().ToMutable();
        Check(leap.Rarity==CardRarity.Uncommon && !leap.Keywords.Contains(CardKeyword.Retain),"Leap base"); leap.UpgradeInternal(); Check(leap.Keywords.Contains(CardKeyword.Retain),"Leap retain upgrade");
        var burn=ModelDb.Card<WylderIntenseBurning>().ToMutable();
        Check(burn.EnergyCost.CostsX && burn.DynamicVars["Vigor"].BaseValue==6 && burn.DynamicVars.Repeat.BaseValue==0,"Burn base");
        burn.UpgradeInternal(); Check(burn.DynamicVars.Repeat.BaseValue==1 && burn.DynamicVars["Vigor"].BaseValue==6,"Burn upgrade");
        var life=ModelDb.Card<WylderLifeFromDeath>().ToMutable();
        Check(life.EnergyCost.GetResolved()==0 && life.Type==CardType.Power && life.Rarity==CardRarity.Ancient && life.TargetType==TargetType.Self,"Life metadata");
        life.UpgradeInternal(); Check(life.IsUpgraded,"Life upgrade");
        var mapping=new Dictionary<ModelId,CardModel>();
        AccessTools.Method(typeof(GuardianArchaicToothPatch),"AddGuardianTranscendence").Invoke(null,new object[]{mapping});
        Check(ModelDb.CardPool<WylderCardPool>().AllCards.Where(c=>c.Rarity==CardRarity.Ancient && !mapping.Values.Contains(c)).Single() is WylderLifeFromDeath,"Dusty Tome exclusive eligible card");
        var owner=TrackingArrowRegression.MakePlayer();
        var other=TrackingArrowRegression.MakePlayer();
        var creature=new Creature(owner,80,80){CombatState=new CombatState()};
        var p=(WylderLifeFromDeathPower)ModelDb.Power<WylderLifeFromDeathPower>().ToMutable();
        AccessTools.Property(typeof(PowerModel),nameof(PowerModel.Owner)).SetValue(p,creature);
        p.SetAmount(2,true); p.PendingBasic=2; p.PendingUpgraded=2;
        Check(p.ModifyHandDraw(owner,5)==3 && p.ModifyHandDraw(owner,1)==0 && p.ModifyHandDraw(other,5)==5,"Draw penalty owner/clamp/stack");
        p.AfterSideTurnStart(CombatSide.Enemy,Array.Empty<Creature>(),creature.CombatState).GetAwaiter().GetResult();
        Check(p.PendingBasic==2 && p.DueBasic==0,"Enemy turn ignored");
        p.AfterSideTurnStart(creature.Side,new[]{creature},creature.CombatState).GetAwaiter().GetResult();
        Check(p.PendingBasic==0 && p.PendingUpgraded==0 && p.DueBasic==2 && p.DueUpgraded==2,"Next-turn mixed upgrade batches");
        p.DueBasic=p.DueUpgraded=0;
        p.AfterSideTurnStart(creature.Side,new[]{creature},creature.CombatState).GetAwaiter().GetResult();
        Check(p.DueBasic==0 && p.DueUpgraded==0 && p.ModifyHandDraw(owner,5)==3,"One-shot generation with lasting draw penalty");
        Console.WriteLine("PASS: new card metadata/upgrades, Tome eligibility, draw penalty and next-turn queue boundaries.");
    }
}
