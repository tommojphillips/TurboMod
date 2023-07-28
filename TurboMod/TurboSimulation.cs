using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MSCLoader;
using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using static TommoJProductions.ModApi.ModClient;
using TommoJProductions.ModApi.Attachable;
using TommoJProductions.ModApi;
using TommoJProductions.ModApi.Database;

namespace TommoJProductions.TurboMod
{
    public partial class TurboSimulation : MonoBehaviour
    {
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

        FsmFloat calculateMixtureMultipler5;
        private float mixtureDiscrepancy;
        private float ventExtentionMixtureDiscrepancyModifier = 0.15f;
        private float turboFriction;
        private float turboSurgeCo = 0.95f;
        private float airFlowCoEff;
        private readonly float turboSurgeCal;
        private GameObject turboMesh;
        private GameObject turboMeshDestoryed;
        private FloatCompare timingAngleRetarded;
        private FloatCompare timingAngleAdvanced;
        private FloatOperator timingMultiplier;
        private FsmFloat sparkTimingMultiplier;
        private float[,] rpmGraphPower;
        private float[,] powerToTorqueGraph;
        private float minRevLimitTime = 0.1f;
        private bool applyRevLimitTime = true;
        private bool resetValues = true;
        private bool applyFuelStarveEvent = true;
        private FsmInt numOfPistonsOK;

        internal bool destroyed
        {
            get => loadedSaveData.turboDestroyed;
            set
            {
                if (value)
                    turboDestroyEvent();
                else
                    turboRepairEvent();
                loadedSaveData.turboDestroyed = value;
            }
        }
        internal float wear
        {
            get => loadedSaveData.turboWear;
            set => loadedSaveData.turboWear = value;
        }
        internal float wastegatePSI
        {
            get => (float)loadedSaveData.wastegatePsi;
            set => loadedSaveData.wastegatePsi = value;
        }
        internal float wastegateRPM => (float)loadedSaveData.wastegatePsi * RPM2PSI;
        internal bool wearEnabled = false;

        private float dT;

        #region Fields

        // Static fields
        public static Part turbo;
        public static Part headers;
        public static Part downPipeRace;
        public static Part downPipeStraight;
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

        public static CarbParts carbParts;

        // Private fields
        private CalculateAirDensityEnum airDensityMethod = CalculateAirDensityEnum.saturationVapour;
        private AudioSource turboWhistle;
        private AudioSource turboFlutter;
        private AudioSource exhaust_fromMuffler_audioSource;
        private AudioSource exhaust_fromPipe_audioSource;
        private Drivetrain drivetrain = null;
        private bool applyCalculatedDensity = true;
        private bool applyCalculatedCombustion = true;
        private bool applyCheckMixture = true;
        private bool applyCalculatedPowerBand = false;
        private bool applyCalculatedPowerMultipler = true;
        private bool applyTiming = true;
        private bool engineCranked = false;
        private bool raceExhaustCheckAssemblyInjected = false;
        private bool raceMufflerCheckAssemblyInjected = false;
        private bool GUIdebug = false;
        private bool systemDepressionOverrideActive = false;
        private bool enableAirDensityTest = false;
        private float PSI;
        private float turboSpool;
        private float wastegateSpool;
        private float maxBoostRpm;
        private float timeBoosting;
        private float turboSpin;
        private float vaccummThrottle;
        private float vaccummSmooth;
        private float throttleSmooth;
        private float boostGaugeNeedle;
        private float turboTargetRPM;
        private float turboRPM;
        private float afFactor;
        private float afr;
        private float mixtureMedian;
        private float systemDepression;
        private readonly float systemDepressionRpm;
        private float systemDepressionOverrideValue;
        private float pressureRatio;
        private float airFlowRate;
        private float outletPressure;
        private float inletPressure;
        private float calculatedPowerMultiplier;
        private float calculatedSurgeRate;
        private float calculatedEngineHeat;
        private float turboEngineHeatFactor = 0.012f;
        private float oilCoolerCoolingRate = 0.102f;
        private float oilCoolerThermostatRating = 85;
        private readonly float carbPipeBlowOffForceFactor = 1f;
        private readonly float carbPipeBlowOffChanceFactor = 0.0125f;
        private float minTorqueRpm = 2300;
        private float minPowerRpm = 3000;
        private float powerToTorqueRPMDifference = 1425;
        private float turboLoad;
        [Range(0, 1)]
        private float relativeHumidity = 0.20534f;
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
        private FsmFloat choke;
        private FsmString InteractText;
        private GameObject turbineModel;
        private GameObject stockFilterTrigger;
        private GameObject headerTriggers;
        private GameObject soundSpool;
        private GameObject soundFlutter;
        private GameObject turboNeedleObject;
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
        private readonly PlayMakerFSM oil;
        private PlayMakerFSM mechanicalWear;
        private Vector3 boostNeedleVector;
        // Internal fields
        internal SimulationSaveData loadedSaveData = default;
        internal float atmosphericPressure;

        // GUI
        private readonly int fontSize = Screen.width / 135;
        private readonly int editsWidth = 240;
        private readonly int valuesWidth = 180;
        private readonly int statesWidth = 180;
        private readonly int height = Screen.height;
        private readonly int top = 50;
        private readonly int left = 50;
        private readonly int playerStatsOffSet = 240;
        private GUIStyle guiStyle;

        // Coroutines
        private Coroutine coolerRoutine;
        private Coroutine turboRoutine;
        private Coroutine pipeRoutine;
        private Coroutine surgeRoutine;

        #endregion

        #region Properties

        // BOOL
        internal bool isPiped => carbInstall || raceCarbInstall;
        internal bool isFiltered => filter.installed || highFlowFilter.installed;
        internal bool isExhaust => headers.installed && turbo.installed && downPipeRace.installed;
        internal bool carbInstall => canCarbWork && carbPipe.installed;
        internal bool raceCarbInstall => raceCarbInstalled.Value && coldSidePipe.installed && intercooler.installed && hotSidePipe.installed;
        internal bool engineOn => engine.activeInHierarchy;
        internal bool canTurboWork => headers.installed && turbo.installed && !destroyed && engineOn;
        internal bool canOilCoolerWork => oilCooler.installed && engineOn;
        internal bool canPipeWork => canTurboWork && isPiped;
        internal bool onThrottlePedal => throttlePedalPosition > 0.1f;
        internal bool turboSurging => PSI > 0 && drivetrain.idlethrottle == 0 && !onThrottlePedal;
        internal bool canCarbWork => stockCarbInstalled.Value && carbParts.throttleBodyAssembly.installed && carbParts.bowlAssembly.installed && carbParts.bowlCoverAssembly.installed;
        // FLOAT
        internal float turboHeight => transform.position.y;
        internal float turboFrictionCo => 0.03f + (wear * 0.001f);
        /// <summary>
        /// throttle pedal postion 0 - 1 float.
        /// </summary>
        internal float throttlePedalPosition => throttlePedal.Value / 8;
        internal float maxBoostFuel => (carbInstall ? 22.2f : (raceCarbInstall ? 36 : 0)) * fuelPumpEfficiency.Value * 75 + 0.25f;

        internal SimulationSaveData defaultSaveData => new SimulationSaveData() { turboDestroyed = false, turboWear = Random.Range(75, 100) + (Random.Range(0, 100) * 0.001f), wastegatePsi = 4.75f };

