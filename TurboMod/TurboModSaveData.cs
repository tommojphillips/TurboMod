using ModApi.Attachable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TommoJProductions.TurboMod.Parts;

namespace TommoJProductions.TurboMod
{
    public class TurboModSaveData
    {
        public PartSaveInfo turbo;
        public PartSaveInfo airFilter;
        public PartSaveInfo boostGauge;
        public PartSaveInfo carbPipe;
        public PartSaveInfo downPipe;
        public PartSaveInfo headers;
        public PartSaveInfo highFlowAirFilter;
        public PartSaveInfo oilCooler;
        public PartSaveInfo oilLines;
        public PartSaveInfo wastegate;

        public float wastegatePsi;
        public bool isDownPipeDown;

        public TurboModSaveData() { }
        internal TurboModSaveData(TurboParts turboParts) 
        {
            // Written, 04.11.2020

            this.turbo = turboParts.turboPart.getSaveInfo();
            this.airFilter = turboParts.airFilterPart.getSaveInfo();
            this.boostGauge = turboParts.boostGaugePart.getSaveInfo();
            this.carbPipe = turboParts.carbPipePart.getSaveInfo();
            this.downPipe = turboParts.turboPart.getSaveInfo();
            if (this.downPipe.installed)
                this.isDownPipeDown = turboParts.downPipePart.isInDownPosition;
            this.headers = turboParts.headersPart.getSaveInfo();
            this.highFlowAirFilter = turboParts.highFlowAirFilterPart.getSaveInfo();
            this.oilCooler = turboParts.oilCoolerPart.getSaveInfo();
            this.oilLines = turboParts.oilLinesPart.getSaveInfo();
            this.wastegate = turboParts.wastegateActuatorPart.getSaveInfo();
            this.wastegatePsi = turboParts.wastegateActuatorPart.wastegateAdjust.turboSimulation?.wastegatePsi ?? 0;
        }
    }
}
