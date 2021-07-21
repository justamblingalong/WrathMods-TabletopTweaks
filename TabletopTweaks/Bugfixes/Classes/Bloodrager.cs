﻿using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using System.Linq;
using TabletopTweaks.Config;
using TabletopTweaks.Utilities;

namespace TabletopTweaks.Bugfixes.Classes {
    class Bloodrager {
        [HarmonyPatch(typeof(BlueprintsCache), "Init")]
        static class BlueprintsCache_Init_Patch {
            static bool Initialized;

            static void Postfix() {
                if (Initialized) return;
                Initialized = true;
                if (ModSettings.Fixes.Bloodrager.DisableAll) { return; }
                Main.LogHeader("Patching Bloodrager");
                PatchBaseClass();
                PatchPrimalist();
                PatchSteelblood();
                PatchReformedFiend();
            }
            static void PatchBaseClass() {
                if (ModSettings.Fixes.Bloodrager.Base.DisableAll) { return; }
                PatchSpellbook();
                PatchAbysalBulk();
                PatchLimitlessRage();

                void PatchAbysalBulk() {
                    if (!ModSettings.Fixes.Bloodrager.Base.Enabled["AbysalBulk"]) { return; }
                    var BloodragerAbyssalBloodlineBaseBuff = Resources.GetBlueprint<BlueprintBuff>("2ba7b4b3b87156543b43d0686404655a");
                    var BloodragerAbyssalDemonicBulkBuff = Resources.GetBlueprint<BlueprintBuff>("031a8053a7c02ab42ad53f50dd2e9437");
                    var BloodragerAbyssalDemonicBulkEnlargeBuff = Resources.GetBlueprint<BlueprintBuff>(ModSettings.Blueprints.GetGUID("BloodragerAbyssalDemonicBulkEnlargeBuff"));

                    var ApplyBuff = new ContextActionApplyBuff() {
                        m_Buff = BloodragerAbyssalDemonicBulkEnlargeBuff.ToReference<BlueprintBuffReference>(),
                        AsChild = true,
                        Permanent = true
                    };
                    var RemoveBuff = new ContextActionRemoveBuff() {
                        m_Buff = BloodragerAbyssalDemonicBulkEnlargeBuff.ToReference<BlueprintBuffReference>()
                    };
                    var AddFactContext = BloodragerAbyssalBloodlineBaseBuff.GetComponent<AddFactContextActions>();

                    AddFactContext.Activated.Actions.OfType<Conditional>().Where(a => a.Comment.Equals("Demonic Bulk")).First().AddActionIfTrue(ApplyBuff);
                    AddFactContext.Deactivated.Actions.OfType<Conditional>().Where(a => a.Comment.Equals("Demonic Bulk")).First().IfTrue = null;
                    AddFactContext.Deactivated.Actions.OfType<Conditional>().Where(a => a.Comment.Equals("Demonic Bulk")).First().AddActionIfTrue(RemoveBuff);
                }
                void PatchSpellbook() {
                    if (!ModSettings.Fixes.Bloodrager.Base.Enabled["Spellbook"]) { return; }
                    BlueprintSpellbook BloodragerSpellbook = Resources.GetBlueprint<BlueprintSpellbook>("e19484252c2f80e4a9439b3681b20f00");
                    var BloodragerSpellKnownTable = BloodragerSpellbook.SpellsKnown;
                    var BloodragerSpellPerDayTable = BloodragerSpellbook.SpellsPerDay;
                    BloodragerSpellbook.CasterLevelModifier = 0;
                    BloodragerSpellKnownTable.Levels = new SpellsLevelEntry[] {
                        CreateSpellLevelEntry(0),
                        CreateSpellLevelEntry(0),
                        CreateSpellLevelEntry(0),
                        CreateSpellLevelEntry(0),
                        CreateSpellLevelEntry(0,2),
                        CreateSpellLevelEntry(0,3),
                        CreateSpellLevelEntry(0,4),
                        CreateSpellLevelEntry(0,4,2),
                        CreateSpellLevelEntry(0,4,3),
                        CreateSpellLevelEntry(0,5,4),
                        CreateSpellLevelEntry(0,5,4,2),
                        CreateSpellLevelEntry(0,5,4,3),
                        CreateSpellLevelEntry(0,6,5,4),
                        CreateSpellLevelEntry(0,6,5,4,2),
                        CreateSpellLevelEntry(0,6,5,4,3),
                        CreateSpellLevelEntry(0,6,6,5,4),
                        CreateSpellLevelEntry(0,6,6,5,4),
                        CreateSpellLevelEntry(0,6,6,5,4),
                        CreateSpellLevelEntry(0,6,6,6,5),
                        CreateSpellLevelEntry(0,6,6,6,5),
                        CreateSpellLevelEntry(0,6,6,6,5)
                    };
                    BloodragerSpellPerDayTable.Levels = new SpellsLevelEntry[] {
                        CreateSpellLevelEntry(0),
                        CreateSpellLevelEntry(0),
                        CreateSpellLevelEntry(0),
                        CreateSpellLevelEntry(0),
                        CreateSpellLevelEntry(0,1),
                        CreateSpellLevelEntry(0,1),
                        CreateSpellLevelEntry(0,1),
                        CreateSpellLevelEntry(0,1,1),
                        CreateSpellLevelEntry(0,1,1),
                        CreateSpellLevelEntry(0,2,1),
                        CreateSpellLevelEntry(0,2,1,1),
                        CreateSpellLevelEntry(0,2,1,1),
                        CreateSpellLevelEntry(0,2,2,1),
                        CreateSpellLevelEntry(0,3,2,1,1),
                        CreateSpellLevelEntry(0,3,2,1,1),
                        CreateSpellLevelEntry(0,3,2,2,1),
                        CreateSpellLevelEntry(0,3,3,2,1),
                        CreateSpellLevelEntry(0,4,3,2,1),
                        CreateSpellLevelEntry(0,4,3,2,2),
                        CreateSpellLevelEntry(0,4,3,3,2),
                        CreateSpellLevelEntry(0,4,4,3,2)
                    };
                    Main.LogPatch("Patched", BloodragerSpellPerDayTable);
                    SpellsLevelEntry CreateSpellLevelEntry(params int[] count) {
                        var entry = new SpellsLevelEntry();
                        entry.Count = count;
                        return entry;
                    }
                }
                void PatchLimitlessRage() {
                    if (!ModSettings.Fixes.Bloodrager.Base.Enabled["LimitlessRage"]) { return; }
                    var BloodragerStandartRageBuff = Resources.GetBlueprint<BlueprintBuff>("5eac31e457999334b98f98b60fc73b2f");
                    var BloodragerRageResource = Resources.GetBlueprint<BlueprintAbilityResource>("4aec9ec9d9cd5e24a95da90e56c72e37");
                    BloodragerStandartRageBuff
                        .GetComponent<TemporaryHitPointsPerLevel>()
                        .m_LimitlessRageResource = BloodragerRageResource.ToReference<BlueprintAbilityResourceReference>();
                    Main.LogPatch("Patched", BloodragerRageResource);
                }
            }
         
