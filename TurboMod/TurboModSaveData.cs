using TommoJProductions.ModApi.Attachable;

namespace TommoJProductions.TurboMod
{
    public class TurboModSaveData
    {
        public PartSaveInfo turbo;
        public PartSaveInfo airFilter;
        public PartSaveInfo boostGauge;
        public PartSaveInfo stockCarbPipe;
        public PartSaveInfo downPipeRace;
        public PartSaveInfo downPipeStraight;
        public PartSaveInfo headers;
        public PartSaveInfo highFlowAirFilter;
        public PartSaveInfo oilCooler;
        public PartSaveInfo oilLines;
        public PartSaveInfo wastegate;
        public PartSaveInfo intercooler;
        public PartSaveInfo HotSidePipe;
        public PartSaveInfo racingCarbPipe;

        public SimulationSaveData simulation = new SimulationSaveData();
    }
}
