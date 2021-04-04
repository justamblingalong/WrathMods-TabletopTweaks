﻿using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TabletopTweaks.Extensions;
using TabletopTweaks.Utilities;
using UnityEngine;

namespace TabletopTweaks.Bugfixes.Classes {
    class Azata {
        [HarmonyPatch(typeof(ResourcesLibrary), "InitializeLibrary")]
        static class ResourcesLibrary_InitializeLibrary_Patch {
            static bool Initialized;

            static bool Prefix() {
                if (Initialized) {
                    // When wrath first loads into the main menu InitializeLibrary is called by Kingmaker.GameStarter.
                    // When loading into maps, Kingmaker.Runner.Start will call InitializeLibrary which will
                    // clear the ResourcesLibrary.s_LoadedBlueprints cache which causes loaded blueprints to be garbage collected.
                    // Return false here to prevent ResourcesLibrary.InitializeLibrary from being called twice 
                    // to prevent blueprints from being garbage collected.
                    return false;
                }
                else {
                    return true;
                }
            }
            static void Postfix() {
                if (!Resources.Settings.FixAzata) { return; }
                if (Initialized) return;
                Initialized = true;
                Main.LogHeader("Patching Azata Resources");
                PatchAzataSpells();
                PatchAzataPerformanceResource();
                Main.LogHeader("Azata Resource Patch Complete");
            }
            static void PatchAzataSpells() {
                PatchOdeToMiraculousMagicBuff();
                PatchBelieveInYourself();

                void PatchOdeToMiraculousMagicBuff() {
                    BlueprintBuff OdeToMiraculousMagicBuff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("f6ef0e25745114d46bf16fd5a1d93cc9");
                    IncreaseCastersSavingThrowTypeDC bonusSaveDC = Helpers.Create<IncreaseCastersSavingThrowTypeDC>(c => {
                        c.Type = SavingThrowType.Will;
                        c.BonusDC = 2;
                    });
                    OdeToMiraculousMagicBuff.AddComponent(bonusSaveDC);
                    Main.LogPatch("Patched", OdeToMiraculousMagicBuff);
                }
                void PatchBelieveInYourself() {
                    BlueprintAbility BelieveInYourself = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("3ed3cef7c267cb847bfd44ed4708b726");
                    BlueprintAbilityReference[] BelieveInYourselfVariants = BelieveInYourself
                        .GetComponent<AbilityVariants>()
                        .Variants;
                    foreach (BlueprintAbility Variant in BelieveInYourselfVariants) {
                        Variant.FlattenAllActions()
                            .OfType<ContextActionApplyBuff>()
                            .ForEach(b => {
                                b.Buff.GetComponent<ContextRankConfig>().m_StepLevel = 2;
                                Main.LogPatch("Patched", b.Buff);
                            });
                    }
                }
            }
            static void PatchAzataPerformanceResource() {
                var AzataPerformanceResource = ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>("83f8a1c45ed205a4a989b7826f5c0687");

                BlueprintCharacterClassReference[] characterClasses = ResourcesLibrary
                    .GetRoot()
                    .Progression
                    .CharacterClasses
                    .Where(c => c != null)
                    .Select(c => c.ToReference<BlueprintCharacterClassReference>())
                    .ToArray();
                AzataPerformanceResource.m_MaxAmount.m_Class = characterClasses;
                Main.LogPatch("Patched", AzataPerformanceResource);
            }
        }
        // Patch for Favorable Magic
        [HarmonyPatch(typeof(AzataFavorableMagic), "CheckReroll", new[] { typeof(RuleSavingThrow), typeof(RuleRollD20) })]
        static class AzataFavorableMagic_CheckReroll_Patch {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                if (!Resources.Settings.FixAzata) { return instructions; }
                var codes = new List<CodeInstruction>(instructions);
                var startIndex = -1;
                var stopIndex = -1;
                var foundNull = false;

                for (int i = 0; i < codes.Count; i++) {
                    if (codes[i].opcode == OpCodes.Ldarg_2) {
                        startIndex = i;
                        foundNull = false;
                        continue;
                    }
                    if (codes[i].opcode == OpCodes.Ldnull && startIndex > -1) {
                        foundNull = true;
                        continue;
                    }
                    if (codes[i].opcode == OpCodes.Brfalse_S && foundNull) {
                        stopIndex = i;
                        break;
                    }
                }
                if (startIndex > -1 && stopIndex > -1) {
                    for (int i = startIndex; i <= stopIndex; i++) {
                        codes[i].opcode = OpCodes.Nop;
                    }
                }
                return codes.AsEnumerable();
            }
        }
        // Patch for Zippy Magic
        [HarmonyPatch(typeof(DublicateSpellComponent), "Kingmaker.PubSubSystem.IRulebookHandler<Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell>.OnEventDidTrigger", new[] { typeof(RuleCastSpell) })]
        static class DublicateSpellComponent_OnEventDidTrigger_Patch {

            static void Postfix(DublicateSpellComponent __instance, ref RuleCastSpell evt) {
                if (!Resources.Settings.FixAzata) { return; }
                Main.Log("Zippy Trigger");
                if (evt.IsSpellFailed ||
                    evt.Spell.IsAOE ||
                    !evt.SpellTarget.Unit.IsPlayersEnemy ||
                    evt.Spell.Blueprint.Animation == UnitAnimationActionCastSpell.CastAnimationStyle.Self) {

                    Main.Log($"{evt.Spell.Name} : Zippy Trigger Early Return");
                    return;
                }
                Main.Log($"{evt.Spell.Name} : Zippy Trigger Entered Damage Trigger");
                DiceFormula dice = new DiceFormula(2, DiceType.D6);
                int mythicLevel = evt.Spell.Caster.Unit.Progression.MythicExperience;
                RuleDealDamage ruleDealDamage = new RuleDealDamage(evt.Spell.Caster, evt.SpellTarget.Unit, new EnergyDamage(dice, mythicLevel, DamageEnergyType.Divine));
                Rulebook.Trigger<RuleDealDamage>(ruleDealDamage);
            }
        }
    }
}
