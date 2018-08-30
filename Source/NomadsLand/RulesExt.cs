using Verse;
using RimWorld;
using RimWorld.Planet;

namespace NomadsLand
{
    public class NomadsLand_RulesExt : GameComponent
    {
        public bool disallowBuildings = false;
        public bool mapsGenerateIncidents = false;
        public bool nothingForbidden = false;
        public bool caravanStart = false;
        public bool extendForceExitTimer = false;

        public NomadsLand_RulesExt() { }
        public NomadsLand_RulesExt(Game game) { }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.disallowBuildings, "disallowAllBuilding", false);
            Scribe_Values.Look<bool>(ref this.mapsGenerateIncidents, "mapsGenerateIncidents", false);
            Scribe_Values.Look<bool>(ref this.nothingForbidden, "nothingForbidden", false);
            Scribe_Values.Look<bool>(ref this.caravanStart, "caravanStart", false);
            Scribe_Values.Look<bool>(ref this.extendForceExitTimer, "extendForceExitTimer", false);
        }

    }

    public class NomadsLand_WorldComponent : WorldComponent
    {
        public NomadsLand_WorldComponent(World world) : base(world) { }

        // NOTE: required to avoid latter errors (TODO: revisit)
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (Current.Game.GetComponent<NomadsLand_RulesExt>().caravanStart)
                Current.Game.tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;
        }
    }

    // TODO: too much Rand...
    public class MapIncidentGenerator : MapComponent
    {
        public MapIncidentGenerator(Map map) : base(map) { }

        public override void MapGenerated()
        {
            base.MapGenerated();
            if (Current.Game.GetComponent<NomadsLand_RulesExt>().mapsGenerateIncidents)
            {
                if (Rand.Chance(0.0625f))
                    Find.Storyteller.incidentQueue.Add(IncidentDefOf.StrangerInBlackJoin, Find.TickManager.TicksGame + Rand.Range(100,10000), this.IncidentParms);
                if (Rand.Chance(0.14f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.RaidEnemy, Find.TickManager.TicksGame + Rand.Range(100,10000), this.IncidentParms);
                if (Rand.Chance(0.091f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.VisitorGroup, Find.TickManager.TicksGame + Rand.Range(100,10000), this.IncidentParms);
                if (Rand.Chance(0.32f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.ManhunterPack, Find.TickManager.TicksGame + Rand.Range(100,10000), this.IncidentParms);
                if (Rand.Chance(0.21f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.ShipChunkDrop, Find.TickManager.TicksGame + Rand.Range(100,10000), new MapIncidentParms(map) { points = Rand.Range(10, 100) });
                if (Rand.Chance(0.017f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.Quest_ItemStashAICore, Find.TickManager.TicksGame, this.IncidentParms);
                if (Rand.Chance(0.007f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.Quest_JourneyOffer, Find.TickManager.TicksGame, this.IncidentParms);
            }
        }

        public IncidentParms IncidentParms
        {
            get => new MapIncidentParms(map);
        }

        public class MapIncidentParms : IncidentParms
        {
            public MapIncidentParms(Map map) : base()
            {
                target = map;
                faction = map.ParentFaction;
                forced = true;
                points = StorytellerUtility.DefaultSiteThreatPointsNow();
            }
        }

    }

}
