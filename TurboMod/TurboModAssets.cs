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

        internal Material turboGlowMat;

        internal TurboModAssets(GameObject inPrefab)
        {
            // Written, 27.10.2020

            this.turboPartsPrefab = inPrefab;
            this.oilLines = inPrefab.transform.GetChild(1).gameObject;
            TurboMod.print("{0}: found", this.oilLines.name);
            this.wastegateActuator = inPrefab.transform.GetChild(2).gameObject;
            TurboMod.print("{0}: found", this.wastegateActuator.name);
            this.oilCooler = inPrefab.transform.GetChild(3).gameObject;
            TurboMod.print("{0}: found", this.oilCooler.name);
            this.airFilter = inPrefab.transform.GetChild(4).gameObject;
            TurboMod.print("{0}: found", this.airFilter.name);
            this.highFlowAirFilter = inPrefab.transform.GetChild(5).gameObject;
            TurboMod.print("{0}: found", this.highFlowAirFilter.name);
            this.headers = inPrefab.transform.GetChild(6).gameObject;
            TurboMod.print("{0}: found", this.headers.name);
            this.chargePipeHotSide_race = inPrefab.transform.GetChild(7).gameObject;
            TurboMod.print("{0}: found", this.chargePipeHotSide_race.name);
            this.intercooler = inPrefab.transform.GetChild(8).gameObject;
            TurboMod.print("{0}: found", this.intercooler.name);
            this.chargePipeColdSide_race = inPrefab.transform.GetChild(9).gameObject;
            TurboMod.print("{0}: found", this.chargePipeColdSide_race.name);
            this.turbo = inPrefab.transform.GetChild(10).gameObject;
            TurboMod.print("{0}: found", this.turbo.name);
            this.boostGauge = inPrefab.transform.GetChild(11).gameObject;
            TurboMod.print("{0}: found", this.boostGauge.name);
            this.carbPipe = inPrefab.transform.GetChild(12).gameObject;
            TurboMod.print("{0}: found", this.carbPipe.name);
            this.downPipe_race = inPrefab.transform.GetChild(13).gameObject;
            TurboMod.print("{0}: found", this.downPipe_race.name);
        }
    }
}
