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
        private TurboModAssets modAssets;

        #endregion

        #region Properties

        internal TurboParts turboParts { get; private set; }
        internal bool assetsLoaded { get; private set; }
        internal static TurboMod instance
        {
            get;
            private set;
        }

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

            // Setting up turbo parts
            this.turboParts = new TurboParts();
            // Oil Lines
            Vector3 oilLinesTriggerPos = new Vector3(0.04f, -0.155f, -0.0845f);
            Quaternion oilLinesTriggerRot = Quaternion.Euler(270, 180, 0);
            Trigger oilLinesTrigger = new Trigger("oilLinesTrigger", block, oilLinesTriggerPos, oilLinesTriggerRot, scale, true) { triggerPosition = oilLinesTriggerPos, triggerRotation = oilLinesTriggerRot };
            this.turboParts.oilLinesPart = new OilLinesPart(null, this.modAssets.oilLines, block, oilLinesTrigger, oilLinesTriggerPos, oilLinesTriggerRot);
            // Oil Cooler
            Vector3 oilCoolerTriggerPos = new Vector3(-0.00385f, -0.1566f, 1.58f);
            Trigger oilCoolerTrigger = new Trigger("oilCoolerTrigger", satsuma, oilCoolerTriggerPos, rotZero, scale, true) { triggerPosition = oilCoolerTriggerPos, triggerRotation = rotZero };
            this.turboParts.oilCoolerPart = new OilCoolerPart(null, this.modAssets.oilLines, satsuma, oilCoolerTrigger, oilCoolerTriggerPos, rotZero);
            // Carb Pipe
            Vector3 carbPipeTriggerPos = new Vector3(0.0605f, -0.063f, 0.038f);
            Quaternion carbPipeTriggerRot = Quaternion.Euler(270, 180, 0);
            Trigger carbPipeTrigger = new Trigger("carbPipeTrigger", stockCarburator, carbPipeTriggerPos, carbPipeTriggerRot, scale, true) { triggerPosition = carbPipeTriggerPos, triggerRotation = carbPipeTriggerRot };
            this.turboParts.carbPipePart = new CarbPipePart(null, this.modAssets.carbPipe, stockCarburator, carbPipeTrigger, carbPipeTriggerPos, carbPipeTriggerRot);
            // Header
            Vector3 headersTriggerPos = new Vector3(-0.0125f, -0.113f, -0.04f);
            Quaternion headersTriggerRot = Quaternion.Euler(270, 180, 0);
            Trigger headersTrigger = new Trigger("headersTrigger", cylinderHead, headersTriggerPos, rotZero, scale, true) { triggerPosition = headersTriggerPos, triggerRotation = headersTriggerRot };
            this.turboParts.headersPart = new HeadersPart(null, this.modAssets.headers, cylinderHead, headersTrigger, headersTriggerPos, headersTriggerRot);
            // Turbo
            Vector3 turboTriggerPos = new Vector3(0, -0.14f, 0);
            Trigger turboTrigger = new Trigger("turboTrigger", this.turboParts.headersPart.activePart, turboTriggerPos, rotZero, scale, true) { triggerPosition = turboTriggerPos, triggerRotation = rotZero };
            this.turboParts.turboPart = new TurboPart(null, this.modAssets.turbo, this.turboParts.headersPart.activePart, turboTrigger, new Vector3(-0.04f, -0.146f, -0.143f), rotZero);
            // Air filter
            Vector3 airFilterTriggerPos = new Vector3(-0.11f, 0, 0);
            Quaternion airFilterTriggerRot = Quaternion.Euler(-90f, 0, 0);
            Trigger airFilterTrigger = new Trigger("airFilterTrigger", this.turboParts.turboPart.activePart, airFilterTriggerPos, airFilterTriggerRot, scale, true) { triggerPosition = airFilterTriggerPos, triggerRotation = airFilterTriggerRot};
            this.turboParts.airFilterPart = new AirFilterPart(null, this.modAssets.airFilter, this.turboParts.turboPart.activePart, airFilterTrigger, airFilterTriggerPos, airFilterTriggerRot);
            // Wastegate
            Vector3 wastgateTriggerPos = new Vector3(-0.054f, 0.023f, 0.0557f);
            Trigger wastegateTrigger = new Trigger("wastegateTrigger", this.turboParts.turboPart.activePart, wastgateTriggerPos, rotZero, scale, true) { triggerPosition = wastgateTriggerPos, triggerRotation = rotZero };
            this.turboParts.wastegateActuatorPart = new WastegateActuatorPart(null, this.modAssets.wastegateActuator, this.turboParts.turboPart.activePart, wastegateTrigger, wastgateTriggerPos, rotZero);
            /*boostGaugePart = new BoostGaugePart(null, this.modAssets.boostGauge, parent, trigger, Vector3.zero, new Quaternion()),
                intercoolerPart = new IntercoolerPart(null, this.modAssets.intercooler, parent, trigger, Vector3.zero, new Quaternion()),
                oilCoolerPart = new OilCoolerPart(null, this.modAssets.oilCooler, parent, trigger, Vector3.zero, new Quaternion()),
                oilLinesPart = new OilLinesPart(null, this.modAssets.oilLines, parent, trigger, Vector3.zero, new Quaternion()),*/
            print("Initialized turbo parts");
        }

        #endregion

        #region Mod Methods

        Keybind up = new Keybind("modapiDump0", "MODAPI DUMP0", KeyCode.Keypad8);
        Keybind down = new Keybind("modapiDump1", "MODAPI DUMP1", KeyCode.Keypad2);
        Keybind left = new Keybind("modapiDump2", "MODAPI DUMP2", KeyCode.Keypad4);
        Keybind right = new Keybind("modapiDump3", "MODAPI DUMP3", KeyCode.Keypad6);
        Keybind tiltLeft = new Keybind("modapiDump4", "MODAPI DUMP4", KeyCode.Keypad7);
        Keybind tiltRight = new Keybind("modapiDump5", "MODAPI DUMP5", KeyCode.Keypad9);
        Keybind printKb = new Keybind("modapiDump6", "MODAPI DUMP6", KeyCode.Keypad0);
        Keybind rotation = new Keybind("modapiDump7", "MODAPI DUMP7", KeyCode.LeftControl);
        Keybind rayCastKb = new Keybind("modapiDump8", "MODAPI DUMP8", KeyCode.R, KeyCode.LeftControl);
        GameObject inspectingPart;
        public override void Update()
        {
            // Written, 27.10.2020

            if (rayCastKb.GetKeybindDown())
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 2f))
                {
                    inspectingPart = hit.collider.gameObject;
                    print("inspecting part: {0}", inspectingPart.name);
                }//move part
            if (inspectingPart != null)
            {
                float increase = 0.01f;
                Vector3 moveVector = Vector3.zero;

                if (up.GetKeybindDown())
                    moveVector = new Vector3(increase, 0, 0);
                else if (down.GetKeybindDown())
                    moveVector = new Vector3(-increase, 0, 0);
                else if (left.GetKeybindDown())
                    moveVector = new Vector3(0, increase, 0);
                else if (right.GetKeybindDown())
                    moveVector = new Vector3(0, -increase, 0);
                else if (tiltLeft.GetKeybindDown())
                    moveVector = new Vector3(0, 0, increase);
                else if (tiltRight.GetKeybindDown())
                    moveVector = new Vector3(0, 0, -increase);
                if (rotation.GetKeybind())
                    inspectingPart.transform.RotateAround(inspectingPart.transform.parent.position, inspectingPart.transform.parent.up, 1 * Time.deltaTime);// = Quaternion.LookRotation(moveVector, Vector3.forward);//.localRotation.SetFromToRotation( = Quaternion.Euler(moveVector + inspectingPart.transform.localRotation.eulerAngles);
                else
                    inspectingPart.transform.localPosition += moveVector;
                if (printKb.GetKeybindDown())
                    print("{0}: pos:{1} rot:{2}", inspectingPart.name, inspectingPart.transform.localPosition, inspectingPart.transform.localEulerAngles);
            }
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
        }

        #endregion
    }
}
