using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace NomadsLand
{
    public class Rule_DisallowAllBuilding : ScenPart_Rule
    {
        protected override void ApplyRule() => Current.Game.GetComponent<NomadsLand_RulesExt>().DisallowBuildings = true;
    }

    public class Rule_MapsGenerateIncidents : ScenPart_Rule
    {
        protected override void ApplyRule() => Current.Game.GetComponent<NomadsLand_RulesExt>().MapsGenerateIncidents = true;
    }

    public class ScenPart_GenStepOutposts : ScenPart
    {
        public override void PostWorldGenerate()
        {
            Log.Message("ScenPart_GenStepOutposts");
            base.PostWorldGenerate();
            WorldGenStep_Helper.GenerateOutpostsIntoWorld();
        }
    }
}
