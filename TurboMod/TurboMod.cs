using MSCLoader;
using System;
using TommoJProductions.ModApi.v0_1_3_0_alpha.Attachable;
using TommoJProductions.TurboMod.Parts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TommoJProductions.TurboMod
{
    public class TurboMod : Mod
    {
        // Project start, 26.10.2020

        #region CONSTRAINTS


        /// <summary>
        /// Represents if the mod has been complied for x64
        /// </summary>
#if x64
        internal const bool IS_64_BIT = true;
#else
        internal const bool IS_64_BIT = false;
#endif

#if DEBUG
        internal const bool IS_DEBUG_CONFIG = true;
#else
        internal const bool IS_DEBUG_CONFIG = false;
#endif

        public string turboPartsSaveFileName = "turboparts_savedata";

        #endregion

        #region Mod Properties

        public override string ID => "TurboMod";
        public override string Name => "Turbo Mod";
        public override string Version => "0.1.1";
        public override string Author => "tommjphillips";

        #endregion

        #region Fields

        /// <summary>
        /// Represents the turbo mod assets.
        /// </summary>
        internal TurboModAssets modAssets;
        /// <summary>
        /// Represents the turbo simulation instance.
        /// </summary>
        internal TurboSimulation turboSimulation;

        #endregion

        #region Properties

        internal TurboParts turboParts { get; private set; }
        internal bool assetsLoaded { get; private set; }
        internal static TurboMod instance
        {
            get;
            private set;
        }
        internal static TurboModSaveData loadedSaveData { get; private set; }

        #endregion

        #region Methods

        // INTERNAL METHODS
        /// <summary>
        /// Prints a message to the console and amends it to the log.
        /// </summary>
        /// <param name="inMessage">The message to log</param>
        internal static void print(string inMessage, params object[] _objects)
        {
            // Written, 26.10.2020

            ModConsole.Print(string.Format("Turbo Mod: " + inMessage, _objects));
        }

        // PRIVATE METHODS
        /// <summary>
        /// Loads all turbo assets.
        /// </summary>
        private bool loadAssets()
        {
            // Written, 26.10.2020

            try
            {
                AssetBundle ab = ModAssets.LoadBundle(this, "dazyturbo.unity3d");
                modAssets = new TurboModAssets(ab.LoadAsset("TURBOPARTS.prefab") as GameObject);
                assetsLoaded = true;
                ab.Unload(false);
                print("Asset bundle loaded and unloaded successfully.");
            }
            catch (Exception ex)
            {
                print("Error when loading asset bundle: M:{0}\nSTACKTRACE:{1}", ex.Message, ex.StackTrace);
                assetsLoaded = false;
            }
            return assetsLoaded;
        }
        /// <summary>
        /// Initializes all turbo parts
        /// </summary>
        private void initParts()
        {
            // Written, 27.10.2020 
            Vector3 scale = new Vector3(0.05f, 0.05f, 0.05f);
            Vector3 rotZero = Vector3.zero;
            GameObject cylinderHead = GameObject.Find("cylinder head(Clone)");
            GameObject stockCarburator = GameObject.Find("carburator(Clone)");
            GameObject block = GameObject.Find("block(Clone)");
            GameObject satsuma = GameObject.Find("SATSUMA(557kg, 248)");
            GameObject dashboard = GameObject.Find("dashboard(Clone)");

            // Setting up turbo parts
            TurboParts turboParts = new TurboParts();

            // Oil Lines
            Vector3 oilLinesTriggerPos = new Vector3(0.04f, -0.155f, -0.0845f);
            Vector3 oilLinesTriggerRot = new Vector3(270, 180, 0);
            turboParts.oilLines = Object.Instantiate(modAssets.oilLines).AddComponent<Part>();
            turboParts.oilLines.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-9.211566f, 0.3555823f, 8.223039f), rotation = new Vector3(1.59594524f, 328.505646f, 110.664665f) };
            turboParts.oilLines.initPart(loadedSaveData.oilLines, new Trigger("oilLinesTrigger", block, oilLinesTriggerPos, oilLinesTriggerRot, scale, false));
            // Oil Cooler
            Vector3 oilCoolerTriggerPos = new Vector3(-0.00385f, -0.1566f, 1.58f);
            turboParts.oilCooler = Object.Instantiate(modAssets.oilCooler).AddComponent<Part>();
            turboParts.oilCooler.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-9.857974f, 0.375454068f, 8.366342f), rotation = new Vector3(51.8120346f, 270.4507f, 3.314321E-05f) };
            turboParts.oilCooler.initPart(loadedSaveData.oilCooler, new Trigger("oilCoolerTrigger", satsuma, oilCoolerTriggerPos, rotZero, scale, false));
            // Stock Carb Pipe
            Vector3 carbPipeTriggerPos = new Vector3(0.0605f, -0.063f, 0.038f);
            Vector3 carbPipeTriggerRot = new Vector3(-90, 0, 180);
            turboParts.carbPipe = Object.Instantiate(modAssets.carbPipe).AddComponent<Part>();
            turboParts.carbPipe.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-9.787034f, 0.191437215f, 6.201254f), rotation = new Vector3(285.1606f, 51.1690636f, 175.335251f) };
            turboParts.carbPipe.initPart(loadedSaveData.stockCarbPipe, new Trigger("carbPipeTrigger", stockCarburator, carbPipeTriggerPos, carbPipeTriggerRot, scale, false));
            // headers
            Vector3 headersTriggerPos = new Vector3(-0.0115f, -0.1105f, -0.04f);
            Vector3 headersTriggerRot = new Vector3(-90, 0, 180);
            turboParts.headers = Object.Instantiate(modAssets.headers).AddComponent<Part>();
            turboParts.headers.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-9.909819f, 0.179119885f, 6.45177937f), rotation = new Vector3(324.365265f, 352.000732f, 179.9617f) };
            turboParts.headers.initPart(loadedSaveData.headers, new Trigger("headersTrigger", cylinderHead, headersTriggerPos, headersTriggerRot, scale, false));
            // turbo
            Vector3 turboTriggerPos = new Vector3(-0.04f, -0.146f, -0.1425f);
            turboParts.turbo = Object.Instantiate(modAssets.turbo).AddComponent<Part>();
            turboParts.turbo.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-10.0278788f, 0.2243311f, 6.13207769f), rotation = new Vector3(359.891632f, 344.759369f, 270.93338f) };
            turboParts.turbo.initPart(loadedSaveData.turbo, new Trigger("turboTrigger", turboParts.headers.gameObject, turboTriggerPos, rotZero, scale, false));
            // Air filter
            Vector3 airFilterTriggerPos = new Vector3(-0.11f, 0, 0);
            Vector3 airFilterTriggerRot = new Vector3(-90, 0, 0);
            turboParts.filter = Object.Instantiate(modAssets.airFilter).AddComponent<Part>();
            turboParts.filter.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-9.6119f, 0.378927648f, 8.4703455f), rotation = new Vector3(0, 313.72f, 270) };
            turboParts.filter.initPart(loadedSaveData.airFilter, new Trigger("airFilterTrigger", turboParts.turbo.gameObject, airFilterTriggerPos, airFilterTriggerRot, scale, false));
            // High Flow Air filter
            turboParts.highFlowFilter = Object.Instantiate(modAssets.highFlowAirFilter).AddComponent<Part>();
            turboParts.highFlowFilter.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-9.758383f, 0.388716f, 8.516996f), rotation = new Vector3(22.13869f, 247.411392f, 266.338776f) };
            turboParts.highFlowFilter.initPart(loadedSaveData.highFlowAirFilter, turboParts.filter.triggers);
            // Wastegate
            Vector3 wastgateTriggerPos = new Vector3(-0.054f, 0.023f, 0.0557f);
            turboParts.act = Object.Instantiate(modAssets.wastegateActuator).AddComponent<Part>();
            turboParts.act.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-9.953638f, 0.164577737f, 6.27556753f), rotation = new Vector3(35.10111f, 256.410431f, 189.827179f) };
            turboParts.act.initPart(loadedSaveData.wastegate, new Trigger("wastegateTrigger", turboParts.turbo.gameObject, wastgateTriggerPos, rotZero, scale, false));
            // Down pipe
            Vector3 downPipeTriggerPos = new Vector3(0.1434f, -0.0273f, 0.0732f);
            turboParts.downPipe = Object.Instantiate(modAssets.downPipe_race).AddComponent<Part>();
            turboParts.downPipe.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-10.1210308f, 0.205112815f, 6.258187f), rotation = new Vector3(304.177856f, 254.448318f, 288.865082f) };
            turboParts.downPipe.initPart(loadedSaveData.downPipe, new Trigger("downPipeTrigger", turboParts.turbo.gameObject, downPipeTriggerPos, rotZero, scale, false));
            // Boost gauge
            turboParts.boostGauge = Object.Instantiate(modAssets.boostGauge).AddComponent<Part>();
            turboParts.boostGauge.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-9.002147f, 0.36299932f, 8.077917f), rotation = new Vector3(8.19497363E-05f, 46.88491f, -1.2716353E-05f) };
            Trigger gaugeTriggerDashboard = new Trigger("boostGaugeTriggerDashboard", dashboard, new Vector3(0.5f, -0.05f, 0.125f), new Vector3(265f, 180f, 0f), scale, false);
            Trigger gaugeTriggerSteeringColumn = new Trigger("boostGaugeTriggerSteeringColumn", satsuma, new Vector3(-0.2580001f, 0.353999f, 0.3900005f), new Vector3(26.0001f, 358.0007f, 0.8386518f), scale, false);
            Trigger gaugeTriggerShell = new Trigger("boostGaugeTriggerShell", satsuma, new Vector3(-0.5279999f, 0.6269986f, 0.3040009f), new Vector3(0, 342.0003f, 261.8372f), scale, false);
            turboParts.boostGauge.initPart(loadedSaveData.boostGauge, gaugeTriggerDashboard, gaugeTriggerSteeringColumn, gaugeTriggerShell);            
            // Intercooler
            turboParts.intercooler = Object.Instantiate(modAssets.intercooler).AddComponent<Part>();
            turboParts.intercooler.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-8.642147f, 0.36299932f, 8.077917f), rotation = new Vector3(8.19497363E-05f, 46.88491f, -1.2716353E-05f) };
            turboParts.intercooler.initPart(loadedSaveData.intercooler, new Trigger("intercoolerTrigger", satsuma, new Vector3(0.5f, -0.05f, 0.125f), new Vector3(265f, 180f, 0f), scale, false));
            // Hotside pipe
            turboParts.hotSidePipe = Object.Instantiate(modAssets.chargePipeHotSide_race).AddComponent<Part>();
            turboParts.hotSidePipe.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-9.042147f, 0.36299932f, 8.077917f), rotation = new Vector3(8.19497363E-05f, 46.88491f, -1.2716353E-05f) };
            turboParts.hotSidePipe.initPart(loadedSaveData.HotSidePipe, new Trigger("hotsidepipeTrigger", turboParts.intercooler.gameObject, new Vector3(0.5f, -0.05f, 0.125f), new Vector3(265f, 180f, 0f), scale, false));
            // racing Carb pipe
            turboParts.coldSidePipe = Object.Instantiate(modAssets.chargePipeHotSide_race).AddComponent<Part>();
            turboParts.coldSidePipe.defaultSaveInfo = new Part.PartSaveInfo() { position = new Vector3(-10.142147f, 0.36299932f, 8.077917f), rotation = new Vector3(8.19497363E-05f, 46.88491f, -1.2716353E-05f) };
            turboParts.coldSidePipe.initPart(loadedSaveData.racingCarbPipe, new Trigger("racingCarbpipeTrigger", turboParts.intercooler.gameObject, new Vector3(0.5f, -0.05f, 0.125f), new Vector3(265f, 180f, 0f), scale, false));

            // Setting up turbo simulation
            TurboSimulation.oilCooler = turboParts.oilCooler;
            TurboSimulation.oilLines = turboParts.oilLines;
            TurboSimulation.headers = turboParts.headers;
            TurboSimulation.carbPipe = turboParts.carbPipe;
            TurboSimulation.coldSidePipe = turboParts.coldSidePipe;
            TurboSimulation.downPipe = turboParts.downPipe;
            TurboSimulation.hotSidePipe = turboParts.hotSidePipe;
            TurboSimulation.act = turboParts.act;
            TurboSimulation.boostGauge = turboParts.boostGauge;
            TurboSimulation.turbo = turboParts.turbo;
            TurboSimulation.filter = turboParts.filter;
            TurboSimulation.highFlowFilter = turboParts.highFlowFilter;
            TurboSimulation.intercooler = turboParts.intercooler;
            turboSimulation = turboParts.turbo.gameObject.AddComponent<TurboSimulation>();
            turboSimulation.loadedSaveData = new TurboSimulation.turboSimulationSaveData() { wastegatePsi = loadedSaveData.wastegatePsi, turboWear = loadedSaveData.turboWear, turboDestroyed = loadedSaveData.turboDestroyed };

            // done
            print("Initialized turbo parts");
        }
        /// <summary>
        /// Saves turbo parts to a file
        /// </summary>
        private void saveParts(TurboModSaveData saveData = null) 
        {
            // Written, 04.11.2020

            if (saveData == null)
            {
                loadedSaveData = new TurboModSaveData();
                loadedSaveData.oilLines = TurboSimulation.oilLines.getSaveInfo();
                loadedSaveData.oilCooler = TurboSimulation.oilCooler.getSaveInfo();
                loadedSaveData.stockCarbPipe = TurboSimulation.carbPipe.getSaveInfo();
                loadedSaveData.headers = TurboSimulation.headers.getSaveInfo();
                loadedSaveData.turbo = TurboSimulation.turbo.getSaveInfo();
                loadedSaveData.airFilter = TurboSimulation.filter.getSaveInfo();
                loadedSaveData.highFlowAirFilter = TurboSimulation.highFlowFilter.getSaveInfo();
                loadedSaveData.wastegate = TurboSimulation.act.getSaveInfo();
                loadedSaveData.downPipe = TurboSimulation.downPipe.getSaveInfo();
                loadedSaveData.boostGauge = TurboSimulation.boostGauge.getSaveInfo();
                loadedSaveData.intercooler = TurboSimulation.intercooler.getSaveInfo();
                loadedSaveData.HotSidePipe= TurboSimulation.hotSidePipe.getSaveInfo();
                loadedSaveData.racingCarbPipe = TurboSimulation.coldSidePipe.getSaveInfo();
                loadedSaveData.wastegatePsi = turboSimulation.wastegateRPM / TurboSimulation.RPM2PSI;
                loadedSaveData.turboDestroyed = turboSimulation.turboDestroyed;
                loadedSaveData.turboWear = turboSimulation.wearMult;
            }
            SaveLoad.SerializeSaveFile(this, saveData ?? loadedSaveData, turboPartsSaveFileName);
        }
        /// <summary>
        /// Loads turbo parts from a file
        /// </summary>
        private void loadParts() 
        {
            try
            {
                loadedSaveData = SaveLoad.DeserializeSaveFile<TurboModSaveData>(this, turboPartsSaveFileName);
                print("loaded save data");
            }
            catch
            {
                print("Error when loading save data... saving new data");
            }
            finally 
            {
                if (loadedSaveData == null)
                    loadedSaveData = new TurboModSaveData();
            }
        }

        #endregion

        #region Mod Methods

        public override void OnSave()
        {
            saveParts();
        }
        public override void OnNewGame()
        {
            ModSave.Delete(turboPartsSaveFileName);
        }
        public override void PostLoad()
        {
            // Written, 26.10.2020

        }
        public override void OnLoad()
        {
            if (loadAssets())
            {
                loadParts();
                initParts();
            }
            print("{0} v{1}: Loaded", Name, Version);
        }

        #endregion
    }
}
