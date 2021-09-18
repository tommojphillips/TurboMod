using static TommoJProductions.ModApi.v0_1_3_0_alpha.Attachable.Part;

namespace TommoJProductions.TurboMod
{
    public class TurboModSaveData
    {
        public PartSaveInfo turbo;
        public PartSaveInfo airFilter;
        public PartSaveInfo boostGauge;
        public PartSaveInfo stockCarbPipe;
        public PartSaveInfo downPipe;
        public PartSaveInfo headers;
        public PartSaveInfo highFlowAirFilter;
        public PartSaveInfo oilCooler;
        public PartSaveInfo oilLines;
        public PartSaveInfo wastegate;
        public PartSaveInfo intercooler;
        public PartSaveInfo HotSidePipe;
        public PartSaveInfo racingCarbPipe;

        public bool turboDestroyed;
        public float turboWear;
        public float wastegatePsi;
    }
}
