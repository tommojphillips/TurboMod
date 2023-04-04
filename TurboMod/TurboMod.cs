using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.IO;
using TommoJProductions.ModApi;
using TommoJProductions.ModApi.Attachable;
using TommoJProductions.ModApi.Database;
using UnityEngine;
using static TommoJProductions.ModApi.Attachable.Part;
using Object = UnityEngine.Object;

namespace TommoJProductions.TurboMod
{
    public class TurboMod : Mod
    {
        // Project start, 26.10.2020

        #region Mod Properties

        public override string ID => "TurboMod";
        public override string Name => "Turbo Mod";
        public override string Description => description;
        public override string Version => VersionInfo.version;
        public override string Author => "tommojphillips";

        #endregion

        #region Fields

        public string turboPartsSaveFileName = "turboparts_savedata";
        private readonly string description = $"CONFIG: {(VersionInfo.IS_64_BIT ? "x64" : "x86")} | {(VersionInfo.IS_DEBUG_CONFIG ? "Debug" : "Release")}\nLatest Release Date: {VersionInfo.lastestRelease}";

        /// <summary>
        /// Represents the turbo mod assets.
        /// </summary>
        internal TurboModAssets modAssets;
        /// <summary>
        /// Represents the turbo simulation instance.
        /// </summary>
        internal TurboSimulation turboSimulation;
        /// <summary>
        /// Represents the turbo simulation instance.
        /// </summary>
        internal Simulation simulation;

        #endregion

        #region Properties

