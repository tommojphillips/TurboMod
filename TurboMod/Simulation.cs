using System;
using System.Net;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MSCLoader;

using TommoJProductions.ModApi;
using TommoJProductions.ModApi.Attachable;
using TommoJProductions.ModApi.Database;
using static TommoJProductions.ModApi.ModClient;

using static TommoJProductions.TurboMod.SimulationExtentions;
using System.Collections;
using Random = UnityEngine.Random;

namespace TommoJProductions.TurboMod
{
    public class Simulation : MonoBehaviour
    {
        #region Constants

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
        /// Represents the wastegate adjust interval. (psi)
        /// </summary>
        public const float WASTEGATE_ADJUST_INTERVAL = 0.25f;

        /// <summary>
        /// Represents the stock max torque rpm for satsuma.
        /// </summary>
        public const int STOCK_MAX_TORQUE_RPM = 4000;
        /// <summary>
        /// Represents the stock max power rpm for satsuma.
        /// </summary>
        public const int STOCK_MAX_POWER_RPM = 6000;

        #endregion

        #region Fields

        private TurboParts turboParts = null;
        private CarbParts carbParts = null;

        private GameObject engine;
        private GameObject fuelGo;

        private FsmBool stockCarbInstalled;

        private FsmFloat throttlePedal;
        private FsmFloat mixture;
        private GameObject exhaust;
        private GameObject exhaust_fromEngine;
        private GameObject exhaust_fromPipe;
        private AudioSource exhaust_fromPipe_audioSource;
        private GameObject exhaust_fromMuffler;
        private AudioSource exhaust_fromMuffler_audioSource;
        private GameObject exhaust_fromHeaders;
        private FsmBool stockHeadersInstalled;
        private FsmBool raceHeadersInstalled;
        private FsmBool raceexhaustinstalled;
        private FsmBool racemufflerinstalled;
        internal Drivetrain drivetrain;

        internal AirSim airSim;
        internal TurboCompressorMap compressorMap;
        private WastegateAdjust wastegateAdjust;
        private BoostGauge boostGaugeLogic;
        private SimulationDebug simulationDebug;

        private int engineDisplacementCc = 988;

        private float dt;

        internal float currentAirFlow;
        internal float airFlowManifold;
        internal float airFlowAtMaxRpm;
        internal float volumetricEfficiency;

        internal float turboRpm;
        internal float turboSpool;
        internal float targetRPM;

        private FsmFloat choke;

        private bool engineCranked = false;

        internal float afrAbsolute;
        private FloatOperator checkMixtureAirFuelMixture;
        private FloatCompare checkMixtureLean;
        private FloatCompare checkMixtureRich;
        private FloatOperator calDensityFloatOp;
        private FloatClamp calDensityFloatClamp;

        #endregion

        #region Properties

        internal SimulationSaveData loadedSaveData { get; private set; } = default;

        private bool partsSetUp = false;

        internal Part turbo => turboParts.turbo;
        internal Part act => turboParts.act;
        internal Part headers => turboParts.headers;
        internal Part boostGauge => turboParts.boostGauge;
        internal Part dumpPipeRace => turboParts.downPipe;
        internal Part dumpPipeStraight => turboParts.downPipe2;

        internal float throttlePedalPosition => throttlePedal.Value / 8;
        internal bool onThrottlePedal => throttlePedalPosition > 0.1f;
        internal bool turboSurging => airSim.pressure > 0 && drivetrain.idlethrottle == 0 && !onThrottlePedal;

        internal float wastegatePsi
        {
            get => loadedSaveData.wastegatePsi;
            set => loadedSaveData.wastegatePsi = value;
        }
        internal float wastegateRPM => loadedSaveData.wastegatePsi * RPM2PSI;
        internal float wastegateSpool => wastegateRPM / (MAX_WASTEGATE_RPM / RPM2PSI);

