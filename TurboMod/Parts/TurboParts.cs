using UnityEngine;

namespace TommoJProductions.TurboMod.Parts
{
    internal class TurboParts
    {
        // Written, 27.10.2020
        internal TurboPart turboPart { get; set; }
        internal HeadersPart headersPart { get; set; }
        internal AirFilterPart airFilterPart { get; set; }
        internal HighFlowAirFilterPart highFlowAirFilterPart { get; set; }
        internal BoostGaugePart boostGaugePart { get; set; }
        internal CarbPipePart carbPipePart { get; set; }
        internal IntercoolerPart intercoolerPart { get; set; }
        internal OilCoolerPart oilCoolerPart { get; set; }
        internal OilLinesPart oilLinesPart { get; set; }
        internal WastegateActuatorPart wastegateActuatorPart { get; set; }
        internal DownPipePart downPipePart { get; set; }
        internal bool isCorePartsInstalled()
        {
            // Written, 28.10.2020

            return (this.turboPart.installed
                && this.headersPart.installed);
        }
    }
}