        internal GameObject blowThroughCarb;
        internal GameObject bowlVentExtention;
        internal CarbParts carbParts { get; private set; }
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
        private bool loadTurboAssets()
        {
            // Written, 26.10.2020

            try
            {
                AssetBundle ab = LoadAssets.LoadBundle(this, "dazyturbo.unity3d");
                modAssets = new TurboModAssets(ab.LoadAsset("TURBOPARTS.prefab") as GameObject);
                assetsLoaded = true;
                ab.Unload(false);
                print("Turbo Asset bundle loaded and unloaded successfully.");
            }
            catch (Exception ex)
            {
                print("Error when loading asset bundle: M:{0}\nSTACKTRACE:{1}", ex.Message, ex.StackTrace);
                assetsLoaded = false;
            }
            return assetsLoaded;
        }
        /// <summary>
        /// Loads all carb assets
        /// </summary>
        private bool loadCarbAssets()
        {
            try
            {
                AssetBundle ab = LoadAssets.LoadBundle(this, "blowthroughcarb.unity3d");
                blowThroughCarb = ab.LoadAsset("blow through carb") as GameObject;
                bowlVentExtention = ab.LoadAsset("bowl vent extention") as GameObject;
                ab.Unload(false);
                print("BlowThroughCarb Asset bundle loaded and unloaded successfully.");
            }
            catch (Exception ex)
            {
                print("Error when loading asset bundle: M:{0}\nSTACKTRACE:{1}", ex.Message, ex.StackTrace);
                assetsLoaded = false;
            }
            return assetsLoaded;
        }
        /// <summary>
        /// Initializes all carb parts
        /// </summary>
        private void initCarbParts()
        {
            blowThroughCarb = Object.Instantiate(blowThroughCarb);
            GameObject stockCarburator = Database.databaseMotor.carburator;
            GameObject intakeManifold = blowThroughCarb.transform.Find("Intake Manifold").gameObject;
            stockCarburator.GetComponent<MeshFilter>().mesh = intakeManifold.GetComponent<MeshFilter>().mesh;

            BoxCollider sc_bc0 = stockCarburator.GetComponents<BoxCollider>()[0];
            BoxCollider sc_bc1 = stockCarburator.GetComponents<BoxCollider>()[1];
            sc_bc0.center = sc_bc1.center;
            sc_bc0.size = sc_bc1.size;

            GameObject gm_t;
            MeshCollider mc_t;
            BoxCollider bc_t;
            Rigidbody r_t;
            PartSaveInfo dsi_t = new PartSaveInfo() { installed = true };
            PartSettings ps_t = new PartSettings() { setPhysicsMaterialOnInitialisePart = true, installEitherDirection = true };
            carbParts = new CarbParts();

            // stock air filter bracket
            gm_t = intakeManifold.transform.FindChild("Stock Air Filter Bracket").gameObject;
            gm_t.name += "(xxxxx)";
            mc_t = gm_t.AddComponent<MeshCollider>();
            mc_t.convex = true;
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 3.12f;
            carbParts.stockAirFilterBracket = gm_t.AddComponent<Part>();
            carbParts.stockAirFilterBracket.defaultSaveInfo = dsi_t;
            carbParts.stockAirFilterBracket.initPart(null, ps_t, new Trigger("stockAirBracketTrigger", stockCarburator, new Vector3(-0.005243294f, -0.1265122f, 0.04283164f)));
            // Throttle Body Assembly
            gm_t = blowThroughCarb.transform.Find("Throttle Body Assembly").gameObject;
            gm_t.name += "(xxxxx)";
            mc_t = gm_t.AddComponent<MeshCollider>();
            mc_t.convex = true;
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 5.75f;
            carbParts.throttleBodyAssembly = gm_t.AddComponent<Part>();
            carbParts.throttleBodyAssembly.defaultSaveInfo = dsi_t;
            carbParts.throttleBodyAssembly.initPart(null, ps_t, new Trigger("ThrottleBodyAssemblyTrigger", stockCarburator, new Vector3(-0.009168934f, -0.03201637f, 0.01415297f)));
            // Choke Linkage
            gm_t = carbParts.throttleBodyAssembly.transform.Find("Choke Linkage").gameObject;
            gm_t.name += "(xxxxx)";
            mc_t = gm_t.AddComponent<MeshCollider>();
            mc_t.convex = true;
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 3.34f;
            carbParts.chokeLinkage = gm_t.AddComponent<Part>();
            carbParts.chokeLinkage.defaultSaveInfo = dsi_t;
            carbParts.chokeLinkage.initPart(null, ps_t, new Trigger("ChokeLinkageTrigger", carbParts.throttleBodyAssembly.gameObject, new Vector3(-0.0394093f, 0.004382544f, 0.01736239f)));
            // Bowl Assembly
            gm_t = blowThroughCarb.transform.Find("Bowl Assembly").gameObject;
            gm_t.name += "(xxxxx)";
            mc_t = gm_t.AddComponent<MeshCollider>();
            mc_t.convex = true;
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 5.5f;
            carbParts.bowlAssembly = gm_t.AddComponent<Part>();
            carbParts.bowlAssembly.defaultSaveInfo = dsi_t;
            carbParts.bowlAssembly.initPart(null, ps_t, new Trigger("BowlAssemblyTrigger", carbParts.throttleBodyAssembly.gameObject, new Vector3(0.04101953f, -0.0005388409f, 0.00732623f)));
            // Jet Plugs
            Trigger primaryJetPlugTrigger = new Trigger("PrimaryMainJetPlugTrigger", carbParts.bowlAssembly.gameObject, new Vector3(0.01506174f, -0.01314122f, -0.01447441f), new Vector3(90, 30, 0), new Vector3(0.025f, 0.025f, 0.025f));
            Trigger secondaryJetPlugTrigger = new Trigger("SecondaryMainJetPlugTrigger", carbParts.bowlAssembly.gameObject, new Vector3(0.01506174f, 0.01010124f, -0.01447441f), new Vector3(90, 30, 0), new Vector3(0.025f, 0.025f, 0.025f));
            // Plug 1
            gm_t = carbParts.bowlAssembly.transform.Find("Primary Main Jet Plug").gameObject;
            gm_t.name = "Plug 1(xxxxx)";
            mc_t = gm_t.AddComponent<MeshCollider>();
            mc_t.convex = true;
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 3.16f;
            carbParts.plug1 = gm_t.AddComponent<Part>();
            carbParts.plug1.defaultSaveInfo = dsi_t;
            carbParts.plug1.initPart(null, ps_t, primaryJetPlugTrigger, secondaryJetPlugTrigger);
            // Plug 2
            gm_t = carbParts.bowlAssembly.transform.Find("Secondary Main Jet Plug").gameObject;
            gm_t.name = "Plug 2(xxxxx)";
            mc_t = gm_t.AddComponent<MeshCollider>();
            mc_t.convex = true;
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 3.16f;
            carbParts.plug2 = gm_t.AddComponent<Part>();
            carbParts.plug2.defaultSaveInfo = dsi_t;
            carbParts.plug2.defaultSaveInfo.installedPointIndex = 1;
            carbParts.plug2.initPart(null, ps_t, primaryJetPlugTrigger, secondaryJetPlugTrigger);
            // Bowl Cover Assembly
            gm_t = blowThroughCarb.transform.Find("Bowl Cover Assembly").gameObject;
            gm_t.name += "(xxxxx)";
            bc_t = gm_t.AddComponent<BoxCollider>();
            bc_t.size = new Vector3(0.11f, 0.09f, 0.04f);
            bc_t.center = new Vector3(0.003f, 0.00725f, 0.001f);
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 4.15f;
            carbParts.bowlCoverAssembly = gm_t.AddComponent<Part>();
            carbParts.bowlCoverAssembly.defaultSaveInfo = dsi_t;
            carbParts.bowlCoverAssembly.initPart(null, ps_t, new Trigger("BowlCoverAssemblyTrigger", carbParts.bowlAssembly.gameObject, new Vector3(-0.02103544f, -0.005265802f, 0.04428745f)));
            // stock fuel line
            gm_t = carbParts.bowlCoverAssembly.transform.Find("fuel line").gameObject;
            gm_t.name += "(xxxxx)";
            mc_t = gm_t.AddComponent<MeshCollider>();
            mc_t.convex = true;
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 3.75f;
            carbParts.fuelLine = gm_t.AddComponent<Part>();
            carbParts.fuelLine.defaultSaveInfo = dsi_t;
            carbParts.fuelLine.initPart(null, ps_t, new Trigger("fuelLineTrigger", carbParts.bowlCoverAssembly.gameObject, new Vector3(0.0698068f, 0.04171099f, 0.001277313f)));
            // Primary Bowl Vent
            gm_t = carbParts.bowlCoverAssembly.transform.Find("Primary Bowl Vent").gameObject;
            gm_t.transform.SetParent(carbParts.bowlCoverAssembly.transform);
            gm_t.transform.localPosition = new Vector3(0.01668233f, -0.01764342f, 0.003289564f);
            gm_t.transform.localRotation = Quaternion.Euler(90, -40.59985f, 0);
            gm_t.transform.localScale = new Vector3(1.2f, 1.3f, 1.2f);
            // Secondary Bowl Vent
            gm_t = carbParts.bowlCoverAssembly.transform.Find("Secondary Bowl Vent").gameObject;
            gm_t.transform.SetParent(carbParts.bowlCoverAssembly.transform);
            gm_t.transform.localPosition = new Vector3(0.01606204f, 0.02470794f, 0.00257996f);
            gm_t.transform.localRotation = Quaternion.Euler(90, -40.59985f, 0);
            gm_t.transform.localScale = new Vector3(1.2f, 1.35f, 1.2f);
            // Bowl Vent Extentions
            Trigger primaryVentExtentionTrigger = new Trigger("PrimaryVentExtentionTrigger", carbParts.bowlCoverAssembly.gameObject, new Vector3(-0.00047f, 0.02413f, 0.0327f), new Vector3(59, -90, -90), new Vector3(0.025f, 0.025f, 0.025f));
            Trigger secondaryVentExtentionTrigger = new Trigger("SecondaryVentExtentionTrigger", carbParts.bowlCoverAssembly.gameObject, new Vector3(0.00155f, -0.0173f, 0.0327f), new Vector3(89, -90, -90), new Vector3(0.025f, 0.025f, 0.025f));
            // Vent Extention 1
            gm_t = Object.Instantiate(bowlVentExtention);
            gm_t.name = "Vent Extention 1(xxxxx)";
            mc_t = gm_t.AddComponent<MeshCollider>();
            mc_t.convex = true;
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 3.2f;
            carbParts.ventExtention1 = gm_t.AddComponent<Part>();
            carbParts.ventExtention1.defaultSaveInfo = dsi_t;
            carbParts.ventExtention1.initPart(null, ps_t, primaryVentExtentionTrigger, secondaryVentExtentionTrigger);
            // Vent Extention 2
            gm_t = Object.Instantiate(bowlVentExtention);
            gm_t.name = "Vent Extention 2(xxxxx)";
            mc_t = gm_t.AddComponent<MeshCollider>();
            mc_t.convex = true;
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 3.2f;
            carbParts.ventExtention2 = gm_t.AddComponent<Part>();
            carbParts.ventExtention2.defaultSaveInfo = dsi_t;
            carbParts.ventExtention2.defaultSaveInfo.installedPointIndex = 1;
            carbParts.ventExtention2.initPart(null, ps_t, primaryVentExtentionTrigger, secondaryVentExtentionTrigger);
            // Choke Flap
            gm_t = carbParts.bowlCoverAssembly.transform.Find("Choke ").gameObject;
            gm_t.name += "Flap(xxxxx)";
            bc_t = gm_t.AddComponent<BoxCollider>();
            bc_t.size = new Vector3(0.06f, 0.02f, 0.03f);
            bc_t.center = new Vector3(0, 0, 0.01f);
            r_t = gm_t.AddComponent<Rigidbody>();
            r_t.mass = 3.75f;
            carbParts.chokeFlap = gm_t.AddComponent<Part>();
            carbParts.chokeFlap.defaultSaveInfo = dsi_t;
            carbParts.chokeFlap.initPart(null, ps_t, new Trigger("ChokeFlapTrigger", carbParts.bowlCoverAssembly.gameObject, new Vector3(-0.01292261f, 0.01317625f, 0.004566887f)));

            // setting up turbo simulation

            TurboSimulation.carbParts = carbParts;

            ModConsole.Print("init blow through carb parts");
        }
        /// <summary>
        /// Initializes all turbo parts
        /// </summary>
        private void initTurboParts()
        {
            // Written, 27.10.2020

            Vector3 rotZero = Vector3.zero;
            GameObject cylinderHead = Database.databaseMotor.cylinderHead;
            GameObject block = Database.databaseMotor.block;
            GameObject satsuma = GameObject.Find("SATSUMA(557kg, 248)");
            GameObject dashboard = GameObject.Find("dashboard(Clone)");

            // Setting up turbo parts
            turboParts = new TurboParts();
            PartSettings ps = new PartSettings() { setPhysicsMaterialOnInitialisePart = true, installEitherDirection = true };
            // Oil Lines
            Vector3 oilLinesTriggerPos = new Vector3(0.04f, -0.155f, -0.0845f);
            Vector3 oilLinesTriggerRot = new Vector3(270, 180, 0);
            turboParts.oilLines = Object.Instantiate(modAssets.oilLines).AddComponent<Part>();
            turboParts.oilLines.name = "Turbo Oil Lines(xxxxx)";
            turboParts.oilLines.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-9.211566f, 0.3555823f, 8.223039f), rotation = new Vector3(1.59594524f, 328.505646f, 110.664665f) };
            turboParts.oilLines.initPart(loadedSaveData.oilLines, ps, new Trigger("oilLinesTrigger", block, oilLinesTriggerPos, oilLinesTriggerRot));
            // Oil Cooler
            Vector3 oilCoolerTriggerPos = new Vector3(-0.00385f, -0.1566f, 1.58f);
            turboParts.oilCooler = Object.Instantiate(modAssets.oilCooler).AddComponent<Part>();
            turboParts.oilCooler.name = "Oil Cooler(xxxxx)";
            turboParts.oilCooler.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-9.857974f, 0.375454068f, 8.366342f), rotation = new Vector3(51.8120346f, 270.4507f, 3.314321E-05f) };
            turboParts.oilCooler.initPart(loadedSaveData.oilCooler, ps, new Trigger("oilCoolerTrigger", satsuma, oilCoolerTriggerPos, rotZero));
            
            // Stock Carb Pipe
            Vector3 carbPipeTriggerPos = new Vector3(0.051f, -0.023f, -0.033f);
            Vector3 carbPipeTriggerRot = new Vector3(270, 180, 0);

            GameObject butterflyNutPrefab = carbParts.bowlCoverAssembly.transform.Find("butterfly nut").gameObject;            
            butterflyNutPrefab.AddComponent<MeshCollider>().convex = true;
            butterflyNutPrefab.sendToLayer(LayerMasksEnum.Parts);

            Trigger carbPipeTrigger = new Trigger("carbPipeTrigger", carbParts.bowlCoverAssembly, carbPipeTriggerPos, carbPipeTriggerRot);
            
            BoltSettings carbPipeBoltSettings = new BoltSettings()
            {                
                boltSize = BoltSize.hand,
                boltType = BoltType.custom,
                customBoltPrefab = butterflyNutPrefab,
                posDirection = Vector3.down,
                rotDirection = Vector3.up,    
                name = "Butterfly Nut(xxxxx)",
                highlightBoltWhenActive = false,
                activeWhenUninstalled = true,
                parentBoltToTrigger = true,
                parentBoltToTriggerIndex = 0,
            };
            Bolt carbPipeBolt = new Bolt(carbPipeBoltSettings, new Vector3(0.0675f, 0.116f, 0.0254f), new Vector3(90, 0, 0));
            
            turboParts.carbPipe = Object.Instantiate(modAssets.carbPipe).AddComponent<Part>();
            turboParts.carbPipe.name = "Turbo Stock Carb Pipe(xxxxx)";
            turboParts.carbPipe.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-9.787034f, 0.191437215f, 6.201254f), rotation = new Vector3(285.1606f, 51.1690636f, 175.335251f) };
            turboParts.carbPipe.initPart(loadedSaveData.stockCarbPipe, ps, new Bolt[] { carbPipeBolt }, carbPipeTrigger);
            Object.Destroy(butterflyNutPrefab);
            // headers
            Vector3 headersTriggerPos = new Vector3(-0.0115f, -0.1105f, -0.04f);
            Vector3 headersTriggerRot = new Vector3(-90, 0, 180);
            turboParts.headers = Object.Instantiate(modAssets.headers).AddComponent<Part>();
            turboParts.headers.name = "Turbo Headers(xxxxx)";
            turboParts.headers.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-9.909819f, 0.179119885f, 6.45177937f), rotation = new Vector3(324.365265f, 352.000732f, 179.9617f) };
            turboParts.headers.initPart(loadedSaveData.headers, ps, new Trigger("headersTrigger", cylinderHead, headersTriggerPos, headersTriggerRot));
            // turbo
            Vector3 turboTriggerPos = new Vector3(-0.04f, -0.146f, -0.1425f);
            turboParts.turbo = Object.Instantiate(modAssets.turbo).AddComponent<Part>();
            turboParts.turbo.name = "Turbo(xxxxx)";
            turboParts.turbo.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-10.0278788f, 0.2243311f, 6.13207769f), rotation = new Vector3(359.891632f, 344.759369f, 270.93338f) };
            turboParts.turbo.initPart(loadedSaveData.turbo, ps, new Trigger("turboTrigger", turboParts.headers, turboTriggerPos, rotZero));
            // Air filter
            Vector3 airFilterTriggerPos = new Vector3(-0.11f, 0, 0);
            Vector3 airFilterTriggerRot = new Vector3(-90, 0, 0);
            turboParts.filter = Object.Instantiate(modAssets.airFilter).AddComponent<Part>();
            turboParts.filter.name = "Turbo Stock Air Filter(xxxxx)";
            turboParts.filter.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-9.6119f, 0.378927648f, 8.4703455f), rotation = new Vector3(0, 313.72f, 270) };
            turboParts.filter.initPart(loadedSaveData.airFilter, ps, new Trigger("airFilterTrigger", turboParts.turbo, airFilterTriggerPos, airFilterTriggerRot));
            // High Flow Air filter
            turboParts.highFlowFilter = Object.Instantiate(modAssets.highFlowAirFilter).AddComponent<Part>();
            turboParts.highFlowFilter.name = "Turbo High Flow Air Filter(xxxxx)";
            turboParts.highFlowFilter.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-9.758383f, 0.388716f, 8.516996f), rotation = new Vector3(22.13869f, 247.411392f, 266.338776f) };
            turboParts.highFlowFilter.initPart(loadedSaveData.highFlowAirFilter, ps, turboParts.filter.triggers);
            // Wastegate
            Vector3 wastgateTriggerPos = new Vector3(-0.054f, 0.023f, 0.0557f);
            turboParts.act = Object.Instantiate(modAssets.wastegateActuator).AddComponent<Part>();
            turboParts.act.name = "Turbo Wastegate Actuator(xxxxx)";
            turboParts.act.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-9.953638f, 0.164577737f, 6.27556753f), rotation = new Vector3(35.10111f, 256.410431f, 189.827179f) };
            turboParts.act.initPart(loadedSaveData.wastegate, ps, new Trigger("wastegateTrigger", turboParts.turbo, wastgateTriggerPos, rotZero));
            // Down pipe Race
            Vector3 downPipeTriggerPos = new Vector3(0.1434f, -0.0273f, 0.0732f);
            Trigger downPipeTrigger = new Trigger("downPipeTrigger", turboParts.turbo, downPipeTriggerPos, rotZero);
            turboParts.downPipe = Object.Instantiate(modAssets.downPipe_race).AddComponent<Part>();
            turboParts.downPipe.name = "Turbo Down Pipe (Race)(xxxxx)";
            turboParts.downPipe.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-10.1210308f, 0.205112815f, 6.258187f), rotation = new Vector3(304.177856f, 254.448318f, 288.865082f) };
            turboParts.downPipe.initPart(loadedSaveData.downPipeRace, ps, downPipeTrigger);
            // Down pipe Straight
            turboParts.downPipe2 = Object.Instantiate(modAssets.downPipe_race).AddComponent<Part>();
            turboParts.downPipe2.name = "Turbo Down Pipe (Straight)(xxxxx)";
            turboParts.downPipe2.transform.localScale = new Vector3(1, -1, 1);
            turboParts.downPipe2.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(1566.848f, 5.702945f, 738.3788f), rotation = new Vector3(304.1781f, 271.5749f, 288.8701f) };
            turboParts.downPipe2.initPart(loadedSaveData.downPipeStraight, ps, downPipeTrigger);
            // Boost gauge
            turboParts.boostGauge = Object.Instantiate(modAssets.boostGauge).AddComponent<Part>();
            turboParts.boostGauge.name = "Turbo Boost Gauge(xxxxx)";
            turboParts.boostGauge.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-9.002147f, 0.36299932f, 8.077917f), rotation = new Vector3(8.19497363E-05f, 46.88491f, -1.2716353E-05f) };
            Trigger gaugeTriggerDashboard = new Trigger("boostGaugeTriggerDashboard", dashboard, new Vector3(0.5f, -0.05f, 0.125f), new Vector3(265f, 180f, 0f));
            Trigger gaugeTriggerSteeringColumn = new Trigger("boostGaugeTriggerSteeringColumn", satsuma, new Vector3(-0.2580001f, 0.353999f, 0.3900005f), new Vector3(26.0001f, 358.0007f, 0.8386518f));
            Trigger gaugeTriggerShell = new Trigger("boostGaugeTriggerShell", satsuma, new Vector3(-0.5279999f, 0.6269986f, 0.3040009f), new Vector3(0, 342.0003f, 261.8372f));
            turboParts.boostGauge.initPart(loadedSaveData.boostGauge, ps, gaugeTriggerDashboard, gaugeTriggerSteeringColumn, gaugeTriggerShell);
            // Intercooler
            turboParts.intercooler = Object.Instantiate(modAssets.intercooler).AddComponent<Part>();
            turboParts.intercooler.name = "Turbo Intercooler(xxxxx)";
            turboParts.intercooler.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-8.642147f, 0.36299932f, 8.077917f), rotation = new Vector3(8.19497363E-05f, 46.88491f, -1.2716353E-05f) };
            turboParts.intercooler.initPart(loadedSaveData.intercooler, ps, new Trigger("intercoolerTrigger", satsuma, new Vector3(0.5f, -0.05f, 0.125f), new Vector3(265f, 180f, 0f)));
            // Hotside pipe
            turboParts.hotSidePipe = Object.Instantiate(modAssets.chargePipeHotSide_race).AddComponent<Part>();
            turboParts.hotSidePipe.name = "Turbo Hotside Pipe(xxxxx)";
            turboParts.hotSidePipe.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-9.042147f, 0.36299932f, 8.077917f), rotation = new Vector3(8.19497363E-05f, 46.88491f, -1.2716353E-05f) };
            turboParts.hotSidePipe.initPart(loadedSaveData.HotSidePipe, ps, new Trigger("hotsidepipeTrigger", turboParts.intercooler, new Vector3(0.5f, -0.05f, 0.125f), new Vector3(265f, 180f, 0f)));
            // racing Carb pipe
            Trigger coldSidePipeTrigger = new Trigger("racingCarbpipeTrigger", turboParts.intercooler.gameObject, new Vector3(0.5f, -0.05f, 0.125f), new Vector3(265f, 180f, 0f));
            turboParts.coldSidePipe = Object.Instantiate(modAssets.chargePipeColdSide_race).AddComponent<Part>();
            turboParts.coldSidePipe.name = "Turbo Racing Carb Pipe (Coldside)(xxxxx)";
            turboParts.coldSidePipe.defaultSaveInfo = new PartSaveInfo() { position = new Vector3(-10.142147f, 0.36299932f, 8.077917f), rotation = new Vector3(8.19497363E-05f, 46.88491f, -1.2716353E-05f) };
            turboParts.coldSidePipe.initPart(loadedSaveData.racingCarbPipe, ps, coldSidePipeTrigger);

            // Setting up turbo simulation

