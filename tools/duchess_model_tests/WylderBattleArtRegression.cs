using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;
using SmartFormat;
using SmartFormat.Extensions;

// Exercise actual card callbacks, native damage/block previews and attack
// builders. Only UI-dependent command execution/power mutation is stubbed.
internal static partial class WylderBattleArtRegression
{
    private record Spec(CardModel Model, int Cost, CardRarity Rarity, int Damage, int UpgradeDamage,
        int Break = 0, int Hits = 1, int Block = 0, int UpgradeBlock = 0, bool Aoe = false);
    private static readonly List<(string Kind, Creature? Target, decimal Amount, CardPlay? Play)> Rewards = new();
    private static readonly List<(bool Aoe, int Hits, decimal Raw)> Attacks = new();
    private static readonly List<decimal> ResolvedDamage = new();
    private static readonly List<CardModel> Generated = new();
    private static Player _owner = null!;
    private static CombatState _combat = null!;
    private static Creature[] _enemies = Array.Empty<Creature>();
    private static bool _fullyBlocked;
    private static int _imbalancePlayerCount = 1;
    private static int _checks;

    private static Spec[] Specs() => new[]
    {
        new Spec(ModelDb.Card<WylderVacuumSlice>(), 1, CardRarity.Common, 9, 12, 20, Aoe: true),
        new Spec(ModelDb.Card<WylderCriticalStrike>(), 3, CardRarity.Uncommon, 16, 21),
        new Spec(ModelDb.Card<WylderStormBlade>(), 0, CardRarity.Uncommon, 4, 5),
        new Spec(ModelDb.Card<WylderFlamingStrike>(), 1, CardRarity.Uncommon, 8, 8),
        new Spec(ModelDb.Card<WylderFlameOfRedmanes>(), 1, CardRarity.Common, 6, 7, Aoe: true),
        new Spec(ModelDb.Card<WylderBloodhoundsFinesse>(), 1, CardRarity.Uncommon, 4, 6, Block: 4, UpgradeBlock: 6),
        new Spec(ModelDb.Card<WylderLionsClaw>(), 2, CardRarity.Uncommon, 18, 24, 40),
        new Spec(ModelDb.Card<WylderSpinningSlash>(), 1, CardRarity.Common, 4, 6, 12, 2, Aoe: true),
    };

