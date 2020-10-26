using UnityEngine;

namespace TommoJProductions.TurboMod
{
    internal class TurboModAssets
    {
        // Written, 26.10.2020

        internal GameObject turboPartsPrefab;
        internal GameObject turbo;
        internal GameObject headers;
        internal GameObject intercooler;
        internal GameObject carbPipe;
        internal GameObject oilLines;
        internal GameObject airFilter;
        internal GameObject highFlowAirFilter;
        internal GameObject boostGauge;
        internal GameObject wastegateActuator;
        internal GameObject oilCooler;

        internal GameObject chargePipeHotSide_race;
        internal GameObject chargePipeColdSide_race;
        internal GameObject downPipe_race;
        internal TurboModAssets(GameObject inTurboPartsPrefab) 
        {
            // Written, 26.10.2020

            this.turboPartsPrefab = inTurboPartsPrefab;
            this.turbo = inTurboPartsPrefab.transform.FindChild("KK90 turbocharger (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.turbo.name);
            this.headers = inTurboPartsPrefab.transform.Find("Headers (Turbo) (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.headers.name);
            this.carbPipe = inTurboPartsPrefab.transform.Find("Carburetor Pipe (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.carbPipe.name);
            this.chargePipeHotSide_race = inTurboPartsPrefab.transform.Find("Charge Pipe Hot-Side (Race) (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.chargePipeHotSide_race.name);
            this.intercooler = inTurboPartsPrefab.transform.Find("Intercooler (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.intercooler.name);
            this.chargePipeColdSide_race = inTurboPartsPrefab.transform.Find("Charge Pipe Cold-Side (Race) (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.chargePipeColdSide_race.name);
            this.oilLines = inTurboPartsPrefab.transform.Find("KK90 Turbo Oil Lines (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.oilLines.name);
            this.airFilter = inTurboPartsPrefab.transform.Find("Air Filter (Turbo) (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.airFilter.name);
            this.highFlowAirFilter = inTurboPartsPrefab.transform.Find("High Flow Air Filter (Turbo) (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.highFlowAirFilter.name);
            this.boostGauge = inTurboPartsPrefab.transform.Find("Boost / Vacuum Gauge (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.boostGauge.name);
            this.downPipe_race = inTurboPartsPrefab.transform.Find("Turbo Downpipe (Race) (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.boostGauge.name);
            this.wastegateActuator = inTurboPartsPrefab.transform.Find("KK90 Wastegate Actuator (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.wastegateActuator.name);
            this.oilCooler = inTurboPartsPrefab.transform.Find("Oil Cooler (bruhx)").gameObject;
            TurboMod.print("{0}: found", this.oilCooler.name);
        }
    }
}
