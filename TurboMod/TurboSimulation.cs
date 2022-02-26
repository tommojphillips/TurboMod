using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MSCLoader;
using System;
using System.Collections;
using System.Linq;
using TommoJProductions.ModApi;
using TommoJProductions.ModApi.Attachable;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TommoJProductions.TurboMod
{
    public class TurboSimulation : MonoBehaviour
    {
        #region Class Structs

        public struct turboSimulationSaveData
        {
            internal bool turboDestroyed;
            internal float turboWear;
            internal float wastegatePsi;
        }

        #endregion

        #region Constraints

        /// <summary>
        /// Represents hp to kw conversion. eg => HP / HP2KW = KW | HP2KW * KW = HP
        /// </summary>
        public const float HP2KW = 1.341f;
        /// <summary>
        /// Represents ft/lb to nm conversion. eg => nm * LB2NM = lb | lb / LB2NM = nm
        /// </summary>
        public const float LB2NM = 0.73756f;
        /// <summary>
        /// represents turbo rpm to psi constant. eg. rpm => {PSI} * {RPM2PSI} | psi => {rpm} / {RPM2PSI} 
        /// </summary>
        public const int RPM2PSI = 11300;
        /// <summary>
        /// Represents the min wastegate rpm.
        /// </summary>
        public const int MIN_WASTEGATE_RPM = 52175;
        /// <summary>
        /// Represents the max wastegate rpm.
        /// </summary>
        public const int MAX_WASTEGATE_RPM = 203400;
        /// <summary>
        /// Represents the wastegate adjust interval.
        /// </summary>
        public const int WASTEGATE_ADJUST_INTERVAL = 2825;
        /// <summary>
        /// Represents 1 pascal.
        /// </summary>
        public const float PASCAL = 6894.76f;
        /// <summary>
        /// Represents 1 kelvin.
        /// </summary>
        public const float KELVIN = 274.15f;
        /// <summary>
        /// Represents the dry gas constant.
        /// </summary>
        public const float DRY_GAS_CONSTANT = 287.058f;
        /// <summary>
        /// Represents the wet gas constant.
        /// </summary>
        public const float WET_GAS_CONSTANT = 461.495f;

        /// <summary>
        /// Represents the stock stock inertia (stock flywheel) for satsuma.
        /// </summary>
        public const float STOCK_INERTIA = 0.04f;
        /// <summary>
        /// Represents the racing inertia (racing flywheel) for satsuma.
        /// </summary>
        public const float RACING_INERTIA = 0.028f;
        /// <summary>
        /// Represents the stock max torque rpm for satsuma.
        /// </summary>
        public const int STOCK_MAX_TORQUE_RPM = 4000;
        /// <summary>
        /// Represents the stock max power rpm for satsuma.
        /// </summary>
        public const int STOCK_MAX_POWER_RPM = 6000;

        /// <summary>
        /// Represents the height reference level in meters.
        /// </summary>
        public const float HEIGHT_REFERENCE_LEVEL = 0; // meters
        /// <summary>
        /// Represents the pressure at <see cref="HEIGHT_REFERENCE_LEVEL"/> in psi.
        /// </summary>
        public const float PRESSURE_REFERENCE_LEVEL = 14.7f; // psi
        /// <summary>
        /// Represents gravity force in meters squared.
        /// </summary>
        public const float GRAVITY = 9.80665f; // m/s²
        /// <summary>
        /// Represents the molar mass or air on earth in kg/mol.
        /// </summary>
        public const float MOLAR_MASS_OF_AIR = 0.0289644f; // kg/mol
        /// <summary>
        /// Represents the universal gas constant
        /// </summary>
        public const float UNIVERSAL_GAS_CONSTANT = 8.31432f; // N·m/(mol·K)

        #endregion

        #region Fields

        // Static fields
        public static Part turbo;
        public static Part headers;
        public static Part downPipe;
        public static Part filter;
        public static Part highFlowFilter;
        public static Part carbPipe;
        public static Part coldSidePipe;
        public static Part hotSidePipe;
        public static Part intercooler;
        public static Part oilCooler;
        public static Part oilLines;
        public static Part act;
        public static Part boostGauge;
        public static ButterflyNut butterflyNut;


        // Private fields
        private AudioSource turboWhistle;
        private AudioSource turboFlutter;
        private AudioSource exhaust_fromMuffler_audioSource;
        private AudioSource exhaust_fromPipe_audioSource;
        private Drivetrain drivetrain = null;
        private bool engineCranked = false;
        private bool raceExhaustCheckAssemblyInjected = false;
        private bool raceMufflerCheckAssemblyInjected = false;
        private bool GUIdebug = true;
        private bool applyCalculatedDensity = false;
        private bool applyCalculatedCombustion = false;
        private bool applyCheckMixture = true;
        private bool applyCalculatedPowerBand = true;
        private bool applyCalculatedPowerMultipler = true;
        private bool systemDepressionOverrideActive = false;
        //private float turboFriction;
        private float turboSpool;
        private float wastegateSpool;
        private float maxBoostRpm;
        private float timeBoosting;
        private float turboSpin;
        private float vaccummThrottle;
        private float vaccummSmooth;
        private float throttleSmooth;
        private float initialRpm;
        private float boostGaugeNeedle;
        private float turboTargetRPM;
        private float turboRPM;
        private float afFactor;
        private float afrAbsolute;
        private float mixtureMedian;
        private float minPowerRpm;
        private float systemDepression;
        private float systemDepressionRpm;
        private float systemDepressionOverrideValue;
        private float pressureRatio;
        private float airFlowRate;
        private float outletPressure;
        private float inletPressure;
        private float airFlowPressureRatio;
        private float calculatedPowerMultiplier;
        private float calculatedSurgeRate;
        private float turboEngineHeatFactor = 0.012f;
        private float boostMultiplier = 0.00013f;
        //private float frictionMultipler = 115;
        private float oilCoolerCoolingRate = 0.054572f;
        private float oilCoolerThermostatRating = 85;
        private float wastegateCutModifier = 0.83125f;
        private float carbPipeBlowOffForceFactor = 3.2f;
        private float carbPipeBlowOffChanceFactor = 0.0125f;
        private float rpmPowerBandFactor = 1;
        private float minTorqueRpm = 2345;
        private float powerToTorqueRPMDifference = 1625;
        [Range(0, 1)]
        private float relativeHumidity = 0.20534f;
        private float airFlowRateModifer = 2.78f;
        private FloatCompare checkMixtureLean;
        private FloatCompare checkMixtureRich;
        private FloatCompare checkMixtureSuperRich;
        private FloatCompare checkMixtureSputter;
        private FloatCompare checkMixtureOff;
        private FloatOperator checkMixtureAirFuelMixture;
        private FloatOperator calMixtureFloatOp;
        private FloatOperator calDensityFloatOp;
        private FloatClamp calDensityFloatClamp;

        private FsmBool stockHeadersInstalled;
        private FsmBool raceCarbInstalled;
        private FsmBool raceexhaustinstalled;
        private FsmBool raceHeadersInstalled;
        private FsmBool racemufflerinstalled;
        private FsmBool stockCarbInstalled;
        private FsmBool stockFilterInstalled;
        private FsmBool playerInMenu;
        private FsmFloat stockInertia;
        private FsmFloat racingInertia;
        private FsmFloat carbBowl;
        private FsmFloat headGasketWear;
        private FsmFloat oilLevel;
        private FsmFloat oilDirt;
        private FsmFloat mixture;
        private FsmFloat fuelPumpEfficiency;
        private FsmFloat engineTemp;
        private FsmFloat throttlePedal;
        private FsmFloat airDensity;
        private FsmFloat ambientTemperature;
        private FsmFloat playerStress;
        private FsmFloat satsumaSpeedKmh;
        private FsmString InteractText;
        private GameObject turboFan;
        private GameObject stockFilterTrigger;
        private GameObject headerTriggers;
        private GameObject soundSpool;
        private GameObject soundFlutter;
        private GameObject turboNeedleObject;
        private GameObject turboMesh;
        private GameObject turboMeshDestoryed;
        private GameObject engine;
        private GameObject fuelGo;
        private GameObject pingingSound;
        private GameObject exhaust_fromEngine;
        private GameObject exhaust_fromMuffler;
        private GameObject exhaust_fromPipe;
        private GameObject exhaust_fromHeaders;
        private GameObject exhaust;
        private GameObject carSimulation;

        private PlayMakerFSM swear;
        private PlayMakerFSM motorBlow;
        private PlayMakerFSM headgasket;
        private PlayMakerFSM backfire;
        private PlayMakerFSM fuel;
        private PlayMakerFSM fuelEvent;
        private PlayMakerFSM oil;
        private PlayMakerFSM mechanicalWear;

        private Vector3 boostNeedleVector;
        // Internal fields
        internal float wastegateRPM;
        internal float maxBoostFuel;
        internal float wastegatePSI;
        internal float PSI;
        internal float wearMultipler;
        internal bool destroyed;
        internal bool wear = false;
        internal turboSimulationSaveData loadedSaveData;
        internal float atmosphericPressure;

        // GUI
        private int fontSize = Screen.width / 135;
        private int width = 240;
        private int height = Screen.height;
        private int top = 100;
        private int left = 50;
        private string propertyString = String.Empty;
        private GUILayout.HorizontalScope horizontalScope = new GUILayout.HorizontalScope("box");
        private GUILayout.VerticalScope verticalScope = new GUILayout.VerticalScope("box");
        private GUIStyle guiStyle;

        // Coroutines
        private Coroutine oilCoolerRoutine;
        private Coroutine turboRoutine;
        private Coroutine pipeRoutine;

        #endregion

        #region Properties

        // BOOL
        internal bool isPiped => carbInstall || raceCarbInstall;
        internal bool isFiltered => filter.installed || highFlowFilter.installed;
        internal bool isExhaust => headers.installed && turbo.installed && downPipe.installed;
        internal bool carbInstall => stockCarbInstalled.Value && carbPipe.installed;
        internal bool raceCarbInstall => raceCarbInstalled.Value && coldSidePipe.installed && intercooler.installed && hotSidePipe.installed;
        internal bool engineOn => engine.activeInHierarchy;
        internal bool canTurboWork => headers.installed && turbo.installed && !destroyed && (engineOn || turboSpoolingDown);
        internal bool canOilCoolerWork => oilCooler.installed && engineOn;
        internal bool canPipeWork => canTurboWork && isPiped;
        internal bool onThrottlePedal => !(throttlePedal.Value < 0.8f);
        internal bool turboSurging => PSI.round() > 0 && !onThrottlePedal;
        internal bool spoolSoundEnabled { set { soundSpool.SetActive(value); } }
        internal bool turboSpoolingDown => !engineOn && PSI.round(1) > 0 && !destroyed;
        // FLOAT
        internal float turboHeight => transform.position.y;
        internal float turboFrictionMultipler => 120 + wearMultipler * 1.2f;
        internal float turboFriction => turboFrictionMultipler * turboRPM / 1500 * 0.5f;
        internal float throttlePedalPosition => throttlePedal.Value / 8;

        internal turboSimulationSaveData defaultSaveData => new turboSimulationSaveData() { turboDestroyed = false, turboWear = Random.Range(75, 100) + (Random.Range(0, 100) * 0.001f), wastegatePsi = 8.25f };

        #endregion

        #region Unity runtime methods.

        private void OnEnable()
        {
            // filter boost multipier check
            filter.onAssemble += updateBoostMultiplier;
            filter.onDisassemble += updateBoostMultiplier;
            highFlowFilter.onAssemble += updateBoostMultiplier;
            highFlowFilter.onDisassemble += updateBoostMultiplier;

            // carb install check
            carbPipe.onAssemble += onCarbSetup;
            carbPipe.onDisassemble += onCarbSetup;
            // race carb install check
            coldSidePipe.onAssemble += onCarbSetup;
            coldSidePipe.onDisassemble += onCarbSetup;
            hotSidePipe.onAssemble += onCarbSetup;
            hotSidePipe.onDisassemble += onCarbSetup;
            intercooler.onDisassemble += onCarbSetup;
            intercooler.onAssemble += onCarbSetup;

            // Boost gauge needle
            boostGauge.onAssemble += boostGaugeNeedleReset;
            boostGauge.onDisassemble += boostGaugeNeedleReset;

            // oil cooler pipes
            oilCooler.onAssemble += oilCoolerOnAssemble;
            oilCooler.onDisassemble += oilCoolerOnDisassemble;

            // turbo fan rpm reset
            headers.onDisassemble += turboRpmReset;
            turbo.onDisassemble += turboRpmReset;

            // turbo fan active check
            filter.onAssemble += turboFanCheck;
            filter.onDisassemble += turboFanCheck;
            highFlowFilter.onAssemble += turboFanCheck;
            highFlowFilter.onDisassemble += turboFanCheck;
            downPipe.onAssemble += turboFanCheck;
            downPipe.onDisassemble += turboFanCheck;
            headers.onAssemble += turboFanCheck;
            headers.onDisassemble += turboFanCheck;
            turbo.onAssemble += turboFanCheck;
            turbo.onDisassemble += turboFanCheck;

            // carb pipe -><- stock air filter trigger toggle
            carbPipe.onAssemble += stockAirFilterTriggerToggle;
            carbPipe.onDisassemble += stockAirFilterTriggerToggle;

            // headers -><- turbo headers trigger toggle
            headers.onAssemble += headersTriggerToggle;
            headers.onDisassemble += headersTriggerToggle;
        }
        private void Start()
        {
            initSimulation();
            initEngineState();
            checkAnyConflictingPart();
        }
        private void Update() 
        {
            guiToggle();        
            cursorFunction();
            turboCondCheck(); // TODO: MAKE MONOBEHAVIOUR
            wasteGateAdjust(); // TODO: MAKE MONOBEHAVIOUR
        }
        private void LateUpdate()
        {
            partCheck(); // TODO: MAKE MONOBEHAVIOUR
        }

        private void OnGUI()
        {
            try
            {
                if (GUIdebug)
                {
                    guiStyle = new GUIStyle();
                    guiStyle.alignment = TextAnchor.MiddleLeft;
                    guiStyle.padding = new RectOffset(0, 0, 0, 0);
                    guiStyle.normal.textColor = Color.white;
                    guiStyle.hover.textColor = Color.blue;
                    guiStyle.fontSize = fontSize;

                    // values
                    using (new GUILayout.AreaScope(new Rect(left, top, width, height - top), "", guiStyle))
                    {
                        using (verticalScope)
                        {
                            string surgingText = "<color=" + (turboSurging ? "red>SURGE" : "green>OK") + "</color>";
                            string spoolStateText = "<color=" + (turboSpoolingDown ? "red>SPOOLING DOWN" : "green>FLOWING") + "</color>";
                            drawProperty("Turbo Stats\n" +
                                $"- {PSI.round(2)}psi\n" +
                                $"- {turboRPM.round()}rpm\n" +
                                $"- {timeBoosting.round(2)}seconds\n" +
                                $"- Starv: {maxBoostFuel.round(3)}psi\n" +
                                $"- TGT: {turboTargetRPM.round(2)}\n" +
                                $"- Spool: {turboSpool.round(2)}\n" +
                                $"- {surgingText}\n" +
                                $"- {spoolStateText}");
                            drawProperty("Wastegate Stats\n" +
                                $"- {Math.Round(wastegateRPM)}rpm\n" +
                                $"- {wastegatePSI.round(2)}psi\n" +
                                $"- Spool: {wastegateSpool.round(2)}");
                            drawProperty("Air-Flow Stats\n" +
                                $"- atmospheric pressure: {atmosphericPressure.round(2)}psi\n" +
                                $"- system depression: {systemDepression.round(2)}psi\n" +
                                $"- pressure ratio: {pressureRatio.round(2)}\n" +
                                $"- air flow press ratio: {airFlowPressureRatio.round(2)}\n" +
                                $"- air flow rate: {airFlowRate.round(2)} cc\"s\n" +
                                $"- surge rate: {calculatedSurgeRate.round(2)} cc\"s\n" +
                                $"- height: {turboHeight.round(2)}m\n" +
                                $"- inlet: {inletPressure.round(2)}psi\n" +
                                $"- outlet: {outletPressure.round(2)}psi");
                            drawProperty("Air-Fuel\n" +
                                $"- Air Density: {airDensity.Value.round(3)}\n" +
                                $"- Factor: {afFactor.round(3)}\n" +
                                $"- Ratio: {afrAbsolute.round(2)}\n" +
                                $"- Median: {mixtureMedian.round(2)}\n" +
                                $"- Ideal: {Math.Round(checkMixtureRich?.float2.Value ?? -1, 2)} ~ {Math.Round(checkMixtureLean?.float2.Value ?? -1, 2)}");
                            drawProperty("Engine Stats\n" +
                                $"- {drivetrain.rpm.round(2)}rpm\n" +
                                $"- {satsumaSpeedKmh.Value.round(2)}Km/h\n" +
                                $"- {(drivetrain.torque / LB2NM).round(2)}Nm\n" +
                                $"- {(drivetrain.currentPower / HP2KW).round(2)}Kw\n" +
                                $"- {engineTemp.Value.round(2)}°C\n" +
                                $"- Inertia: {drivetrain.engineInertia.round(3)}");
                            drawProperty("Oil Stats\n" +
                                $"- Level: {oilLevel.round(3)}\n" +
                                $"- Dirt: {oilDirt.round(3)}");
                            drawProperty("Carb Stats\n" +
                                $"- Bowl Level: {carbBowl.round(3)}");

                            drawProperty($"Power Multi: {Math.Round(drivetrain.powerMultiplier, 3)} / {Math.Round(calculatedPowerMultiplier, 3)}");

                            drawProperty("Max Torque Rpm", Mathf.RoundToInt(drivetrain.maxTorqueRPM));
                            drawProperty("Max Power Rpm", Mathf.RoundToInt(drivetrain.maxPowerRPM));
                            drawProperty("Throttle", Math.Round(drivetrain.throttle, 3));
                            drawProperty("Ambient Temp", Math.Round(ambientTemperature.Value, 2));
                        }
                    }
                    //edits
                    using (new GUILayout.AreaScope(new Rect((width * 2) - left, top + 135, width, height - top + 40), "", guiStyle))
                    {
                        using (verticalScope)
                        {
                            // floats
                            drawPropertyEdit("Oil Cooling Rate", ref oilCoolerCoolingRate);
                            drawPropertyEdit("Themostat rating", ref oilCoolerThermostatRating);
                            drawPropertyEdit("Initial RPM", ref initialRpm);
                            drawPropertyEdit("Max Boost RPM", ref maxBoostRpm);
                            drawPropertyEdit("Boost Multi", ref boostMultiplier);
                            drawPropertyEdit("Wastegate cut", ref wastegateCutModifier);
                            drawPropertyEdit("Carb Pipe blow off factor", ref carbPipeBlowOffForceFactor);
                            drawPropertyEdit("engine heat factor", ref turboEngineHeatFactor);
                            drawPropertyEdit("Humidity", ref relativeHumidity);
                            drawPropertyEdit("Power band rpm factor", ref rpmPowerBandFactor);
                            drawPropertyEdit("Min Torque RPM", ref minTorqueRpm);
                            drawPropertyEdit($"Min Power RPM {minPowerRpm}\nPower RPM Diff", ref powerToTorqueRPMDifference);
                            if (systemDepressionOverrideActive)
                                drawPropertyEdit("System Depression", ref systemDepressionOverrideValue);
                            GUILayout.Space(10);
                            // bools
                            drawPropertyBool("Apply Power multipler calculation?", ref applyCalculatedPowerMultipler);
                            drawPropertyBool("Apply Density calculation?", ref applyCalculatedDensity);
                            drawPropertyBool("Apply Combustion calculation?", ref applyCalculatedCombustion);
                            drawPropertyBool("Apply Power band calculation?", ref applyCalculatedPowerBand);
                            drawPropertyBool("Apply Check mixture?", ref applyCheckMixture);
                            drawPropertyBool("Apply System Depression Override?", ref systemDepressionOverrideActive);
                        }
                    }
                    // states
                    using (new GUILayout.AreaScope(new Rect((width * 3) - left, top + 135, width, height - top), "", guiStyle))
                    {
                        drawProperty("ENGINE & TURBO STATE");
                        GUILayout.Space(10);
                        using (verticalScope)
                        {
                            drawProperty("Fuel <color=" + (PSI > maxBoostFuel ? "red>STARVING</color>" : "green>OK</color>") + $" ({maxBoostFuel})");
                            drawProperty(!isPiped ? "<color=yellow>Not Piped</color>" : $"<color=blue>Piped</color> | {(canPipeWork ? "<color=green>Working</color>" : "<color=yellow>Idle</color>")}");
                            drawProperty(!isExhaust ? "<color=yellow>Non Turbo</color>" : $"<color=blue>Turbo</color> | {(canTurboWork ? "<color=green>Working</color>" : "<color=yellow>Idle</color>")}");
                            drawProperty("<color=" + (!isFiltered ? "yellow>Unfiltered</color>" : "blue>Filtered</color>"));
                            drawProperty($"Air-Fuel: <b>{printAFState()}</b>");
                        }
                    }
                    /*// Tests
					bool enableAirDensityTest = false;
					using (new GUILayout.AreaScope(new Rect((width * 4) - left, top + 135, width, Screen.height - top), "", guiStyle)) 
					{
						if (enableAirDensityTest)
						{
							if (engineCranked)
							{
								drawProperty("cal Ad (sats vapour, ambient temp)", calDensityFloatOp.float1.Value + calculateAirDensity(PSI, calculateSaturationVapourPressure(ambientTemperature.Value) * relativeHumidity));
								drawProperty("cal Ad  (sats vapour, engine temp)", calDensityFloatOp.float1.Value + calculateAirDensity(PSI, calculateSaturationVapourPressure(engineTemp.Value) * relativeHumidity));
								drawProperty("cal Ad  (dew point, ambient temp)", calDensityFloatOp.float1.Value + calculateAirDensity(PSI, calculateDewPoint(ambientTemperature.Value, relativeHumidity)));
								drawProperty("cal Ad  (dew point, engine temp)", calDensityFloatOp.float1.Value + calculateAirDensity(PSI, calculateDewPoint(engineTemp.Value, relativeHumidity)));
							}
							drawProperty("Saturation Vapour Pressure (ambient temp)", calculateSaturationVapourPressure(ambientTemperature.Value) * relativeHumidity);
							drawProperty("Saturation Vapour Pressure (engine temp)", calculateSaturationVapourPressure(engineTemp.Value) * relativeHumidity);
							drawProperty("Dew Point (ambient temp)", calculateDewPoint(ambientTemperature.Value, relativeHumidity));
							drawProperty("Dew Point (engine temp)", calculateDewPoint(engineTemp.Value, relativeHumidity));
						}
					}*/
                }
            }
            catch (Exception ex)
            {
                TurboMod.print($"Error, {ex}");
            }
        }

        #endregion

        #region Methods

        // PRIVATE
        private void guiToggle()
        {
            // Written, 15.01.2022

            if (cInput.GetButtonDown("DrivingMode") && cInput.GetButtonDown("Finger"))
            {
                GUIdebug = !GUIdebug;
            }
        }
        private void drawPropertyEdit(string inPropertyName, ref float inProperty)
        {
            using (horizontalScope)
            {
                drawProperty(inPropertyName);
                propertyString = GUILayout.TextField(inProperty.ToString(), 10);
            }
            float.TryParse(propertyString, out inProperty);

        }
        private void drawPropertyBool(string inPropertyName, ref bool inProperty)
        {
            using (horizontalScope)
            {
                inProperty = GUILayout.Toggle(inProperty, inPropertyName);
            }

        }
        private void drawProperty(string inPropertyName, object inProperty = null)
        {
            if (inProperty != null)
                GUILayout.Label($"{inPropertyName}: {inProperty}");
            else
                GUILayout.Label($"{inPropertyName}");
        }
        private void checkAnyConflictingPart()
        {
            // stock carb pipe =><= stockairfilter
            if (carbPipe.installed && stockFilterInstalled.Value)
                carbPipe.disassemble();
            // turbo headers =><= stockheaders & steel headers
            if (headers.installed && (stockHeadersInstalled.Value || raceHeadersInstalled.Value))
                headers.disassemble();
            // turbo filters
            if (highFlowFilter.installed && filter.installed)
                highFlowFilter.disassemble();
        }
        private void boostGaugeNeedleReset()
        {
            boostGaugeNeedle = 18;
            boostGaugeNeedleUpdate();
        }
        private void initEngineState()
        {
            //checkAnyConflictingPart();
            //partCheck();
            updateBoostMultiplier();
            updateCarbSetup();
            stockAirFilterTriggerToggle();
            headersTriggerToggle();
            butterflyNutCheck(!butterflyNut.loose && butterflyNut.tightness < ButterflyNut.MAX_TIGHTNESS);
            boostGaugeNeedleReset();
        }
        private void initSimulation()
        {
            try
            {
                wearMultipler = loadedSaveData.turboWear;
                wastegateRPM = Mathf.Clamp(loadedSaveData.wastegatePsi * RPM2PSI, MIN_WASTEGATE_RPM, MAX_WASTEGATE_RPM);
                wastegatePSI = wastegateRPM / RPM2PSI;
                destroyed = loadedSaveData.turboDestroyed;

                #region game field assignments 

                satsumaSpeedKmh = PlayMakerGlobals.Instance.Variables.FindFsmFloat("SpeedKMH");
                playerStress = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerStress");
                ambientTemperature = PlayMakerGlobals.Instance.Variables.FindFsmFloat("AmbientTemperature");
                GameObject satsuma = GameObject.Find("SATSUMA(557kg, 248)");
                carSimulation = satsuma.transform.Find("CarSimulation").gameObject;
                GameObject starter = carSimulation.transform.Find("Car/Starter").gameObject;
                engine = carSimulation.transform.Find("Engine").gameObject;
                exhaust = carSimulation.transform.Find("Exhaust").gameObject;
                soundSpool = GameObject.Find("turbospool");
                soundFlutter = GameObject.Find("flutter");
                turboFlutter = soundFlutter.GetComponent<AudioSource>();
                turboWhistle = soundSpool.GetComponent<AudioSource>();
                drivetrain = satsuma.GetComponent<Drivetrain>();
                engineTemp = FsmVariables.GlobalVariables.FindFsmFloat("EngineTemp");
                InteractText = FsmVariables.GlobalVariables.FindFsmString("GUIinteraction");
                exhaust_fromEngine = exhaust.transform.Find("FromEngine").gameObject;
                exhaust_fromPipe = exhaust.transform.Find("FromPipe").gameObject;
                exhaust_fromPipe_audioSource = exhaust_fromPipe.GetComponent<AudioSource>();
                exhaust_fromMuffler = exhaust.transform.Find("FromMuffler").gameObject;
                exhaust_fromMuffler_audioSource = exhaust_fromMuffler.GetComponent<AudioSource>();
                exhaust_fromHeaders = exhaust.transform.Find("FromHeaders").gameObject;
                motorBlow = carSimulation.transform.Find("Car/Redlining").GetComponent<PlayMakerFSM>();
                backfire = engine.transform.Find("Symptoms").GetComponent<PlayMakerFSM>();
                headgasket = GameObject.Find("Database/DatabaseMotor/Headgasket").GetComponent<PlayMakerFSM>();
                pingingSound = engine.transform.Find("SoundPinging").gameObject;
                turboNeedleObject = boostGauge.transform.GetChild(2).gameObject;
                headerTriggers = GameObject.Find("cylinder head(Clone)/Triggers Headers");
                stockFilterTrigger = GameObject.Find("carburator(Clone)/trigger_airfilter");
                turboFan = GameObject.Find("motor_turbocharger_blades");
                fuelGo = engine.transform.Find("Fuel").gameObject;
                fuel = fuelGo.GetComponent<PlayMakerFSM>();
                fuelEvent = fuelGo.GetComponents<PlayMakerFSM>()[1];
                oil = carSimulation.transform.Find("Engine/Oil").GetComponent<PlayMakerFSM>();
                swear = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SpeakDatabase").GetComponent<PlayMakerFSM>();
                mechanicalWear = carSimulation.transform.Find("MechanicalWear").GetComponent<PlayMakerFSM>();
                headGasketWear = mechanicalWear.FsmVariables.GetFsmFloat("WearHeadgasket");
                fuelPumpEfficiency = fuel.FsmVariables.GetFsmFloat("FuelPumpEfficiency");
                mixture = fuelEvent.FsmVariables.FindFsmFloat("Mixture");
                airDensity = fuelEvent.FsmVariables.FindFsmFloat("AirDensity");
                carbBowl = fuel.FsmVariables.GetFsmFloat("CarbReserve");
                oilLevel = oil.FsmVariables.GetFsmFloat("Oil");
                oilDirt = oil.FsmVariables.GetFsmFloat("OilContaminationRate");
                stockCarbInstalled = GameObject.Find("Database/DatabaseMotor/Carburator").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                stockFilterInstalled = GameObject.Find("Database/DatabaseMotor/Airfilter").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                raceCarbInstalled = GameObject.Find("Database/DatabaseOrders/Racing Carburators").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                stockHeadersInstalled = GameObject.Find("Database/DatabaseMotor/Headers").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                raceHeadersInstalled = GameObject.Find("Database/DatabaseOrders/Steel Headers").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                raceexhaustinstalled = GameObject.Find("Database/DatabaseOrders/Racing Exhaust").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                racemufflerinstalled = GameObject.Find("Database/DatabaseOrders/Racing Muffler").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                turboMesh = turbo.transform.Find("turbomesh").gameObject;
                turboMeshDestoryed = turbo.transform.Find("turbomesh_D").gameObject;
                throttlePedal = satsuma.transform.Find("Dashboard/Pedals/pedal_throttle").GetComponent<PlayMakerFSM>().FsmVariables.GetFsmFloat("Data");
                playerInMenu = FsmVariables.GlobalVariables.FindFsmBool("PlayerInMenu");

                #endregion

                #region Carb install set up/check/inject

                GameObject cylinderHead = GameObject.Find("cylinder head(Clone)");

                // Carb install set up check inject
                cylinderHead.transform.Find("Triggers Carbs/trigger_carburator").gameObject.FsmInject("Assemble", onCarbSetup);
                cylinderHead.transform.Find("Triggers Carbs/trigger_carburator_racing").gameObject.FsmInject("Assemble", onCarbSetup);
                GameObject.Find("carburator(Clone)").FsmInject("Remove part", onCarbSetup);

                GameObject orderRaceCarb = GameObject.Find("Database/DatabaseOrders/Racing Carburators");
                PlayMakerFSM orderRaceCarbData = orderRaceCarb.GetPlayMaker("Data");
                if (!orderRaceCarbData.FsmVariables.GetFsmBool("Purchased").Value)
                {
                    orderRaceCarb.FsmInject("State 3", onRaceCarbPurchased);
                }
                else
                {
                    onRaceCarbPurchased();
                }

                #endregion

                #region starter inject

                // starter inject
                starter.injectAction("Starter", "Start engine", PlayMakerExtentions.injectEnum.append, onEngineCrankUp);

                ModConsole.Print("[starter hooked]");

                #endregion

                #region racing exhaust check injects


                // Pipe
                GameObject orderRaceExhaust = GameObject.Find("Database/DatabaseOrders/Racing Exhaust");
                PlayMakerFSM orderRaceExhaustData = orderRaceExhaust.GetPlayMaker("Data");
                if (!orderRaceExhaustData.FsmVariables.GetFsmBool("Purchased").Value)
                {
                    orderRaceExhaust.FsmInject("Activate", onRaceExhaustPurchased);
                }
                else
                {
                    onRaceExhaustPurchased();
                }

                // Muffler
                GameObject orderRaceMuffler = GameObject.Find("Database/DatabaseOrders/Racing Muffler");
                PlayMakerFSM orderRaceMufflerData = orderRaceMuffler.GetPlayMaker("Data");
                if (!orderRaceMufflerData.FsmVariables.GetFsmBool("Purchased").Value)
                {
                    orderRaceMuffler.FsmInject("Activate", onRaceMufflerPurchased);
                }
                else
                {
                    onRaceMufflerPurchased();
                }

                #endregion

                #region lux

                // rwd  cause why not
                if (cInput.GetButton("Finger"))
                    drivetrain.transmission = Drivetrain.Transmissions.RWD;
                drivetrain.clutchTorqueMultiplier = 400;

                #endregion
            }
            catch (Exception ex)
            {
                ModConsole.Error(ex.ToString());
            }
        }
        private void fuelStarveEvent()
        {
            if (timeBoosting > Random.Range(3, 12.6f))
            {
                drivetrain.revLimiterTriggered = true;
                exhaustCrackle(true);
                if (wear)
                    wearMultipler += Random.Range(0.3f, 3f);
            }
        }
        private void carbPipeBlowOffChance()
        {
            float t = butterflyNut.tightness;
            float mT = ButterflyNut.MAX_TIGHTNESS;
            float chance = 1 - t / mT;
            if (chance > 0)
            {
                if (PSI.round() > Random.Range(3, 18))
                {
                    if (chance > Random.Range(0f, 1) && carbPipeBlowOffChanceFactor > Random.Range(0.0f, 10))
                    {
                        carbPipe.disassemble(true);
                        Rigidbody rb = carbPipe.GetComponent<Rigidbody>();
                        rb.AddForce(Vector3.forward * (PSI * carbPipeBlowOffForceFactor * rb.mass), ForceMode.Impulse);
                    }
                }
            }
        }
        private float mapValue(float mainValue, float inValueMin, float inValueMax, float outValueMin, float outValueMax)
        {
            return (mainValue - inValueMin) * (outValueMax - outValueMin) / (inValueMax - inValueMin) + outValueMin;
        }
        private string printAFState()
        {
            string rv;
            if (engineCranked)
            {
                rv = "<color=";
                if (afrAbsolute >= checkMixtureLean.float2.Value)
                    rv += "red>Lean";
                else if (afrAbsolute >= checkMixtureRich.float2.Value)
                    rv += "green>Optimal";
                else if (afrAbsolute >= checkMixtureSuperRich.float2.Value)
                    rv += "yellow>Rich";
                else if (afrAbsolute >= checkMixtureSputter.float2.Value)
                    rv += "orange>Super Rich";
                else if (afrAbsolute >= checkMixtureOff.float2.Value)
                    rv += "red>Sputter";
                else if (afrAbsolute < checkMixtureOff.float2.Value)
                    rv += "red>Off (Too Rich)";
                else
                    rv += "red>ERR";
                rv += "</color>";
            }
            else
            {
                rv = "Crank engine to see info.";
            }

            return rv;
        }
        private void cursorFunction()
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(2))
            {
                playerInMenu.Value = !playerInMenu.Value;
            }
        }
        private void turboCondCheck()
        {
            if (turboFan.gameObject.isPlayerLookingAt())
            {
                ModClient.guiUse = true;
                turboFan.transform.localEulerAngles = new Vector3(turboSpin, 0f, 0f);
                if (isFiltered)
                {
                    if (cInput.GetButtonUp("Use") && downPipe.installed)
                    {
                        InteractText.Value = "Can't check with the filter on!";
                        swear.SendEvent("SWEARING");
                    }
                }
                else
                {
                    if (turboRPM > 200)
                    {
                        InteractText.Value = "That looks dangerous!";
                    }
                    if (cInput.GetButtonUp("Use"))
                    {
                        if (!engineOn)
                        {
                            turboSpin += 10f;
                            if (wearMultipler > 99 || destroyed)
                            {
                                InteractText.Value = "There's nothing left to check...";
                                swear.SendEvent("SWEARING");
                            }
                            else if (wearMultipler > 65)
                            {
                                InteractText.Value = "Feels worn out, a bit of shaft play";
                                MasterAudio.PlaySound3DAndForget("Motor", turbo.transform, variationName: "damage_bearing");
                            }
                            else if (wearMultipler > 30)
                            {
                                InteractText.Value = "A little used, seems fine";
                                MasterAudio.PlaySound3DAndForget("Motor", turbo.transform, variationName: "valve_knock");
                            }
                            else
                            {
                                InteractText.Value = "Looks brand new";
                            }
                        }
                        else if (turbo.installed && turboRPM > 200)
                        {
                            turboDestroyEvent();
                            deathByTurbo();
                        }
                    }
                }
            }
        }
        private void deathByTurbo()
        {
            GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera").GetComponents<PlayMakerFSM>().FirstOrDefault((PlayMakerFSM x) => x.Fsm.Name == "Death").SendEvent("DEATH");
            GameObject.Find("Systems").transform.Find("Death").GetComponent<PlayMakerFSM>().FsmVariables.GetFsmBool("RunOver").Value = true;
            GameObject.Find("Systems").transform.Find("Death").GetComponent<PlayMakerFSM>().FsmVariables.GetFsmBool("Crash").Value = false;
            turbo.transform.Find("handgrind").gameObject.SetActive(true);
            GameObject.Find("Systems").transform.Find("Death/GameOverScreen/Paper/HitAndRun/TextEN").GetComponent<TextMesh>().text = "Boy's \n hand eaten \n by\n turbo";
            GameObject.Find("Systems").transform.Find("Death/GameOverScreen/Paper/HitAndRun/TextFI").GetComponent<TextMesh>().text = "Pojan käsi tuhoutui \n turboahtimella";
        }
        private void wasteGateAdjust()
        {
            if (act.installed && !turbo.gameObject.isPlayerHolding() && !headers.gameObject.isPlayerHolding())
            {
                if (act.gameObject.isPlayerLookingAt())
                {
                    wastegatePSI = wastegateRPM / RPM2PSI;
                    InteractText.Value = "Wastegate Pressure: " + wastegatePSI.ToString("0.00") + " PSI";
                    if (Input.mouseScrollDelta.y > 0 && wastegateRPM <= 194925)
                    {
                        wastegateRPM += WASTEGATE_ADJUST_INTERVAL;
                        MasterAudio.PlaySound3DAndForget("CarBuilding", act.transform, variationName: "bolt_screw");
                    }
                    if (Input.mouseScrollDelta.y < 0 && wastegateRPM >= MIN_WASTEGATE_RPM)
                    {
                        wastegateRPM -= WASTEGATE_ADJUST_INTERVAL;
                        MasterAudio.PlaySound3DAndForget("CarBuilding", act.transform, variationName: "bolt_screw");
                    }
                    if (cInput.GetButtonDown("Finger") && wastegateRPM <= MAX_WASTEGATE_RPM)
                    {
                        wastegateRPM = MAX_WASTEGATE_RPM;
                        MasterAudio.PlaySound3DAndForget("CarBuilding", act.transform, variationName: "bolt_screw");
                    }
                }
            }
            else if (InteractText.Value != string.Empty)
                InteractText.Value = string.Empty;
        }
        private void partCheck()
        {
            // Written, 18.09.2021

            if (wearMultipler > 55f && !turboMeshDestoryed.activeInHierarchy)
            {
                turboMeshDestoryed.SetActive(true);
                turboMesh.SetActive(false);
            }
            else if (!turboMesh.activeInHierarchy)
            {
                turboMesh.SetActive(true);
                turboMeshDestoryed.SetActive(false);
            }
        }
        private void updateExhaust()
        {
            // Written, 27.09.2021

            if (isExhaust)
            {
                if (raceexhaustinstalled.Value)
                {
                    if (racemufflerinstalled.Value)
                    {
                        exhaust_fromEngine.SetActive(false);
                        exhaust_fromHeaders.SetActive(false);
                        exhaust_fromPipe.SetActive(false);
                        exhaust_fromMuffler.SetActive(true);
                        exhaust_fromMuffler_audioSource.volume = 0.4f;
                    }
                    else
                    {
                        exhaust_fromMuffler.SetActive(false);
                        exhaust_fromEngine.SetActive(false);
                        exhaust_fromHeaders.SetActive(false);
                        exhaust_fromPipe.SetActive(true);
                        exhaust_fromPipe_audioSource.volume = 0.87f;
                    }
                }
                else
                {
                    exhaust_fromPipe.SetActive(false);
                    exhaust_fromMuffler.SetActive(false);
                    exhaust_fromEngine.SetActive(false);
                    exhaust_fromHeaders.SetActive(true);
                }
            }
            else
            {
                if (stockHeadersInstalled.Value || raceHeadersInstalled.Value)
                {
                    exhaust_fromPipe_audioSource.volume = 0.8f;
                    exhaust_fromMuffler_audioSource.volume = 0.2f;
                    return;
                }
                if (headers.installed)
                {
                    exhaust_fromPipe.SetActive(false);
                    exhaust_fromMuffler.SetActive(false);
                    exhaust_fromEngine.SetActive(false);
                    exhaust_fromHeaders.SetActive(true);
                }
                else
                {
                    exhaust_fromHeaders.SetActive(false);
                    exhaust_fromPipe.SetActive(false);
                    exhaust_fromMuffler.SetActive(false);
                    exhaust_fromEngine.SetActive(true);
                }
            }
        }
        private void exhaustCrackle(bool force = false)
        {
            if ((PSI > 3.0 && carbBowl.Value > 1.065f) || force)
            {
                backfire.SendEvent("TIMINGBACKFIRE");
            }
        }
        private void turboSounds()
        {
            spoolSoundEnabled = engineOn || turboSpoolingDown;//engineOn;//PSI > 0;
            turboWhistle.pitch = mapValue(turboRPM, 0, MAX_WASTEGATE_RPM, 1.76f, Mathf.Min(2.24f, 3)) * (onThrottlePedal ? 1 : -1);
            turboWhistle.volume = mapValue(turboRPM, MIN_WASTEGATE_RPM, MAX_WASTEGATE_RPM, 0.15f, 1);
            turboWhistle.volume *= isFiltered ? 0.7f : 1.25f;
        }
        private void boostGaugeNeedleUpdate()
        {
            vaccummSmooth = PSI.round() > 1 ? throttlePedalPosition * 117 : 0;
            vaccummSmooth = Mathf.Clamp(vaccummSmooth, drivetrain.idlethrottle * 117, 117);
            vaccummThrottle = Mathf.SmoothDamp(vaccummThrottle, vaccummSmooth, ref throttleSmooth, 0.05f);
            boostGaugeNeedle = 133 + -vaccummThrottle + -turboRPM / 1600;
            boostNeedleVector.z = boostGaugeNeedle;
            turboNeedleObject.transform.localEulerAngles = boostNeedleVector;
        }
        private void turboDestroyEvent()
        {
            wearMultipler = 100;
            destroyed = true;
            MasterAudio.PlaySound3DAndForget("Motor", turbo.transform, variationName: "damage_oilpan");
            if (engineOn)
            {
                if (Random.Range(0f, drivetrain.rpm / drivetrain.minRPM) > Random.Range(1, 5))
                {
                    motorBlow.SendEvent("Finished");
                }
            }
            swear.SendEvent("SWEARING");
            turboFanCheck();
        }
        private void turboRepairEvent()
        {
            wearMultipler = Random.Range(0.5f, 9f);
            destroyed = false;
            turboFanCheck();
        }
        private turboSimulationSaveData getSave()
        {
            return new turboSimulationSaveData() { turboDestroyed = destroyed, turboWear = wearMultipler, wastegatePsi = wastegatePSI };
        }
        private void calculateSystemDepression()
        {
            if (systemDepressionOverrideActive)
                systemDepression = systemDepressionOverrideValue;
            else
                systemDepression = (filter.installed ? 1.15f : highFlowFilter ? 0.75f : 0) + (carbInstall ? 1.12f : raceCarbInstall ? 1.28f : 0);
            systemDepressionRpm = systemDepression * RPM2PSI;
        }
        private void resetTurboValues() 
        {
            // Written, 26.02.2022

            spoolSoundEnabled = false;
        }
        // INTERNAL
        internal float calculateSaturationVapourPressure(float T)
        {
            // p₁ = 6.1078 * 10^[7.5*T /(T + 237.3)]

            float p1 = 6.1078f * Mathf.Log10(7.5f * T / (T + 237.3f));

            return p1;
        }
        internal float calculateDewPoint(float T, float RH)
        {
            // DP = 243.12 * α / (17.62 - α)
            // α = ln(RH/100) + 17.62 * T / (243.12 + T).

            float a = Mathf.Log(RH / 100) + 17.62f * T / (243.12f + T);

            float DP = 243.12f * a / (17.62f - a); // calculated dew point

            return DP;
        }
        internal float calculateAirDensity(float pressure, float pv)
        {
            // FORMULA: ρ = (pd / (Rd * T)) + (pv / (Rv * T))

            float Tc = ambientTemperature.Value;  // temp in c
            float Tk = Tc + KELVIN; // temperature in kelvin

            //float pv = calculateSaturationVapourPressure(Tc) * RH; // water vapour in C
            float pd = pressure - pv; // pressure in pounds pre square inch (psi)

            pv *= PASCAL; // water vapour in pascals
            pd *= PASCAL; // pressure in pascals

            float result = (pd / (DRY_GAS_CONSTANT * Tk)) + (pv / (WET_GAS_CONSTANT * Tk)); // in kilogram per meter cubic (Kg/m3)

            return result;

            //return result / 1000; // in gram per meter cubic (g/m3
        }
        internal void calculateAtmosphericPressure()
        {
            // Written, 15.01.2022

            //barometric formula: P = P₀ exp(-gM(h - h₀) / (RT))
            atmosphericPressure = PRESSURE_REFERENCE_LEVEL * (float)Math.Exp(-GRAVITY * MOLAR_MASS_OF_AIR * (turboHeight - HEIGHT_REFERENCE_LEVEL) / (UNIVERSAL_GAS_CONSTANT * ambientTemperature.Value));

        }

        // STATIC
        internal static void butterflyNutCheck(bool outLoose = false)
        {
            if (butterflyNut.tight || outLoose)
                carbPipe.setActiveAllTriggers(false, true);
            else if (butterflyNut.loose)
                carbPipe.setActiveAllTriggers(true, true);
            if (carbPipe.mouseOver)
                carbPipe.mouseOverReset();
        }

        #endregion

        #region IEnumerators

        private IEnumerator pipeFunction()
        {
            //ModConsole.Print("Pipe Simulation: Started");
            while (canPipeWork)
            {
                boostGaugeNeedleUpdate();
                updateMaxFuelBoost();
                onCalculateCombusion(applyCalculatedCombustion);
                onCalculateDensity(applyCalculatedDensity);
                onCheckMixture(applyCheckMixture);
                onCalculatePowerBand(applyCalculatedPowerBand);
                carbPipeBlowOffChance();

                if (applyCalculatedPowerMultipler)
                    drivetrain.powerMultiplier *= calculatedPowerMultiplier;

                if (PSI.round() > 0)
                {
                    timeBoosting += Time.deltaTime;
                    engineTemp.Value += drivetrain.powerMultiplier * 4f * throttlePedalPosition * turboEngineHeatFactor;
                    drivetrain.revLimiterTime = Mathf.Clamp(0.2f - (PSI / 175), 0.13f, 1);
                    if (PSI > maxBoostFuel)
                    {
                        fuelStarveEvent();
                    }
                }
                else
                {
                    timeBoosting = 0f;
                    drivetrain.revLimiterTime = 0.2f;
                }
                yield return null;
            }
            onCalculateCombusion(false);
            onCalculateDensity(false);
            onCheckMixture(false);
            onCalculatePowerBand(false);
            boostGaugeNeedleReset();
            pipeRoutine = null;
            //ModConsole.Print("Pipe Simulation: Finished");
        }
        private IEnumerator turboFunction()
        {
            ModConsole.Print("Turbo Simulation: Started");
            while (canTurboWork)
            {
                calculateAtmosphericPressure();
                calculateSystemDepression();
                PSI = turboRPM / RPM2PSI;
                outletPressure = PSI + atmosphericPressure;
                inletPressure = atmosphericPressure - systemDepression;
                pressureRatio = outletPressure / inletPressure;
                airFlowPressureRatio = drivetrain.rpm > maxBoostRpm ? mapValue(pressureRatio * maxBoostRpm / drivetrain.rpm, pressureRatio, 0, pressureRatio, 1) : pressureRatio;
                airFlowRate = 988 * drivetrain.rpm / 60 / 60 * airFlowPressureRatio * airFlowRateModifer * (throttlePedalPosition + drivetrain.idlethrottle); // (cc/millisec) | engine displacement: 988cc
                turboSpool = airFlowRate;

                calculatedPowerMultiplier = (maxBoostRpm + initialRpm) * boostMultiplier + PSI / 22;

                if (act.installed && turboTargetRPM != wastegateRPM + systemDepressionRpm)
                {
                    turboTargetRPM = wastegateRPM + systemDepressionRpm;
                }

                turboRPM = Mathf.Clamp(turboRPM, 0f, turboTargetRPM * 5f);
                wastegateSpool = wastegateRPM / 22 - turboFrictionMultipler * 3f;
                turboSpool = Mathf.Clamp(turboSpool, turboSpool, turboRPM > turboTargetRPM * wastegateCutModifier && act.installed ? wastegateSpool : 55555);
                turboFan.transform.localEulerAngles = new Vector3(turboSpin, 0f, 0f);
                turboRPM += turboSpool;
                turboRPM -= turboFriction;
                turboSpin += turboRPM;

                turboSounds();

                if (turboSurging && turboSurgeRoutine == null)
                    turboSurgeRoutine = StartCoroutine(surgeFunction());

                yield return null;
            }
            resetTurboValues();
            if (destroyed)
            {
                turboDestroyEvent();
            }

            turboRoutine = null;
            ModConsole.Print("Turbo Simulation: Finished");
        }
        private IEnumerator oilCoolerFunction()
        {
            ModConsole.Print("Oil Cooler: Started");
            while (canOilCoolerWork)
            {
                if (engineTemp.Value > oilCoolerThermostatRating)
                {
                    engineTemp.Value = engineTemp.Value -= oilCoolerCoolingRate;
                }
                yield return null;
            }
            oilCoolerRoutine = null;
            ModConsole.Print("Oil Cooler: Finished");
        }
        private Coroutine turboSurgeRoutine;
        private IEnumerator surgeFunction()
        {
            ModConsole.Print("Surge: Started");

            soundFlutter.SetActive(true);
            while (canTurboWork && turboSurging)
            {
                if (!turboFlutter.isPlaying)
                {
                    soundFlutter.SetActive(false);
                    yield return null;
                }
                turboFlutter.volume = mapValue(turboRPM, 0, wastegateRPM, 0, 1);
                turboFlutter.pitch = mapValue(turboRPM, 0, wastegateRPM, 0.87f, 2.15f);
                turboFlutter.loop = false;
                turboFlutter.mute = false;
                calculatedSurgeRate = airFlowRate * pressureRatio;
                turboRPM -= calculatedSurgeRate;
                yield return null;
            }
            soundFlutter.SetActive(false);
            playerStress.Value -= 0.2f;
            turboSurgeRoutine = null;

            ModConsole.Print("Surge: Finished");
        }

        private void checkRoutinesRunning()
        {
            if (oilCoolerRoutine == null)
                if (canOilCoolerWork)
                    oilCoolerRoutine = StartCoroutine(oilCoolerFunction());
            if (turboRoutine == null)
                if (canTurboWork)
                    turboRoutine = StartCoroutine(turboFunction());
            if (pipeRoutine == null)
                if (canPipeWork)
                    pipeRoutine = StartCoroutine(pipeFunction());
        }

        #endregion

        #region EventHandlers

        internal void onCalculateCombusion(bool pipeWorking)
        {
            float rich = checkMixtureRich.float2.Value;
            float afm = checkMixtureAirFuelMixture.float2.Value;
            mixtureMedian = ((checkMixtureLean.float2.Value - rich) / 2) + rich;

            afrAbsolute = mixture.Value * afm;

            if (afrAbsolute > mixtureMedian)
                afFactor = afm / afrAbsolute;
            else if (afrAbsolute > rich)
                afFactor = mixtureMedian / afrAbsolute;
            else
                afFactor = afrAbsolute / afm;

            if (pipeWorking)
            {
                /*stockInertia.Value = STOCK_INERTIA - mapValue(turboRPM, 3000, MAX_WASTEGATE_RPM, 0, STOCK_INERTIA - turboStockMinInertia);
				racingInertia.Value = RACING_INERTIA - mapValue(turboRPM, 3000, MAX_WASTEGATE_RPM, 0, RACING_INERTIA - turboRacingMinInertia);*/

                calculatedPowerMultiplier *= afFactor;
            }
            else // Reset values
            {
                /*stockInertia.Value = STOCK_INERTIA;
				racingInertia.Value = RACING_INERTIA;*/
            }
        }
        internal void onCheckMixture(bool pipeWorking)
        {
            if (pipeWorking)
            {
                checkMixtureAirFuelMixture.float2 = 14.7f;
                checkMixtureLean.float2 = 14f;
                checkMixtureRich.float2 = 10.7f;
                checkMixtureSuperRich.float2 = 10f;
                checkMixtureSputter.float2 = 9.3f;
                checkMixtureOff.float2 = 8.85f;
            }
            else
            {
                checkMixtureAirFuelMixture.float2 = 14.7f;
                checkMixtureLean.float2 = 16;
                checkMixtureRich.float2 = 14.05f;
                checkMixtureSuperRich.float2 = 12.7f;
                checkMixtureSputter.float2 = 10;
                checkMixtureOff.float2 = 8;
            }
        }
        internal void onCalculateDensity(bool pipeWorking)
        {
            calDensityFloatOp.float1.Value = 0.36f;
            if (pipeWorking && PSI > 1)
            {
                calDensityFloatOp.float1.Value += calculateAirDensity(PSI, calculateSaturationVapourPressure(ambientTemperature.Value) * relativeHumidity);
                updateCalculateDensityClamp(true);
                updateCaculateMixutureOperation(true);
            }
            else
            {
                updateCalculateDensityClamp(false);
                updateCaculateMixutureOperation(false);
            }
        }
        internal void updateCaculateMixutureOperation(bool pipeWorking)
        {
            if (pipeWorking)
            {
                calMixtureFloatOp.operation = FloatOperator.Operation.Multiply;
            }
            else
            {
                calMixtureFloatOp.operation = FloatOperator.Operation.Divide;
            }
        }
        internal void updateCalculateDensityClamp(bool pipeWorking)
        {
            if (pipeWorking)
            {
                calDensityFloatClamp.minValue.Value = -10;
                calDensityFloatClamp.maxValue.Value = 10;
            }
            else
            {
                calDensityFloatClamp.minValue.Value = 0.9f;
                calDensityFloatClamp.maxValue.Value = 1.1f;
            }
        }
        private void onCalculatePowerBand(bool pipeWorking)
        {
            float torque;
            float power;

            if (pipeWorking)
            {
                float turboLoad = PSI / (MAX_WASTEGATE_RPM / RPM2PSI);
                float cal = mapValue(turboRPM, 3000, MAX_WASTEGATE_RPM, drivetrain.maxRPM - (maxBoostRpm * turboLoad * rpmPowerBandFactor), 0);
                minPowerRpm = minTorqueRpm + powerToTorqueRPMDifference;

                torque = cal;
                power = cal + powerToTorqueRPMDifference;

            }
            else
            {
                torque = STOCK_MAX_TORQUE_RPM;
                power = STOCK_MAX_POWER_RPM;
            }

            // Apply
            drivetrain.maxTorqueRPM = Mathf.Clamp(torque, minTorqueRpm, STOCK_MAX_TORQUE_RPM);
            drivetrain.maxPowerRPM = Mathf.Clamp(power, minPowerRpm, STOCK_MAX_POWER_RPM);
        }

        private void exhaustCheck()
        {
            updateExhaust();
            checkRoutinesRunning();
        }
        private void onEngineCrankUp()
        {
            string message = $"[Engine Cranked Over {(engineCranked ? "" : "First Time")}]";

            if (!engineCranked)
            {
                engineCranked = true;

                // cal mixture inject
                FsmState calMixture = fuelGo.GetPlayMakerState("Calculate mixture");
                calMixtureFloatOp = calMixture.Actions[4] as FloatOperator;

                // check mixture
                FsmState checkMixture = fuelGo.GetPlayMakerState("Check mixture");
                checkMixtureLean = checkMixture.Actions[6] as FloatCompare;
                checkMixtureRich = checkMixture.Actions[5] as FloatCompare;
                checkMixtureSuperRich = checkMixture.Actions[4] as FloatCompare;
                checkMixtureSputter = checkMixture.Actions[3] as FloatCompare;
                checkMixtureOff = checkMixture.Actions[2] as FloatCompare;
                checkMixtureAirFuelMixture = checkMixture.Actions[0] as FloatOperator;

                // cal density
                FsmState calDensity = fuelGo.GetPlayMakerState("Calculate density");
                calDensityFloatOp = calDensity.Actions[5] as FloatOperator;
                calDensityFloatClamp = calDensity.Actions[6] as FloatClamp;

                // exhaust update
                exhaust.injectAction("Logic", "Engine", PlayMakerExtentions.injectEnum.insert, updateExhaust, index: 5, finish: false);

                // exhaust check
                turbo.onAssemble += exhaustCheck;
                turbo.onDisassemble += exhaustCheck;
                headers.onAssemble += exhaustCheck;
                headers.onDisassemble += exhaustCheck;
                downPipe.onAssemble += exhaustCheck;
                downPipe.onDisassemble += exhaustCheck;

                // Combustion | engine inertia
                GameObject combustion = engine.transform.FindChild("Combustion").gameObject;
                PlayMakerFSM cylinders = combustion.GetPlayMaker("Cylinders");
                stockInertia = cylinders.FsmVariables.GetFsmFloat("EngineInertiaStock");
                racingInertia = cylinders.FsmVariables.GetFsmFloat("EngineInertiaRacing");

                // car sim red lining inject
                GameObject carRedlining = carSimulation.transform.FindChild("Car/Redlining").gameObject;
                carRedlining.injectAction("RedLining", "Get RPM", PlayMakerExtentions.injectEnum.insert, redLiningFinishedInject, index: 9, finish: true);
            }
            checkRoutinesRunning();

            TurboMod.print(message);
        }

        private void redLiningFinishedInject()
        {
            if (drivetrain.rpm >= 10500)
            {
                ModConsole.Print("10500 rpm exceeded");
            }
        }

        private void oilCoolerOnAssemble()
        {
            //oilCooler.transform.GetChild(0).gameObject.SetActive(true);
        }
        private void oilCoolerOnDisassemble()
        {
            //oilCooler.transform.GetChild(0).gameObject.SetActive(false);
        }
        private void updateCarbSetup()
        {
            if (raceCarbInstall)
            {
                initialRpm = 1625;
                maxBoostRpm = 6400;
            }
            if (carbInstall)
            {
                initialRpm = 1750;
                maxBoostRpm = 6000;
            }
        }
        private void updateMaxFuelBoost()
        {
            maxBoostFuel = (carbInstall ? 36 : (raceCarbInstall ? 18 : 0)) * fuelPumpEfficiency.Value * 60 + 0.25f;
        }
        private void updateBoostMultiplier()
        {
            if (isFiltered)
            {
                if (highFlowFilter.installed)
                    boostMultiplier = 0.0001275f;
                else if (filter.installed)
                    boostMultiplier = 0.000125f;
            }
            else
            {
                boostMultiplier = 0.00013f;
            }
        }
        private void onRaceCarbPurchased()
        {
            GameObject.Find("racing carburators(Clone)").FsmInject("Remove part", onCarbSetup);
        }
        private void onRaceExhaustPurchased()
        {
            GameObject re = GameObject.Find("racing exhaust(Clone)");
            re.injectAction("Removal", "Remove part", PlayMakerExtentions.injectEnum.append, updateExhaust);

            GameObject orderRaceExhaust = GameObject.Find("Database/DatabaseOrders/Racing Exhaust");
            PlayMakerFSM orderRaceExhaustData = orderRaceExhaust.GetPlayMaker("Data");
            if (orderRaceExhaustData.FsmVariables.GetFsmBool("Installed").Value)
            {
                re.injectAction("Removal", "Remove part", PlayMakerExtentions.injectEnum.append, raceExhaustCheckAssembly);
            }
            else
            {
                raceExhaustCheckAssembly();
            }
        }
        private void onRaceMufflerPurchased()
        {
            GameObject rm = GameObject.Find("racing muffler(Clone)");
            rm.injectAction("Removal", "Remove part", PlayMakerExtentions.injectEnum.append, updateExhaust);

            GameObject orderRaceMuffler = GameObject.Find("Database/DatabaseOrders/Racing Muffler");
            PlayMakerFSM orderRaceMufflerData = orderRaceMuffler.GetPlayMaker("Data");
            if (orderRaceMufflerData.FsmVariables.GetFsmBool("Installed").Value)
            {
                rm.injectAction("Removal", "Remove part", PlayMakerExtentions.injectEnum.append, raceMufflerCheckAssembly);
            }
            else
            {
                raceMufflerCheckAssembly();
            }
        }
        private void raceExhaustCheckAssembly()
        {
            if (!raceExhaustCheckAssemblyInjected)
            {
                raceExhaustCheckAssemblyInjected = true;
                GameObject exhaustTriggers = GameObject.Find("SATSUMA(557kg, 248)/MiscParts/Triggers Exhaust Pipes");
                exhaustTriggers.transform.Find("trigger_racing exhaust").gameObject.injectAction("Assembly", "Assemble 2", PlayMakerExtentions.injectEnum.append, updateExhaust);
            }
        }
        private void raceMufflerCheckAssembly()
        {
            if (!raceMufflerCheckAssemblyInjected)
            {
                raceMufflerCheckAssemblyInjected = true;
                GameObject mufflerTriggers = GameObject.Find("SATSUMA(557kg, 248)/MiscParts/Triggers Mufflers");
                mufflerTriggers.transform.Find("trigger_racing_muffler").gameObject.injectAction("Assembly", "Assemble 2", PlayMakerExtentions.injectEnum.append, updateExhaust);
            }
        }
        private void turboRpmReset()
        {
            turboRPM = 0;
        }
        private void turboFanCheck()
        {
            turboFan.SetActive(!destroyed || !(isFiltered && downPipe.installed && turbo.installed && headers.installed));
        }
        private void onCarbSetup()
        {
            updateCarbSetup();
            checkRoutinesRunning();
        }
        private void headersTriggerToggle()
        {
            headerTriggers.SetActive(!headers.installed);
        }
        private void stockAirFilterTriggerToggle()
        {
            stockFilterTrigger.SetActive(!carbPipe.installed);
        }
        public static void butterflyNut_onCheck()
        {
            butterflyNutCheck();
        }
        public static void butterflyNut_outLoose()
        {
            butterflyNutCheck(outLoose: true);
        }

        #endregion
    }
}
