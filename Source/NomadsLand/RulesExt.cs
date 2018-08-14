using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace NomadsLand
{
    public class NomadsLand_RulesExt : GameComponent
    {
        private bool disallowBuildings = false;
        public bool DisallowBuildings { get; set; }

        public NomadsLand_RulesExt() { }

        public NomadsLand_RulesExt(Game game) { }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.disallowBuildings, "disallowAllBuilding", false);
        }
    }
}