        internal bool engineOn => engine.activeInHierarchy;
        internal bool turboInstalledToHead => headers.installed && turbo.installed;
        internal bool canTurboWork => turboInstalledToHead && engineOn && engineCranked && drivetrain.rpm > 0;
        internal bool canPipeWork => canTurboWork && carbInstall;
        internal bool isFiltered => turboParts.filter.installed || turboParts.highFlowFilter.installed;

        internal bool canCarbWork => stockCarbInstalled.Value && carbParts.throttleBodyAssembly.installed && carbParts.bowlAssembly.installed && carbParts.bowlCoverAssembly.installed;
        internal bool carbInstall => canCarbWork && turboParts.carbPipe.installed;

        internal float turboHeight => transform.position.y;

        /// <summary>
        /// Represents the engine displacement in cubic inches. (ci)
        /// </summary>
        internal float engineDisplacementCI => engineDisplacementCc / 16.387f;

        internal bool canTurboSetupConnectToRaceExhaust => turboInstalledToHead && dumpPipeRace; 
        
        internal float calHP => calculateAirFlowActual() / afrAbsolute / 0.46f * 60; // test

        #endregion

        #region Unity Runtime

        private void Start()
        {
            if (!partsSetUp)
                ModConsole.Print("[TurboMod.Sim] error: turbo and/or carb parts are not set up!, Turbo Sim will not work.");

            getReferences();

            simulationDebug = gameObject.AddComponent<SimulationDebug>();
            simulationDebug.sim = this;

            wastegateAdjust = act.gameObject.AddComponent<WastegateAdjust>();
            wastegateAdjust.sim = this;

            boostGaugeLogic = boostGauge.gameObject.AddComponent<BoostGauge>();
            boostGaugeLogic.sim = this;

            // end = cfm
            // to = pressure ratio.
            GraphData[] graphData = new GraphData[]
            {
                new GraphData(24, 0.62f),
                new GraphData(32, 0.94f),
                new GraphData(64, 1.12f),
                new GraphData(88, 1.33f),
                new GraphData(130, 1.9f),
                new GraphData(160, 2.35f),
                new GraphData(236, 3.15f),
            };
            compressorMap = new TurboCompressorMap(graphData);
            
            airSim = new AirSim();
        }
        private void Update()
        {
            dt = Time.fixedDeltaTime;

            turboFunction();
            pipeFunction();
        }

        #endregion

        #region Methods

        private void turboFunction()
        {
            if (canTurboWork)
            {
                currentAirFlow = calculateCfm();
                airFlowAtMaxRpm = calculateCfm(drivetrain.maxRPM);

                airFlowManifold = compressorMapCal() * airSim.airDensity;

                volumetricEfficiency = airFlowManifold / airFlowAtMaxRpm;

                targetRPM = Mathf.Clamp(MAX_WASTEGATE_RPM * volumetricEfficiency, 0, wastegateRPM);


                if (onThrottlePedal)
                {
                    if (turboSpool < 0)
                    {
                        turboSpool = 0;
                    }
                }
                else
                {
                    airFlowManifold *= -0.99f;

                    if (turboSpool > 0)
                    {
                        turboSpool = 0;
                    }
                }

                turboSpool = Mathf.Clamp(turboSpool + airFlowManifold, -9999, wastegateSpool);
                turboRpm = Mathf.Clamp(turboRpm + turboSpool,0, targetRPM);
            }
            else
            {
                turboRpm = 0;
                turboSpool = 0;
            }
        }
        private void pipeFunction()
        {
            if (canPipeWork)
            {
                airSim.update(turboRpm / RPM2PSI, turboHeight);
                calDensityFloatOp.float1.Value = airSim.airDensity;

                calculateCombusion();
                calculateRevLimitTime(true);

                if (turboSurging && surgeRoutine == null)
                    surgeRoutine = StartCoroutine(surgeFunction());
            }
            else if (engineCranked)
            {
                calculateRevLimitTime(false);
            }
        }

