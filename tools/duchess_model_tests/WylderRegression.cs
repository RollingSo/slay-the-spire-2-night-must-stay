using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;

internal static class WylderRegression
{
    private sealed record TestPowerTip(AbstractModel CanonicalModel) : MegaCrit.Sts2.Core.HoverTips.IHoverTip
    {
        public string Id => CanonicalModel.Id.Entry;
        public bool IsSmart => false;
        public bool IsDebuff => false;
        public bool IsInstanced => false;
    }
    private static bool PowerTip(PowerModel model, ref MegaCrit.Sts2.Core.HoverTips.IHoverTip __result)
    { __result = new TestPowerTip(model); return false; }
    private static int _draws, _removes, _energy;
    public static void Run()
    {
        var h=new HarmonyLib.Harmony("night-must-stay.tests.wylder");
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderPursuitDamageBranchPatch")!).Patch();
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderSweepTargetPatch")!).Patch();
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderSmallShopPoolPatch")!).Patch();
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderSmallRewardPoolPatch")!).Patch();
        h.Patch(typeof(PowerCmd).GetMethods().Single(m=>m.Name=="Remove" && !m.IsGenericMethod),
            prefix:new HarmonyMethod(typeof(WylderRegression),nameof(Remove)));
        h.Patch(AccessTools.Method(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy)),
            prefix:new HarmonyMethod(typeof(WylderRegression),nameof(GainEnergy)));
        h.Patch(typeof(CardPileCmd).GetMethods().Single(m=>m.Name=="Draw" && m.GetParameters().Length==4),
            prefix:new HarmonyMethod(typeof(WylderRegression),nameof(Draw)));
        h.Patch(AccessTools.PropertyGetter(typeof(CombatManager),nameof(CombatManager.IsInProgress)),
            prefix:new HarmonyMethod(typeof(ReversalStepRegression),nameof(ReversalStepRegression.InCombat)));
        h.Patch(AccessTools.PropertyGetter(typeof(CombatManager),nameof(CombatManager.IsOverOrEnding)),
            prefix:new HarmonyMethod(typeof(MajestyRegression),nameof(MajestyRegression.CombatEndingFixture)));
        try
        {
            var character=ModelDb.Character<Wylder>();
            h.Patch(AccessTools.Method(typeof(MegaCrit.Sts2.Core.HoverTips.HoverTipFactory), "FromPower", new[] { typeof(PowerModel), typeof(int?) }), prefix: new HarmonyMethod(typeof(WylderRegression), nameof(PowerTip)));
            var extraTips = typeof(WylderCard).GetProperty("ExtraHoverTips", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var getter = new System.Reflection.Emit.DynamicMethod("BaseWylderTips", typeof(IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip>), new[] { typeof(WylderCard) }, typeof(WylderRegression), true);
            var il = getter.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Call, extraTips.GetMethod!);
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            var getTips = (Func<WylderCard, IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip>>)getter.CreateDelegate(typeof(Func<WylderCard, IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip>>));
            foreach (var template in character.CardPool.AllCards.OfType<WylderCard>())
            foreach (bool upgraded in new[] { false, true })
            {
                var card = (WylderCard)template.ToMutable();
                if (upgraded) card.UpgradeInternal();
                var tips = getTips(card);
                bool expected = card.HasBreak || card is WylderVeteranEdge or WylderEruption or WylderMultiplayerCard;
                bool hasBreakTip = tips.Any(tip => tip.Id == ModelDb.Power<WylderBreakDescriptionPower>().Id.Entry);
                Check(hasBreakTip == expected, $"Relevant Break tooltip only: {card.Id}, upgraded={upgraded}");
            }
            Check(character.StartingHp == 78, "Wylder starting HP is 78");
            var defendFilter = AccessTools.Method(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Models.GuardianCardFilters"),
                "HasDefendInName", new[] { typeof(string), typeof(string), typeof(bool) });
            Check((bool)defendFilter.Invoke(null, new object[] { "WYLDER_TURNING_GUARD", "Turning Defense", true })!,
                "Turning Defense canonical Defend membership survives localization");
            Check(!(bool)defendFilter.Invoke(null, new object[] { "WYLDER_STEADY_BREATH", "Defend", true })!,
                "Unrelated Wylder cards cannot enter Defend set through translated title");
            Check(ModelDb.Card<WylderStampSweep>().TargetType == TargetType.AnyEnemy, "Canonical sweep library target is safe");
            var authoredPool = character.CardPool.AllCards.Where(c => c.Rarity == CardRarity.Common).ToArray();
            var shopRepair = AccessTools.Method(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderSmallShopPoolPatch"), "Restore");
            foreach (var requested in new[] { CardType.Attack, CardType.Skill, CardType.Power })
            {
                object[] repairArgs = { authoredPool, Array.Empty<CardModel>(), requested };
                shopRepair.Invoke(null, repairArgs);
                var repaired = ((IEnumerable<CardModel>)repairArgs[1]).ToArray();
                Check(repaired.Any(c => c.Type == (CardType)repairArgs[2] && c.Rarity == CardRarity.Common), "Exhausted shop slot fallback " + requested);
                Check(repaired.All(c => c is WylderCard && c.Rarity != CardRarity.Basic), "Shop fallback stays in authored pool");
            }
            var rewardRepair = AccessTools.Method(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderSmallRewardPoolPatch"), "Restore");
            object[] rewardArgs = { authoredPool, character.CardPool.AllCards.ToArray() };
            rewardRepair.Invoke(null, rewardArgs);
            Check(!((IEnumerable<CardModel>)rewardArgs[1]).Any(), "Reward pool exhaustion resets blacklist");
            Check(character.MapDrawingColor == character.CardPool.DeckEntryCardColor
                && character.MapDrawingColor == new Godot.Color("252E4A"), "Coat-navy shared theme");
            var deck=character.StartingDeck.ToArray();
            Check(deck.Length==10 && deck.Count(c=>c is WylderStrike)==4 && deck.Count(c=>c is WylderDefend)==4
                && deck.Count(c=>c is WylderClawMovement)==1 && deck.Count(c=>c is WylderDashAttack)==1,"Starting deck");
            Check(character.CardPool.AllCards.Count()==90,"90 registered cards");
            Check(character.CardPool.AllCards.Select(c=>c.Id).Distinct().Count()==90,"Unique registered IDs");
            Check(!character.CardPool.AllCards.Any(c=>c is WylderExecution),"Removed cards excluded from pool");
            foreach(bool up in new[]{false,true})
            {
                foreach(var canonical in character.CardPool.AllCards)
                {
                    var c=canonical.ToMutable(); if(up && c.MaxUpgradeLevel > 0)c.UpgradeInternal();
                    int cost=c switch { WylderFlamelightOnSnow => up?1:2, WylderTriumph => 0,
                        WylderSiluriaVortex or WylderVerticalCleave => 2, WylderEndlessWill or WylderLastResort or WylderShriekOfMilos or WylderTwinToken or WylderGrappleShot => up?0:1, WylderGamble or WylderLoneCourage or WylderAgility => 0, WylderHeroesAssemble => up?0:1, WylderEndTheNight => 5, WylderTrueFlame => up?4:5, WylderDesperateFlurry => 3, WylderClawMovement or WylderAncientClawLeap or WylderMagnanimity or WylderOffensiveInstinct => up?0:1,
                        WylderRuinousGhostflame => 1,
                        WylderSpark or WylderResolve or WylderDebut or WylderSixthSense or WylderBideTheMoment => 0,
                        WylderExecution => up?1:2,
                        WylderUndying => up?0:1,
                        WylderGoldenParry or WylderNightForm => 3, WylderMartialMastery => up?1:2, WylderDefiance => 2, WylderMultiplayerCard or WylderTinder or WylderBreakthrough or WylderLifeFromDeath => 0,
                        WylderAerialStake => up?2:3,
                        WylderCounterRush => 3,
                        WylderTacticalRetreat or WylderParry or WylderDesperateSurvival => 4,
                        WylderSlidingLeap => 2,
                        WylderSidestep or WylderRelentlessAdvance or WylderForcedBreakout or WylderNightRespite => 0,
                        WylderAssaultDrop or WylderCriticalStrike => 3,
                        WylderAirCleavingChase or WylderTakersFlames or WylderInfernalSlash or WylderFlameChaseSlash
                            or WylderCounteroffensive or WylderCliffStep or WylderAllInDuel or WylderBattlefieldMight
                            or WylderPursuitStance or WylderNightEdge => 2,
                        WylderShieldPress or WylderLightStepGuard or WylderGroundDash or WylderPatientFocus or WylderStormBlade or WylderEochaidsDancingBlade => 0,
                        WylderAssaultStake => 3,
                        WylderStampUpwardCut
                            or WylderLionsClaw or WylderCliffbreaker
                            or WylderJumpingHeavy or WylderDualGreatswordLeap or WylderGhostflameExplosion or WylderVerticalPressure
                            or WylderLastStandGuard or WylderFinalGuard => 2, _=>1 };
                    if(c is WylderIntenseBurning) Check(c.EnergyCost.CostsX,"Intense Burning X cost"); else Check(c.EnergyCost.GetResolved()==cost,"Cost "+c.GetType().Name);
                    if(c is WylderValor)Check(c.Type==CardType.Skill && c.Rarity==CardRarity.Common && c.TargetType==TargetType.Self && c.DynamicVars.Block.BaseValue==(up?7:6) && c.DynamicVars["Strength"].BaseValue==(up?3:2),"Valor metadata and upgrades");
                    if(c is WylderEruption)Check(c.Type==CardType.Power && c.Rarity==CardRarity.Uncommon && c.DynamicVars["Damage"].BaseValue==(up?9:7),"Eruption stats");
                    if(c is WylderCounterRush)Check(c.DynamicVars.Damage.BaseValue==(up?14:12) && c.DynamicVars["Imbalance"].BaseValue==(up?3:2),"Recoil Stake values");
                    if(c is WylderDesperateSurvival)Check(c.TargetType==TargetType.AnyEnemy && c.Keywords.Contains(CardKeyword.Exhaust) && c.Keywords.Contains(CardKeyword.Retain)==up && c.DynamicVars["Threshold"].BaseValue==50,"Desperate Survival targeting, threshold and keywords");
                    if(c is WylderCorpseWaxCutter)Check(c.Rarity==CardRarity.Uncommon && c.DynamicVars.Damage.BaseValue==(up?14:10) && c.DynamicVars["Break"].BaseValue==20 && c.DynamicVars["Vulnerable"].BaseValue==(up?3:2) && c.DynamicVars["Weak"].BaseValue==(up?3:2),"Corpse Wax Cutter rarity and values");
                    if(c is WylderWildfire)Check(c.TargetType==TargetType.Self && c.Keywords.Contains(CardKeyword.Innate)==up,"Spreading Wildfire target and Innate upgrade");
                    if(c is WylderNightForm)Check(c.Rarity==CardRarity.Rare && c.Keywords.Contains(CardKeyword.Retain)==up,"Night Form Retain upgrade");
                    if(c is WylderClawMovement)Check(c.Keywords.Contains(CardKeyword.Retain),"Claw retains");
                    decimal damage=c switch { WylderStrike=>up?9:6, WylderDashAttack=>up?11:9,
                        WylderStampUpwardCut=>up?18:16, WylderStampSweep=>up?13:10, _=>0 };
                    if(damage>0)Check(c.DynamicVars.Damage.BaseValue==damage,"Damage "+c.GetType().Name);
                    if(c is WylderDefend or WylderCenteringBreath)
                        Check(c.DynamicVars.Block.BaseValue==(c is WylderDefend?(up?8:5):(up?7:6)),"Block");
                    if(c is WylderDashAttack)Check(c.DynamicVars["Vulnerable"].BaseValue==(up?2:1),"Vulnerable");
                    if(c is WylderStampUpwardCut)Check(c.DynamicVars["StrengthLoss"].BaseValue==(up?6:5),"Strength loss");
                    if(c is WylderCenteringBreath)Check(c.DynamicVars.Energy.BaseValue==(up?3:2),"Centering Breath energy");
                    if(c is WylderInspire)Check(c.Type==CardType.Skill && c.Rarity==CardRarity.Uncommon
                        && c.TargetType==TargetType.Self && c.DynamicVars.Cards.BaseValue==(up?3:2)
                        && c.DynamicVars["HandThreshold"].BaseValue==2,"Inspire metadata and upgrade");
                    if(c is WylderDualGreatswordLeap)Check(c.DynamicVars.Energy.BaseValue==1,"Dual Leap Break energy");
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
            h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderUndyingVigorPatch")!).Patch();
            h.Patch(AccessTools.Method(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount)),
                prefix:new HarmonyMethod(typeof(WylderBattleArtRegression), nameof(WylderBattleArtRegression.Modify)));
            T Attach<T>(int amount) where T : PowerModel
            {
                var power = (T)ModelDb.Power<T>().ToMutable();
                AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(power, creature);
                ((List<PowerModel>)AccessTools.Field(typeof(Creature), "_powers").GetValue(creature)!).Add(power);
                power.SetAmount(amount, true);
                return power;
            }
            Attach<WylderUndyingPower>(1);
            var vigor = Attach<MegaCrit.Sts2.Core.Models.Powers.VigorPower>(7);
            foreach (var remaining in new[] { 5, 2, 1, 0 })
            {
                var attack = DamageCmd.Attack(1);
                AccessTools.Property(attack.GetType(), "Attacker").SetValue(attack, creature);
                vigor.BeforeAttack(attack).GetAwaiter().GetResult();
                if (remaining == 5) vigor.SetAmount(9, true); // two gained after the initial snapshot
                vigor.AfterAttack(null!, attack).GetAwaiter().GetResult();
                Check(vigor.Amount == remaining, "Undying Vigor retention across successive attacks");
            }
            Console.WriteLine("PASS: Undying preserves later gains, rounds down and triggers on successive attacks.");
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
            Check(_removes==2 && _draws==1 && _energy==1,"Skill consumes once, gains one energy and draws one");
            var consumesOnPlay=AccessTools.Method(typeof(WylderPursuitPower),"ConsumesOnPlay");
            Check((bool)consumesOnPlay.Invoke(null,new object[]{CardType.Attack})!
                && (bool)consumesOnPlay.Invoke(null,new object[]{CardType.Skill})!
                && !(bool)consumesOnPlay.Invoke(null,new object[]{CardType.Power})!
                && !(bool)consumesOnPlay.Invoke(null,new object[]{CardType.Status})!,
                "Only Attack and Skill cards consume Pursuit");
            p=Power();var ability=Card<WylderEruption>();play=TrackingArrowRegression.MakePlay(ability,owner);
            p.BeforeCardPlayed(play).GetAwaiter().GetResult();p.AfterCardPlayedLate(null!,play).GetAwaiter().GetResult();
            Check(_removes==2 && creature.HasPower<WylderPursuitPower>(),"Power card preserves Pursuit without rewards");
            play=TrackingArrowRegression.MakePlay(claw,owner);
            p.BeforeCardPlayed(play).GetAwaiter().GetResult();
            WylderPursuitPower.Enter(null!,creature,claw).GetAwaiter().GetResult();
            p.AfterCardPlayedLate(null!,play).GetAwaiter().GetResult();
            Check(_removes==2 && _draws==1 && creature.HasPower<WylderPursuitPower>(),"Attack-type Claw re-entry survives without skill draw");
            // A newly applied pursuit was not present for BeforeCardPlayed.
            ((List<PowerModel>)AccessTools.Field(typeof(Creature),"_powers").GetValue(creature)!).Clear();
            p=Power();p.AfterCardPlayedLate(null!,play).GetAwaiter().GetResult();
            Check(_removes==2 && _draws==1,"New pursuit not consumed by granting skill");
            Attach<WylderNightFormPower>(1);
            Check((decimal)nativeModifier.Invoke(p,modifierArgs)! == 2m,"Night Form doubles Pursuit damage bonus");
            play=TrackingArrowRegression.MakePlay(skill,owner);
            p.BeforeCardPlayed(play).GetAwaiter().GetResult();
            p.AfterCardPlayedLate(null!,play).GetAwaiter().GetResult();
            Check(_draws==3 && _energy==3,"Night Form grants two cards and two energy from Pursuit");
            Console.WriteLine("PASS: Wylder unchanged 10-card starting deck, 90 unique cards, revised rarity/type counts, base/upgraded costs/stats and Attack/Skill-only Pursuit consumption.");
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
            Hit(probe,a,2,5);Hit(probe,b,0,8);
            Check(probe.Triggers==0,"Break counts pre-block damage per enemy, not across enemies");
            Hit(copy,a,10);Hit(probe,a,7);
            Check(probe.Triggers==0 && copy.Triggers==0,"Break copies have independent ledgers");
            Hit(probe,a,0,1);Hit(probe,a,20);Hit(probe,b,30);
            Check(probe.Triggers==1,"Break exact threshold and only once per play");
            probe.BeforeCardPlayed(probePlay).GetAwaiter().GetResult();
            Hit(probe,a,20,source:strike);Hit(probe,a,20,props:ValueProp.Unpowered);
            Check(probe.Triggers==1,"Break ignores other cards and passive damage");
            Hit(probe,a,15);Check(probe.Triggers==2,"Break resets next play");
            probe.AfterCardPlayedLate(null!,probePlay).GetAwaiter().GetResult();
            Hit(probe,a,100);Check(probe.Triggers==2,"Break only while resolving");
            probe.BeforeCardPlayed(probePlay).GetAwaiter().GetResult();
            Hit(probe,a,0,15);Check(probe.Triggers==3,"Fully blocked attack meets Break threshold");
            Console.WriteLine("PASS: Break uses pre-block damage including fully blocked hits, requires one qualifying hit for single-hit cards, never accumulates sub-threshold hits, and preserves target/copy/play isolation.");
        }
        finally{h.UnpatchAll(h.Id);}
    }
    public static bool Remove(PowerModel power,ref Task __result)
    {
        _removes++; ((List<PowerModel>)AccessTools.Field(typeof(Creature),"_powers").GetValue(power.Owner)!).Remove(power);
        __result=Task.CompletedTask;return false;
    }
    public static bool GainEnergy(decimal amount, ref Task __result)
    { _energy += (int)amount; __result = Task.CompletedTask; return false; }
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