    public static void Run()
    {
        var h = new HarmonyLib.Harmony("NightMustStay.Wylder.BattleArt.Regression");
        void Patch(MethodBase method, string prefix) => h.Patch(method,
            prefix: new HarmonyMethod(typeof(WylderBattleArtRegression), prefix));
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderPursuitDamageBranchPatch")!).Patch();
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderCaptureHandSourcePatch")!).Patch();
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderRecordHandSourcePatch")!).Patch();
        h.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)),
            prefix: new HarmonyMethod(typeof(ReversalStepRegression), nameof(ReversalStepRegression.InCombat)));
        h.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)),
            prefix: new HarmonyMethod(typeof(MajestyRegression), nameof(MajestyRegression.CombatEndingFixture)));
        h.Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.CombatState)),
            prefix: new HarmonyMethod(typeof(MajestyRegression), nameof(MajestyRegression.CombatStateFixture)));
        // The minimal fixture omits native Character/Unlock models. Keep the
        // real energy helper/formatter, supplying only this verified pool lookup.
        Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Pool)), nameof(Pool));
        Patch(AccessTools.Method(typeof(CombatState), nameof(CombatState.IterateHookListeners)), nameof(Listeners));
        Patch(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute)), nameof(Attack));
        Patch(AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardToCombat)), nameof(Generate));
        Patch(AccessTools.Method(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy)), nameof(Energy));
        Patch(typeof(CreatureCmd).GetMethods().Single(m => m.Name == "GainBlock" && m.GetParameters()[1].ParameterType == typeof(decimal)), nameof(Block));
        Patch(typeof(CardPileCmd).GetMethods().Single(m => m.Name == "Draw" && m.GetParameters().Length == 4), nameof(Draw));
        Patch(typeof(PowerCmd).GetMethods().Single(m => m.Name == "Apply" && !m.IsGenericMethod), nameof(Apply));
        Patch(AccessTools.Method(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount)), nameof(Modify));
        Patch(typeof(PowerCmd).GetMethods().Single(m => m.Name == "Remove" && !m.IsGenericMethod), nameof(Remove));
        Patch(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Stun), new[] { typeof(Creature), typeof(string) }), nameof(Stun));
        try
        {
            Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsEnding)), nameof(NotEnding));
            VortexCards();
            KickAndPoiseCards();
            FinalThreeCards();
            NovemberCards();
            SparkAndRollingAttack();
            InspireRevision();
            MetadataAndText();
            foreach (bool upgraded in new[] { false, true })
            foreach (Spec spec in Specs())
            foreach (bool pursuit in new[] { false, true })
            {
                Fixture(pursuit);
                CardModel card = Card(spec.Model, upgraded);
                decimal raw = upgraded ? spec.UpgradeDamage : spec.Damage;
                card.DynamicVars.Damage.UpdateCardPreview(card, CardPreviewMode.Normal, _enemies[0], true);
                Check(card.DynamicVars.Damage.PreviewValue == raw * (pursuit ? 1.5m : 1m), "Native Pursuit preview " + card.Id);
                Play(card);
                int hits = card is WylderStormBlade && pursuit ? 3 : spec.Hits;
                Check(Attacks.Count == 1 && Attacks[0] == (spec.Aoe, hits, raw), "One native attack builder/hit count/targeting " + card.Id);
                Check(ResolvedDamage[0] == Math.Floor(raw * (pursuit ? 1.5m : 1m)), "Preview agrees with first resolved hit " + card.Id);
                if (card is WylderStormBlade)
                    Check(Rewards.Count == 0 && ResolvedDamage.Count == hits, "Storm native additional hits, no obsolete draw");
                if (card is WylderFlamingStrike)
                    Check(Generated.Count == 1 && Generated[0] is WylderFireCoating && Generated[0].IsUpgraded == upgraded,
                        "Flaming Strike generates matching upgraded Fire Coating per actual CardPlay");
                if (card is WylderFlameOfRedmanes)
                    Check(Rewards.Single().Kind == "VigorPower" && Rewards.Single().Amount == (upgraded ? 5 : 4),
                        "Redmanes gains Vigor once after AOE, not per enemy or Break");
                if (card is WylderSpinningSlash)
                    Check(Rewards.Count(r => r.Kind == "Draw") == 0, "Spinning requires every hit to reach Break12");
                if (card is WylderBloodhoundsFinesse)
                {
                    Check(Rewards.Single().Kind == "Block" && Rewards.Single().Amount == (upgraded ? 6 : 4), "Finesse native Block");
                    Check(_owner.Creature.HasPower<WylderPursuitPower>(), "Finesse new/reentered Pursuit survives cleanup");
                    var followup = Card(ModelDb.Card<WylderStrike>(), false);
                    Play(followup);
                    Check(!_owner.Creature.HasPower<WylderPursuitPower>(), "Next attack consumes Finesse Pursuit normally");
                }
                else Check(!_owner.Creature.HasPower<WylderPursuitPower>(), "Ordinary battle art consumes Pursuit");
            }
            BreakBoundaries();
            VacuumRevision();
            StormBladeRevision();
            WylderNewRevisionsRegression.Run();
            WylderEochaidRegression.Run();
            FlameRevisionCards();
            FinesseRevision();
            LionsClawRevision();
            WylderParryRegression.Run();
            WylderStunReturnRegression.Run();
            // Native additive Strength + target Vulnerable + Pursuit all reach
            // the same damage variable used by the actual card's attack.
            Fixture(true);
            AddPower<StrengthPower>(_owner.Creature, 2);
            AddPower<VulnerablePower>(_enemies[0], 1);
            var vacuum = Card(ModelDb.Card<WylderVacuumSlice>(), false);
            vacuum.DynamicVars.Damage.UpdateCardPreview(vacuum, CardPreviewMode.Normal, _enemies[0], true);
            Check(vacuum.DynamicVars.Damage.PreviewValue == 24.75m, "Strength/Vulnerable/Pursuit native preview");
            Play(vacuum);
            Check(ResolvedDamage.SequenceEqual(new decimal[] { 24, 16 }) && Rewards.Count == 2
                && Rewards.All(reward => reward.Kind == nameof(WeakPower) && reward.Amount == 2), "Modified AOE damage crosses Break20 on one target and applies Weak once to each enemy");
            Fixture();
            AddPower<StrengthPower>(_owner.Creature, 3);
            AddPower<VulnerablePower>(_enemies[0], 1);
            var lion = Card(ModelDb.Card<WylderLionsClaw>(), true);
            lion.DynamicVars.Damage.UpdateCardPreview(lion, CardPreviewMode.Normal, _enemies[0], true);
            Check(lion.DynamicVars.Damage.PreviewValue == 40.5m, "Lion native Strength/Vulnerable preview");
            Play(lion);
            Check(ResolvedDamage.Single() == 40
                && Rewards.Any(r => r.Kind == nameof(PhantomImbalancePower) && r.Amount == 3 && r.Target == _enemies[0]),
                "Upgraded Lion actual modified damage reaches Break40 and applies Imbalance3 to the struck enemy");
            Console.WriteLine($"PASS: {_checks} Wylder battle-art checks: base/upgraded models, four-locale rendered text/energy icon, native damage/block previews, native multihit/AOE builders, Break rewards and Finesse Pursuit cleanup (UI commands stubbed).");
            ReviewedPlannerCards();
            HorizontalReviewCards();
            SheetRevisionCards();
            SheetLifecycle();
        }
        finally { h.UnpatchAll(h.Id); }
    }

    private static int _sheetAutoPlays, _sheetDeckRemovals, _sheetHeals;
    private static void SettleSheetPlay()
    {
        var play=(CardPlay)AccessTools.Method(typeof(WylderCombatEvents),"Current").Invoke(null,new object[]{_owner})!;
        WylderCombatEvents.CompleteCard(null!,play).GetAwaiter().GetResult();
    }
    private static void SheetLifecycle()
    {
        var h=new HarmonyLib.Harmony("NightMustStay.SheetLifecycle");
        h.Patch(AccessTools.Method(typeof(CardCmd),nameof(CardCmd.AutoPlay)),prefix:new HarmonyMethod(typeof(WylderBattleArtRegression),nameof(SheetAutoPlay)));
        h.Patch(AccessTools.Method(typeof(CreatureCmd),nameof(CreatureCmd.Heal)),prefix:new HarmonyMethod(typeof(WylderBattleArtRegression),nameof(SheetHeal)));
        h.Patch(AccessTools.Method(typeof(CardPileCmd),nameof(CardPileCmd.RemoveFromDeck),new[]{typeof(CardModel),typeof(bool)}),prefix:new HarmonyMethod(typeof(WylderBattleArtRegression),nameof(SheetRemoveDeck)));
        h.Patch(AccessTools.Method(typeof(CardPileCmd),nameof(CardPileCmd.RemoveFromCombat),new[]{typeof(CardModel),typeof(bool)}),prefix:new HarmonyMethod(typeof(WylderBattleArtRegression),nameof(SheetRemoveCombat)));
        try
        {
            Fixture(true);_sheetAutoPlays=0;
            var follow=Card(ModelDb.Card<WylderDualGreatswordLeap>(),false);Put(follow,PileType.Hand);
            var rend=Card(ModelDb.Card<WylderClawRend>(),false);Put(rend,PileType.Hand);
            var flame=Card(ModelDb.Card<WylderFlameChaseSlash>(),false);Put(flame,PileType.Hand);
            var strike=Card(ModelDb.Card<WylderStrike>(),false);Put(strike,PileType.Hand);
            Play(strike);SettleSheetPlay();
            Check(_sheetAutoPlays==2 && follow.Pile?.Type==PileType.Hand && rend.Pile?.Type==PileType.Discard
                && flame.Pile?.Type==PileType.Discard,"Pursuit exit plays Claw Rend and Flame Chase but not Dual Leap");
            Fixture(true);_sheetAutoPlays=0;
            Put(Card(ModelDb.Card<WylderDualGreatswordLeap>(),false),PileType.Hand);
            var claw=Card(ModelDb.Card<WylderClawMovement>(),false);
            Play(claw);SettleSheetPlay();
            Check(Rewards.Count(r=>r.Kind=="Energy")==1 && Rewards.Count(r=>r.Kind=="Draw")==1
                && _owner.Creature.HasPower<WylderPursuitPower>(),
                "Claw Movement receives the old Pursuit Skill reward and keeps its new Pursuit");
            Check(_sheetAutoPlays==0,"Pursuit reentry does not trigger Follow Through");
            foreach(bool up in new[]{false,true})
            {
                Fixture();_sheetDeckRemovals=_sheetHeals=0;
                var deck=Card(ModelDb.Card<WylderSixthSense>(),up);Put(deck,PileType.Deck);
                var sense=Card(ModelDb.Card<WylderSixthSense>(),up);Put(sense,PileType.Draw);
                typeof(CardModel).GetProperty(nameof(CardModel.DeckVersion))!.SetValue(sense,deck);
                _owner.Creature.SetCurrentHpInternal(0);
                sense.AfterPreventingDeath(_owner.Creature).GetAwaiter().GetResult();
                Check(_sheetDeckRemovals==1 && _sheetHeals==1 && _owner.Creature.CurrentHp==(up?500:300),"Sixth Sense heals correct percent and removes permanent source");
                Check(sense.Pile==null && !PileType.Deck.GetPile(_owner).Cards.Contains(deck),"Sixth Sense removes combat and deck copies");
                sense.AfterPreventingDeath(_owner.Creature).GetAwaiter().GetResult();
                Check(_sheetHeals==1 && sense.ShouldDie(_owner.Creature),"Spent Sixth Sense cannot prevent another death");
            }
            Console.WriteLine("PASS: Follow Through native autoplay queue/reentry filtering; Sixth Sense 30/50% revival, permanent/combat removal and single-use lifetime.");
        }
        finally {h.UnpatchAll(h.Id);}
    }
    public static bool SheetAutoPlay(CardModel card,Creature target,ref Task __result)
    {
        if(card is not WylderFollowThroughCard)return true;
        _sheetAutoPlays++;Play(card,true,true,target:target);Put(card,PileType.Discard);SettleSheetPlay();
        __result=Task.CompletedTask;return false;
    }
    public static bool SheetHeal(Creature creature,decimal amount,ref Task __result)
    {_sheetHeals++;creature.SetCurrentHpInternal(Math.Min(creature.MaxHp,creature.CurrentHp+amount));__result=Task.CompletedTask;return false;}
    public static bool SheetRemoveDeck(CardModel card,ref Task __result)
    {_sheetDeckRemovals++;((List<CardModel>)AccessTools.Field(typeof(CardPile),"_cards").GetValue(card.Pile)!).Remove(card);__result=Task.CompletedTask;return false;}
    public static bool SheetRemoveCombat(CardModel card,ref Task __result)
    {((List<CardModel>)AccessTools.Field(typeof(CardPile),"_cards").GetValue(card.Pile)!).Remove(card);__result=Task.CompletedTask;return false;}

    private static void SheetRevisionCards()
    {
        int before = _checks;
        foreach (bool up in new[] { false, true })
        {
            Fixture();
            var valor = Card(ModelDb.Card<WylderValor>(), up);
            Play(valor);
            Check(Rewards.Single(r=>r.Kind=="Block").Amount==(up?7:6), "Valor block preview and resolution");
            Check(_owner.Creature.GetPowerAmount<WylderValorStrengthPower>()==(up?3:2), "Valor uses native temporary Strength");
            Fixture();
            var claw = Card(ModelDb.Card<WylderClawMovement>(), up);
            Check(claw.Type == CardType.Skill && claw.TargetType == TargetType.Self, "Claw is a self-targeted Skill");
            var veteran = Card(ModelDb.Card<WylderVeteranEdge>(), up);
            Check(veteran.EnergyCost.GetResolved() == 1 && veteran.Keywords.Contains(CardKeyword.Innate) == up, "Veteran cost and native Innate upgrade");
            Play(Card(ModelDb.Card<WylderClawPull>(), up));
            Check(Rewards.All(r => r.Kind != "Block") && _enemies[0].GetPowerAmount<WeakPower>() == (up ? 2 : 1) && _owner.Creature.HasPower<WylderPursuitPower>(), "Claw Pull applies Weak and enters Pursuit");
            Fixture();
            Play(Card(ModelDb.Card<WylderCounterRush>(),up));
            Check(Attacks.Single().Raw == (up ? 14 : 12) && Rewards.Any(r => r.Kind == nameof(PhantomImbalancePower) && r.Target == _enemies[0] && r.Amount == (up ? 3 : 2)), "Recoil Stake damage and applied Imbalance");
            Fixture();
            Play(Card(ModelDb.Card<WylderJumpingHeavy>(),up));
            Check(Attacks.Single().Raw == (up ? 17 : 13), "Jumping Heavy damage upgrade");
            var jump = Card(ModelDb.Card<WylderJumpingHeavy>(), up);
            Check(!jump.TryModifyEnergyCostInCombat(jump, 2, out var normalCost) && normalCost == 2, "Jumping Heavy normal cost");
            Fixture(true); jump = Card(ModelDb.Card<WylderJumpingHeavy>(), up);
            Check(jump.TryModifyEnergyCostInCombat(jump, 2, out var pursuitCost) && pursuitCost == 0, "Jumping Heavy Pursuit zero cost");
            Fixture();
            Play(Card(ModelDb.Card<WylderDebut>(),up));
            Check(Rewards.Single(r=>r.Kind=="Energy").Amount == (up ? 3 : 2) && _owner.Creature.HasPower<WylderPursuitPower>(), "Debut entry and energy");
            Fixture();
            Play(Card(ModelDb.Card<WylderAerialStake>(),up));
            Check(!Attacks.Any() && !Rewards.Any(r => r.Kind == nameof(PhantomImbalancePower)), "Aerial Stake requires Pursuit for both effects");
            Fixture(true);
            Play(Card(ModelDb.Card<WylderAerialStake>(),up));
            Check(Attacks.Single().Raw == 20 && Rewards.Any(r => r.Kind == nameof(PhantomImbalancePower) && r.Target == _enemies[0] && r.Amount == 3), "Aerial Stake pursuit damage and three Imbalance");
            Fixture();
            Play(Card(ModelDb.Card<WylderClawRend>(),up));
            Check(Attacks.Single().Raw == 6 && _enemies[0].GetPowerAmount<VulnerablePower>()==(up ? 2 : 1), "Claw Rend damage/Vulnerable");
            Fixture(true); AddPower<StrengthPower>(_owner.Creature,5); _fullyBlocked=true;
            var wax = Card(ModelDb.Card<WylderCorpseWaxCutter>(),up);
            Check(wax.Rarity==CardRarity.Uncommon && wax.TargetType==TargetType.AllEnemies,"Corpse Wax revised metadata");
            Play(wax);
            Check(Attacks.Single().Aoe && Attacks.Single().Raw==(up?14:10) && _enemies.All(e=>e.GetPowerAmount<VulnerablePower>()==(up?3:2) && e.GetPowerAmount<WeakPower>()==(up?3:2)),"Corpse Wax group Break applies both debuffs once");
            Fixture();
            var ghost = AddPower<WylderRuinousGhostflamePower>(_owner.Creature,1); ghost.UpgradedCopies=up?1:0;
            var attack=Card(ModelDb.Card<WylderStrike>(),false);
            foreach (int amount in new[] {20,21})
            {
                var result=new DamageResult(_enemies[0],ValueProp.Move){BlockedDamage=amount,UnblockedDamage=0};
                WylderCombatEvents.AfterDamage(null!,_owner.Creature,_enemies[0],result,ValueProp.Move,attack).GetAwaiter().GetResult();
                Check(_enemies[0].GetPowerAmount<FreezePower>()==(amount==20?0:up?3:2),"Ghostflame strict >20 with full Block and mixed upgrade support");
            }
            Fixture();
            var bold=AddPower<WylderBoldnessPower>(_owner.Creature,1);bold.UpgradedCopies=up?1:0;
            attack=Card(ModelDb.Card<WylderStrike>(),false);
            WylderCombatEvents.AfterDamage(null!,_owner.Creature,_enemies[0],new DamageResult(_enemies[0],ValueProp.Move){BlockedDamage=19},ValueProp.Move,attack).GetAwaiter().GetResult();
            Check(!Rewards.Any(r=>r.Kind=="Block"),"Boldness does not trigger below 20 damage");
            WylderCombatEvents.AfterDamage(null!,_owner.Creature,_enemies[0],new DamageResult(_enemies[0],ValueProp.Move){BlockedDamage=20},ValueProp.Move,attack).GetAwaiter().GetResult();
            Check(Rewards.Single(r=>r.Kind=="Block").Amount==(up?6:4),"Boldness triggers at exact 20 and includes blocked attack damage");
            Fixture();
            var sense=Card(ModelDb.Card<WylderSixthSense>(),up);
            Put(sense,PileType.Hand);
            Check(sense.EnergyCost.GetResolved()==4 && !sense.ShouldDie(_owner.Creature) && sense.ShouldDie(_enemies[0])
                && !sense.Keywords.Contains(CardKeyword.Unplayable) && sense is WylderDesperationCard,
                "Sixth Sense 4-cost death prevention owner and Desperation");
            Play(sense);
            Check(Rewards.Single(r=>r.Kind=="Block").Amount==10 && sense.DynamicVars.Block.BaseValue==10,
                "Sixth Sense gains 10 Block");
            Check(sense.DynamicVars["HealPercent"].BaseValue==(up?50:30),"Sixth Sense heal upgrade");
        }
        Console.WriteLine($"PASS: {_checks-before} sheet revision checks: revised metadata, native effects, full-Block thresholds, upgrades and death-prevention eligibility.");
    }

    private static void ReviewedPlannerCards()
    {
        int before = _checks;
        var models = new CardModel[] { ModelDb.Card<WylderFlameReady>(), ModelDb.Card<WylderSpineShock>(), ModelDb.Card<WylderFlameMomentum>(),
            ModelDb.Card<WylderCliffbreaker>(), ModelDb.Card<WylderDualGreatswordLeap>(), ModelDb.Card<WylderGhostflameExplosion>(),
            ModelDb.Card<WylderTurningGuard>(), ModelDb.Card<WylderShieldPress>(), ModelDb.Card<WylderSlidingThrust>(),
            ModelDb.Card<WylderSteadyBreath>(), ModelDb.Card<WylderGrappleSwing>(),
            ModelDb.Card<WylderLightStepGuard>(), ModelDb.Card<WylderLowStance>(), ModelDb.Card<WylderAerialRoll>(),
            ModelDb.Card<WylderPursuitSlash>(), ModelDb.Card<WylderGroundDash>() };
        var formatter = new SmartFormatter();
        formatter.AddExtensions(new DictionarySource(), new ReflectionSource(), new DefaultSource());
        formatter.AddExtensions(new HighlightDifferencesFormatter(), new EnergyIconsFormatter(), new ShowIfUpgradedFormatter());
        foreach (var model in models)
        {
            // Legacy designs remain testable but no longer define reward-pool membership.
            string portrait = model.PortraitPath.Replace("res://", "");
            Check(File.Exists(portrait.StartsWith("images/packed/card_portraits/wylder/")
                ? portrait : Path.Combine("D:/STS2", portrait)), "Verified runtime or interim native portrait");
            if (model is WylderGhostflameExplosion)
                Check(model.PortraitPath == "res://images/packed/card_portraits/wylder/wylder_ghostflame_explosion.png", "Ghostflame uses reviewed hooked-bar artwork");
            foreach (bool upgraded in new[] { false, true })
            {
                Fixture();
                var card = Card(model, upgraded);
                bool heavy = card is WylderCliffbreaker or WylderDualGreatswordLeap or WylderGhostflameExplosion;
                bool attack = card is WylderSpineShock or WylderSlidingThrust or WylderPursuitSlash || heavy;
                Check(card.EnergyCost.GetResolved() == (heavy ? 2 : card is WylderShieldPress or WylderLightStepGuard or WylderGroundDash ? 0 : 1), "Reviewed planner cost");
                Check(card.Type == (attack ? CardType.Attack : CardType.Skill), "Reviewed planner type");
                Check(card.TargetType == (attack ? TargetType.AnyEnemy : TargetType.Self), "Reviewed planner target");
                Check(card.Rarity == (card is WylderGhostflameExplosion ? CardRarity.Rare : card is WylderDualGreatswordLeap or WylderFlameMomentum or WylderGrappleSwing or WylderAerialRoll ? CardRarity.Uncommon : CardRarity.Common), "Reviewed planner rarity");
                foreach (var pair in card.DynamicVars)
                    Check(pair.Value.WasJustUpgraded == (upgraded && (pair.Key == "Block" || pair.Key == "Damage" || (card is WylderLightStepGuard && pair.Key is "CalculationBase" or "CalculatedBlock") || (card is WylderFlameReady && pair.Key == "Vigor") || (card is WylderGhostflameExplosion && pair.Key == "Freeze"))), "Reviewed planner only changed values green " + card.Id + " " + pair.Key);
            }
            foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
            {
                using var json = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
                string text = json.RootElement.GetProperty(model.Id.Entry + ".description").GetString()!;
                string Render(bool upgraded)
                {
                    var card = Card(model, upgraded);
                    foreach (var energy in card.DynamicVars.Values.OfType<EnergyVar>()) energy.ColorPrefix = EnergyIconHelper.GetPrefix(card);
                    return formatter.Format(System.Globalization.CultureInfo.InvariantCulture, text,
                        card.DynamicVars.ToDictionary(p => p.Key, p => (object)p.Value).Concat(new[] { new KeyValuePair<string,object>("IfUpgraded", new IfUpgradedVar(upgraded ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal)) }).ToDictionary(p=>p.Key,p=>p.Value));
                }
                Check(Render(false) != Render(true) && !Render(false).Contains('{'), "Reviewed planner four-locale rendered upgrades");
                Check(text == json.RootElement.GetProperty(model.Id.Entry + ".upgradeDescription").GetString(), "Reviewed planner upgrade copy");
                if (model is WylderFlameMomentum or WylderGrappleSwing or WylderLowStance or WylderPursuitSlash)
                    Check(Render(false).Contains("[img]res://images/packed/sprite_fonts/wylder_energy_icon.png[/img]"), "Flame Momentum actual energy image");
            }
        }
        foreach (bool upgraded in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        foreach (int vigor in new[] { 0, 2, 3 })
        {
            Fixture(pursuit); if (vigor > 0) AddPower<VigorPower>(_owner.Creature, vigor);
            AddPower<DexterityPower>(_owner.Creature, 3);
            var ready = Card(ModelDb.Card<WylderFlameReady>(), upgraded);
            Play(ready);
            Check(Rewards[0].Kind == "Block" && Rewards[0].Amount == (upgraded ? 10 : 8), "Flame Ready native Dexterity block first");
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == vigor + (upgraded ? 3 : 2), "Flame Ready native Vigor amount and upgrade");
            Check(Rewards.Last().Kind == (pursuit ? "Draw" : "VigorPower"), "Flame Ready old Pursuit draw is last");
            Check(!_owner.Creature.HasPower<WylderPursuitPower>() && Attacks.Count == 0, "Flame Ready consumes old Pursuit without attacking");

            Fixture(pursuit); if (vigor > 0) AddPower<VigorPower>(_owner.Creature, vigor);
            AddPower<DexterityPower>(_owner.Creature, 3);
            var momentum = Card(ModelDb.Card<WylderFlameMomentum>(), upgraded);
            Play(momentum);
            Check(Rewards[0].Kind == "Block" && Rewards[0].Amount == (upgraded ? 12 : 9), "Momentum native Dexterity block first");
            Check(Rewards.Count(r => r.Kind == "Energy") == (vigor >= 3 ? 1 : 0) + (pursuit ? 1 : 0), "Momentum exact threshold energy branch");
            Check(Rewards.Count(r => r.Kind == "VigorPower") == (vigor < 3 ? 1 : 0), "Momentum mutually exclusive Vigor branch");
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == (vigor < 3 ? vigor + 2 : vigor), "Momentum preserves old Vigor");
            Check(Rewards.Last().Kind == (pursuit ? "Draw" : vigor >= 3 ? "Energy" : "VigorPower"), "Momentum branch resolves before Pursuit draw");

            Fixture(pursuit);
            var shock = Card(ModelDb.Card<WylderSpineShock>(), upgraded);
            Play(shock);
            Check(ResolvedDamage.Single() == Math.Floor((upgraded ? 12m : 9m) * (pursuit ? 1.5m : 1m)), "Spine Shock native rounding preview/resolution");
            Check(Rewards.Count(r => r.Kind == "WeakPower") == (pursuit ? 1 : 0), "Spine Shock Break 13 once");
        }
        Fixture(); AddPower<VigorPower>(_owner.Creature, 2);
        Play(Card(ModelDb.Card<WylderFlameMomentum>(), false));
        Play(Card(ModelDb.Card<WylderFlameMomentum>(), false));
        Check(Rewards.Count(r => r.Kind == "Energy") == 1 && _owner.Creature.GetPowerAmount<VigorPower>() == 4, "Second Momentum reevaluates actual Vigor after 2 to 4");

        Fixture();
        var shifted = Card(ModelDb.Card<WylderSpineShock>(), false);
        shifted.BeforeCardPlayed(TrackingArrowRegression.MakePlay(shifted, _owner)).GetAwaiter().GetResult();
        Hit(shifted, _enemies[1], 13, true);
        Check(Rewards.Single().Target == _enemies[1] && Rewards.Single().Amount == 1, "Break reward targets actual fully blocked damage recipient");
        shifted.AfterCardPlayedLate(null!, TrackingArrowRegression.MakePlay(shifted, _owner)).GetAwaiter().GetResult();
        Fixture();
        var lethal = Card(ModelDb.Card<WylderSpineShock>(), false);
        lethal.BeforeCardPlayed(TrackingArrowRegression.MakePlay(lethal, _owner)).GetAwaiter().GetResult();
        _enemies[0].SetCurrentHpInternal(0);
        Hit(lethal, _enemies[0], 13, false); Hit(lethal, _enemies[1], 30, false);
        Check(Rewards.Count == 0, "Lethal Break skips dead target and cannot redirect reward");
        foreach (bool upgraded in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        foreach (bool fullyBlocked in new[] { false, true })
        {
            Fixture(pursuit); _fullyBlocked = fullyBlocked;
            Play(Card(ModelDb.Card<WylderCliffbreaker>(), upgraded));
            Check(Attacks.Single() == (false, 1, upgraded ? 22m : 17m), "Cliffbreaker native single attack");
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == (pursuit ? 3 : 0), "Cliffbreaker deferred Vigor exact pre-block threshold");
            Check(ResolvedDamage.Single() == Math.Floor((upgraded ? 22m : 17m) * (pursuit ? 1.5m : 1m)), "Cliffbreaker reward cannot boost its own hit");

            Fixture(pursuit); _fullyBlocked = fullyBlocked;
            var leap = Card(ModelDb.Card<WylderDualGreatswordLeap>(), upgraded);
            Check(leap.EnergyCost.GetResolved() == 2 && leap.Rarity == CardRarity.Uncommon, "Dual Leap cost and rarity");
            Play(leap);
            Check(Attacks.Single() == (false, 2, upgraded ? 10m : 9m), "Dual Leap native two hits");
            Check(ResolvedDamage.Count == 2 && ResolvedDamage.All(d => d == Math.Floor((upgraded ? 10m : 9m) * (pursuit ? 1.5m : 1m))), "Both Leap hits use old Pursuit state only");
            Check(!_owner.Creature.HasPower<WylderPursuitPower>(), "Dual Leap consumes old Pursuit");
            Check(Rewards.Where(r => r.Kind == "Energy").Sum(r => r.Amount) == 0, "Dual Leap cannot sum two sub-threshold hits into Break20");
            AccessTools.Method(typeof(CombatState), "AddCard", new[] { typeof(CardModel) }).Invoke(_combat, new object[] { leap });
            Put(leap, PileType.Discard);
            leap.AfterCardChangedPilesLate(leap, PileType.Play, null!).GetAwaiter().GetResult();
            Check(leap.Pile!.Type == PileType.Discard, "Dual Leap returns only when every hit independently reaches Break20");

            Fixture(pursuit); _fullyBlocked = fullyBlocked;
            Play(Card(ModelDb.Card<WylderGhostflameExplosion>(), upgraded));
            Check(Attacks.Single() == (false, 1, upgraded ? 17m : 15m), "Ghostflame native single target");
            Check(ResolvedDamage.Count == 1 && ResolvedDamage.Single() == Math.Floor((upgraded ? 17m : 15m) * (pursuit ? 1.5m : 1m)), "Ghostflame damage before Break reward");
            Check(_enemies.All(e => !e.HasPower<FreezePower>()), "Ghostflame no Freeze below Break30");
        }
        Fixture(); AddPower<StrengthPower>(_owner.Creature, 20);
        Play(Card(ModelDb.Card<WylderDualGreatswordLeap>(), false));
        Check(ResolvedDamage.SequenceEqual(new[] { 29m, 29m }) && !_owner.Creature.HasPower<WylderPursuitPower>(), "Dual Leap native Strength on both hits");
        Check(Rewards.Where(r => r.Kind == "Energy").Sum(r => r.Amount) == 1, "Dual Leap Breaks once when both hits independently meet the threshold");
        Fixture();
        var lethalLeap = Card(ModelDb.Card<WylderDualGreatswordLeap>(), false);
        var lethalLeapPlay = TrackingArrowRegression.MakePlay(lethalLeap, _owner);
        lethalLeap.BeforeCardPlayed(lethalLeapPlay).GetAwaiter().GetResult();
        Hit(lethalLeap, _enemies[0], 20, false, killed: true);
        Check((bool)AccessTools.Property(typeof(WylderCard), "BrokeThisPlay").GetValue(lethalLeap)!,
            "Dual Leap qualifying lethal first hit completes Break when the second hit is cancelled");
        Fixture(true); AddPower<VigorPower>(_owner.Creature, 3);
        Play(Card(ModelDb.Card<WylderCliffbreaker>(), false));
        Check(ResolvedDamage.Single() == 30 && _owner.Creature.GetPowerAmount<VigorPower>() == 3, "Native old Vigor consumption precedes fresh Cliffbreaker Vigor");
        Fixture(); _enemies[0].SetCurrentHpInternal(0);
        Play(Card(ModelDb.Card<WylderGhostflameExplosion>(), false));
        Check(_enemies.All(e => !e.HasPower<FreezePower>()), "Ghostflame skips dead recipient and unrelated enemies");
        foreach (bool upgraded in new[] { false, true })
        foreach (bool blocked in new[] { false, true })
        foreach (int totalDamage in new[] { 29, 30, 31 })
        {
            Fixture(); _fullyBlocked = blocked;
            AddPower<StrengthPower>(_owner.Creature, totalDamage - (upgraded ? 17 : 15));
            Play(Card(ModelDb.Card<WylderGhostflameExplosion>(), upgraded));
            Check(_enemies[0].GetPowerAmount<FreezePower>() == (totalDamage >= 30 ? upgraded ? 7 : 6 : 0), "Ghostflame exact pre-block Break boundary and upgrade");
            Check(!_enemies[1].HasPower<FreezePower>(), "Ghostflame Break only affects selected enemy");
        }
        Fixture(); AddPower<FreezePower>(_enemies[0], 3); AddPower<StrengthPower>(_owner.Creature, 20);
        Play(Card(ModelDb.Card<WylderGhostflameExplosion>(), false));
        Check(_enemies[0].GetPowerAmount<FreezePower>() == 9, "Ghostflame stacks existing Freeze after Break");
        _enemies[0].GetPower<FreezePower>()!.AfterSideTurnEnd(null!, CombatSide.Enemy, _enemies).GetAwaiter().GetResult();
        Check(_enemies[0].GetPowerAmount<FreezePower>() == 8, "Ghostflame reuses native project Freeze decay");
        HistoryCards();
        HandSourceCards();
        Console.WriteLine($"PASS: {_checks - before} reviewed planner checks: sixteen cards; four-locale upgrades/energy icons, Vigor, Dexterity once, Pursuit/Break timing, native play history, pre-move hand snapshots, replay isolation and reversible native cost discounts (UI commands stubbed).");
    }

    private static void HorizontalReviewCards()
    {
        int start = _checks;
        var specs = new[]
        {
            new Spec(ModelDb.Card<WylderVerticalPressure>(), 2, CardRarity.Uncommon, 18, 23, 30),
            new Spec(ModelDb.Card<WylderGhostflameCinders>(), 1, CardRarity.Uncommon, 0, 0, Block:7, UpgradeBlock:9),
            new Spec(ModelDb.Card<WylderFleetFootwork>(), 1, CardRarity.Uncommon, 0, 0, Block:4, UpgradeBlock:6),
            new Spec(ModelDb.Card<WylderOffensiveShift>(), 1, CardRarity.Uncommon, 0, 0, Block:5, UpgradeBlock:8),
            new Spec(ModelDb.Card<WylderLastStandGuard>(), 2, CardRarity.Uncommon, 0, 0, Block:15, UpgradeBlock:20),
            new Spec(ModelDb.Card<WylderPatientFocus>(), 0, CardRarity.Uncommon, 0, 0, Block:5, UpgradeBlock:8),
            new Spec(ModelDb.Card<WylderTwinEdgeCombo>(), 1, CardRarity.Rare, 7, 9, Hits:2),
            new Spec(ModelDb.Card<WylderBraveBlessing>(), 1, CardRarity.Rare, 0, 0, Block:8, UpgradeBlock:11),
            new Spec(ModelDb.Card<WylderFinalGuard>(), 2, CardRarity.Rare, 0, 0, Block:16, UpgradeBlock:20),
            new Spec(ModelDb.Card<WylderWildBreath>(), 1, CardRarity.Rare, 0, 0)
        };
        var formatter = new SmartFormatter();
        formatter.AddExtensions(new DictionarySource(), new ReflectionSource(), new DefaultSource());
        formatter.AddExtensions(new HighlightDifferencesFormatter(), new EnergyIconsFormatter(), new ShowIfUpgradedFormatter());
        foreach (var spec in specs)
        {
            // Legacy designs remain testable but no longer define reward-pool membership.
            Check(File.Exists(spec.Model.PortraitPath.Replace("res://", "D:/STS2/")), "Horizontal native portrait exists");
            foreach (bool up in new[] { false, true })
            {
                Fixture(); var card = Card(spec.Model, up);
                Check(card.EnergyCost.GetResolved() == spec.Cost && card.Rarity == spec.Rarity, "Horizontal cost and rarity");
                Check(card.Type == (spec.Damage > 0 ? CardType.Attack : CardType.Skill), "Horizontal type");
                Check(card.TargetType == (spec.Damage > 0 || card is WylderGhostflameCinders ? TargetType.AnyEnemy : TargetType.Self), "Horizontal target");
                foreach (var pair in card.DynamicVars)
                {
                    bool changes = pair.Key is "Damage" or "Block"
                        || (card is WylderGhostflameCinders && pair.Key == "Freeze")
                        || (card is WylderPatientFocus or WylderWildBreath && pair.Key == "Vigor");
                    Check(pair.Value.WasJustUpgraded == (up && changes), "Horizontal changed values green " + card.Id + " " + pair.Key);
                }
            }
            foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
            {
                using var json = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
                string text = json.RootElement.GetProperty(spec.Model.Id.Entry + ".description").GetString()!;
                string Render(bool up)
                {
                    var card = Card(spec.Model, up);
                    foreach (var energy in card.DynamicVars.Values.OfType<EnergyVar>()) energy.ColorPrefix = EnergyIconHelper.GetPrefix(card);
                    return formatter.Format(System.Globalization.CultureInfo.InvariantCulture, text,
                        card.DynamicVars.ToDictionary(p => p.Key, p => (object)p.Value).Concat(new[] { new KeyValuePair<string,object>("IfUpgraded", new IfUpgradedVar(up ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal)) }).ToDictionary(p=>p.Key,p=>p.Value));
                }
                Check(Render(false) != Render(true) && !Render(false).Contains('{'), "Horizontal visible four-locale upgrade");
                Check(text == json.RootElement.GetProperty(spec.Model.Id.Entry + ".upgradeDescription").GetString(), "Horizontal upgrade audit copy");
                if (spec.Model is WylderFinalGuard)
                    Check(Render(false).Contains("[img]res://images/packed/sprite_fonts/wylder_energy_icon.png[/img]"), "Final Guard actual energy icon");
            }
        }
        foreach (bool up in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        foreach (bool attacked in new[] { false, true })
        foreach (int vigor in new[] { 0, 4, 5 })
        {
            void Begin()
            {
                Fixture(pursuit);
                if (vigor > 0) AddPower<VigorPower>(_owner.Creature, vigor);
                if (attacked) CombatManager.Instance.History.CardPlayStarted(_combat,
                    TrackingArrowRegression.MakePlay(Card(ModelDb.Card<WylderStrike>(), false), _owner));
                AddPower<DexterityPower>(_owner.Creature, 3);
            }
            Begin(); var vertical = Card(ModelDb.Card<WylderVerticalPressure>(), up); Play(vertical);
            decimal verticalDamage = Math.Floor(((up ? 23m : 18m) + vigor) * (pursuit ? 1.5m : 1m));
            Check(ResolvedDamage.Single() == verticalDamage, "Vertical Pressure modified damage");
            Check(Rewards.Where(r => r.Kind == "Draw").Sum(r => r.Amount) == (verticalDamage >= 30 ? 2 : 0), "Vertical Pressure deferred Break draw");
            Begin(); Play(Card(ModelDb.Card<WylderTwinEdgeCombo>(), up));
            Check(Attacks.Single().Hits == 2 && ResolvedDamage.All(d => d == Math.Floor(((up ? 9m : 7m) + vigor) * (pursuit ? 1.5m : 1m))), "Twin Edge native multihit keeps old bonus for both hits");
            Check(_owner.Creature.HasPower<WylderPursuitPower>() == pursuit, "Twin Edge reenters only old Pursuit");
            Begin(); Play(Card(ModelDb.Card<WylderGhostflameCinders>(), up));
            Check(Rewards[0].Kind == "Block" && Rewards[0].Amount == (up ? 12 : 10), "Cinders block first with Dexterity once");
            Check(_enemies[0].GetPowerAmount<FreezePower>() == (up ? 2 : 1) && !_enemies[1].HasPower<FreezePower>(), "Cinders only selected living target");
            Check(Rewards.Last().Kind == (pursuit ? "Draw" : "FreezePower"), "Cinders Pursuit draw last");
            Begin(); var footwork = Card(ModelDb.Card<WylderFleetFootwork>(), up); Hand(footwork, 1);
            Check(footwork.EnergyCost.GetResolved() == 1, "Reviewed Footwork not discounted by attack history"); Play(footwork);
            Check(Rewards[0].Amount == (up ? 9 : 7) && Rewards.Count(r => r.Kind == "Block") == 1, "Footwork block once");
            Check(Rewards.Where(r => r.Kind == "Draw").Sum(r => r.Amount) == 1 + (attacked ? 1 : 0) + (pursuit ? 1 : 0), "Footwork reviewed draw ceiling plus old Pursuit");
            Begin(); Play(Card(ModelDb.Card<WylderOffensiveShift>(), up));
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == vigor + (pursuit ? 3 : 0), "Offensive Shift branches exclusive");
            Check(_owner.Creature.HasPower<WylderPursuitPower>() == !pursuit, "Offensive Shift new Pursuit survives, old consumed");
            Begin(); Play(Card(ModelDb.Card<WylderPatientFocus>(), up));
            Check(Rewards.Count(r => r.Kind == "Block") == (attacked ? 1 : 0), "Patient Focus history branch");
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == vigor + (attacked ? 0 : up ? 3 : 2), "Patient Focus Vigor upgrade");
            Begin(); Play(Card(ModelDb.Card<WylderBraveBlessing>(), up));
            Check(Rewards[0].Amount == (up ? 14 : 11), "Brave Blessing block first");
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == vigor + (vigor >= 5 ? 0 : 3), "Brave Blessing exact threshold");
            Check(Rewards.Where(r => r.Kind == "Draw").Sum(r => r.Amount) == (vigor >= 5 ? 2 : 0) + (pursuit ? 1 : 0), "Brave Blessing mutually exclusive draw plus Pursuit");
            Begin(); Play(Card(ModelDb.Card<WylderWildBreath>(), up));
            Check(Rewards[0].Kind == "VigorPower" && _owner.Creature.GetPowerAmount<VigorPower>() == vigor + (up ? 6 : 4), "Wild Breath Vigor first");
            Check(Rewards.Where(r => r.Kind == "Draw").Sum(r => r.Amount) == (attacked ? 2 : 0) + (pursuit ? 1 : 0), "Wild Breath history draw and old Pursuit");
        }
        foreach (bool up in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        foreach (int count in new[] { 1, 2, 3, 4 })
        foreach (bool hand in new[] { false, true })
        {
            Fixture(pursuit); var card = Card(ModelDb.Card<WylderFinalGuard>(), up);
            if (hand) Hand(card, count); else Put(card, PileType.Draw);
            Play(card);
            Check(Rewards[0].Kind == "Block" && Rewards[0].Amount == (up ? 20 : 16), "Final Guard reviewed Block");
            Check(Rewards.Where(r => r.Kind == "Energy").Sum(r => r.Amount) == (hand && count == 2 ? 1 : 0) + (pursuit ? 1 : 0), "Final Guard exactly two, only refunds one");
        }
        Fixture(); var last = (WylderLastStandGuard)Card(ModelDb.Card<WylderLastStandGuard>(), true); Hand(last, 3);
        Check(last.EnergyCost.GetResolved() == 1, "Last Stand native low-hand discount");
        var fourth = Card(ModelDb.Card<WylderDefend>(), false); Put(fourth, PileType.Hand);
        Check(last.EnergyCost.GetResolved() == 2, "Last Stand native discount reverses");
        Put(fourth, PileType.Discard);
        Check(last.TryModifyEnergyCostInCombat(last, 3, out var stack) && stack == 2, "Last Stand subtracts one with other modifiers");
        Play(last); Check(Rewards.Single().Amount == 20, "Last Stand upgraded actual Block");
        Fixture(true); var automatic = Card(ModelDb.Card<WylderFinalGuard>(), false); Hand(automatic, 2);
        Play(automatic); Play(automatic, false, playIndex:1);
        Check(Rewards.Where(r => r.Kind == "Energy").Sum(r => r.Amount) == 2, "Final Guard replay cannot inherit exactly-two refund");
        Console.WriteLine($"PASS: {_checks - start} final-horizontal-review checks: eleven cards, reviewed no-refund/no-free-draw rules, exact hand snapshots, native multihit/Break, four-locale upgrades and reward ordering (UI commands stubbed).");
    }

    private static void Put(CardModel card, PileType destination)
    {
        foreach (var pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Play, PileType.Exhaust })
            ((List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards").GetValue(pile.GetPile(_owner))!).Remove(card);
        ((List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards").GetValue(destination.GetPile(_owner))!).Add(card);
    }

    private static void Hand(CardModel card, int count)
    {
        Put(card, PileType.Hand);
        for (int i = 1; i < count; i++) Put(Card(ModelDb.Card<WylderDefend>(), false), PileType.Hand);
    }

    private static void HandSourceCards()
    {
        foreach (bool upgraded in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        foreach (int count in new[] { 1, 2, 3, 4 })
        foreach (bool fromHand in new[] { false, true })
        {
            void Begin(CardModel c) { if (fromHand) Hand(c, count); else Put(c, PileType.Draw); }
            Fixture(pursuit); AddPower<DexterityPower>(_owner.Creature, 3);
            var guard = Card(ModelDb.Card<WylderLightStepGuard>(), upgraded); Begin(guard);
            guard.DynamicVars.CalculatedBlock.UpdateCardPreview(guard, CardPreviewMode.Normal, null!, true);
            decimal expected = (upgraded ? 5 : 3) + (fromHand && count <= 2 ? 3 : 0) + 3;
            Check(guard.DynamicVars.CalculatedBlock.PreviewValue == expected, "Low-hand combined preview with Dexterity once");
            Play(guard);
            Check(Rewards.Count(r => r.Kind == "Block") == 1 && Rewards[0].Amount == expected, "Low-hand combined block one command, source snapshot includes self");
            Check(Rewards.Count(r => r.Kind == "Draw") == (pursuit ? 1 : 0), "Low-hand skill old Pursuit draw once");
            Fixture(pursuit);
            var stance = Card(ModelDb.Card<WylderLowStance>(), upgraded); Begin(stance); Play(stance);
            Check(Rewards[0].Amount == (upgraded ? 11 : 8), "Low Stance block");
            Check(Rewards.Count(r => r.Kind == "Energy") == (fromHand && count <= 3 ? 1 : 0) + (pursuit ? 1 : 0), "Low Stance before-move hand threshold");
            Check(Rewards.Last().Kind == (pursuit ? "Draw" : fromHand && count <= 3 ? "Energy" : "Block"), "Low Stance reward before Pursuit draw");
            Fixture(pursuit);
            var slash = Card(ModelDb.Card<WylderPursuitSlash>(), upgraded); Begin(slash); Play(slash);
            Check(ResolvedDamage.Single() == Math.Floor((upgraded ? 11m : 8m) * (pursuit ? 1.5m : 1m)), "Pursuit Slash native attack bonus");
            Check(Rewards.Count(r => r.Kind == "Energy") == (pursuit && fromHand && count <= 3 ? 1 : 0), "Pursuit Slash requires both source hand and Pursuit");
            Fixture(pursuit);
            var dash = Card(ModelDb.Card<WylderGroundDash>(), upgraded); Begin(dash); Play(dash);
            Check(Rewards[0].Amount == (upgraded ? 5 : 3), "Ground Dash block upgrade");
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == (fromHand && count <= 3 ? 2 : 0), "Ground Dash source hand threshold");
        }
        Fixture();
        var roll = (WylderAerialRoll)Card(ModelDb.Card<WylderAerialRoll>(), false); Hand(roll, 3);
        Check(roll.EnergyCost.GetResolved() == 0, "Aerial Roll native hook hand three discount");
        var fourth = Card(ModelDb.Card<WylderDefend>(), false); Put(fourth, PileType.Hand);
        Check(roll.EnergyCost.GetResolved() == 1, "Aerial Roll cost restores at hand four");
        Put(fourth, PileType.Discard);
        Check(roll.EnergyCost.GetResolved() == 0, "Aerial Roll cost dynamically returns to zero");
        Check(roll.TryModifyEnergyCostInCombat(roll, 2, out var stacked) && stacked == 1, "Aerial Roll subtracts rather than forces zero");
        Check(roll.TryModifyEnergyCostInCombat(roll, 0, out var floor) && floor == 0, "Aerial Roll zero floor");
        Check(!roll.TryModifyEnergyCostInCombat(fourth, 2, out var other) && other == 2, "Aerial Roll does not discount other cards");
        Put(roll, PileType.Discard);
        Check(roll.EnergyCost.GetResolved() == 1, "Aerial Roll outside hand no discount");
        Fixture();
        var repeat = Card(ModelDb.Card<WylderLowStance>(), false); Hand(repeat, 3);
        Play(repeat); Play(repeat, false, playIndex: 1);
        Check(Rewards.Count(r => r.Kind == "Energy") == 1, "Same wrapper replay cannot reuse hand source");
        Fixture();
        var automatic = Card(ModelDb.Card<WylderLowStance>(), false); Hand(automatic, 3);
        Play(automatic, isAutoPlay: true);
        Check(Rewards.Count(r => r.Kind == "Energy") == 1, "Automatic play originating in hand retains real hand source");
        Fixture();
        automatic = Card(ModelDb.Card<WylderLowStance>(), false); Put(automatic, PileType.Draw);
        Play(automatic, isAutoPlay: true);
        Check(Rewards.All(r => r.Kind != "Energy"), "Automatic draw-pile play is not hand-sourced");
        Fixture();
        var firstCopy = Card(ModelDb.Card<WylderLowStance>(), false);
        var secondCopy = Card(ModelDb.Card<WylderLowStance>(), false);
        Hand(firstCopy, 2); Put(secondCopy, PileType.Draw);
        Play(firstCopy); Play(secondCopy);
        Check(Rewards.Count(r => r.Kind == "Energy") == 1, "Different mutable copies never share source snapshot");
        Fixture();
        var nonWylder = Card(ModelDb.Card<WylderLowStance>(), false); Hand(nonWylder, 1);
        AccessTools.Field(typeof(Player), "<Character>k__BackingField").SetValue(_owner, ModelDb.Character<Duchess>());
        Play(nonWylder);
        Check(Rewards.All(r => r.Kind != "Energy"), "Source-capture patch leaves other characters untouched");
        Fixture();
        var frozen = Card(ModelDb.Card<WylderLowStance>(), false); Hand(frozen, 4);
        var helper = typeof(Wylder).Assembly.GetType("NightMustStay.Core.Models.Cards.WylderHandPlayContext")!;
        AccessTools.Method(helper, "CaptureWrapper").Invoke(null, new object[] { frozen });
        foreach (var c in PileType.Hand.GetPile(_owner).Cards.Where(c => c != frozen).ToArray()) Put(c, PileType.Discard);
        Play(frozen, false);
        Check(Rewards.All(r => r.Kind != "Energy"), "Post-capture hand changes cannot alter frozen count");
    }

    private static void HistoryCards()
    {
        foreach (bool upgraded in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        foreach (CardType? previous in new CardType?[] { null, CardType.Attack, CardType.Skill })
        {
            void Begin()
            {
                Fixture();
                if (previous != null)
                {
                    var old = Card(previous == CardType.Attack ? ModelDb.Card<WylderStrike>() : ModelDb.Card<WylderDefend>(), false);
                    CombatManager.Instance.History.CardPlayStarted(_combat, TrackingArrowRegression.MakePlay(old, _owner));
                }
                if (pursuit) AddPower<WylderPursuitPower>(_owner.Creature, 1);
            }
            Begin();
            Play(Card(ModelDb.Card<WylderTurningGuard>(), upgraded));
            Check(Rewards[0].Kind == "Block" && Rewards[0].Amount == (upgraded ? 10 : 7), "Turning Guard base/upgraded block");
            Check(_owner.Creature.HasPower<WylderPursuitPower>() == (previous == CardType.Attack), "Turning Guard requires earlier attack and keeps new Pursuit");
            Check(Rewards.Count(r => r.Kind == "Draw") == (pursuit ? 1 : 0), "Turning Guard old Pursuit draws once after effects");

            Begin();
            Play(Card(ModelDb.Card<WylderShieldPress>(), upgraded));
            Check(Rewards[0].Kind == "Block" && Rewards[0].Amount == (upgraded ? 6 : 4), "Shield Press zero-cost block");
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == (previous == CardType.Skill ? 2 : 0), "Shield Press excludes itself from earlier Skills");
            Check(Rewards.Count(r => r.Kind == "Draw") == (pursuit ? 1 : 0), "Shield Press Pursuit draw once");

            Begin();
            Play(Card(ModelDb.Card<WylderSlidingThrust>(), upgraded));
            Check(_owner.Creature.HasPower<WylderPursuitPower>() == (previous == null), "Sliding Thrust only first card reenters Pursuit");
            Check(ResolvedDamage.Single() == Math.Floor((upgraded ? 10m : 7m) * (pursuit ? 1.5m : 1m)), "Sliding Thrust new Pursuit never boosts itself");

            Begin();
            Play(Card(ModelDb.Card<WylderSteadyBreath>(), upgraded));
            Check(Rewards[0].Kind == "Block" && Rewards[0].Amount == (upgraded ? 10 : 7), "Steady Breath block");
            Check(_owner.Creature.HasPower<WylderPursuitPower>() == (previous != CardType.Attack), "Steady Breath inverse attack-history condition");
            Check(Rewards.Count(r => r.Kind == "Draw") == (pursuit ? 1 : 0), "Steady Breath old Pursuit draws after entering new");

            Begin();
            Play(Card(ModelDb.Card<WylderGrappleSwing>(), upgraded));
            Check(Rewards[0].Kind == "Block" && Rewards[0].Amount == (upgraded ? 9 : 6), "Grapple Swing block");
            Check(Rewards.Count(r => r.Kind == "Energy") == (previous == CardType.Attack ? 1 : 0) + (pursuit ? 1 : 0), "Grapple Swing earlier attack energy branch");
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == (previous != CardType.Attack ? 2 : 0), "Grapple Swing branches exclusive");
        }
        Fixture();
        var press = Card(ModelDb.Card<WylderShieldPress>(), false);
        Play(press); Play(press);
        Check(_owner.Creature.GetPowerAmount<VigorPower>() == 2, "Second native CardPlay of same Skill sees first, first cannot see itself");
        Fixture();
        var thrust = Card(ModelDb.Card<WylderSlidingThrust>(), false);
        Play(thrust); Play(thrust);
        Check(!_owner.Creature.HasPower<WylderPursuitPower>(), "Second native CardPlay is no longer first card");
        _owner.PlayerCombatState!.IncrementTurnNumber();
        Play(thrust);
        Check(_owner.Creature.HasPower<WylderPursuitPower>(), "New player turn resets first-card eligibility without deleting history");

        Fixture();
        var current = Card(ModelDb.Card<WylderShieldPress>(), false);
        var play = TrackingArrowRegression.MakePlay(current, _owner);
        current.BeforeCardPlayed(play).GetAwaiter().GetResult();
        CombatManager.Instance.History.CardPlayStarted(_combat, play);
        var nested = Card(ModelDb.Card<WylderDefend>(), false);
        CombatManager.Instance.History.CardPlayStarted(_combat, TrackingArrowRegression.MakePlay(nested, _owner));
        ((Task)AccessTools.Method(current.GetType(), "OnPlay").Invoke(current, new object?[] { null, play })!).GetAwaiter().GetResult();
        Check(!_owner.Creature.HasPower<VigorPower>(), "Later/nested Skill cannot become previous to current CardPlay");
        Fixture();
        var other = TrackingArrowRegression.MakePlayer();
        var otherCreature = new Creature(other, 100, 100) { CombatState = _combat };
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(other, otherCreature);
        var otherAttack = ModelDb.Card<WylderStrike>().ToMutable();
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(otherAttack, other);
        CombatManager.Instance.History.CardPlayStarted(_combat, TrackingArrowRegression.MakePlay(otherAttack, other));
        Play(Card(ModelDb.Card<WylderTurningGuard>(), false));
        Check(!_owner.Creature.HasPower<WylderPursuitPower>(), "Ally attack does not satisfy owner's prior attack condition");
    }

    public static bool NotEnding(ref bool __result) { __result = false; return false; }

    private static void SparkAndRollingAttack()
    {
        foreach (bool up in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        {
            Fixture(pursuit);
            var retreat = Card(ModelDb.Card<WylderTacticalRetreat>(), up);
            Check(retreat.EnergyCost.GetResolved() == 4 && retreat.Type == CardType.Skill && retreat.Rarity == CardRarity.Uncommon && retreat.TargetType == TargetType.Self && retreat.Keywords.SetEquals(new[] { CardKeyword.Exhaust }), "Tactical Retreat metadata and Exhaust");
            AddPower<DexterityPower>(_owner.Creature, 3);
            retreat.DynamicVars.Block.UpdateCardPreview(retreat, CardPreviewMode.Normal, null!, true);
            Check(retreat.DynamicVars.Block.PreviewValue == (up ? 33 : 23), "Tactical Retreat native Block preview");
            Play(retreat);
            Check(Rewards.Single(r => r.Kind == "Block").Amount == (up ? 33 : 23), "Tactical Retreat actual Block with Dexterity once");
            Fixture(pursuit);
            var golden = Card(ModelDb.Card<WylderGoldenParry>(), up);
            Check(golden.EnergyCost.GetResolved() == 3 && golden.Type == CardType.Skill && golden.Rarity == CardRarity.Uncommon && golden.TargetType == TargetType.AllEnemies && golden.Keywords.Contains(CardKeyword.Retain) == up, "Golden Parry metadata and Retain upgrade");
            Play(golden);
            Check(_enemies.All(e => Rewards.Any(r => r.Kind == nameof(PhantomImbalancePower) && r.Target == e && r.Amount == 3)), "Golden Parry applies three Imbalance to every enemy");
            Fixture(pursuit);
            var spark = Card(ModelDb.Card<WylderSpark>(), up);
            Check(spark.EnergyCost.GetResolved() == 0 && spark.Type == CardType.Skill && spark.Rarity == CardRarity.Common && spark.TargetType == TargetType.Self, "Spark metadata");
            Check(spark.Keywords.Contains(CardKeyword.Exhaust), "Spark exhausts");
            Play(spark);
            Check(Rewards.Where(r => r.Kind == "Draw").Sum(r => r.Amount) == (pursuit ? 2 : 1), "Spark draws one plus Pursuit reward");
            Check(Rewards.Single(r => r.Kind == nameof(VigorPower)).Amount == (up ? 5 : 3), "Spark actual vigor");
            Check(!_owner.Creature.HasPower<WylderPursuitPower>(), "Spark follows normal skill Pursuit consumption");
            Fixture(pursuit);
            var roll = Card(ModelDb.Card<WylderRollingAttack>(), up);
            Check(roll.EnergyCost.GetResolved() == 1 && roll.Type == CardType.Attack && roll.Rarity == CardRarity.Common && roll.TargetType == TargetType.AnyEnemy, "Rolling Attack metadata");
            roll.DynamicVars.Damage.UpdateCardPreview(roll, CardPreviewMode.Normal, _enemies[0], true);
            Check(roll.DynamicVars.Damage.BaseValue == (up ? 10 : 9) && roll.DynamicVars.Cards.BaseValue == (up ? 3 : 2), "Rolling Attack upgrade values");
            Play(roll);
            Check(ResolvedDamage.Single() == Math.Floor((up ? 10m : 9m) * (pursuit ? 1.5m : 1m)), "Rolling Attack actual damage");
            Check(Rewards.Where(r => r.Kind == "Draw").Sum(r => r.Amount) == (pursuit ? (up ? 3 : 2) : 0), "Rolling Attack conditional draw");
            Check(!_owner.Creature.HasPower<WylderPursuitPower>(), "Rolling Attack consumes Pursuit normally");
        }
        Console.WriteLine("PASS: Spark and Rolling Attack base/upgraded, Pursuit on/off actual resolution.");
    }

    private static void InspireRevision()
    {
        foreach (bool upgraded in new[] { false, true })
        foreach (int handCount in new[] { 1, 2, 3, 4 })
        {
            Fixture();
            var inspire = Card(ModelDb.Card<WylderInspire>(), upgraded);
            Hand(inspire, handCount);
            bool shouldTrigger = handCount - 1 <= 2;
            Check((bool)AccessTools.PropertyGetter(typeof(WylderInspire), "ShouldGlowGoldInternal").Invoke(inspire, null)! == shouldTrigger,
                "Inspire glow previews the post-play hand condition");
            Play(inspire);
            Check(_owner.Creature.HasPower<WylderPursuitPower>(), "Inspire always enters Pursuit");
            decimal expectedDraw = shouldTrigger ? upgraded ? 3m : 2m : 0m;
            Check(Rewards.Where(r => r.Kind == "Draw").Sum(r => r.Amount) == expectedDraw,
                "Inspire uses the post-play hand count and upgrades draw 2 to 3");
        }

        foreach (int handCount in new[] { 0, 1, 2, 3 })
        {
            Fixture();
            for (int i = 0; i < handCount; i++)
                Put(Card(ModelDb.Card<WylderDefend>(), false), PileType.Hand);
            var fromDraw = Card(ModelDb.Card<WylderInspire>(), false);
            Put(fromDraw, PileType.Draw);
            Check(!(bool)AccessTools.PropertyGetter(typeof(WylderInspire), "ShouldGlowGoldInternal").Invoke(fromDraw, null)!,
                "Inspire only glows while it is in hand");
            Play(fromDraw);
            decimal expectedDraw = handCount <= 2 ? 2m : 0m;
            Check(_owner.Creature.HasPower<WylderPursuitPower>()
                && Rewards.Where(r => r.Kind == "Draw").Sum(r => r.Amount) == expectedDraw,
                "Inspire reads the actual post-play hand count even when played from another pile");
        }
    }

    private static void MetadataAndText()
    {
        var formatter = new SmartFormatter();
        formatter.AddExtensions(new DictionarySource(), new ReflectionSource(), new DefaultSource());
        formatter.AddExtensions(new HighlightDifferencesFormatter(), new EnergyIconsFormatter(), new ShowIfUpgradedFormatter());
        foreach (Spec spec in Specs())
        {
            Check(spec.Model is WylderSpinningSlash || ModelDb.Character<Wylder>().CardPool.AllCards.Contains(spec.Model), "Pool registration " + spec.Model.Id);
            string art = spec.Model.PortraitPath.Replace("res://", "");
            Check(File.Exists(art.StartsWith("images/packed/card_portraits/wylder/") ? art : Path.Combine("D:/STS2", art)),
                "Verified approved runtime or native interim portrait");
            foreach (bool upgraded in new[] { false, true })
            {
                Fixture();
                CardModel card = Card(spec.Model, upgraded);
                Check(card.EnergyCost.GetResolved() == spec.Cost && card.Type == CardType.Attack && card.Rarity == spec.Rarity, "Metadata " + card.Id);
                Check(card.TargetType == (spec.Aoe ? TargetType.AllEnemies : TargetType.AnyEnemy), "Target metadata " + card.Id);
                Check(card.DynamicVars.Damage.BaseValue == (upgraded ? spec.UpgradeDamage : spec.Damage), "Damage upgrade " + card.Id);
                if (spec.Break > 0) Check(card.DynamicVars["Break"].BaseValue == spec.Break, "Fixed Break threshold");
                if (spec.Hits > 1) Check(card.DynamicVars.Repeat.BaseValue == spec.Hits, "Repeat variable");
                if (spec.Block > 0) Check(card.GainsBlock && card.DynamicVars.Block.BaseValue == (upgraded ? spec.UpgradeBlock : spec.Block), "Block upgrade");
                foreach (var pair in card.DynamicVars)
                    Check(pair.Value.WasJustUpgraded == (upgraded && card.MaxUpgradeLevel > 0
                        && ((pair.Key == "Damage" && card is not WylderFlamingStrike) || pair.Key == "Block" || (card is WylderFlameOfRedmanes && pair.Key == "Vigor")
                            || (card is WylderVacuumSlice && pair.Key == "Weak")
                            || (card is WylderLionsClaw && pair.Key == "Imbalance"))),
                        "Only changed variables green " + card.Id + "/" + pair.Key);
            }
            foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
            {
                using var json = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
                string text = json.RootElement.GetProperty(spec.Model.Id.Entry + ".description").GetString()!;
                Check(json.RootElement.GetProperty(spec.Model.Id.Entry + ".title").GetString()!.Length > 0, "Localized title");
                Check(text == json.RootElement.GetProperty(spec.Model.Id.Entry + ".upgradeDescription").GetString(), "Upgrade audit copy synced");
                string Render(bool upgraded)
                {
                    var card = Card(spec.Model, upgraded);
                    foreach (var energy in card.DynamicVars.Values.OfType<EnergyVar>()) energy.ColorPrefix = EnergyIconHelper.GetPrefix(card);
                    return formatter.Format(System.Globalization.CultureInfo.InvariantCulture, text,
                        card.DynamicVars.ToDictionary(p => p.Key, p => (object)p.Value).Concat(new[] { new KeyValuePair<string,object>("IfUpgraded", new IfUpgradedVar(upgraded ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal)) }).ToDictionary(p=>p.Key,p=>p.Value));
                }
                string basic = Render(false), upgrade = Render(true);
                Check((spec.Model.MaxUpgradeLevel == 0 ? basic == upgrade : basic != upgrade)
                    && !basic.Contains('{') && !upgrade.Contains('{'), "Authored upgrade difference / all variables bind");
                if (spec.Break > 0) Check(text.Contains("[gold]" + (locale == "zhs" ? "破势" : "Break") + "[/gold]"), "Gold Break text");
                if (spec.Model is WylderVacuumSlice)
                    Check(text.Contains("[gold]" + (locale == "zhs" ? "虚弱" : "Weak") + "[/gold]") && !text.Contains("{Energy"), "Vacuum Weak text replaces obsolete energy reward");
            }
        }
    }

    private static void FlameRevisionCards()
    {
        int before = _checks;
        foreach (bool pursuit in new[] { false, true })
        foreach (bool blocked in new[] { false, true })
        {
            Fixture(pursuit); _fullyBlocked = blocked;
            var strike = Card(ModelDb.Card<WylderFlamingStrike>(), false);
            Put(strike, PileType.Hand);
            int plays = Hook.ModifyCardPlayCount(_combat, strike, 1, _enemies[0], out _);
            Check(plays == (pursuit ? 2 : 1), "Native Replay count selected before Pursuit consumption");
            Check(strike.MaxUpgradeLevel == 1 && !((WylderCard)strike).HasBreak, "Flaming Strike supports sheet upgrade without old Break");
            if (pursuit)
            {
                var power = _owner.Creature.GetPower<WylderPursuitPower>()!;
                Check(power.ModifyCardPlayCount(strike, _enemies[0], 3) == 4, "Pursuit Replay adds to other replay sources");
                var other = Card(ModelDb.Card<WylderStrike>(), false);
                Check(power.ModifyCardPlayCount(other, _enemies[0], 1) == 1, "No Replay leaked to other attacks");
                var foreign = ModelDb.Card<WylderFlamingStrike>().ToMutable();
                AccessTools.Field(typeof(CardModel), "_owner").SetValue(foreign, TrackingArrowRegression.MakePlayer());
                Check(power.ModifyCardPlayCount(foreign, _enemies[0], 1) == 1, "No Replay leaked to ally Flaming Strike");
            }
            for (int i = 0; i < plays; i++)
                Play(strike, captureSource: i == 0, isAutoPlay: i > 0, playIndex: i, playCount: plays);
            Check(Attacks.Count == plays && Attacks.All(a => a == (false, 1, 8m)), "One native single-hit attack per actual play");
            Check(ResolvedDamage.SequenceEqual(pursuit ? new decimal[] { 12, 8 } : new decimal[] { 8 }),
                "First play consumes Pursuit, native Replay does not regain its bonus");
            Check(Generated.Count == plays && Generated.All(c => c is WylderFireCoating && !c.IsUpgraded
                && c.Pile?.Type == PileType.Hand), "Each actual play adds a distinct base coating to hand even when blocked");
            Check(Generated.Distinct().Count() == plays, "Replay creates separate token instances");
            Check(!Rewards.Any(r => r.Kind == nameof(StrengthPower)), "Old permanent Strength reward removed");
        }

        foreach (bool upgraded in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        {
            Fixture(pursuit);
            var coating = Card(ModelDb.Card<WylderFireCoating>(), upgraded);
            Check(coating.Type == CardType.Skill && coating.Rarity == CardRarity.Token && coating.TargetType == TargetType.Self
                && coating.EnergyCost.GetResolved() == 0, "Coating generated Skill metadata");
            Check(coating.Pool is TokenCardPool && coating.VisualCardPool is ColorlessCardPool,
                "Coating uses native token pool and colorless frame");
            Check(!ModelDb.Character<Wylder>().CardPool.AllCards.Any(c => c is WylderFireCoating), "Coating excluded from character reward pool");
            Check(coating.Keywords.SetEquals(new[] { CardKeyword.Retain, CardKeyword.Exhaust }), "Coating native Retain/Exhaust exactly once");
            Check(coating.DynamicVars["Vigor"].BaseValue == (upgraded ? 5 : 3), "Coating upgrade Vigor 3 to 5");
            Play(coating);
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == (upgraded ? 5 : 3), "Coating applies original native Vigor");
            Check(Rewards.Count(r => r.Kind == "Draw") == (pursuit ? 1 : 0), "Colorless Skill also consumes Pursuit and draws once");
            Play(Card(ModelDb.Card<WylderStrike>(), false));
            Check(ResolvedDamage.Last() == 6 + (upgraded ? 5 : 3), "Native coating Vigor increases next attack damage");
            Check(!_owner.Creature.HasPower<VigorPower>(), "Native Vigor consumed by next attack");

            Fixture(pursuit);
            var redmanes = Card(ModelDb.Card<WylderFlameOfRedmanes>(), upgraded);
            Play(redmanes);
            Check(Attacks.Single() == (true, 1, upgraded ? 7m : 6m), "Redmanes native AOE base/upgraded damage");
            Check(_owner.Creature.GetPowerAmount<VigorPower>() == (upgraded ? 5 : 4), "Redmanes retains newly gained Vigor for next attack");
            Check(!Rewards.Any(r => r.Kind == nameof(WeakPower)), "Redmanes old Break/Weak removed");
            Play(Card(ModelDb.Card<WylderStrike>(), false));
            Check(ResolvedDamage.Last() == 6 + (upgraded ? 5 : 4), "Redmanes Vigor applies to following attack");
        }
        Fixture();
        var previewSource = Card(ModelDb.Card<WylderFlamingStrike>(), false);
        var hoverFixture = new HarmonyLib.Harmony("NightMustStay.Wylder.FireCoating.HoverFixture");
        void HoverStub(MethodBase method, string stub) => hoverFixture.Patch(method,
            prefix: new HarmonyMethod(typeof(CardGlossaryRegression), stub));
        HoverStub(AccessTools.Method(typeof(HoverTipFactory), nameof(HoverTipFactory.FromPower), new[] { typeof(PowerModel), typeof(int?) }), nameof(CardGlossaryRegression.Power));
        HoverStub(AccessTools.Method(typeof(HoverTipFactory), nameof(HoverTipFactory.ForEnergy), new[] { typeof(CardModel) }), nameof(CardGlossaryRegression.Energy));
        HoverStub(AccessTools.Method(typeof(LocString), nameof(LocString.GetRawText)), nameof(CardGlossaryRegression.Raw));
        HoverStub(AccessTools.Method(typeof(LocString), nameof(LocString.GetFormattedText)), nameof(CardGlossaryRegression.Formatted));
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                previewSource = Card(ModelDb.Card<WylderFlamingStrike>(), upgraded);
                var tips = (IEnumerable<IHoverTip>)AccessTools.PropertyGetter(typeof(WylderFlamingStrike), "ExtraHoverTips")
                    .Invoke(previewSource, null)!;
                var generatedPreview = tips.OfType<CardHoverTip>().Single().Card;
                Check(generatedPreview is WylderFireCoating && generatedPreview.IsUpgraded == upgraded
                    && generatedPreview.DynamicVars["Vigor"].BaseValue == (upgraded ? 5 : 3), "Flaming Strike hover matches generated coating upgrade");
            }
        }
        finally { hoverFixture.UnpatchAll(hoverFixture.Id); }
        var formatter = new SmartFormatter();
        formatter.AddExtensions(new DictionarySource(), new ReflectionSource(), new DefaultSource());
        formatter.AddExtensions(new HighlightDifferencesFormatter());
        foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
        {
            using var json = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
            string text = json.RootElement.GetProperty("WYLDER_FIRE_COATING.description").GetString()!;
            string Render(bool upgraded)
            {
                Fixture();
                var coating = Card(ModelDb.Card<WylderFireCoating>(), upgraded);
                return formatter.Format(System.Globalization.CultureInfo.InvariantCulture, text,
                    coating.DynamicVars.ToDictionary(p => p.Key, p => (object)p.Value));
            }
            Check(Render(false).Contains('3') && Render(true).Contains('5'), "Four-locale coating upgrade renders real values");
            Check(text == json.RootElement.GetProperty("WYLDER_FIRE_COATING.upgradeDescription").GetString(), "Coating upgrade copy synced");
            Check(!text.Contains("消耗") && !text.Contains("保留") && !text.Contains("Exhaust") && !text.Contains("Retain"),
                "Coating keywords only from native model, never duplicated in body");
        }
        Console.WriteLine($"PASS: {_checks-before} flame revision checks: native Replay dispatch/count, generated colorless Retain/Exhaust tokens, native Vigor, upgrades, blocked hits and AOE reward ordering (UI commands stubbed).");
    }

    private static void FinesseRevision()
    {
        int before = _checks;
        foreach (bool upgraded in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        {
            Fixture(pursuit); _fullyBlocked = true;
            AddPower<DexterityPower>(_owner.Creature, 3);
            var finesse = Card(ModelDb.Card<WylderBloodhoundsFinesse>(), upgraded);
            Check(finesse.EnergyCost.GetResolved() == 1 && finesse.Rarity == CardRarity.Uncommon
                && finesse.Type == CardType.Attack, "Revised Finesse cost/type/rarity");
            finesse.DynamicVars.Block.UpdateCardPreview(finesse, CardPreviewMode.Normal, null!, true);
            Check(finesse.DynamicVars.Block.PreviewValue == (upgraded ? 9 : 7), "Finesse native Dexterity block preview");
            Play(finesse);
            Check(Attacks.Single() == (false, 1, upgraded ? 6m : 4m), "Finesse single native attack base/upgraded raw damage");
            Check(Rewards.Single().Kind == "Block" && Rewards.Single().Amount == (upgraded ? 9 : 7),
                "Finesse actual Block agrees with native preview after fully blocked attack");
            Check(_owner.Creature.HasPower<WylderPursuitPower>(), "Finesse enters/re-enters Pursuit even when attack fully blocked");
            Check(ResolvedDamage.Single() == Math.Floor((upgraded ? 6m : 4m) * (pursuit ? 1.5m : 1m)),
                "Newly entered Pursuit does not retroactively amplify Finesse damage");
            _fullyBlocked = false;
            Play(Card(ModelDb.Card<WylderStrike>(), false));
            Check(ResolvedDamage.Last() == 9 && !_owner.Creature.HasPower<WylderPursuitPower>(),
                "Next attack receives newly entered Pursuit and consumes it normally");
        }
        Check(ModelDb.Card<WylderBloodhoundsFinesse>().PortraitPath ==
            "res://images/packed/card_portraits/wylder/wylder_bloodhounds_finesse.png", "Finesse uses new reference-weapon portrait");
        Console.WriteLine($"PASS: {_checks-before} revised Finesse checks: cost/rarity, native damage/block previews, fully blocked hits and Pursuit entry/re-entry ordering.");
    }

    private static void LionsClawRevision()
    {
        int before = _checks;
        // This shared fixture represents enemies with synthetic Player-backed
        // Creatures. Supply the real solo player count for Imbalance's party
        // scaling instead of counting those synthetic enemies as allies.
        var partyFixture = new HarmonyLib.Harmony("NightMustStay.Wylder.Lion.PartyFixture");
        partyFixture.Patch(AccessTools.PropertyGetter(typeof(CombatState), nameof(CombatState.Players)),
            prefix: new HarmonyMethod(typeof(WylderBattleArtRegression), nameof(ImbalancePlayers)));
        _imbalancePlayerCount = 1;
        try
        {
        foreach (bool upgraded in new[] { false, true })
        foreach (bool blocked in new[] { false, true })
        {
            Fixture();
            var lion = Card(ModelDb.Card<WylderLionsClaw>(), upgraded);
            Check(lion.EnergyCost.GetResolved() == 2 && lion.Type == CardType.Attack && lion.Rarity == CardRarity.Uncommon,
                "Lions Claw revised metadata");
            Check(lion.DynamicVars.Damage.BaseValue == (upgraded ? 24 : 18) && !lion.GainsBlock
                && !lion.DynamicVars.ContainsKey("Block"), "Lions Claw damage upgrade; old Block removed");
            Check(lion.DynamicVars["Break"].BaseValue == 40 && lion.DynamicVars["Imbalance"].BaseValue == (upgraded ? 3 : 2)
                && !lion.DynamicVars["Break"].WasJustUpgraded && lion.DynamicVars["Imbalance"].WasJustUpgraded == upgraded,
                "Fixed Break40 and upgraded Imbalance2-to-3");
            var play = TrackingArrowRegression.MakePlay(lion, _owner);
            lion.BeforeCardPlayed(play).GetAwaiter().GetResult();
            Hit(lion, _enemies[0], 39, blocked);
            Check(!_enemies[0].HasPower<PhantomImbalancePower>(), "39 pre-block damage cannot trigger Break40");
            Hit(lion, _enemies[0], 1, blocked);
            Check(!_enemies[0].HasPower<PhantomImbalancePower>(), "Sub-threshold hits do not accumulate into Break40");
            Hit(lion, _enemies[0], 40, blocked);
            Check(Rewards.Any(r => r.Kind == nameof(PhantomImbalancePower) && r.Amount == (upgraded ? 3 : 2)
                    && r.Target == _enemies[0]) && !_enemies[1].HasPower<PhantomImbalancePower>(),
                "One hit at exactly40 applies the upgraded Imbalance only to the damaged enemy");
            Check(Rewards.Any(r => r.Kind == "Stun")
                    && _enemies[0].GetPowerAmount<PhantomImbalancePower>() == (upgraded ? 1 : 0),
                "Imbalance2/3 reaches the dynamic solo Stun threshold and preserves overflow");
            int rewardCount = Rewards.Count;
            Hit(lion, _enemies[0], 100, blocked); Hit(lion, _enemies[1], 100, blocked);
            Check(Rewards.Count == rewardCount && Rewards[0].Kind == nameof(PhantomImbalancePower), "Break triggers once per CardPlay, no self Block");
            lion.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
            Hit(lion, _enemies[1], 40, blocked);
            Check(Rewards.Count == rewardCount, "Post-play hits cannot trigger Break");

            Fixture();
            AddPower<PhantomImbalancePower>(_enemies[0], 1);
            lion = Card(ModelDb.Card<WylderLionsClaw>(), upgraded);
            play = TrackingArrowRegression.MakePlay(lion, _owner);
            lion.BeforeCardPlayed(play).GetAwaiter().GetResult();
            Hit(lion, _enemies[0], 40, blocked);
            Check(Rewards.Count(r => r.Kind == "Stun") == 1 && Rewards.Single(r => r.Kind == "Stun").Target == _enemies[0],
                "Existing1 plus new2 uses existing Imbalance threshold to Stun once");
            Check(_enemies[0].GetPowerAmount<PhantomImbalancePower>() == (upgraded ? 2 : 1),
                "Stun consumes the current two-stack threshold and preserves excess");
        }
        Fixture();
        var lethal = Card(ModelDb.Card<WylderLionsClaw>(), false);
        var lethalPlay = TrackingArrowRegression.MakePlay(lethal, _owner);
        lethal.BeforeCardPlayed(lethalPlay).GetAwaiter().GetResult();
        _enemies[0].SetCurrentHpInternal(0);
        Hit(lethal, _enemies[0], 40, false);
        Check(Rewards.Count == 0 && !_enemies[0].HasPower<PhantomImbalancePower>(), "No Imbalance or Stun is applied to an already-dead target");
        foreach (int players in new[] { 2, 3 })
        {
            _imbalancePlayerCount = players;
            Fixture();
            int threshold = players == 2 ? 3 : 5;
            Check(PhantomImbalancePower.GetThreshold(players) == threshold, "Dynamic multiplayer Imbalance threshold retained");
            AddPower<PhantomImbalancePower>(_enemies[0], threshold - 2);
            var multiplayerLion = Card(ModelDb.Card<WylderLionsClaw>(), false);
            var multiplayerPlay = TrackingArrowRegression.MakePlay(multiplayerLion, _owner);
            multiplayerLion.BeforeCardPlayed(multiplayerPlay).GetAwaiter().GetResult();
            Hit(multiplayerLion, _enemies[0], 40, false);
            Check(Rewards.Count(r => r.Kind == "Stun") == 1 && !_enemies[0].HasPower<PhantomImbalancePower>(),
                "Lions Claw uses existing multiplayer Imbalance resolution and consumes threshold");
        }
        Check(ModelDb.Card<WylderLionsClaw>().PortraitPath == "res://images/packed/card_portraits/wylder/wylder_lions_claw.png",
            "Lions Claw reviewed landing portrait path");
        Console.WriteLine($"PASS: {_checks-before} revised Lions Claw checks: fixed threshold/reward, damage upgrade, pre-block boundaries, once-per-play and native project Imbalance Stun threshold.");
        }
        finally { partyFixture.UnpatchAll(partyFixture.Id); _imbalancePlayerCount = 1; }
    }

    private static void StormBladeRevision()
    {
        int before = _checks;
        foreach (bool upgraded in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        foreach (bool blocked in new[] { false, true })
        {
            Fixture(pursuit); _fullyBlocked = blocked;
            var card = Card(ModelDb.Card<WylderStormBlade>(), upgraded);
            int hits = pursuit ? 3 : 1;
            decimal raw = upgraded ? 5 : 4;
            Check(card.EnergyCost.GetResolved() == 0 && card.Type == CardType.Attack && card.Rarity == CardRarity.Uncommon,
                "Storm Blade is zero-cost Uncommon attack before/after upgrade");
            Check(card.TargetType == TargetType.AnyEnemy && !card.IsValidTarget(_owner.Creature), "Storm Blade remains single-enemy targeted");
            Check(card.DynamicVars.Repeat.BaseValue == 2 && !card.DynamicVars.Repeat.WasJustUpgraded,
                "Storm Blade extra two hits are fixed, not green on upgrade");
            Check((bool)AccessTools.PropertyGetter(typeof(WylderCard), "ShouldGlowGoldInternal").Invoke(card, null)! == pursuit,
                "Storm Blade condition glow matches Pursuit");
            Check(Hook.ModifyCardPlayCount(_combat, card, 1, _enemies[0], out _) == 1,
                "Storm extra attacks do not add CardPlay Replay");
            Play(card);
            Check(Attacks.Single() == (false, hits, raw) && ResolvedDamage.Count == hits,
                "Storm Blade uses one native single-target attack with 1 or 3 hits");
            Check(ResolvedDamage.All(damage => damage == Math.Floor(raw * (pursuit ? 1.5m : 1m))),
                "Pursuit damage multiplier applies to every extra hit, including blocked attacks");
            Check(Rewards.Count == 0 && Generated.Count == 0, "No obsolete draw, energy or generated-card reward");
            Check(!_owner.Creature.HasPower<WylderPursuitPower>(), "Pursuit exits only after complete Storm attack");
            Attacks.Clear(); ResolvedDamage.Clear(); _fullyBlocked = false;
            Play(Card(ModelDb.Card<WylderStrike>(), false));
            Check(ResolvedDamage.Single() == 6, "Following attack receives neither extra Storm hits nor consumed Pursuit bonus");
        }
        Fixture(true);
        AddPower<VigorPower>(_owner.Creature, 3);
        var empowered = Card(ModelDb.Card<WylderStormBlade>(), false);
        Play(empowered);
        Check(Attacks.Single() == (false, 3, 4m) && ResolvedDamage.SequenceEqual(new decimal[] { 10, 10, 10 }),
            "Native Vigor and Pursuit apply to all three hits of the same attack");
        Check(!_owner.Creature.HasPower<VigorPower>(), "Native Vigor is consumed once after the complete attack");
        Check(ModelDb.Card<WylderStormBlade>().PortraitPath == "res://images/packed/card_portraits/wylder/wylder_storm_blade.png",
            "Storm Blade uses reviewed white-spiral artwork");
        Console.WriteLine($"PASS: {_checks-before} revised Storm Blade checks: zero-cost Uncommon, single target, native 1/3 hits, no Replay/draw, full multihit Pursuit/Vigor and upgrade/glow.");
    }

    private static void VacuumRevision()
    {
        int before = _checks;
        foreach (bool upgraded in new[] { false, true })
        foreach (bool blocked in new[] { false, true })
        {
            Fixture();
            var card = Card(ModelDb.Card<WylderVacuumSlice>(), upgraded);
            Check(card.TargetType == TargetType.AllEnemies && card.IsValidTarget(null), "Vacuum AOE needs no manual target");
            Check(card.DynamicVars["Weak"].BaseValue == (upgraded ? 3 : 2)
                && !card.DynamicVars["Break"].WasJustUpgraded, "Vacuum upgrades only damage and Weak, not Break20");
            var play = TrackingArrowRegression.MakePlay(card, _owner);
            card.BeforeCardPlayed(play).GetAwaiter().GetResult();
            Hit(card, _enemies[0], 19, blocked);
            Hit(card, _enemies[1], 19, blocked);
            Check(Rewards.Count == 0, "Vacuum does not sum damage across targets");
            Hit(card, _enemies[0], 1, blocked);
            Check(Rewards.Count == 0, "Vacuum does not accumulate sub-threshold hits on one target");
            Hit(card, _enemies[0], 20, blocked);
            Check(Rewards.Count == 2 && Rewards.All(r => r.Kind == nameof(WeakPower)
                && r.Amount == (upgraded ? 3 : 2)), "Vacuum exact pre-block20 applies Weak to every living enemy");
            Hit(card, _enemies[1], 1, blocked);
            Check(Rewards.Count == 2, "A second qualifying enemy does not duplicate Vacuum Weak");
            card.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
            Hit(card, _enemies[0], 30, blocked);
            Check(Rewards.Count == 2, "Vacuum cannot trigger after resolution");
        }
        Fixture();
        var lethal = Card(ModelDb.Card<WylderVacuumSlice>(), true);
        lethal.BeforeCardPlayed(TrackingArrowRegression.MakePlay(lethal, _owner)).GetAwaiter().GetResult();
        _enemies[0].SetCurrentHpInternal(0);
        Hit(lethal, _enemies[0], 20, false);
        Check(Rewards.Count == 1 && Rewards[0].Target == _enemies[1] && Rewards[0].Amount == 3,
            "A lethal qualifying hit still applies Vacuum Weak to surviving enemies only");
        Fixture(true); _fullyBlocked = true;
        AddPower<StrengthPower>(_owner.Creature, 5);
        Play(Card(ModelDb.Card<WylderVacuumSlice>(), false));
        Check(Attacks.Single() == (true, 1, 9m) && ResolvedDamage.SequenceEqual(new decimal[] { 21, 21 }),
            "Native AOE builder resolves both modified hits even when fully blocked");
        Check(Rewards.Count == 2 && Rewards.All(r => r.Kind == nameof(WeakPower) && r.Amount == 2),
            "Both targets exceeding20 while blocked gives only one Weak application per enemy and no energy refund");
        Check(ModelDb.Card<WylderVacuumSlice>().PortraitPath == "res://images/packed/card_portraits/wylder/wylder_vacuum_slice.png",
            "Vacuum uses reviewed upright wind-blade portrait");
        Console.WriteLine($"PASS: {_checks-before} revised Vacuum Slice checks: AOE target, base/upgraded Weak, per-hit pre-block threshold, no accumulation, once-per-play, lethal target and native blocked AOE.");
    }

    private static void BreakBoundaries()
    {
        foreach (Spec spec in Specs().Where(s => s.Break > 0))
        {
            Fixture();
            var card = Card(spec.Model, false);
            var play = TrackingArrowRegression.MakePlay(card, _owner);
            card.BeforeCardPlayed(play).GetAwaiter().GetResult();
            int requiredHits = card is WylderSpinningSlash ? 2 : 1;
            for (int i = 0; i < requiredHits; i++) Hit(card, _enemies[0], spec.Break - 1, true);
            for (int i = 0; i < requiredHits; i++) Hit(card, _enemies[1], spec.Break, true);
            Check(Rewards.Count == (card is WylderSpinningSlash ? 1 : card is WylderVacuumSlice ? 2 : 1),
                "Each target is checked independently and sub-threshold hits never accumulate " + card.Id);
            card.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();

            Rewards.Clear();
            play = TrackingArrowRegression.MakePlay(card, _owner);
            card.BeforeCardPlayed(play).GetAwaiter().GetResult();
            for (int i = 0; i < requiredHits; i++) Hit(card, _enemies[0], spec.Break, true);
            int count = card is WylderVacuumSlice ? 2 : 1;
            Check(Rewards.Count == count, "Every required hit at the exact fully blocked threshold Breaks " + card.Id);
            if (card is WylderVacuumSlice) Check(Rewards.All(reward => reward.Kind == nameof(WeakPower) && reward.Amount == 2)
                && Rewards.Select(reward => reward.Target).Distinct().Count() == 2, "Vacuum Weak2 once to all living opponents");
            if (card is WylderLionsClaw) Check(Rewards.Single().Kind == nameof(PhantomImbalancePower)
                && Rewards.Single().Amount == 2 && Rewards.Single().Target == _enemies[0], "Lion exact Break gives fixed Imbalance to broken target");
            if (card is WylderSpinningSlash) Check(Rewards.Single().Amount == 1, "Spinning draw one");
            Hit(card, _enemies[0], 100, false); Hit(card, _enemies[1], 100, false);
            Check(Rewards.Count == count, "No repeat reward in same play " + card.Id);
            card.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
            Hit(card, _enemies[0], 100, false);
            Check(Rewards.Count == count, "No reward outside resolution");
            card.BeforeCardPlayed(play).GetAwaiter().GetResult();
            for (int i = 0; i < requiredHits; i++) Hit(card, _enemies[1], spec.Break, false);
            Check(Rewards.Count == count * 2, "Independent replay/next-play reset " + card.Id);
        }
        Fixture(true); _fullyBlocked = true;
        Play(Card(ModelDb.Card<WylderSpinningSlash>(), false));
        Check(Rewards.Count(r => r.Kind == "Draw") == 0, "Fully blocked sub-threshold native AOE hits do not accumulate");
        Fixture(true); _fullyBlocked = true; AddPower<StrengthPower>(_owner.Creature, 4);
        Play(Card(ModelDb.Card<WylderSpinningSlash>(), false));
        Check(Rewards.Count(r => r.Kind == "Draw") == 1, "Native AOE multi-hit Breaks once when every hit reaches the threshold");
    }

    private static void Fixture(bool pursuit = false)
    {
        Rewards.Clear(); Attacks.Clear(); ResolvedDamage.Clear(); Generated.Clear(); _fullyBlocked = false;
        CombatManager.Instance.History.Clear();
        _owner = TrackingArrowRegression.MakePlayer();
        AccessTools.Field(typeof(Player), "_relics").SetValue(_owner, new List<RelicModel>());
        AccessTools.Field(typeof(Player), "<Character>k__BackingField").SetValue(_owner, ModelDb.Character<Wylder>());
        AccessTools.Field(typeof(Player), "_runState").SetValue(_owner, NullRunState.Instance);
        _combat = new CombatState();
        var creature = new Creature(_owner, 1000, 1000) { CombatState = _combat };
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(_owner, creature);
        ((List<Creature>)AccessTools.Field(typeof(CombatState), "_allies").GetValue(_combat)!).Add(creature);
        _enemies = Enumerable.Range(0, 2).Select(_ => new Creature(_owner, 1000, 1000) { CombatState = _combat }).ToArray();
        foreach (var enemy in _enemies)
        {
            AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(enemy, CombatSide.Enemy);
            ((List<Creature>)AccessTools.Field(typeof(CombatState), "_enemies").GetValue(_combat)!).Add(enemy);
        }
        if (pursuit) AddPower<WylderPursuitPower>(creature, 1);
    }

    private static CardModel Card(CardModel model, bool upgraded)
    {
        var card = model.ToMutable();
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, _owner);
        if (upgraded && card.MaxUpgradeLevel > 0) card.UpgradeInternal();
        return card;
    }
    private static void Play(CardModel card, bool captureSource = true, bool isAutoPlay = false, int playIndex = 0, int? playCount = null, Creature? target = null)
    {
        var sourceType = typeof(Wylder).Assembly.GetType("NightMustStay.Core.Models.Cards.WylderHandPlayContext")!;
        if (captureSource) AccessTools.Method(sourceType, "CaptureWrapper").Invoke(null, new object[] { card });
        if (card.Pile?.Type == PileType.Hand) Put(card, PileType.Play);
        var play = TrackingArrowRegression.MakePlay(card, _owner);
        typeof(CardPlay).GetProperty(nameof(CardPlay.IsAutoPlay))!.SetValue(play, isAutoPlay);
        typeof(CardPlay).GetProperty(nameof(CardPlay.PlayIndex))!.SetValue(play, playIndex);
        typeof(CardPlay).GetProperty(nameof(CardPlay.PlayCount))!.SetValue(play, playCount ?? playIndex + 1);
        typeof(CardPlay).GetProperty(nameof(CardPlay.Target))!.SetValue(play, target ?? _enemies[0]);
        AccessTools.Method(sourceType, "Record").Invoke(null, new object[] { play });
        card.BeforeCardPlayed(play).GetAwaiter().GetResult();
        foreach (var power in _owner.Creature.Powers.OfType<WylderPursuitPower>().ToArray()) power.BeforeCardPlayed(play).GetAwaiter().GetResult();
        CombatManager.Instance.History.CardPlayStarted(_combat, play);
        ((Task)AccessTools.Method(card.GetType(), "OnPlay").Invoke(card, new object?[] { null, play })!).GetAwaiter().GetResult();
        CombatManager.Instance.History.CardPlayFinished(_combat, play);
        card.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
        foreach (var power in _owner.Creature.Powers.OfType<WylderPursuitPower>().ToArray()) power.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
    }
    private static void Hit(CardModel card, Creature target, decimal amount, bool blocked, bool killed = false)
    {
        var result = new DamageResult(target, ValueProp.Move)
        {
            UnblockedDamage = blocked ? 0 : (int)amount,
            BlockedDamage = blocked ? (int)amount : 0,
            WasTargetKilled = killed
        };
        card.AfterDamageGiven(null!, _owner.Creature, result, ValueProp.Move, target, card).GetAwaiter().GetResult();
    }
    private static T AddPower<T>(Creature target, int amount) where T : PowerModel
    {
        var p = (T)ModelDb.Power<T>().ToMutable();
        Install(p, target, amount);
        return p;
    }
    private static void Install(PowerModel power, Creature target, decimal amount)
    {
        typeof(PowerModel).GetProperty(nameof(PowerModel.Owner))!.SetValue(power, target);
        power.SetAmount((int)amount, true);
        ((List<PowerModel>)AccessTools.Field(typeof(Creature), "_powers").GetValue(target)!).Add(power);
    }
    public static bool Listeners(CombatState __instance, ref IEnumerable<AbstractModel> __result)
    { __result = __instance.Creatures.SelectMany(c => c.Powers).Cast<AbstractModel>()
        .Concat(PileType.Hand.GetPile(_owner).Cards).ToArray(); return false; }
    public static bool Pool(CardModel __instance, ref CardPoolModel __result)
    {
        if (__instance is not WylderCard) return true;
        __result = ModelDb.Character<Wylder>().CardPool;
        return false;
    }
    public static bool Attack(AttackCommand __instance, PlayerChoiceContext choiceContext, ref Task<AttackCommand> __result)
    {
        // Eochaid's changing hit budget must exercise the real engine loop.
        if (__instance.ModelSource is WylderEochaidsDancingBlade or WylderOrdovisVortex) return true;
        __result = Resolve(__instance, choiceContext); return false;
    }
    private static async Task<AttackCommand> Resolve(AttackCommand attack, PlayerChoiceContext context)
    {
        var card = (CardModel)attack.ModelSource!;
        int hits = (int)AccessTools.Field(typeof(AttackCommand), "_hitCount").GetValue(attack)!;
        decimal raw = (decimal)AccessTools.Field(typeof(AttackCommand), "_damagePerHit").GetValue(attack)!;
        Attacks.Add((attack.IsMultiTargeted, hits, raw));
        foreach (var power in _owner.Creature.Powers.ToArray())
            await power.BeforeAttack(attack);
        for (int i = 0; i < hits; i++)
        foreach (var target in attack.IsMultiTargeted ? _enemies.Where(c => c.IsAlive) : new[] { (Creature)AccessTools.Field(typeof(AttackCommand), "_singleTarget").GetValue(attack)! })
        {
            DynamicVar preview = card is WylderSiluriaVortex ? card.DynamicVars.CalculatedDamage : card.DynamicVars.Damage;
            preview.UpdateCardPreview(card, CardPreviewMode.Normal, target, true);
            decimal damage = Math.Floor(preview.PreviewValue);
            ResolvedDamage.Add(damage);
            var result = new DamageResult(target, attack.DamageProps) { UnblockedDamage = _fullyBlocked ? 0 : (int)damage, BlockedDamage = _fullyBlocked ? (int)damage : 0 };
            await card.AfterDamageGiven(context, attack.Attacker!, result, attack.DamageProps, target, card);
        }
        foreach (var power in _owner.Creature.Powers.ToArray())
            await power.AfterAttack(context, attack);
        return attack;
    }
    public static bool Energy(decimal amount, ref Task __result)
    { Rewards.Add(("Energy", _owner.Creature, amount, null)); __result = Task.CompletedTask; return false; }
    public static bool Generate(CardModel __0, PileType __1, Player __2, ref Task<CardPileAddResult> __result)
    {
        Check(__1 == PileType.Hand && __2 == _owner && __0.Owner == _owner, "Generated coating goes to caster hand");
        Generated.Add(__0);
        Put(__0, __1);
        __result = Task.FromResult(new CardPileAddResult { success = true, cardAdded = __0 });
        return false;
    }
    public static bool Block(Creature creature, decimal amount, ValueProp props, CardPlay? cardPlay, ref Task<decimal> __result)
    {
        if (cardPlay != null)
        {
            var block = cardPlay.Card.DynamicVars.TryGetValue("CalculatedBlock", out var calculated) ? calculated : cardPlay.Card.DynamicVars.Block;
            block.UpdateCardPreview(cardPlay.Card, CardPreviewMode.Normal, null!, true);
            amount = block.PreviewValue;
        }
        Rewards.Add(("Block", creature, amount, cardPlay)); __result = Task.FromResult(amount); return false;
    }
    public static bool Draw(decimal count, ref Task<IEnumerable<CardModel>> __result)
    { Rewards.Add(("Draw", _owner.Creature, count, null)); __result = Task.FromResult<IEnumerable<CardModel>>(Array.Empty<CardModel>()); return false; }
    public static bool Apply(PowerModel power, Creature target, decimal amount, ref Task __result)
    {
        var existing = target.Powers.FirstOrDefault(p => p.GetType() == power.GetType());
        if (existing != null && power.StackType == PowerStackType.Counter)
            existing.SetAmount(existing.Amount + (int)amount, true);
        else Install(power, target, amount);
        if (power is not WylderPursuitPower) Rewards.Add((power.GetType().Name, target, amount, null));
        __result = Task.CompletedTask; return false;
    }
    public static bool Modify(PowerModel power, decimal offset, ref Task<int> __result)
    {
        power.SetAmount(power.Amount + (int)offset, true);
        Rewards.Add((power.GetType().Name, power.Owner, offset, null));
        if (power.Amount <= 0) ((List<PowerModel>)AccessTools.Field(typeof(Creature), "_powers").GetValue(power.Owner)!).Remove(power);
        __result = Task.FromResult(power.Amount); return false;
    }
    public static bool Remove(PowerModel power, ref Task __result)
    { ((List<PowerModel>)AccessTools.Field(typeof(Creature), "_powers").GetValue(power.Owner)!).Remove(power); __result = Task.CompletedTask; return false; }
    public static bool Stun(Creature __0, ref Task __result)
    { Rewards.Add(("Stun", __0, 1m, null)); __result = Task.CompletedTask; return false; }
    public static bool ImbalancePlayers(ref IReadOnlyList<Player> __result)
    {
        __result = Enumerable.Range(0, _imbalancePlayerCount)
            .Select(index => index == 0 ? _owner : TrackingArrowRegression.MakePlayer()).ToArray();
        return false;
    }
    private static void Check(bool condition, string message)
    { _checks++; if (!condition) throw new Exception("Wylder battle arts: " + message); }
}