/*          
            TurboSimulation.oilCooler = turboParts.oilCooler;
            TurboSimulation.oilLines = turboParts.oilLines;
            TurboSimulation.headers = turboParts.headers;
            TurboSimulation.carbPipe = turboParts.carbPipe;
            TurboSimulation.coldSidePipe = turboParts.coldSidePipe;
            TurboSimulation.downPipeRace = turboParts.downPipe;
            TurboSimulation.downPipeStraight = turboParts.downPipe2;
            TurboSimulation.hotSidePipe = turboParts.hotSidePipe;
            TurboSimulation.act = turboParts.act;
            TurboSimulation.boostGauge = turboParts.boostGauge;
            TurboSimulation.turbo = turboParts.turbo;
            TurboSimulation.filter = turboParts.filter;
            TurboSimulation.highFlowFilter = turboParts.highFlowFilter;
            TurboSimulation.intercooler = turboParts.intercooler;
            turboSimulation = turboParts.turbo.gameObject.AddComponent<TurboSimulation>();
            turboSimulation.loadedSaveData = loadedSaveData.simulation;
*/
          
            simulation = turboParts.turbo.gameObject.AddComponent<Simulation>();
            simulation.setupParts(turboParts, carbParts, loadedSaveData.simulation);


            Database.databaseVehicles.satsuma.gameObject.AddComponent<VehiclePushStartLogic>();

            print("Initialized turbo parts");

        }
        /// <summary>
        /// Saves turbo parts to a file
        /// </summary>
        private void saveParts()
        {
            // Written, 04.11.2020

            loadedSaveData = new TurboModSaveData
            {
                oilLines = turboParts.oilLines.getSaveInfo(),
                oilCooler = turboParts.oilCooler.getSaveInfo(),
                stockCarbPipe = turboParts.carbPipe.getSaveInfo(),
                headers = turboParts.headers.getSaveInfo(),
                turbo = turboParts.turbo.getSaveInfo(),
                airFilter = turboParts.filter.getSaveInfo(),
                highFlowAirFilter = turboParts.highFlowFilter.getSaveInfo(),
                wastegate = turboParts.act.getSaveInfo(),
                downPipeRace = turboParts.downPipe.getSaveInfo(),
                downPipeStraight = turboParts.downPipe2.getSaveInfo(),
                boostGauge = turboParts.boostGauge.getSaveInfo(),
                intercooler = turboParts.intercooler.getSaveInfo(),
                HotSidePipe = turboParts.hotSidePipe.getSaveInfo(),
                racingCarbPipe = turboParts.coldSidePipe.getSaveInfo(),
                simulation = simulation.loadedSaveData
            };
            SaveLoad.SerializeSaveFile(this, loadedSaveData, turboPartsSaveFileName);
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
        /// <summary>
        /// Opens repairshop dyno indefinitely
        /// </summary>
        private void initDyno()
        {
            // Written, 21.11.2021

            GameObject g = GameObject.Find("REPAIRSHOP/LOD/Dyno/dyno_computer/Computer");
            if (g)
            {
                PlayMakerFSM dyno = g.GetPlayMaker("Use");
                FsmFloat time = dyno.FsmVariables.GetFsmFloat("TimeBought");

                time.Value = float.PositiveInfinity;
                dyno.SendEvent("OPEN");
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
            File.Delete(Path.Combine(ModLoader.GetModSettingsFolder(this), turboPartsSaveFileName));            
        }
        public override void OnLoad()
        {
            if (loadTurboAssets() && loadCarbAssets())
            {
                loadParts();
                initCarbParts();
                initTurboParts();
                initDyno();
            }
            ConsoleCommand.Add(new testCaculations(turboSimulation));

            print("{0} v{1}: Loaded", Name, Version);            
        }

        #endregion

        /*// Debugging/Development tools
        bool reloaded = false;
        float reloadTextTime;
        public override void Update()
        {
            if (reloaded)
            {
                if (Time.time - reloadTextTime > 120)
                {
                    reloaded = false;
                    reloadTextTime = 0;
                }
            }
            else
            {
                if (cInput.GetButton("Use") && cInput.GetButtonDown("Finger"))
                {
                    ModClient.guiInteract("Deleting previous assets..");
                    Object.DestroyImmediate(blowThroughCarb);
                    ModClient.guiInteract("Reloading asset bundle..");
                    loadCarbAssets();
                    ModClient.guiInteract("initializing carb parts..");
                    initCarbParts();
                    ModClient.guiInteract("Carb parts reloaded");
                    reloaded = true;
                    reloadTextTime = Time.time;
                }
            }
        }
        */
    }
}
