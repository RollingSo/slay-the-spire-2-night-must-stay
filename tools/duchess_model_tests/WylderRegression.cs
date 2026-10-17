using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;

internal static class WylderRegression
{
    private static int _draws, _removes;
    public static void Run()
    {
        var h=new HarmonyLib.Harmony("night-must-stay.tests.wylder");
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderPursuitDamageBranchPatch")!).Patch();
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderSweepTargetPatch")!).Patch();
        h.Patch(typeof(PowerCmd).GetMethods().Single(m=>m.Name=="Remove" && !m.IsGenericMethod),
            prefix:new HarmonyMethod(typeof(WylderRegression),nameof(Remove)));
        h.Patch(typeof(CardPileCmd).GetMethods().Single(m=>m.Name=="Draw" && m.GetParameters().Length==4),
            prefix:new HarmonyMethod(typeof(WylderRegression),nameof(Draw)));
        h.Patch(AccessTools.PropertyGetter(typeof(CombatManager),nameof(CombatManager.IsInProgress)),
            prefix:new HarmonyMethod(typeof(ReversalStepRegression),nameof(ReversalStepRegression.InCombat)));
        h.Patch(AccessTools.PropertyGetter(typeof(CombatManager),nameof(CombatManager.IsOverOrEnding)),
            prefix:new HarmonyMethod(typeof(MajestyRegression),nameof(MajestyRegression.CombatEndingFixture)));
        try
        {
            var character=ModelDb.Character<Wylder>();
            var deck=character.StartingDeck.ToArray();
            Check(deck.Length==10 && deck.Count(c=>c is WylderStrike)==4 && deck.Count(c=>c is WylderDefend)==4
                && deck.Count(c=>c is WylderClawMovement)==1 && deck.Count(c=>c is WylderDashAttack)==1,"Starting deck");
            Check(character.CardPool.AllCards.Count(c=>c.Rarity==CardRarity.Common)==3,"Three droppable common cards");
            foreach(bool up in new[]{false,true})
            {
                foreach(var canonical in character.CardPool.AllCards)
                {
                    var c=canonical.ToMutable(); if(up)c.UpgradeInternal();
                    int cost=c switch { WylderClawMovement => up?0:1, WylderStampUpwardCut=>2, _=>1 };
                    Check(c.EnergyCost.GetResolved()==cost,"Cost "+c.GetType().Name);
                    if(c is WylderClawMovement)Check(c.Keywords.Contains(CardKeyword.Retain),"Claw retains");
                    decimal damage=c switch { WylderStrike=>up?9:6, WylderDashAttack=>up?11:9,
                        WylderStampUpwardCut=>up?18:16, WylderStampSweep=>up?13:10, _=>0 };
                    if(damage>0)Check(c.DynamicVars.Damage.BaseValue==damage,"Damage "+c.GetType().Name);
                    if(c is WylderDefend or WylderSlidingLeap)
                        Check(c.DynamicVars.Block.BaseValue==(c is WylderDefend?(up?8:5):(up?10:7)),"Block");
                    if(c is WylderDashAttack)Check(c.DynamicVars["Vulnerable"].BaseValue==(up?2:1),"Vulnerable");
                    if(c is WylderStampUpwardCut)Check(c.DynamicVars["StrengthLoss"].BaseValue==(up?13:10),"Strength loss");
                    if(c is WylderSlidingLeap)Check(!c.DynamicVars["Energy"].WasJustUpgraded,"Unchanged energy not green");
                }
            }
            var owner=TrackingArrowRegression.MakePlayer();
            var creature=new Creature(owner,80,80){CombatState=new CombatState()};
            AccessTools.Field(typeof(Player),"<Creature>k__BackingField").SetValue(owner,creature);
            CardModel Card<T>() where T:CardModel
            {
                var c=ModelDb.Card<T>().ToMutable(); AccessTools.Field(typeof(CardModel),"_owner").SetValue(c,owner); return c;
            }
            WylderPursuitPower Power()
            {
                var p=(WylderPursuitPower)ModelDb.Power<WylderPursuitPower>().ToMutable();
                AccessTools.Property(typeof(PowerModel),nameof(PowerModel.Owner)).SetValue(p,creature);
                ((List<PowerModel>)AccessTools.Field(typeof(Creature),"_powers").GetValue(creature)!).Add(p);
                return p;
            }
            var strike=Card<WylderStrike>();var skill=Card<WylderDefend>();var claw=Card<WylderClawMovement>();
            var multiplier=AccessTools.Method(typeof(WylderPursuitPower),"AttackMultiplier");
            var p=Power();
            var sweep=Card<WylderStampSweep>();
            Check(sweep.TargetType==TargetType.AllEnemies,"Sweep uses native AOE targeting while pursuing");
            Check((bool)AccessTools.PropertyGetter(typeof(WylderCard),"ShouldGlowGoldInternal").Invoke(sweep,null)!,"Pursuit condition glow");
            var nativeModifier=typeof(PowerModel).GetMethods().Single(m=>m.Name=="ModifyDamageMultiplicative");
            var modifierArgs=nativeModifier.GetParameters().Select(parameter=>parameter.ParameterType==typeof(Creature)?(object)creature
                :parameter.ParameterType==typeof(ValueProp)?ValueProp.Move
                :parameter.ParameterType==typeof(CardModel)?strike
                :parameter.ParameterType==typeof(decimal)?9m:null).ToArray();
            Check((decimal)nativeModifier.Invoke(p,modifierArgs)! == 1.5m,"Runtime damage patch");
            Check((decimal)multiplier.Invoke(p,new object[]{creature,ValueProp.Move,strike})! ==1.5m,"Pursuit 50% attack bonus");
            Check((decimal)multiplier.Invoke(p,new object[]{creature,ValueProp.Unpowered,strike})! ==1m,"No passive bonus");
            Check((decimal)multiplier.Invoke(p,new object[]{creature,ValueProp.Move,skill})! ==1m,"No skill damage bonus");
            p.BeforeSideTurnEnd(null!,default,new[]{creature}).GetAwaiter().GetResult();
            Check(_removes==0,"Pursuit survives turn end");
            var play=TrackingArrowRegression.MakePlay(strike,owner);
            p.BeforeCardPlayed(play).GetAwaiter().GetResult();p.AfterCardPlayedLate(null!,play).GetAwaiter().GetResult();
            Check(_removes==1 && _draws==0,"Attack consumes without draw");
            Check(sweep.TargetType==TargetType.AnyEnemy,"Sweep returns to single target after pursuit");
            p=Power(); play=TrackingArrowRegression.MakePlay(skill,owner);
            p.BeforeCardPlayed(play).GetAwaiter().GetResult();p.AfterCardPlayedLate(null!,play).GetAwaiter().GetResult();
            p.AfterCardPlayedLate(null!,play).GetAwaiter().GetResult();
            Check(_removes==2 && _draws==1,"Skill consumes once and draws one");
            p=Power();play=TrackingArrowRegression.MakePlay(claw,owner);
            p.BeforeCardPlayed(play).GetAwaiter().GetResult();
            WylderPursuitPower.Enter(null!,creature,claw).GetAwaiter().GetResult();
            p.AfterCardPlayedLate(null!,play).GetAwaiter().GetResult();
            Check(_removes==2 && _draws==2 && creature.HasPower<WylderPursuitPower>(),"Claw re-entry survives but old pursuit draws");
            // A newly applied pursuit was not present for BeforeCardPlayed.
            ((List<PowerModel>)AccessTools.Field(typeof(Creature),"_powers").GetValue(creature)!).Clear();
            p=Power();p.AfterCardPlayedLate(null!,play).GetAwaiter().GetResult();
            Check(_removes==2 && _draws==2,"New pursuit not consumed by granting skill");
            Console.WriteLine("PASS: Wylder 10-card deck, 3 common drops, all 7 base/upgraded costs/stats, unchanged upgrade color, pursuit damage/turn retention/skill draw/re-entry.");
            var inject=typeof(ModelDb).GetMethod("Inject",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!;
            inject.Invoke(null,new object[]{typeof(WylderBreakProbe)});
            WylderBreakProbe Probe()
            {
                var card=(WylderBreakProbe)ModelDb.Card<WylderBreakProbe>().ToMutable();
                AccessTools.Field(typeof(CardModel),"_owner").SetValue(card,owner);return card;
            }
            Creature Enemy()
            {
                var target=new Creature(owner,100,100);
                AccessTools.Field(typeof(Creature),"<Side>k__BackingField").SetValue(target,CombatSide.Enemy);
                return target;
            }
            var probe=Probe();var copy=Probe();var a=Enemy();var b=Enemy();
            var probePlay=TrackingArrowRegression.MakePlay(probe,owner);
            probe.BeforeCardPlayed(probePlay).GetAwaiter().GetResult();
            copy.BeforeCardPlayed(TrackingArrowRegression.MakePlay(copy,owner)).GetAwaiter().GetResult();
            void Hit(WylderBreakProbe card,Creature target,int raw,int blocked=0,CardModel? source=null,ValueProp props=ValueProp.Move)
            {
                var result=new DamageResult(target,props){UnblockedDamage=raw,BlockedDamage=blocked};
                card.AfterDamageGiven(null!,creature,result,props,target,source??card).GetAwaiter().GetResult();
            }
            Hit(probe,a,7,100);Hit(probe,b,8);
            Check(probe.Triggers==0,"Break does not count blocked damage or combine separate enemies");
            Hit(copy,a,10);Hit(probe,a,7);
            Check(probe.Triggers==0 && copy.Triggers==0,"Break copies have independent ledgers");
            Hit(probe,a,1);Hit(probe,a,20);Hit(probe,b,30);
            Check(probe.Triggers==1,"Break exact threshold and only once per play");
            probe.BeforeCardPlayed(probePlay).GetAwaiter().GetResult();
            Hit(probe,a,20,source:strike);Hit(probe,a,20,props:ValueProp.Unpowered);
            Check(probe.Triggers==1,"Break ignores other cards and passive damage");
            Hit(probe,a,15);Check(probe.Triggers==2,"Break resets next play");
            probe.AfterCardPlayedLate(null!,probePlay).GetAwaiter().GetResult();
            Hit(probe,a,100);Check(probe.Triggers==2,"Break only while resolving");
            Console.WriteLine("PASS: Break unblocked damage, exact threshold, same-target multihit accumulation, separate AOE targets, once per play, isolated copies, reset and source/passive filtering.");
        }
        finally{h.UnpatchAll(h.Id);}
    }
    public static bool Remove(PowerModel power,ref Task __result)
    {
        _removes++; ((List<PowerModel>)AccessTools.Field(typeof(Creature),"_powers").GetValue(power.Owner)!).Remove(power);
        __result=Task.CompletedTask;return false;
    }
    public static bool Draw(decimal count,ref Task<IEnumerable<CardModel>> __result)
    { _draws+=(int)count;__result=Task.FromResult<IEnumerable<CardModel>>(Array.Empty<CardModel>());return false; }
    private static void Check(bool value,string message){if(!value)throw new Exception("Wylder: "+message);}
}

// Test-only model, injected into the standalone fixture, never the mod/card pool.
public sealed class WylderBreakProbe : WylderCard
{
    public int Triggers;
    public WylderBreakProbe():base(0,CardType.Attack,CardRarity.Common,TargetType.AnyEnemy){}
    protected override decimal BreakThreshold=>15m;
    protected override Task OnBreak(PlayerChoiceContext context){Triggers++;return Task.CompletedTask;}
    protected override Task OnPlay(PlayerChoiceContext context,CardPlay play)=>Task.CompletedTask;
}