        #endregion

        #region Unity runtime methods.

        private void OnEnable()
        {
            #region remove events
            // REMOVE

            // carb install check
            carbPipe.onAssemble -= onCarbSetup;
            carbPipe.onDisassemble -= onCarbSetup;
            // race carb install check
            coldSidePipe.onAssemble -= onCarbSetup;
            coldSidePipe.onDisassemble -= onCarbSetup;
            hotSidePipe.onAssemble -= onCarbSetup;
            hotSidePipe.onDisassemble -= onCarbSetup;
            intercooler.onDisassemble -= onCarbSetup;
            intercooler.onAssemble -= onCarbSetup;

            // Boost gauge needle
            boostGauge.onAssemble -= boostGaugeNeedleReset;
            boostGauge.onDisassemble -= boostGaugeNeedleReset;

            // oil cooler pipes
            oilCooler.onAssemble -= oilCoolerOnAssemble;
            oilCooler.onDisassemble -= oilCoolerOnDisassemble;

            // turbo fan rpm reset
            headers.onDisassemble -= turboRpmReset;
            turbo.onDisassemble -= turboRpmReset;

            // turbo fan active check
            filter.onAssemble -= turboFanCheck;
            filter.onDisassemble -= turboFanCheck;
            highFlowFilter.onAssemble -= turboFanCheck;
            highFlowFilter.onDisassemble -= turboFanCheck;
            downPipeRace.onAssemble -= turboFanCheck;
            downPipeRace.onDisassemble -= turboFanCheck;
            downPipeStraight.onAssemble -= turboFanCheck;
            downPipeStraight.onDisassemble -= turboFanCheck;
            headers.onAssemble -= turboFanCheck;
            headers.onDisassemble -= turboFanCheck;
            turbo.onAssemble -= turboFanCheck;
            turbo.onDisassemble -= turboFanCheck;

            // carb pipe -><- stock air filter trigger toggle
            carbPipe.onAssemble -= stockAirFilterTriggerToggle;
            carbPipe.onDisassemble -= stockAirFilterTriggerToggle;

            // headers -><- turbo headers trigger toggle
            headers.onAssemble -= headersTriggerToggle;
            headers.onDisassemble -= headersTriggerToggle;

            // down pipe trigger transform switch
            downPipeRace.onAssemble -= downPipeTriggerSwitch;
            downPipeStraight.onAssemble -= downPipeTriggerSwitch;

            #endregion

            #region assign events

            // ASSIGN

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
            downPipeRace.onAssemble += turboFanCheck;
            downPipeRace.onDisassemble += turboFanCheck;
            downPipeStraight.onAssemble += turboFanCheck;
            downPipeStraight.onDisassemble += turboFanCheck;
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

            // down pipe trigger transform switch
            downPipeRace.onAssemble += downPipeTriggerSwitch;
            downPipeStraight.onAssemble += downPipeTriggerSwitch;

            #endregion
        }
        private void Start()
        {
            initSimulation();
            initEngineState();            
        }

        private void Update()
        {
            dT = Time.deltaTime;

            guiToggle();
            turboCondCheck();
            wasteGateAdjust();
            updateTurboMesh();
        }

