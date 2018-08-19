using Verse;
using RimWorld;

namespace NomadsLand
{
    public class NomadsLand_RulesExt : GameComponent
    {
        private bool disallowBuildings = false;
        public bool DisallowBuildings { get; set; }

        private bool mapsGenerateIncidents = false;
        public bool MapsGenerateIncidents { get; set; }

        private bool nothingForbidden = false;
        public bool NothingForbidden { get; set; }

        private bool caravanStart = false;
        public bool CaravanStart { get; set; }

        public NomadsLand_RulesExt() { }

        public NomadsLand_RulesExt(Game game) { }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.disallowBuildings, "disallowAllBuilding", false);
            Scribe_Values.Look<bool>(ref this.mapsGenerateIncidents, "mapsGenerateIncidents", false);
            Scribe_Values.Look<bool>(ref this.nothingForbidden, "nothingForbidden", false);
            Scribe_Values.Look<bool>(ref this.caravanStart, "caravanStart", false);
        }
    }

    public class MapIncidentGenerator : MapComponent
    {
        public MapIncidentGenerator(Map map) : base(map) { }

        public override void MapGenerated()
        {
            base.MapGenerated();
            if (Current.Game.GetComponent<NomadsLand_RulesExt>().MapsGenerateIncidents)
            {
                if (Rand.Chance(0.0625f))
                    Find.Storyteller.incidentQueue.Add(IncidentDefOf.StrangerInBlackJoin, Find.TickManager.TicksGame + Rand.Range(100,1000), this.IncidentParms);
                if (Rand.Chance(0.14f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.RaidEnemy, Find.TickManager.TicksGame + Rand.Range(100,1000), this.IncidentParms);
                if (Rand.Chance(0.091f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.VisitorGroup, Find.TickManager.TicksGame + Rand.Range(100,1000), this.IncidentParms);
                if (Rand.Chance(0.32f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.ManhunterPack, Find.TickManager.TicksGame + Rand.Range(100,1000), this.IncidentParms);
                if (Rand.Chance(0.21f))
                    Find.Storyteller.incidentQueue.Add(RimWorld.IncidentDefOf.ShipChunkDrop, Find.TickManager.TicksGame + Rand.Range(100,1000), this.IncidentParms);
            }
        }

        public IncidentParms IncidentParms
        {
            get => new IncidentParms()
            {
                target = map,
                faction = map.ParentFaction,
                forced = true
            };
        }

        
    }

}
