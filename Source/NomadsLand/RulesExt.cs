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

        public NomadsLand_RulesExt() { }

        public NomadsLand_RulesExt(Game game) { }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.disallowBuildings, "disallowAllBuilding", false);
            Scribe_Values.Look<bool>(ref this.mapsGenerateIncidents, "mapsGenerateIncidents", false);
            Scribe_Values.Look<bool>(ref this.nothingForbidden, "nothingForbidden", false);
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
                IncidentParms incidentParms = new IncidentParms()
                {
                    target = map,
                    faction = map.ParentFaction,
                    forced = true
                };
                Find.Storyteller.incidentQueue.Add(IncidentDefOf.StrangerInBlackJoin, Find.TickManager.TicksGame + 360, incidentParms);
            }
        }
    }

}