        internal int flutterI = 0;
        internal float calculatedSurgeRate;
        internal float turboSurgeCoEff = 0.87f;
        internal float flutterAudioMaxPitch = 1.93f;
        internal float flutterAudioMinPitch = 0.87f;
        internal float flutterAudioPitchMaxT = 2;
        internal float flutterAudioWaitFrom = 0.2f;
        internal float flutterAudioWaitTo = 0.0075f;
        private AudioSource turboWhistleAudio;
        private AudioSource turboFlutterAudio;
        internal Coroutine surgeRoutine;

        private IEnumerator surgeFunction()
        {
            turboFlutterAudio.gameObject.SetActive(true);
            turboSpool = 0;
            while (canTurboWork && turboSurging)
            {
                calculatedSurgeRate = airSim.pressure * turboSurgeCoEff;
                turboSpool -= calculatedSurgeRate;

                //float rand = Random.Range(0.005f, 0.06f);
                float wait = ModClient.unclampedLerp(flutterAudioWaitFrom, flutterAudioWaitTo, volumetricEfficiency);
                if (flutterI <= (airSim.pressure / 5).round())
                {
                    flutterI++;
                    if (turboFlutterAudio.isPlaying)
                    {
                        turboFlutterAudio.Stop();
                        yield return new WaitForSecondsRealTime(wait);
                    }
                    turboFlutterAudio.Play();
                }

                turboFlutterAudio.volume = ModClient.unclampedLerp(0, isFiltered ? 0.4f : 0.7f, volumetricEfficiency);
                turboFlutterAudio.pitch = ModClient.clampedLerp(flutterAudioMinPitch, flutterAudioMaxPitch, volumetricEfficiency, max: flutterAudioPitchMaxT);
                turboFlutterAudio.loop = false;
                turboFlutterAudio.mute = false;

                yield return null;
            }
            flutterI = 0;
            turboFlutterAudio.gameObject.SetActive(false);
            surgeRoutine = null;
        }

        private void getReferences()
        {
            Satsuma satsuma = Database.databaseVehicles.satsuma;
            stockCarbInstalled = Database.databaseMotor.carburator.installed;

            GameObject satsumaGo = satsuma.gameObject;
            GameObject carSimulation = satsuma.carSimulation;
            GameObject starter = carSimulation.transform.Find("Car/Starter").gameObject;

            engine = satsuma.engine;
            drivetrain = satsuma.drivetrain;

            throttlePedal = satsumaGo.transform.Find("Dashboard/Pedals/pedal_throttle").GetComponent<PlayMakerFSM>().FsmVariables.GetFsmFloat("Data");
            fuelGo = engine.transform.Find("Fuel").gameObject;
            PlayMakerFSM fuelEvent = fuelGo.GetComponents<PlayMakerFSM>()[1];
            mixture = fuelEvent.FsmVariables.FindFsmFloat("Mixture");

            #region Exhaust

            exhaust = carSimulation.transform.Find("Exhaust").gameObject;
            exhaust_fromEngine = exhaust.transform.Find("FromEngine").gameObject;
            exhaust_fromPipe = exhaust.transform.Find("FromPipe").gameObject;
            exhaust_fromPipe_audioSource = exhaust_fromPipe.GetComponent<AudioSource>();
            exhaust_fromMuffler = exhaust.transform.Find("FromMuffler").gameObject;
            exhaust_fromMuffler_audioSource = exhaust_fromMuffler.GetComponent<AudioSource>();
            exhaust_fromHeaders = exhaust.transform.Find("FromHeaders").gameObject;

            stockHeadersInstalled = Database.databaseMotor.headers.installed;
            raceHeadersInstalled = GameObject.Find("Database/DatabaseOrders/Steel Headers").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
            raceexhaustinstalled = GameObject.Find("Database/DatabaseOrders/Racing Exhaust").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
            racemufflerinstalled = GameObject.Find("Database/DatabaseOrders/Racing Muffler").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");

            #endregion

            #region turbo sounds

            GameObject spool = GameObject.Find("turbospool");
            GameObject flutter = GameObject.Find("flutter");
            turboWhistleAudio = spool.GetComponent<AudioSource>();
            turboFlutterAudio = flutter.GetComponent<AudioSource>();

            #endregion

            #region starter inject

            // starter inject
            starter.injectAction("Starter", "Start engine", InjectEnum.append, onEngineCrankUp);

            #endregion
        }

