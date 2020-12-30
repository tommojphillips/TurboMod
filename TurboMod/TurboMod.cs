using ModApi;
using ModApi.Attachable;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
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

        #endregion

        #region Mod Properties

        public override string ID => "TurboMod";
        public override string Name => "Turbo Mod";
        public override string Version => "0.1";
        public override string Author => "tommjphillips";
        public override bool UseAssetsFolder => true;
        public override bool SecondPass => true;

        #endregion

        #region Fields

        /// <summary>
        /// Represents the turbo mod assets.
        /// </summary>
        internal TurboModAssets modAssets;

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

        #region Constructors

        public TurboMod()
        {
            // Written, 26.10.2020
            
            instance = this;

            print("ready");
        }

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
                AssetBundle ab = LoadAssets.LoadBundle(this, "dazyturbo.unity3d");
                this.modAssets = new TurboModAssets(ab.LoadAsset("TURBOPARTS.prefab") as GameObject);
                this.modAssets.turboGlowMat = Resources.Load<Material>("Mods/Assets/TurboMod/StandardSpecular.mat");
                print("turbo Glowing loaded: {0}", this.modAssets.turboGlowMat ?? null);
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
            Quaternion rotZero = Quaternion.Euler(Vector3.zero);
            GameObject cylinderHead = GameObject.Find("cylinder head(Clone)");
            GameObject stockCarburator = GameObject.Find("carburator(Clone)");
            GameObject block = GameObject.Find("block(Clone)");
            GameObject satsuma = GameObject.Find("SATSUMA(557kg, 248)");
            GameObject dashboard = GameObject.Find("dashboard(Clone)");

            // Setting up turbo parts
            this.turboParts = new TurboParts();
            // Oil Lines
            Vector3 oilLinesTriggerPos = new Vector3(0.04f, -0.155f, -0.0845f);
            Quaternion oilLinesTriggerRot = Quaternion.Euler(270, 180, 0);
            Trigger oilLinesTrigger = new Trigger("oilLinesTrigger", block, oilLinesTriggerPos, oilLinesTriggerRot, scale, true) { triggerPosition = oilLinesTriggerPos, triggerRotation = oilLinesTriggerRot };
            this.turboParts.oilLinesPart = new OilLinesPart(loadedSaveData.oilLines, this.modAssets.oilLines, block, oilLinesTrigger, oilLinesTriggerPos, oilLinesTriggerRot);
            // Oil Cooler
            Vector3 oilCoolerTriggerPos = new Vector3(-0.00385f, -0.1566f, 1.58f);
            Trigger oilCoolerTrigger = new Trigger("oilCoolerTrigger", satsuma, oilCoolerTriggerPos, rotZero, scale, true) { triggerPosition = oilCoolerTriggerPos, triggerRotation = rotZero };
            this.turboParts.oilCoolerPart = new OilCoolerPart(loadedSaveData.oilCooler, this.modAssets.oilCooler, satsuma, oilCoolerTrigger, oilCoolerTriggerPos, rotZero);
            // Carb Pipe
            Vector3 carbPipeTriggerPos = new Vector3(0.0605f, -0.063f, 0.038f);
            Quaternion carbPipeTriggerRot = Quaternion.Euler(270, 180, 0);
            Trigger carbPipeTrigger = new Trigger("carbPipeTrigger", stockCarburator, carbPipeTriggerPos, carbPipeTriggerRot, scale, true) { triggerPosition = carbPipeTriggerPos, triggerRotation = carbPipeTriggerRot };
            this.turboParts.carbPipePart = new CarbPipePart(loadedSaveData.carbPipe, this.modAssets.carbPipe, stockCarburator, carbPipeTrigger, carbPipeTriggerPos, carbPipeTriggerRot);
            // Header
            Vector3 headersTriggerPos = new Vector3(-0.0125f, -0.113f, -0.04f);
            Quaternion headersTriggerRot = Quaternion.Euler(270, 180, 0);
            Trigger headersTrigger = new Trigger("headersTrigger", cylinderHead, headersTriggerPos, rotZero, scale, true) { triggerPosition = headersTriggerPos, triggerRotation = headersTriggerRot };
            this.turboParts.headersPart = new HeadersPart(loadedSaveData.headers, this.modAssets.headers, cylinderHead, headersTrigger, headersTriggerPos, headersTriggerRot);
            // Turbo
            Vector3 turboTriggerPos = new Vector3(0, -0.14f, 0);
            Trigger turboTrigger = new Trigger("turboTrigger", this.turboParts.headersPart.activePart, turboTriggerPos, rotZero, scale, true) { triggerPosition = turboTriggerPos, triggerRotation = rotZero };
            this.turboParts.turboPart = new TurboPart(loadedSaveData.turbo, this.modAssets.turbo, this.turboParts.headersPart.activePart, turboTrigger, new Vector3(-0.04f, -0.146f, -0.143f), rotZero);
            // Air filter
            Vector3 airFilterTriggerPos = new Vector3(-0.11f, 0, 0);
            Quaternion airFilterTriggerRot = Quaternion.Euler(-90f, 0, 0);
            Trigger airFilterTrigger = new Trigger("airFilterTrigger", this.turboParts.turboPart.activePart, airFilterTriggerPos, airFilterTriggerRot, scale, true) { triggerPosition = airFilterTriggerPos, triggerRotation = airFilterTriggerRot };
            this.turboParts.airFilterPart = new AirFilterPart(loadedSaveData.airFilter, this.modAssets.airFilter, this.turboParts.turboPart.activePart, airFilterTrigger, airFilterTriggerPos, airFilterTriggerRot);
            // High Flow Air filter
            Quaternion highFlowAirFilterRot = Quaternion.Euler(-90, 0, 0);
            this.turboParts.highFlowAirFilterPart = new HighFlowAirFilterPart(loadedSaveData.highFlowAirFilter, this.modAssets.highFlowAirFilter, this.turboParts.turboPart.activePart, airFilterTrigger, airFilterTriggerPos, highFlowAirFilterRot);
            // Wastegate
            Vector3 wastgateTriggerPos = new Vector3(-0.054f, 0.023f, 0.0557f);
            Trigger wastegateTrigger = new Trigger("wastegateTrigger", this.turboParts.turboPart.activePart, wastgateTriggerPos, rotZero, scale, true) { triggerPosition = wastgateTriggerPos, triggerRotation = rotZero };
            this.turboParts.wastegateActuatorPart = new WastegateActuatorPart(loadedSaveData.wastegate, this.modAssets.wastegateActuator, this.turboParts.turboPart.activePart, wastegateTrigger, wastgateTriggerPos, rotZero);
            Vector3 downPipeTriggerPos = new Vector3(0.1434f, -0.0273f, 0.0732f);
            Trigger downPipeTrigger = new Trigger("downPipeTrigger", this.turboParts.turboPart.activePart, downPipeTriggerPos, rotZero, scale, true) { triggerPosition = downPipeTriggerPos, triggerRotation = rotZero };
            this.turboParts.downPipePart = new DownPipePart(loadedSaveData.downPipe, this.modAssets.downPipe_race, this.turboParts.turboPart.activePart, downPipeTrigger, downPipeTriggerPos, rotZero);
            Vector3 gaugeTriggerPos = new Vector3(0.5f, -0.05f, 0.125f);
            Quaternion gaugeTriggerRot = Quaternion.Euler(265f, 180f, 0f);
            Trigger gaugeTrigger = new Trigger("boostGaugeTrigger", dashboard, gaugeTriggerPos, gaugeTriggerRot, scale, true) { triggerPosition = gaugeTriggerPos, triggerRotation = gaugeTriggerRot };
            this.turboParts.boostGaugePart = new BoostGaugePart(loadedSaveData.boostGauge, this.modAssets.boostGauge, dashboard, gaugeTrigger, gaugeTriggerPos, gaugeTriggerRot);
            /*boostGaugePart = new BoostGaugePart(null, this.modAssets.boostGauge, parent, trigger, Vector3.zero, new Quaternion()),
                intercoolerPart = new IntercoolerPart(null, this.modAssets.intercooler, parent, trigger, Vector3.zero, new Quaternion()),*/
            print("Initialized turbo parts");
        }
        private void saveParts() 
        {
            // Written, 04.11.2020

            loadedSaveData = new TurboModSaveData(this.turboParts);
            SaveLoad.SerializeSaveFile(this, loadedSaveData, "turbomodsavedata.txt");
        }
        private void loadParts() 
        {
            try
            {
                loadedSaveData = SaveLoad.DeserializeSaveFile<TurboModSaveData>(this, "turbomodsavedata.txt");
                print("loaded save data");
            }
            catch
            {
                print("Error when loading save data... saving new data");
            }
            finally
            {
                if (loadedSaveData == null)
                {
                    print("Loaded save data was null. creating new instance.");
                    loadedSaveData = new TurboModSaveData();
                }
            }
        }
        private GameObject inspectingGameobject;
        private float increaseBy = 0.0001f;
        private Keybind raycastK = new Keybind("raycast", "raycast", KeyCode.R, KeyCode.LeftControl);
        private Keybind rotation = new Keybind("rot", "rot", KeyCode.LeftShift);
        private Keybind left = new Keybind("l", "l", KeyCode.Keypad4);
        private Keybind right = new Keybind("r", "r", KeyCode.Keypad6);
        private Keybind up = new Keybind("u", "u", KeyCode.Keypad8);
        private Keybind down = new Keybind("d", "d", KeyCode.Keypad2);
        private Keybind tiltleft = new Keybind("tl", "tl", KeyCode.Keypad7);
        private Keybind tiltright = new Keybind("tr", "tr", KeyCode.Keypad9);
        private void moveObject()
        {
            // Written, 04.11.2020

            if (this.raycastK.GetKeybindDown())
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 2f))
                {
                    inspectingGameobject = hit.collider.gameObject;
                    print("inspecting part: {0}", inspectingGameobject.name);
                }

            if (this.inspectingGameobject != null)
            {
                Vector3 move = new Vector3();
                if (this.left.GetKeybind())
                    move.x += increaseBy;
                if (this.right.GetKeybind())
                    move.x += -increaseBy;

                if (this.up.GetKeybind())
                    move.y += increaseBy;
                if (this.down.GetKeybind())
                    move.y += -increaseBy;

                if (this.tiltleft.GetKeybind())
                    move.z += increaseBy;
                if (this.tiltright.GetKeybind())
                    move.z += -increaseBy;

                if (this.rotation.GetKeybind())
                    this.inspectingGameobject.transform.localRotation = Quaternion.Euler(this.inspectingGameobject.transform.localRotation.eulerAngles + move);
                else
                    this.inspectingGameobject.transform.localPosition += move;
            }
        }
        private void moveObjectGui() 
        {
            // Written, 04.11.2020

            using (new GUILayout.AreaScope(new Rect(500, 20, 500, 500), "box") { })
            {
                if (inspectingGameobject != null)
                {
                    GUILayout.Label(inspectingGameobject.name);
                    GUILayout.Label(String.Format("local pos: X{0} | Y{1} | Z{2}", inspectingGameobject.transform.localPosition.x, inspectingGameobject.transform.localPosition.y, inspectingGameobject.transform.localPosition.z));
                    GUILayout.Label(String.Format("local euler rot: X{0} | Y{1} | Z{2} | W{3}", inspectingGameobject.transform.localRotation.x, inspectingGameobject.transform.localRotation.y, inspectingGameobject.transform.localRotation.z, inspectingGameobject.transform.localRotation.w));
                }
                else
                    GUILayout.Label("inspecting object null");
            }
        }

        #endregion

        #region Mod Methods

        public override void OnSave()
        {
            this.saveParts();
        }
        public override void Update()
        {
            this.moveObject();
        }
        public override void OnGUI()
        {
            this.moveObjectGui();
        }
        public override void SecondPassOnLoad()
        {
            // Written, 26.10.2020

            if (this.loadAssets())
                this.initParts();
            print("{0} v{1}: Loaded", this.Name, this.Version);
        }
        public override void OnLoad()
        {
            this.loadParts();
        }

        #endregion
    }
}