        private void OnGUI()
        {
            try
            {
                if (GUIdebug)
                {
                    guiStyle = new GUIStyle
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(0, 0, 0, 0)
                    };
                    guiStyle.normal.textColor = Color.white;
                    guiStyle.hover.textColor = Color.blue;
                    guiStyle.fontSize = fontSize;

                    // values
                    using (new GUILayout.AreaScope(new Rect(left, top, valuesWidth, height - top), "", guiStyle))
                    {
                        drawProperty("VALUES");
                        using (new GUILayout.VerticalScope())
                        {
                            string surgingText = "<color=" + (turboSurging ? "red>SURGE" : "green>OK") + "</color>";
                            drawProperty("Turbo Stats\n" +
                                $"- {PSI.round(2)}psi\n" +
                                $"- {turboRPM.round()}rpm\n" +
                                $"- {timeBoosting.round(2)}seconds\n" +
                                $"- Starv: {maxBoostFuel.round(3)}psi\n" +
                                $"- TGT: {turboTargetRPM.round(2)}\n" +
                                $"- Spool: {turboSpool.round(2)}\n" +
                                $"- Friction: {turboFriction.round(2)}\n" +
                                $"- Friction Co: {turboFrictionCo.round(3)}\n" +
                                $"- Load: {turboLoad.round(2)}");
                            drawProperty("Wastegate Stats\n" +
                                $"- {Math.Round(wastegateRPM)}rpm\n" +
                                $"- {wastegatePSI.round(2)}psi\n" +
                                $"- Spool: {wastegateSpool.round(2)}");
                            drawProperty("Air-Flow Stats\n" +
                                $"- atmospheric pressure: {atmosphericPressure.round(2)}psi\n" +
                                $"- system depression: {systemDepression.round(2)}psi\n" +
                                $"- pressure ratio: {pressureRatio.round(2)}\n" +
                                $"- air flow rate: {airFlowRate.round(2)} cc\"s\n" +
                                $"- surge rate: {calculatedSurgeRate.round(2)} cc\"s\n" +
                                $"- height: {turboHeight.round(2)}m\n" +
                                $"- inlet: {inletPressure.round(2)}psi\n" +
                                $"- outlet: {outletPressure.round(2)}psi");
                            drawProperty("Air-Fuel\n" +
                                $"- Mixture Discrepancy: {mixtureDiscrepancy.round(3)}\n" +
                                $"- Mixture Multiplier5: {calculateMixtureMultipler5?.Value.round(3) ?? 0}\n" +
                                $"- Air Density: {airDensity.Value.round(3)}\n" +
                                $"- Factor: {afFactor.round(3)}\n" +
                                $"- Ratio: {afr.round(2)}\n" +
                                $"- Median: {mixtureMedian.round(2)}\n" +
                                $"- Ideal: {checkMixtureRich?.float2.Value.round(2) ?? -1} ~ {checkMixtureLean?.float2.Value.round(2) ?? -1}\n" +
                                $"- Fuel Pump Efficiency", fuelPumpEfficiency.Value);
                            drawProperty("Engine Stats\n" +
                                $"- {drivetrain.rpm.round(2)}rpm\n" +
                                $"- {satsumaSpeedKmh.Value.round(2)}Km/h\n" +
                                $"- {(drivetrain.torque / LB2NM).round(2)}Nm\n" +
                                $"- {(drivetrain.currentPower / HP2KW).round(2)}Kw\n" +
                                $"- {engineTemp.Value.round(2)}°C\n" +
                                $"- +{calculatedEngineHeat}°C per frame\n" +
                                $"- Inertia: {drivetrain.engineInertia.round(3)}\n" +
                                $"- Max Torque RPM: {drivetrain.maxTorqueRPM.round()}\n" +
                                $"- Max Power RPM: {drivetrain.maxPowerRPM.round()}\n" +
                                $"- Power Multi: {drivetrain.powerMultiplier.round(3)}" + (drivetrain.powerMultiplier == calculatedPowerMultiplier ? "" : $" / {calculatedPowerMultiplier.round(3)}"));
                            drawProperty("Oil Stats\n" +
                                $"- Level: {oilLevel.round(3)}\n" +
                                $"- Dirt: {oilDirt.round(3)}");
                            drawProperty("Carb Stats\n" +
                                $"- Bowl Level: {carbBowl.round(3)}");
                            drawProperty("SparkTiming Stats\n" +
                                $"- Angle: {timingMultiplier?.float1.Value ?? -1}\n" +
                                $"- Timing Multiplier: {sparkTimingMultiplier?.Value ?? -1}\n" +
                                $"- ");

                            drawProperty($"Throttle: {throttlePedalPosition.round(3)} / {drivetrain.throttle},\nIdle Throttle: {drivetrain.idlethrottle}");
                            drawProperty("Ambient Temp", ambientTemperature.round(2));
                            drawProperty("Rev Limiter Time", drivetrain.revLimiterTime.round(2));
                        }
                    }
                    //edits 1
                    using (new GUILayout.AreaScope(new Rect((left * 2) + valuesWidth, playerStatsOffSet, editsWidth, height - playerStatsOffSet), "", guiStyle))
                    {
                        drawProperty("EDITS 1");
                        using (new GUILayout.VerticalScope())
                        {
                            // floats
                            drawPropertyEdit("Oil Cooling Rate", ref oilCoolerCoolingRate);
                            drawPropertyEdit("Oil Themostat rating", ref oilCoolerThermostatRating);
                            drawPropertyEdit("Max Boost RPM", ref maxBoostRpm);
                            drawPropertyEdit("Engine Heat Factor", ref turboEngineHeatFactor);
                            drawPropertyEdit("Relative Humidity", ref relativeHumidity);
                            if (!applyCalculatedPowerBand)
                            {
                                drawPropertyEdit("max torque rpm", ref drivetrain.maxTorqueRPM);
                                drawPropertyEdit($"Power RPM Diff: {powerToTorqueRPMDifference}\nmax power rpm", ref drivetrain.maxPowerRPM);
                            }
                            else
                            {
                                drawPropertyEdit("Min Torque RPM", ref minTorqueRpm);
                                drawPropertyEdit($"Min Power RPM {minPowerRpm}\nPower RPM Diff", ref powerToTorqueRPMDifference);
                            }
                            //drawPropertyEdit("Wastegate PSI", ref loadedSaveData.wastegatePsi);
                            drawPropertyEdit("Turbo Wear", ref loadedSaveData.turboWear);
                            drawPropertyEdit("Turbo Surge Co", ref turboSurgeCo);
                            drawPropertyEdit("Air Flow Co", ref airFlowCoEff);
                            drawPropertyEdit("Min Rev Limit Time", ref minRevLimitTime);
                            drawPropertyEdit("Vent extention mixture discrepancy", ref ventExtentionMixtureDiscrepancyModifier);
                        }
                    }
                    //edits 2 (new column)
                    using (new GUILayout.AreaScope(new Rect((left * 3) + valuesWidth + editsWidth, top, editsWidth, height - top), "", guiStyle))
                    {
                        drawProperty("EDITS 2");
                        using (new GUILayout.VerticalScope())
                        {
                            if (systemDepressionOverrideActive)
                                drawPropertyEdit("System Depression", ref systemDepressionOverrideValue);
                            // bools
                            drawPropertyBool("Apply power multipler calculation?", ref applyCalculatedPowerMultipler);
                            drawPropertyBool("Apply density calculation?", ref applyCalculatedDensity);
                            drawPropertyBool("Apply combustion calculation?", ref applyCalculatedCombustion);
                            drawPropertyBool("Apply power band calculation?", ref applyCalculatedPowerBand);
                            drawPropertyBool("Apply check mixture?", ref applyCheckMixture);
                            drawPropertyBool("Apply Timing?", ref applyTiming);
                            drawPropertyBool("Apply system Depression Override?", ref systemDepressionOverrideActive);
                            drawPropertyBool("Enable air density tests?", ref enableAirDensityTest);
                            drawPropertyBool("Enable Wear?", ref wearEnabled);
                            drawPropertyBool("Turbo Destroyed?", ref loadedSaveData.turboDestroyed);
                            drawPropertyBool("Apply Rev Limit Time Cal?", ref applyRevLimitTime);
                            drawPropertyBool("Reset Values on coroutine end?", ref resetValues);
                            drawPropertyBool("Apply fuel starve event?", ref applyFuelStarveEvent);
                            drawPropertyEnum(ref airDensityMethod);

                            drawProperty("My Physic Materials");
                            MyPhysicMaterial[] mpm = Database.databaseVehicles.satsuma.carDynamics.physicMaterials;
                            for (int i = 0; i < mpm.Length; i++)
                            {
                                drawProperty("MPM " + i + 1);
                                drawPropertyEdit("Rolling Friction", ref mpm[i].rollingFriction);
                                drawPropertyEdit("Static Friction", ref mpm[i].staticFriction);
                                drawPropertyEdit("Grip", ref mpm[i].grip);
                                drawPropertyEnum(ref mpm[i].surfaceType);
                            }
                            Database.databaseVehicles.satsuma.carDynamics.physicMaterials = mpm;
                        }
                    }
                    // states
                    using (new GUILayout.AreaScope(new Rect((left * 4) + valuesWidth + (editsWidth * 2), top, statesWidth, height - top), "", guiStyle))
                    {
                        drawProperty("ENGINE & TURBO STATE");
                        GUILayout.Space(10);
                        using (new GUILayout.VerticalScope())
                        {
                            if (isPiped)
                                drawProperty($"Fuel: <color={(PSI > maxBoostFuel ? "red>STARVING</color>" : "green>OK</color>")} {maxBoostFuel}");
                            drawProperty(!isPiped ? "<color=yellow>Not Piped</color>" : $"<color=blue>Piped</color> | {(canPipeWork ? "<color=green>Working</color>" : "<color=yellow>Idle</color>")}");
                            drawProperty(!(turbo.installed && headers.installed) ? "<color=yellow>Non Turbo</color>" : $"<color=blue>Turbo</color> | {(canTurboWork ? "<color=green>Working</color>" : "<color=yellow>Idle</color>")}");
                            drawProperty("<color=" + (!isExhaust ? "yellow>turbo setup != exhaust</color>" : "blue>turbo == exhaust</color>"));
                            drawProperty("<color=" + (!isFiltered ? "yellow>Unfiltered</color>" : "blue>Filtered</color>"));
                            drawProperty($"Air-Fuel: <b>{printAFState()}</b>");
                            drawProperty($"Turbine: <color={(turbineModel.activeInHierarchy ? $"green>Active" : "yellow>Inactive")}</color> | Turbo Spin Rot: {turboSpin}");
                            drawProperty(stockCarbInstalled.Value ? $"Carb setup: {(carbInstall ? "OK" : canCarbWork ? "FUNCTIONAL" : "NOT WORKING")}" : raceCarbInstalled.Value ? $"Race carb setup: {(raceCarbInstall ? "OK" : "NOT WORKING")}" : "No Carb Found");
                            drawProperty($"Clutch: <color={(drivetrain.torque > drivetrain.clutch.maxTorque ? "red>SLIPPING" : "green>OK")}</color>");
                            drawProperty($"Clutch:\n" +
                                $"Speed Difference: {drivetrain.clutch.speedDiff}\n" +
                                $"Max Torque: {drivetrain.clutchMaxTorque}\n" +
                                $"Torque Multiplier: {drivetrain.clutchTorqueMultiplier}\n" +
                                $"Drag Impulse: {drivetrain.clutchDragImpulse}");
                        }
                        drawProperty("ROUTINES");
                        GUILayout.Space(10);
                        using (new GUILayout.VerticalScope())
                        {
                            drawProperty($"Oil Cooler routine: <color={(coolerRoutine is null ? "yellow>Inactive" : "green>Active")}</color>");
                            drawProperty($"Turbo routine: <color={(turboRoutine is null ? "yellow>Inactive" : "green>Active")}</color>");
                            drawProperty($"Pipe routine: <color={(pipeRoutine is null ? "yellow>Inactive" : "green>Active")}</color>");
                            drawProperty($"Surge routine: <color={(surgeRoutine is null ? "yellow>Inactive" : "green>Active")}</color>");
                        }
                    }
                    // Tests
                    if (enableAirDensityTest)
                    {
                        using (new GUILayout.AreaScope(new Rect((left * 5) + valuesWidth + (editsWidth * 2) + statesWidth, top, 300, height - top), "", guiStyle))
                        {
                            drawProperty("Saturation Vapour (ambient)", calculateSaturationVapourPressure(ambientTemperature.Value) * relativeHumidity);
                            drawProperty("Saturation Vapour (engine)", calculateSaturationVapourPressure(engineTemp.Value) * relativeHumidity);
                            drawProperty("Dew Point (ambient)", calculateDewPoint(ambientTemperature.Value, relativeHumidity));
                            drawProperty("Dew Point (engine)", calculateDewPoint(engineTemp.Value, relativeHumidity));
                        }
                    }
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

            if (cInput.GetButtonDown("DrivingMode") && cInput.GetButton("Finger"))
            {
                GUIdebug = !GUIdebug;
            }
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
            checkAnyConflictingPart();
            updateCarbSetup();
            stockAirFilterTriggerToggle();
            headersTriggerToggle();
            boostGaugeNeedleReset();
            turboFanCheck();
        }
        private void initSimulation()
        {
            try
            {
                #region game field assignments 

                GameObject cylinderHead = Database.databaseMotor.cylinderHead;
                GameObject carburator = Database.databaseMotor.carburator;
                oilLevel = Database.databaseMotor.oilPan.oilLevel;
                oilDirt = Database.databaseMotor.oilPan.oilContamination;
                stockCarbInstalled = Database.databaseMotor.carburator.installed;
                stockFilterInstalled = Database.databaseMotor.airFilter.installed;
                GameObject satsuma = GameObject.Find("SATSUMA(557kg, 248)");
                carSimulation = satsuma.transform.Find("CarSimulation").gameObject;
                GameObject starter = carSimulation.transform.Find("Car/Starter").gameObject;
                satsumaSpeedKmh = PlayMakerGlobals.Instance.Variables.FindFsmFloat("SpeedKMH");
                playerStress = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerStress");
                ambientTemperature = PlayMakerGlobals.Instance.Variables.FindFsmFloat("AmbientTemperature");
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
                headerTriggers = cylinderHead.transform.FindChild("Triggers Headers").gameObject;
                stockFilterTrigger = carburator.transform.FindChild("trigger_airfilter").gameObject;
                turbineModel = GameObject.Find("motor_turbocharger_blades");
                fuelGo = engine.transform.Find("Fuel").gameObject;
                fuel = fuelGo.GetComponent<PlayMakerFSM>();
                fuelEvent = fuelGo.GetComponents<PlayMakerFSM>()[1];
                swear = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SpeakDatabase").GetComponent<PlayMakerFSM>();
                mechanicalWear = carSimulation.transform.Find("MechanicalWear").GetComponent<PlayMakerFSM>();
                headGasketWear = mechanicalWear.FsmVariables.GetFsmFloat("WearHeadgasket");
                fuelPumpEfficiency = fuel.FsmVariables.GetFsmFloat("FuelPumpEfficiency");
                mixture = fuelEvent.FsmVariables.FindFsmFloat("Mixture");
                airDensity = fuelEvent.FsmVariables.FindFsmFloat("AirDensity");
                carbBowl = fuel.FsmVariables.GetFsmFloat("CarbReserve");
                raceCarbInstalled = GameObject.Find("Database/DatabaseOrders/Racing Carburators").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                stockHeadersInstalled = Database.databaseMotor.headers.installed;
                raceHeadersInstalled = GameObject.Find("Database/DatabaseOrders/Steel Headers").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                raceexhaustinstalled = GameObject.Find("Database/DatabaseOrders/Racing Exhaust").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                racemufflerinstalled = GameObject.Find("Database/DatabaseOrders/Racing Muffler").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
                turboMesh = turbo.transform.Find("turbomesh").gameObject;
                turboMeshDestoryed = turbo.transform.Find("turbomesh_D").gameObject;
                throttlePedal = satsuma.transform.Find("Dashboard/Pedals/pedal_throttle").GetComponent<PlayMakerFSM>().FsmVariables.GetFsmFloat("Data");

                //GameObject throttleLinkage = GameObject.Find("carburator(Clone)/throttle linkage(xxxxx)");
                //throttleLinkage.GetPlayMakerState("Screw").GetAction<SetProperty>(4);

                numOfPistonsOK = engine.transform.Find("Combustion").gameObject.GetPlayMaker("Cylinders").FsmVariables.GetFsmInt("Power");

                #endregion

                #region Carb install set up/check/inject

                // Carb install set up check inject
                cylinderHead.transform.Find("Triggers Carbs/trigger_carburator").gameObject.FsmInject("Assemble", onCarbSetup);
                cylinderHead.transform.Find("Triggers Carbs/trigger_carburator_racing").gameObject.FsmInject("Assemble", onCarbSetup);
                carburator.FsmInject("Remove part", onCarbSetup);

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
                starter.injectAction("Starter", "Start engine", InjectEnum.append, onEngineCrankUp);

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
        private void fuelStarveEvent(bool force = false)
        {
            if (timeBoosting > Random.Range(3, 12.6f) || force)
            {
                drivetrain.revLimiterTriggered = true;
                exhaustCrackle(true);
                if (wearEnabled)
                    wear += Random.Range(0.3f, 3f);
            }
        }
        private void carbPipeBlowOffChance()
        {
            float t = carbPipe.bolts[0].loadedSaveInfo.boltTightness;
            float mT = carbPipe.bolts[0].boltSettings.maxTightness;
            float chance = 1 - t / mT;
            if (chance > 0)
            {
                if (PSI.round() > Random.Range(3f, 18))
                {
                    if (carbPipeBlowOffChanceFactor * chance > Random.Range(0f, 1))
                    {
                        carbPipe.disassemble(true);
                        carbPipe.cachedRigidBody.AddForce(Vector3.up * PSI * carbPipeBlowOffForceFactor, ForceMode.Impulse);
                    }
                }
            }
        }
        private string printAFState()
        {
            string rv;
            if (engineCranked)
            {
                rv = "<color=";
                if (afr >= checkMixtureLean.float2.Value)
                    rv += "red>Lean";
                else if (afr >= checkMixtureRich.float2.Value)
                    rv += "green>Optimal";
                else if (afr >= checkMixtureSuperRich.float2.Value)
                    rv += "yellow>Rich";
                else if (afr >= checkMixtureSputter.float2.Value)
                    rv += "orange>Super Rich";
                else if (afr >= checkMixtureOff.float2.Value)
                    rv += "red>Sputter";
                else if (afr < checkMixtureOff.float2.Value)
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
        private void turboCondCheck()
        {
            if (turbineModel.gameObject.isPlayerLookingAt())
            {
                guiUse = true;
                turbineModel.transform.localEulerAngles = new Vector3(turboSpin, 0f, 0f);
                if ((isFiltered && !isExhaust) || (isExhaust && !isFiltered))
                {
                    if (cInput.GetButtonUp("Use"))
                    {
                        InteractText.Value = "Can't check with the" + (isFiltered ? "filter" : "down pipe") + " on!";
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
                            if (wear > 99 || destroyed)
                            {
                                InteractText.Value = "There's nothing left to check...";
                                swear.SendEvent("SWEARING");
                            }
                            else if (wear > 65)
                            {
                                InteractText.Value = "Feels worn out, a bit of shaft play";
                                MasterAudio.PlaySound3DAndForget("Motor", turbo.transform, variationName: "damage_bearing");
                            }
                            else if (wear > 30)
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
            turbo.transform.Find("handgrind").gameObject.SetActive(true);

            getPOV.GetPlayMaker("Death").SendEvent("DEATH");

            GameObject systems = GameObject.Find("Systems");
            PlayMakerFSM death = systems.transform.Find("Death").GetComponent<PlayMakerFSM>();
            death.FsmVariables.GetFsmBool("RunOver").Value = true;
            death.FsmVariables.GetFsmBool("Crash").Value = false;
            systems.transform.Find("Death/GameOverScreen/Paper/HitAndRun/TextEN").GetComponent<TextMesh>().text = "Boy's \n hand eaten \n by\n turbo";
            systems.transform.Find("Death/GameOverScreen/Paper/HitAndRun/TextFI").GetComponent<TextMesh>().text = "Pojan käsi tuhoutui \n turboahtimella";
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
                        wastegatePSI += WASTEGATE_ADJUST_INTERVAL / RPM2PSI;
                        MasterAudio.PlaySound3DAndForget("CarBuilding", act.transform, variationName: "bolt_screw");
                    }
                    if (Input.mouseScrollDelta.y < 0 && wastegateRPM >= MIN_WASTEGATE_RPM)
                    {
                        wastegatePSI -= WASTEGATE_ADJUST_INTERVAL / RPM2PSI;
                        MasterAudio.PlaySound3DAndForget("CarBuilding", act.transform, variationName: "bolt_screw");
                    }
                    if (cInput.GetButtonDown("Finger") && wastegateRPM <= MAX_WASTEGATE_RPM)
                    {
                        wastegatePSI = MAX_WASTEGATE_RPM / RPM2PSI;
                        MasterAudio.PlaySound3DAndForget("CarBuilding", act.transform, variationName: "bolt_screw");
                    }
                }
            }
            else if (InteractText.Value != string.Empty)
                InteractText.Value = string.Empty;
        }
        private void updateTurboMesh()
        {
            // Written, 18.09.2021

            if (wear > 55f)
            {
                if (!turboMeshDestoryed.activeInHierarchy)
                {
                    turboMeshDestoryed.SetActive(true);
                    turboMesh.SetActive(false);
                }
            }
            else if (!turboMesh.activeInHierarchy)
            {
                turboMeshDestoryed.SetActive(false);
                turboMesh.SetActive(true);
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
            soundSpool.SetActive(true);
            turboWhistle.pitch = Mathf.Lerp(1.9f, 2.82f, turboLoad);
            turboWhistle.volume = Mathf.Lerp(0, isFiltered ? 0.4f : 0.7f, turboLoad);
        }
        private void boostGaugeNeedleUpdate()
        {
            vaccummSmooth = PSI.round() > 1 ? onThrottlePedal ? 117 : drivetrain.idlethrottle * 117 : 0;
            vaccummSmooth = Mathf.Clamp(vaccummSmooth, drivetrain.idlethrottle * 117, 117);
            vaccummThrottle = Mathf.SmoothDamp(vaccummThrottle, vaccummSmooth, ref throttleSmooth, 0.05f);
            boostGaugeNeedle = 133 + -vaccummThrottle + -(PSI * RPM2PSI) / 1600;
            boostNeedleVector.z = boostGaugeNeedle;
            turboNeedleObject.transform.localEulerAngles = boostNeedleVector;
        }
        private void turboDestroyEvent()
        {
            wear = 100;
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
            wear = Random.Range(0.5f, 9f);
            turboFanCheck();
        }
        private void calculateSystemDepression()
        {
            if (systemDepressionOverrideActive)
                systemDepression = systemDepressionOverrideValue;
            else
                systemDepression = (filter.installed ? 0.25f : highFlowFilter.installed ? 0.15f : 0) + (carbInstall ? carbParts.chokeFlap.installed ? 0.2f : 0.13f : raceCarbInstall ? 0.1f : 0);//(filter.installed ? 1.15f : highFlowFilter ? 0.75f : 0) + (carbInstall ? 1.12f : raceCarbInstall ? 1.28f : 0);
            //systemDepressionRpm = systemDepression * RPM2PSI;
        }
        private void resetTurboValues()
        {
            // Written, 26.02.2022

            soundSpool.SetActive(false);
            turboRPM = 0;
            turboSpool = 0;
            turboFriction = 0;
            turboLoad = 0;
            wastegateSpool = 0;
        }
        private void spinTurbo()
        {
            // Written, 25.04.2022

            turboSpin += turboRPM / MAX_WASTEGATE_RPM * 359;
            if (turbineModel.activeInHierarchy)
                turbineModel.transform.localEulerAngles = new Vector3(turboSpin, 0f, 0f);
            if (turboSpin > 359)
                turboSpin = 0;
        }
        private void calculateCombusion()
        {
            float rich = checkMixtureRich.float2.Value;
            float afm = checkMixtureAirFuelMixture.float2.Value;
            mixtureMedian = ((checkMixtureLean.float2.Value - rich) / 2) + rich;

            afr = mixture.Value * afm;

            if (afr > mixtureMedian)
                afFactor = afm / afr;
            else if (afr > rich)
                afFactor = mixtureMedian / afr;
            else
                afFactor = afr / afm;
        }
        private void checkMixture(bool pipeWorking)
        {
            if (pipeWorking)
            {
                checkMixtureAirFuelMixture.float2 = 12.7f;
                checkMixtureLean.float2 = 15f;
                checkMixtureRich.float2 = 11.3f;
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
        private void onCalculateDensity(bool pipeWorking)
        {
            calDensityFloatOp.float1.Value = 0.36f;
            if (pipeWorking)
            {
                float pv;
                switch (airDensityMethod)
                {
                    case CalculateAirDensityEnum.saturationVapour:
                    default:
                        pv = calculateSaturationVapourPressure(engineTemp.Value) * relativeHumidity;
                        break;
                    case CalculateAirDensityEnum.dewPoint:
                        pv = calculateDewPoint(engineTemp.Value, Mathf.Clamp(relativeHumidity, 0.01f, 1));
                        break;
                }
                calDensityFloatOp.float1.Value = calculateAirDensity(outletPressure, pv);
            }
        }
        private void updateCaculateMixutureOperation(bool pipeWorking)
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
        private void updateCalculateDensityClamp(bool pipeWorking)
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
        private void calculatePowerBand(bool pipeWorking)
        {
            if (applyCalculatedPowerBand)
            {
                applyTiming = false;

                float torque;
                float power;

                if (pipeWorking)
                {
                    float cal = Mathf.Lerp(STOCK_MAX_POWER_RPM, minTorqueRpm, turboLoad);
                    minPowerRpm = (minTorqueRpm + powerToTorqueRPMDifference) / afFactor;

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
        }
        private void calculateEngineHeat()
        {
            // Written, 21.03.2022

            if (engineOn)
            {
                calculatedEngineHeat = drivetrain.powerMultiplier * 4f * throttlePedalPosition * turboEngineHeatFactor;
                engineTemp.Value += calculatedEngineHeat;
            }
        }
        private void timing(bool pipeWorking)
        {
            // Written,  04.05.2022

            if (applyTiming)
            {
                if (pipeWorking)
                {
                    applyCalculatedPowerBand = false;

                    timingAngleAdvanced.float2 = 17;
                    timingAngleRetarded.float2 = 9.25f;
                    if (timingMultiplier.float1.Value < 13)
                        timingMultiplier.float2 = 16;
                    else
                        timingMultiplier.float2 = 15f - airDensity.Value;
                }
                else
                {
                    timingAngleAdvanced.float2 = 15f;
                    timingAngleRetarded.float2 = 5f;
                    timingMultiplier.float2 = 15f;
                    drivetrain.maxTorqueRPM = STOCK_MAX_TORQUE_RPM;
                    drivetrain.maxPowerRPM = STOCK_MAX_POWER_RPM;
                    powerToTorqueRPMDifference = 2000;
                }
            }
        }
        private float graphCal(float startPower, float endPower, float currentRpm, float atRpm, float prevAtRpm)
        {
            return Mathf.Lerp(startPower, endPower, (currentRpm - prevAtRpm) / (atRpm - prevAtRpm));
        }
        private float[,] createRpmGraph()
        {
            float[,] rpmGraph = new float[8000, 2];
            rpmGraph[0, 0] = 0;
            rpmGraph[0, 1] = 1;
            for (int i = 1; i < rpmGraph.GetLength(0); i++)
            {
                rpmGraph[i, 0] = i;
                if (i < 1750)
                {
                    rpmGraph[i, 1] = graphCal(1, 1.25f, i, 1750, 1);
                }
                else if (i < 3500)
                    rpmGraph[i, 1] = graphCal(1.25f, 1.7f, i, 3500, 1750);
                else if (i < 6000)
                    rpmGraph[i, 1] = graphCal(1.7f, 2.3f, i, 6000, 3500);
                else if (i < 8000)
                    rpmGraph[i, 1] = graphCal(2.3f, 1.12f, i, 8000, 6000);
                else
                    rpmGraph[i, 1] = graphCal(1.12f, 0.875f, i, 10500, 8000);
            }
            return rpmGraph;
        }
        private float calculateValueFromGraph(float[,] graph, float rpm)
        {
            float subValue = Mathf.Abs(graph[0, 0] - rpm);
            int closestIndex = 0;
            for (int i = 1; i < graph.GetLength(0); i++)
            {
                if (Mathf.Abs(graph[i, 0] - rpm) < subValue)
                {
                    subValue = Mathf.Abs(graph[i, 0] - rpm);
                    closestIndex = i;

                    if (closestIndex == rpm)
                        break;
                }
            }

            return graph[closestIndex, 1];
        }
        private float[,] createPowerToTorqueRPMDifferenceGraph() 
        {
            // Written, 21.05.2022

            float[,] graph = new float[8000, 2];
            graph[0, 0] = 0;
            graph[0, 1] = 1;
            for (int i = 1; i < graph.GetLength(0); i++)
            {
                graph[i, 0] = i;
                if (i < 1750)
                {
                    graph[i, 1] = graphCal(2000, 1800, i, 1750, 1);
                }
                else if (i < 3500)
                    graph[i, 1] = graphCal(1800, 1575, i, 3500, 1750);
                else if (i < 6000)
                    graph[i, 1] = graphCal(1575, 1400, i, 6000, 3500);
                else if (i <= 8000)
                    graph[i, 1] = graphCal(1400, 2200, i, 8000, 6000);
                else
                    graph[i, 1] = graphCal(2200, 3000, i, 10500, 8000);
            }
            return graph;
        }
        private void calculateRevLimitTime(bool pipeWorking)
        {
            // Written, 28.05.2022
            
            if (pipeWorking)
                if (applyRevLimitTime)
                    drivetrain.revLimiterTime = Mathf.Lerp(0.2f, minRevLimitTime, drivetrain.rpm < 8300 ? turboLoad : 0);   
            else
                drivetrain.revLimiterTime = 0.2f;
        }

        // INTERNAL
        /// <summary>
        /// Calculates saturation vapour pressure at a given temperature. (C)
        /// </summary>
        /// <param name="T">The temperature</param>
        internal float calculateSaturationVapourPressure(float T)
        {
            // Modified, 27.02.2022

            // Tetens equation
            // p = 0.61078exp(17.27 * T / (T + 237.3)) in kilopascals for greater then 0 temp c
            // p = 0.61078exp(21.875 * T / (T + 265.5)) in kilopascals for less then 0 temp c

            float resultKiloPascals;
            if (T > 0)
                resultKiloPascals = 0.61078f * Mathf.Log10(17.27f * T / (T + 237.3f));
            else
                resultKiloPascals = 0.61078f * Mathf.Log10(21.875f * T / (T + 265.5f));

            return resultKiloPascals * 6.89476f; // convert kilopascal to psi
        }
        /// <summary>
        /// Calculates Dew Point at a given temperature. (C)
        /// </summary>
        /// <param name="temp">The temperature in cel</param>
        /// <param name="relativeHumidity">Relative Humidity. Range:(0 - 1).</param>
        internal float calculateDewPoint(float temp, float relativeHumidity)
        {
            // DP = 243.12 * α / (17.62 - α)
            // α = ln(RH/100) + 17.62 * T / (243.12 + T).

            float a = Mathf.Log(relativeHumidity / 100) + 17.62f * temp / (243.12f + temp);

            float DP = 243.12f * a / (17.62f - a); // calculated dew point

            return DP;
        }
        /// <summary>
        /// Calculates air density at a given pressure (PSI) 
        /// </summary>
        /// <param name="pressure">The pressure to calculate air density at. (PSI)</param>
        /// <param name="_waterVapourTempC">Water Vapour pressure</param>
        internal float calculateAirDensity(float pressure, float waterVapourPressure)
        {
            // FORMULA: ρ = (pd / (Rd * T)) + (pv / (Rv * T))

            float tempKelvin = ambientTemperature.Value + KELVIN;

            float dryPressure = pressure - waterVapourPressure;

            float waterVapourPascals = waterVapourPressure  * PASCAL; // water vapour in pascals
            float dryPressurePascals = dryPressure * PASCAL; // pressure in pascals

            float result = (dryPressurePascals / (DRY_GAS_CONSTANT * tempKelvin)) + (waterVapourPascals / (WET_GAS_CONSTANT * tempKelvin)); // in kilogram per meter cubic (Kg/m3)

            return result;
        }
        /// <summary>
        /// Calculates atmospheric pressure at the turbo's attitude.
        /// </summary>
        internal void calculateAtmosphericPressure()
        {
            // Written, 15.01.2022

            //barometric formula: P = P₀ exp(-gM(h - h₀) / (RT))
            atmosphericPressure = PRESSURE_REFERENCE_LEVEL * (float)Math.Exp(-GRAVITY * MOLAR_MASS_OF_AIR * (turboHeight - HEIGHT_REFERENCE_LEVEL) / (UNIVERSAL_GAS_CONSTANT * ambientTemperature.Value));
        }

        #endregion

        #region IEnumerators

        private IEnumerator pipeFunction()
        {
            while (canPipeWork)
            {
                calculateAtmosphericPressure();
                calculateSystemDepression();
                PSI = turboRPM / RPM2PSI;
                inletPressure = atmosphericPressure - systemDepression;
                outletPressure = PSI + atmosphericPressure;
                pressureRatio = outletPressure / inletPressure;
                calculatedPowerMultiplier = calculateValueFromGraph(rpmGraphPower, drivetrain.rpm) * (PSI / 22) * afFactor;
                if (applyCalculatedPowerMultipler)
                    drivetrain.powerMultiplier += calculatedPowerMultiplier;
                boostGaugeNeedleUpdate();
                calculateCombusion();
                onCalculateDensity(applyCalculatedDensity);
                updateCalculateDensityClamp(applyCalculatedDensity);
                updateCaculateMixutureOperation(true);
                checkMixture(applyCheckMixture);
                calculatePowerBand(true);
                carbPipeBlowOffChance();
                calculateEngineHeat();
                timing(true);
                calculateRevLimitTime(true);
                if (PSI.round() > 0)
                {
                    timeBoosting += Time.deltaTime;
                    if (applyFuelStarveEvent)
                    {
                        if (PSI > maxBoostFuel || afr > checkMixtureLean.float2.Value * 1.2f)
                        {
                            fuelStarveEvent(afr > checkMixtureLean.float2.Value * 1.65f);
                        }
                    }
                    if (!onThrottlePedal || afr < checkMixtureRich.float2.Value)
                    {
                        exhaustCrackle(afr < checkMixtureSputter.float2.Value);
                    }
                }
                if (turboSurging && surgeRoutine == null)
                    surgeRoutine = StartCoroutine(surgeFunction());

                yield return null;
            }
            if (resetValues)
            {
                onCalculateDensity(false);
                updateCalculateDensityClamp(false);
                updateCaculateMixutureOperation(false);
                checkMixture(false);
                calculatePowerBand(false);
                boostGaugeNeedleReset();
                timing(false);
                calculateRevLimitTime(false);

                PSI = 0;
                pressureRatio = 0;
                outletPressure = 0;
                inletPressure = 0;
            }
            pipeRoutine = null;
        }
        private IEnumerator turboFunction()
        {
            while (canTurboWork)
            {
                turboLoad = turboRPM / MAX_WASTEGATE_RPM;
                wastegateSpool = act.installed ? wastegateRPM / 22 - turboFrictionCo * 3 : MIN_WASTEGATE_RPM;
                turboTargetRPM = act.installed ? wastegateRPM : MIN_WASTEGATE_RPM;

               // turboSurgeCal = drivetrain.rpm > maxBoostRpm ? Mathf.Lerp(0, turboSurgeCo, drivetrain.rpm.mapValue(maxBoostRpm, 10500, 1, 2)) : 1;

                airFlowRate = (988 * drivetrain.rpm / 60 / 60 * airFlowCoEff * Mathf.Max(throttlePedalPosition, 0.05f) * numOfPistonsOK.Value / choke.Value * dT) / dT; // (cc/millisec) | engine displacement: 988cc                  
                turboFriction = (turboRPM * turboFrictionCo * dT) / dT;

                turboRPM = Mathf.Clamp(turboRPM + turboSpool, 0, turboTargetRPM);
                turboSpool = Mathf.Clamp(turboSpool + airFlowRate - turboFriction, -wastegateSpool, wastegateSpool);

                turboSounds();
                spinTurbo();

                yield return null;
            }
            if (resetValues)
                resetTurboValues();
            turboRoutine = null;
        }
        private IEnumerator oilCoolerFunction()
        {
            while (canOilCoolerWork)
            {
                if (engineTemp.Value > oilCoolerThermostatRating)
                {
                    engineTemp.Value -= (oilCoolerCoolingRate * dT) / dT;
                }
                yield return null;
            }
            coolerRoutine = null;
        }
        int flutterI = 0;
        private IEnumerator surgeFunction()
        {
            soundFlutter.SetActive(true);
            turboSpool = 0;
            timeBoosting = 0f;
            while (canTurboWork && turboSurging)
            {
                calculatedSurgeRate = PSI * turboSurgeCo;
                turboSpool -= calculatedSurgeRate;

                float rand = Random.Range(dT / 2f, dT * 2f);//Random.Range(0.01f, 0.09f);
                if (flutterI <= (PSI / 5).round() && dT > rand) 
                {
                    flutterI++;
                    if (turboFlutter.isPlaying)
                    {
                        turboFlutter.Stop();
                        yield return new WaitForSeconds(rand);
                    }
                    turboFlutter.Play();
                }

                turboFlutter.volume = turboWhistle.volume * (isFiltered ? 1.5f : 2f);
                turboFlutter.pitch = Mathf.Lerp(0.87f, 1.93f, turboLoad);
                turboFlutter.loop = false;
                turboFlutter.mute = false;
                yield return null;
            }
            if (canCarbWork)
                turboSpool = 0;
            flutterI = 0;
            soundFlutter.SetActive(false);
            playerStress.Value -= 0.2f;
            surgeRoutine = null;
        }

        private void checkRoutinesRunning()
        {
            if (coolerRoutine == null)
                if (canOilCoolerWork)
                    coolerRoutine = StartCoroutine(oilCoolerFunction());
            if (turboRoutine == null)
                if (canTurboWork)
                    turboRoutine = StartCoroutine(turboFunction());
            if (pipeRoutine == null)
                if (canPipeWork)
                    pipeRoutine = StartCoroutine(pipeFunction());
        }

        #endregion

        #region EventHandlers
        
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
                try
                {
                    engineCranked = true;

                    PlayMakerFSM mixtureFSM = fuelGo.GetPlayMaker("Mixture");
                    GameObject combustion = engine.transform.FindChild("Combustion").gameObject;
                    FsmState calMixture = mixtureFSM.GetState("Calculate mixture");
                    FsmState checkMixture = mixtureFSM.GetState("Check mixture");
                    FsmState calDensity = mixtureFSM.GetState("Calculate density");

                    // cal mixture inject
                    calMixtureFloatOp = calMixture.Actions[4] as FloatOperator;

                    // check mixture
                    checkMixtureLean = checkMixture.Actions[6] as FloatCompare;
                    checkMixtureRich = checkMixture.Actions[5] as FloatCompare;
                    checkMixtureSuperRich = checkMixture.Actions[4] as FloatCompare;
                    checkMixtureSputter = checkMixture.Actions[3] as FloatCompare;
                    checkMixtureOff = checkMixture.Actions[2] as FloatCompare;
                    checkMixtureAirFuelMixture = checkMixture.Actions[0] as FloatOperator;

                    // cal density
                    calDensityFloatOp = calDensity.Actions[5] as FloatOperator;
                    calDensityFloatClamp = calDensity.Actions[6] as FloatClamp;

                    // exhaust update
                    exhaust.injectAction("Logic", "Engine", InjectEnum.insert, updateExhaust, index: 5);

                    // exhaust check
                    // ensuring no event handler exists on Event
                    turbo.onAssemble -= exhaustCheck;
                    turbo.onDisassemble -= exhaustCheck;
                    headers.onAssemble -= exhaustCheck;
                    headers.onDisassemble -= exhaustCheck;
                    downPipeRace.onAssemble -= exhaustCheck;
                    downPipeRace.onDisassemble -= exhaustCheck;
                    downPipeStraight.onAssemble -= exhaustCheck;
                    downPipeStraight.onDisassemble -= exhaustCheck;
                    // assigning event handler
                    turbo.onAssemble += exhaustCheck;
                    turbo.onDisassemble += exhaustCheck;
                    headers.onAssemble += exhaustCheck;
                    headers.onDisassemble += exhaustCheck;
                    downPipeRace.onAssemble += exhaustCheck;
                    downPipeRace.onDisassemble += exhaustCheck;
                    downPipeStraight.onAssemble += exhaustCheck;
                    downPipeStraight.onDisassemble += exhaustCheck;

                    // cal mixture
                    calculateMixtureMultipler5 = mixtureFSM.FsmVariables.FindFsmFloat("Multiplier5");
                    calMixture.insertNewAction(onCalculateMixture, 8);

                    //combustion
                    combustion.injectAction("Cylinders", "Wait", InjectEnum.replace, updateMaxTorqueRPM, 0);

                    // choke value
                    choke = mixtureFSM.FsmVariables.GetFsmFloat("Choke");

                    // timing
                    FsmState ts2 = combustion.GetPlayMakerState("State 2");
                    timingAngleRetarded = ts2.GetAction<FloatCompare>(3);
                    timingAngleAdvanced = ts2.GetAction<FloatCompare>(4);
                    timingMultiplier = ts2.GetAction<FloatOperator>(5);
                    sparkTimingMultiplier = ts2.Fsm.Variables.GetFsmFloat("SparkTimingMultiplier");

                    //graphs creation
                    rpmGraphPower = createRpmGraph();
                    powerToTorqueGraph = createPowerToTorqueRPMDifferenceGraph();
                    
                }
                catch (Exception e)
                {
                    TurboMod.print($"{e}");
                }
            }
            checkRoutinesRunning();

            TurboMod.print(message);
        }
        private void updateMaxTorqueRPM()
        {

        }
        private void onCalculateMixture()
        {
            // Written, 16.04.2022

            if (canPipeWork && applyCalculatedDensity)
            {
                mixtureDiscrepancy = 0;
                float mdm = airDensity.Value * ventExtentionMixtureDiscrepancyModifier;
                if (!carbParts.ventExtention1.installed)
                    mixtureDiscrepancy += mdm;
                if (!carbParts.ventExtention2.installed)
                    mixtureDiscrepancy += mdm;

                float optimalTemp = 85; // c degrees
                float cal = engineTemp.Value / optimalTemp * choke.Value;
                calculateMixtureMultipler5.Value *= (airDensity.Value * cal) - mixtureDiscrepancy;
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
                maxBoostRpm = 6400;
                airFlowCoEff = 4.5f;
            }
            if (carbInstall)
            {
                maxBoostRpm = 6000;
                airFlowCoEff = 3f;
            }
        }
        private void onRaceCarbPurchased()
        {
            GameObject.Find("racing carburators(Clone)").FsmInject("Remove part", onCarbSetup);
        }
        private void onRaceExhaustPurchased()
        {
            GameObject re = GameObject.Find("racing exhaust(Clone)");
            re.injectAction("Removal", "Remove part", InjectEnum.append, updateExhaust);

            GameObject orderRaceExhaust = GameObject.Find("Database/DatabaseOrders/Racing Exhaust");
            PlayMakerFSM orderRaceExhaustData = orderRaceExhaust.GetPlayMaker("Data");
            if (orderRaceExhaustData.FsmVariables.GetFsmBool("Installed").Value)
            {
                re.injectAction("Removal", "Remove part", InjectEnum.append, raceExhaustCheckAssembly);
            }
            else
            {
                raceExhaustCheckAssembly();
            }
        }
        private void onRaceMufflerPurchased()
        {
            GameObject rm = GameObject.Find("racing muffler(Clone)");
            rm.injectAction("Removal", "Remove part", InjectEnum.append, updateExhaust);

            GameObject orderRaceMuffler = GameObject.Find("Database/DatabaseOrders/Racing Muffler");
            PlayMakerFSM orderRaceMufflerData = orderRaceMuffler.GetPlayMaker("Data");
            if (orderRaceMufflerData.FsmVariables.GetFsmBool("Installed").Value)
            {
                rm.injectAction("Removal", "Remove part", InjectEnum.append, raceMufflerCheckAssembly);
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
                exhaustTriggers.transform.Find("trigger_racing exhaust").gameObject.injectAction("Assembly", "Assemble 2", InjectEnum.append, updateExhaust);
            }
        }
        private void raceMufflerCheckAssembly()
        {
            if (!raceMufflerCheckAssemblyInjected)
            {
                raceMufflerCheckAssemblyInjected = true;
                GameObject mufflerTriggers = GameObject.Find("SATSUMA(557kg, 248)/MiscParts/Triggers Mufflers");
                mufflerTriggers.transform.Find("trigger_racing_muffler").gameObject.injectAction("Assembly", "Assemble 2", InjectEnum.append, updateExhaust);
            }
        }
        private void turboRpmReset()
        {
            turboRPM = 0;
        }
        private void turboFanCheck()
        {
            turbineModel.SetActive(!destroyed && !(isFiltered && (downPipeRace.installed || downPipeStraight.installed) && turbo.installed && headers.installed));
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
        private void downPipeTriggerSwitch() 
        {
            // Written, 31.05.2022
            
            Vector3 v;
            if (downPipeRace.installed)
            {
                //v = downPipeRace.triggers[0].triggerGameObject.transform.localScale;
                //downPipeRace.triggers[0].triggerGameObject.transform.localScale = new Vector3(v.x, Mathf.Abs(v.y), v.z);
                v = downPipeRace.triggers[0].triggerGameObject.transform.localPosition;
                downPipeRace.triggers[0].triggerGameObject.transform.localPosition = new Vector3(v.x, -Mathf.Abs(v.y), v.z);
            }
            if (downPipeStraight.installed)
            {
                //v = downPipeStraight.triggers[0].triggerGameObject.transform.localScale;
                //downPipeStraight.triggers[0].triggerGameObject.transform.localScale = new Vector3(v.x, -Mathf.Abs(v.y), v.z);
                v = downPipeStraight.triggers[0].triggerGameObject.transform.localPosition;
                downPipeStraight.triggers[0].triggerGameObject.transform.localPosition = new Vector3(v.x, Mathf.Abs(v.y), v.z);
            }
        }

        #endregion
    } 
}