        private void calculateRevLimitTime(bool pipeWorking)
        {
            // Written, 28.05.2022

            if (pipeWorking)
                drivetrain.revLimiterTime = Mathf.Lerp(0.2f, 0.08f, drivetrain.rpm < 8300 ? volumetricEfficiency : 0);
            else
                drivetrain.revLimiterTime = 0.2f;
        }        
        private void calculateCombusion()
        {
            float AirFuelMixture = checkMixtureAirFuelMixture.float2.Value;
            afrAbsolute = mixture.Value * AirFuelMixture;
        }

        internal float compressorMapCal() => compressorMap.cal(airSim.pressureRatio);

        private float calculateCfm(float rpm) => airSim.calculateCfm(rpm, engineDisplacementCI);
        private float calculateCfm() => airSim.calculateCfm(drivetrain.rpm, engineDisplacementCI);

        #endregion

        private void onEngineCrankUp()
        {
            if (!engineCranked)
            {
                PlayMakerFSM mixtureFSM = fuelGo.GetPlayMaker("Mixture");
                FsmState checkMixture = mixtureFSM.GetState("Check mixture");
                FsmState calDensity = mixtureFSM.GetState("Calculate density");

                // check mixture
                checkMixtureAirFuelMixture = checkMixture.Actions[0] as FloatOperator;
                checkMixtureLean = checkMixture.Actions[6] as FloatCompare;
                checkMixtureRich = checkMixture.Actions[5] as FloatCompare;

                // cal density
                calDensityFloatOp = calDensity.Actions[5] as FloatOperator;
                calDensityFloatClamp = calDensity.Actions[6] as FloatClamp;

                // exhaust update
                exhaust.injectAction("Logic", "Engine", InjectEnum.insert, updateExhaust, index: 5);

                // exhaust check
                // ensuring no event handler exists on Event
                turbo.onAssemble -= updateExhaust;
                turbo.onDisassemble -= updateExhaust;
                headers.onAssemble -= updateExhaust;
                headers.onDisassemble -= updateExhaust;
                dumpPipeRace.onAssemble -= updateExhaust;
                dumpPipeRace.onDisassemble -= updateExhaust;
                dumpPipeStraight.onAssemble -= updateExhaust;
                dumpPipeStraight.onDisassemble -= updateExhaust;
                // assigning event handler
                turbo.onAssemble += updateExhaust;
                turbo.onDisassemble += updateExhaust;
                headers.onAssemble += updateExhaust;
                headers.onDisassemble += updateExhaust;
                dumpPipeRace.onAssemble += updateExhaust;
                dumpPipeRace.onDisassemble += updateExhaust;
                dumpPipeStraight.onAssemble += updateExhaust;
                dumpPipeStraight.onDisassemble += updateExhaust;

                // choke value
                choke = mixtureFSM.FsmVariables.GetFsmFloat("Choke");

                engineCranked = true;
            }
        }
        private void updateExhaust()
        {
            // Written, 27.09.2021

            if (canTurboSetupConnectToRaceExhaust)
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

        private float calculateAirFlowActual() 
        {
            // Written, 31.10.2022

            return (airSim.absoluteOutletPressure * volumetricEfficiency * (drivetrain.rpm / 2) * engineDisplacementCI);
        }

        public void setupParts(TurboParts turboParts, CarbParts carbParts, SimulationSaveData saveData)
        {
            // Written, 31.10.2022

            if (!partsSetUp)
            {
                this.turboParts = turboParts;
                this.carbParts = carbParts;
                loadedSaveData = saveData;
                partsSetUp = true;
            }
        }
    }    
}

