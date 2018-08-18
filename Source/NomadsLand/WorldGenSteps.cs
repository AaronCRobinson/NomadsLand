using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace NomadsLand
{
    public class WorldGenStep_Outposts : WorldGenStep
    {
        public override int SeedPart
        {
            get => 1655430014;
        }

        public override void GenerateFresh(string seed) => WorldGenStep_Helper.GenerateOutpostsIntoWorld();

        public override void GenerateWithoutWorldData(string seed) { }
    }


    public static class WorldGenStep_Helper
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
