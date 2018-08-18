using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

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
            ScenPart_Helper.GenerateOutpostsIntoWorld();
        }
    }

    public static class ScenPart_Helper
    {
        private static readonly FloatRange OutpostsPer100kTiles = new FloatRange(75f, 85f);

        public static void GenerateOutpostsIntoWorld()
        {
            int num = GenMath.RoundRandom((float)Find.WorldGrid.TilesCount / 100000f * OutpostsPer100kTiles.RandomInRange);
            for (int k = 0; k < num; k++)
            {
                Faction faction3 = (from x in Find.World.factionManager.AllFactionsListForReading
                                    where !x.def.isPlayer && !x.def.hidden
                                    select x).RandomElementByWeight((Faction x) => x.def.settlementGenerationWeight);
                int tile = TileFinder.RandomSettlementTileFor(faction3, false, null);
                Site site = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, SitePartDefOf.Outpost, tile, faction3, true, null);
                site.sitePartsKnown = true;
                Find.WorldObjects.Add(site);
            }
        }
    }
}
