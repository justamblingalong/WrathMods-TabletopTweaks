﻿using Kingmaker.EntitySystem.Stats;
using TabletopTweaks.Config;
using TabletopTweaks.Extensions;
using TabletopTweaks.Utilities;

namespace TabletopTweaks.NewContent.Feats {
    static class StreetSmarts {
        public static void AddStreetSmarts() {
            var StreetSmarts = FeatTools.CreateSkillFeat(StatType.SkillKnowledgeWorld, StatType.SkillPerception, bp => {
                bp.AssetGuid = ModSettings.Blueprints.GetGUID("StreetSmarts");
                bp.name = "StreetSmarts";
                bp.SetName("Street Smarts");
                bp.SetDescription("You are able to navigate the streets and personalities of whatever locale you run across." +
                    "\nYou get a +2 bonus on {g|Encyclopedia:Knowledge_World}Knowledge (World){/g} and " +
                    "{g|Encyclopedia:Perception}Perception{/g} skill checks. If you have 10 or more ranks in one of these skills," +
                    " the bonus increases to +4 for that skill.");
            });
            Resources.AddBlueprint(StreetSmarts);
            if (ModSettings.AddedContent.Feats.DisableAll || !ModSettings.AddedContent.Feats.Enabled["StreetSmarts"]) { return; }
            FeatTools.AddToFeatList(StreetSmarts);
        }
    }
}