                static void PatchSteelblood() {
                if (ModSettings.Fixes.Bloodrager.Archetypes["Steelblood"].DisableAll) { return; }
                PatchArmoredSwiftness();

                void PatchArmoredSwiftness() {
                    if (!ModSettings.Fixes.Bloodrager.Archetypes["Steelblood"].Enabled["ArmoredSwiftness"]) { return; }
                    var ArmoredHulkArmoredSwiftness = Resources.GetBlueprint<BlueprintFeature>("f95f4f3a10917114c82bcbebc4d0fd36");
                    var SteelbloodArmoredSwiftness = Resources.GetBlueprint<BlueprintFeature>("bd4397ee26a3baf4cadaeb766b018cff");
                    SteelbloodArmoredSwiftness.ComponentsArray = ArmoredHulkArmoredSwiftness.ComponentsArray;
                }
            }
            static void PatchReformedFiend() {
                if (ModSettings.Fixes.Bloodrager.Archetypes["ReformedFiend"].DisableAll) { return; }
                PatchHatredAgainstEvil();
                PatchDamageReduction();

                void PatchHatredAgainstEvil() {
                    if (!ModSettings.Fixes.Bloodrager.Archetypes["ReformedFiend"].Enabled["HatredAgainstEvil"]) { return; }
                    var BloodragerClass = Resources.GetBlueprint<BlueprintCharacterClass>("d77e67a814d686842802c9cfd8ef8499");
                    var ReformedFiendBloodrageBuff = Resources.GetBlueprint<BlueprintBuff>("72a679f712bd4f69a07bf03d5800900b");
                    var rankConfig = ReformedFiendBloodrageBuff.GetComponent<ContextRankConfig>();

                    rankConfig.m_BaseValueType = ContextRankBaseValueType.ClassLevel;
                    rankConfig.m_Class = new BlueprintCharacterClassReference[] { BloodragerClass.ToReference<BlueprintCharacterClassReference>() };
                    rankConfig.m_UseMin = true;
                }
                void PatchDamageReduction() {
                    if (!ModSettings.Fixes.Bloodrager.Archetypes["ReformedFiend"].Enabled["DamageReduction"]) { return; }
                    var ReformedFiendDamageReductionFeature = Resources.GetBlueprint<BlueprintFeature>("2a3243ad1ccf43d5a5d69de3f9d0420e");
                    ReformedFiendDamageReductionFeature.GetComponent<AddDamageResistancePhysical>().BypassedByAlignment = true;
                }
            }
        }
    }
}
