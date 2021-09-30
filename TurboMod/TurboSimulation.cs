using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MSCLoader;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using TommoJProductions.ModApi.Attachable;
using TommoJProductions.ModApi;

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

		#endregion

		#region Fields

		// Static fields
		public static FsmString Interacttext;
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


		// Private fields
		private AudioSource turboWhistle;
		private AudioSource turboFlutter;
		private Drivetrain drivetrain = null;
		private bool engineCrankFirstTime = false;
		private bool raceExhaustCheckAssemblyInjected = false;
		private bool raceMufflerCheckAssemblyInjected = false;
		private bool GUIdebug = false;
		private float afrFactor;
		private float BLOWCHANCE;
		private float pistonWear;
		private float timeBoost;
		private float turbospin;
		private float throtsmooth;
		private float wgspool;
		private float vacsmooth;
		private FsmFloat engineTemp;
		private FsmFloat THROTPEDAL;
		private FsmFloat AIRDENSE;
		private FsmFloat AMBIENTTEMPERATURE = PlayMakerGlobals.Instance.Variables.FindFsmFloat("AmbientTemperature");
		private FsmFloat STRESS = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerStress");
		private FsmFloat SPEEDKMH = PlayMakerGlobals.Instance.Variables.FindFsmFloat("SpeedKMH");
		private GameObject turbofan;
		private GameObject n2otrigger;
		private GameObject stockfiltertrigger;
		private GameObject soundSpool;
		private GameObject turboneedleobject;
		private GameObject TURBOMESH;
		private GameObject TURBOMESH_D;
		private GameObject engine;
		private GameObject FIRE;
		private GameObject fuelGo;
		private GameObject soundFlutter;
		private GameObject PINGING;
		private GameObject headertriggers;
		private GameObject exhaust_fromEngine;
		private GameObject exhaust_fromMuffler;
		private GameObject exhaust_fromPipe;
		private GameObject exhaust_fromHeaders;
		private GUIStyle guiStyle = new GUIStyle();
		GameObject exhaust;

		// Internal fields
		internal PlayMakerFSM SWEAR;
		internal PlayMakerFSM MOTORBLOW;
		internal PlayMakerFSM HEADGASKET;
		internal PlayMakerFSM BACKFIRE;
		internal PlayMakerFSM FUEL;
		internal PlayMakerFSM FUELevent;
		internal PlayMakerFSM OIL;
		internal PlayMakerFSM WEAR;
		internal float wastegateRPM = 115000f;
		internal float maxBoostFuel;
		internal float turboNeedleBoost;
		internal float wastegatePSI;
		internal float PSI;

		//Public fields
		public bool turboDestroyed;
		public bool wear = false;
		public FsmBool headersinstalled;
		public FsmBool n2oinstalled;
		public FsmBool racecarbinstalled;
		public FsmBool raceexhaustinstalled;
		public FsmBool raceheadersinstalled;
		public FsmBool racemufflerinstalled;
		public FsmBool stockcarbinstalled;
		public FsmBool stockfilterinstalled;
		public FsmBool GUIuse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
		public FsmFloat AFR;
		public FsmFloat CarbBowl;
		public FsmFloat Fuelpumpefficiency;
		public FsmFloat HEADGASKETwear;
		public FsmFloat OilLevel;
		public FsmFloat OilDirt;
		public FsmFloat PISTON1wear;
		public FsmFloat PISTON2wear;
		public FsmFloat PISTON3wear;
		public FsmFloat PISTON4wear;
		public float coolMult;
		public float adjustTime;
		public float maxSafeTorque;
		public float motorStress;
		public float turboFriction;
		public float turboSpool;
		public float turbowearrate;
		public float maxBoostRpm;
		public float vacthrot;
		public float wearMult;
		public float boostMultiplier = 0.00013f;
		public float bovtimesince = 0f;
		public float boostneedle = 20f;
		public float frictionmult = 115f;
		public float initialRpm = 2500f;
		public float turboTargetRPM = 180001f;
		public float turboRPM = 0f;
		public float oilCoolerCoolingRate = 0.007392f;
		public float oilCoolerThermostatRating = 95;
		public GameObject DEATH;
		public static GameObject PLAYER;
		internal int sizef;
		internal int widthlol;
		internal int heightlol;

		internal turboSimulationSaveData loadedSaveData;
		internal turboSimulationSaveData defaultSaveData => new turboSimulationSaveData() { turboDestroyed = false, turboWear = Random.Range(75, 100) + (Random.Range(0, 100) * 0.001f), wastegatePsi = 8.25f };

		private Coroutine oilCoolerRoutine;
		private Coroutine turboRoutine;
		private Coroutine pipeRoutine;

		#endregion

		#region Properties

		public bool isPiped => carbInstall || raceCarbInstall;
		public bool isFiltered => filter.installed || highFlowFilter.installed;
		public bool isExhaust => headers.installed && turbo.installed && downPipe.installed;
		public bool carbInstall => stockcarbinstalled.Value && carbPipe.installed;
		public bool raceCarbInstall => racecarbinstalled.Value && coldSidePipe.installed && intercooler.installed && hotSidePipe.installed;
		public bool engineOn => engine.activeInHierarchy;
		public bool canTurboWork => turbo.installed && headers.installed && !turboDestroyed && engineOn;
		public bool canOilCoolerWork => oilCooler.installed && engineOn;
		public bool canPipeWork => canTurboWork && isPiped;
		
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
			carbPipe.onAssemble += checkRoutinesRunning;
			carbPipe.onDisassemble += checkRoutinesRunning;
			// race carb install check
			coldSidePipe.onAssemble += checkRoutinesRunning;
			coldSidePipe.onDisassemble += checkRoutinesRunning;
			hotSidePipe.onAssemble += checkRoutinesRunning;
			hotSidePipe.onDisassemble += checkRoutinesRunning;
			intercooler.onDisassemble += checkRoutinesRunning;
			intercooler.onAssemble += checkRoutinesRunning;

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

		}
        private void OnDisable()
		{
			// filter boost multipier check
			filter.onAssemble -= updateBoostMultiplier;
			filter.onDisassemble -= updateBoostMultiplier;
			highFlowFilter.onAssemble -= updateBoostMultiplier;
			highFlowFilter.onDisassemble -= updateBoostMultiplier;

			// carb install check
			carbPipe.onAssemble -= checkRoutinesRunning;
			carbPipe.onDisassemble -= checkRoutinesRunning;
			// race carb install check
			coldSidePipe.onAssemble -= checkRoutinesRunning;
			coldSidePipe.onDisassemble -= checkRoutinesRunning;
			hotSidePipe.onAssemble -= checkRoutinesRunning;
			hotSidePipe.onDisassemble -= checkRoutinesRunning;
			intercooler.onDisassemble -= checkRoutinesRunning;
			intercooler.onAssemble -= checkRoutinesRunning;

			// Boost gauge needle
			boostGauge.onAssemble -= boostGaugeNeedleReset;
			boostGauge.onDisassemble -= boostGaugeNeedleReset;

			// oil cooler pipes
			oilCooler.onAssemble -= oilCoolerOnAssemble;
			oilCooler.onDisassemble -= oilCoolerOnDisassemble;

			// turbo fan rpm reset
			headers.onDisassemble -= turboRpmReset;
			turbo.onDisassemble -= turboRpmReset;

			// turbo fan rpm reset
			headers.onDisassemble -= turboRpmReset;
			turbo.onDisassemble -= turboRpmReset;

			// turbo fan active check
			filter.onAssemble -= turboFanCheck;
			filter.onDisassemble -= turboFanCheck;
			highFlowFilter.onAssemble -= turboFanCheck;
			highFlowFilter.onDisassemble -= turboFanCheck;
			downPipe.onAssemble -= turboFanCheck;
			downPipe.onDisassemble -= turboFanCheck;
			headers.onAssemble -= turboFanCheck;
			headers.onDisassemble -= turboFanCheck;
			turbo.onAssemble -= turboFanCheck;
			turbo.onDisassemble -= turboFanCheck;
		}
		private void Start()
		{
			initSimulation();
			initEngineState();
		}
		private void LateUpdate()
		{
			if (cInput.GetButton("DrivingMode") && cInput.GetButton("Finger") && adjustTime >= 4)
			{
				GUIdebug = !GUIdebug;
				if (GUIdebug)
				{
					Interacttext.Value = "Turbo Debug UI: ENABLED";
					adjustTime = 0f;
				}
				else
				{
					Interacttext.Value = "Turbo Debug UI: DISABLED";
					adjustTime = 0f;
				}
			}
			if (adjustTime != 4)
			{
				adjustTime += 0.125f;
			}
			if (turbofan.gameObject.isPlayerLookingAt())
			{
				turboCondCheck();
			}
			if (act.installed && !turbo.gameObject.isPlayerHolding() && act.gameObject.isPlayerLookingAt())
			{
				wasteGateAdjust();
			}
			partCheck();

			// boost gauge needle rotation update
			if (turboneedleobject.transform.localEulerAngles.z != boostneedle)
				turboneedleobject.transform.localEulerAngles = new Vector3(0f, 0f, boostneedle);
		}

		#region OnGUI & GUI Fields

		private float whistleModifier = 1.1f;
		private float flutterModifier = 1.3563f;

		private string propertyString = "";
		private void drawPropertyEdit(string inPropertyName, float inProperty, out float outProperty) 
		{
			drawProperty(inPropertyName, inProperty);
			propertyString = GUILayout.TextField(inProperty.ToString(), 10, guiStyle, new GUILayoutOption[0]);
			float.TryParse(propertyString, out outProperty);

		}
		private void drawProperty(string inPropertyName, object inProperty = null)
		{
			if (inProperty != null)
				GUILayout.Label($"{inPropertyName}: {inProperty}", guiStyle, new GUILayoutOption[0]);
			else
				GUILayout.Label($"{inPropertyName}", guiStyle, new GUILayoutOption[0]);
		}

		private void OnGUI()
		{
			sizef = Screen.width / 110;
			widthlol = 1800;
			heightlol = 650;
			bool guidebug = GUIdebug;
			if (guidebug)
			{
				using (new GUILayout.AreaScope(new Rect((Screen.width - widthlol), Screen.height - heightlol - 200, widthlol, heightlol), "", new GUIStyle()))
				{
					guiStyle.fontSize = sizef;
					guiStyle.normal.textColor = Color.white;

					drawPropertyEdit("whistle modifer", whistleModifier, out whistleModifier);
					drawPropertyEdit("flutter modifier", flutterModifier, out flutterModifier);
					drawPropertyEdit("Curve factor", drivetrain.curveFactor, out drivetrain.curveFactor);

					drawProperty("Turbo STATS");
					drawProperty("Turbo RPM", Math.Round(turboRPM));
					drawProperty("WasegateRPM", Math.Round(wastegateRPM));
					drawProperty("PSI", PSI.ToString("0.00"));
					drawProperty("WastegatePSI", Math.Round(wastegatePSI, 2));
					drawProperty("fuel starve boost", Math.Round(maxBoostFuel, 2));					
					GUILayout.Space(3);
					GUILayout.Label("WEAR", guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("TurboWear:        " + Math.Round(wearMult, 2).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Space(3);
					GUILayout.Label("DRIVETRAIN STATS", guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("RPM:             " + Math.Round(drivetrain.rpm).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("Kph:             " + Math.Round(SPEEDKMH.Value).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("Current Torque:  " + Math.Round(drivetrain.torque, 2).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("Current Power:   " + Math.Round(drivetrain.currentPower, 2).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("Power Mult:	  " + Math.Round(drivetrain.powerMultiplier, 3).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("Block Temp:      " + Math.Round(engineTemp.Value).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("Motor Stress:    " + Math.Round(motorStress / 2.5f).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Space(3);
					GUILayout.Label("OTHER STATS", guiStyle, new GUILayoutOption[0]);
					GUILayout.Label($"AFR:                {Math.Round(AFR.Value, 2)} ({Math.Round(AFR.Value, 2) * 14.7})", guiStyle, new GUILayoutOption[0]);
					GUILayout.Label($"Ambient Temp:       {Math.Round(AMBIENTTEMPERATURE.Value, 2)}", guiStyle, new GUILayoutOption[0]);
					GUILayout.Label($"Air Density:        {AIRDENSE.Value}", guiStyle, new GUILayoutOption[0]);
					GUILayout.Label($"Throttle Pedal:     {THROTPEDAL.Value}", guiStyle, new GUILayoutOption[0]);
					GUILayout.Label($"curveFactor:     {drivetrain.curveFactor}", guiStyle, new GUILayoutOption[0]);
					GUILayout.Space(3);
					GUILayout.Label("CALCULATED STATS", guiStyle, new GUILayoutOption[0]);
					drawProperty("Calculated density", calDensityFloatOp?.float1.Value);
				}
			}
		}

		#endregion

		#endregion

		#region Methods

		private void checkAnyConflictingPart() 
		{
			// stock carb pipe =><= stockairfilter
			if (carbPipe.installed && stockfilterinstalled.Value)
				carbPipe.disassemble();
			// turbo headers =><= stockheaders & steel headers
			if (headers.installed && (headersinstalled.Value || raceheadersinstalled.Value))
				headers.disassemble();
			// turbo filters
			if (highFlowFilter.installed && filter.installed)
				highFlowFilter.disassemble();
		}
		private void boostGaugeNeedleReset()
		{
			boostneedle = 18;
		}
		private void initEngineState()
		{
			//checkAnyConflictingPart();
			partCheck();
			updateBoostMultiplier();
		}
		private void initSimulation()
		{
			try
			{
				wearMult = loadedSaveData.turboWear;
				wastegateRPM = Mathf.Clamp(loadedSaveData.wastegatePsi * RPM2PSI, MIN_WASTEGATE_RPM, MAX_WASTEGATE_RPM);
				wastegatePSI = wastegateRPM / RPM2PSI;
				turboDestroyed = loadedSaveData.turboDestroyed;
								                
				adjustTime = 1f;

                #region game field assignments 

				GameObject satsuma = GameObject.Find("SATSUMA(557kg, 248)");
				GameObject carSimulation = satsuma.transform.Find("CarSimulation").gameObject;
				GameObject starter = carSimulation.transform.Find("Car/Starter").gameObject;
				engine = carSimulation.transform.Find("Engine").gameObject;
				exhaust = carSimulation.transform.Find("Exhaust").gameObject;
				soundSpool = GameObject.Find("turbospool");
				soundFlutter = GameObject.Find("flutter");
				turboFlutter = soundFlutter.GetComponent<AudioSource>();
				turboWhistle = soundSpool.GetComponent<AudioSource>();
				drivetrain = satsuma.GetComponent<Drivetrain>();
				engineTemp = FsmVariables.GlobalVariables.FindFsmFloat("EngineTemp");
				Interacttext = FsmVariables.GlobalVariables.FindFsmString("GUIinteraction");
				exhaust_fromEngine = exhaust.transform.Find("FromEngine").gameObject;
				exhaust_fromPipe = exhaust.transform.Find("FromPipe").gameObject;
				exhaust_fromMuffler = exhaust.transform.Find("FromMuffler").gameObject;
				exhaust_fromHeaders = exhaust.transform.Find("FromHeaders").gameObject;
				MOTORBLOW = carSimulation.transform.Find("Car/Redlining").GetComponent<PlayMakerFSM>();
				BACKFIRE = engine.transform.Find("Symptoms").GetComponent<PlayMakerFSM>();
				HEADGASKET = GameObject.Find("Database/DatabaseMotor/Headgasket").GetComponent<PlayMakerFSM>();
				FIRE =satsuma.transform.Find("FIRE").gameObject;
				PINGING = engine.transform.Find("SoundPinging").gameObject;
				turboneedleobject = boostGauge.transform.GetChild(2).gameObject;
				headertriggers = GameObject.Find("cylinder head(Clone)/Triggers Headers");
				stockfiltertrigger = GameObject.Find("carburator(Clone)/trigger_airfilter");
				n2otrigger = GameObject.Find("racing carburators(Clone)/trigger_n2o_injectors");
				turbofan = GameObject.Find("motor_turbocharger_blades");
				fuelGo = engine.transform.Find("Fuel").gameObject;
				FUEL = fuelGo.GetComponent<PlayMakerFSM>();
				FUELevent = fuelGo.GetComponents<PlayMakerFSM>()[1];
				DEATH = GameObject.Find("Systems").transform.Find("Death").gameObject;
				OIL = carSimulation.transform.Find("Engine/Oil").GetComponent<PlayMakerFSM>();
				SWEAR = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SpeakDatabase").GetComponent<PlayMakerFSM>();
				WEAR = carSimulation.transform.Find("MechanicalWear").GetComponent<PlayMakerFSM>();
				HEADGASKETwear = WEAR.FsmVariables.GetFsmFloat("WearHeadgasket");
				PISTON1wear = WEAR.FsmVariables.GetFsmFloat("WearPiston1");
				PISTON2wear = WEAR.FsmVariables.GetFsmFloat("WearPiston2");
				PISTON3wear = WEAR.FsmVariables.GetFsmFloat("WearPiston3");
				PISTON4wear = WEAR.FsmVariables.GetFsmFloat("WearPiston4");
				Fuelpumpefficiency = FUEL.FsmVariables.GetFsmFloat("FuelPumpEfficiency");
				AFR = FUELevent.FsmVariables.FindFsmFloat("Mixture");
				AIRDENSE = FUELevent.FsmVariables.FindFsmFloat("AirDensity");
				CarbBowl = FUEL.FsmVariables.GetFsmFloat("CarbReserve");
				OilLevel = OIL.FsmVariables.GetFsmFloat("Oil");
				OilDirt = OIL.FsmVariables.GetFsmFloat("OilContaminationRate");
				motorStress = 0f;
				stockcarbinstalled = GameObject.Find("Database/DatabaseMotor/Carburator").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
				stockfilterinstalled = GameObject.Find("Database/DatabaseMotor/Airfilter").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
				n2oinstalled = GameObject.Find("Database/DatabaseOrders/N2O Injectors").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
				racecarbinstalled = GameObject.Find("Database/DatabaseOrders/Racing Carburators").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
				headersinstalled = GameObject.Find("Database/DatabaseMotor/Headers").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
				raceheadersinstalled = GameObject.Find("Database/DatabaseOrders/Steel Headers").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
				raceexhaustinstalled = GameObject.Find("Database/DatabaseOrders/Racing Exhaust").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
				racemufflerinstalled = GameObject.Find("Database/DatabaseOrders/Racing Muffler").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Installed");
				TURBOMESH = turbo.transform.Find("turbomesh").gameObject;
				TURBOMESH_D = turbo.transform.Find("turbomesh_D").gameObject;
				PLAYER = GameObject.Find("PLAYER");
				THROTPEDAL = satsuma.transform.Find("Dashboard/Pedals/pedal_throttle").GetComponent<PlayMakerFSM>().FsmVariables.GetFsmFloat("Data");

				GameObject head = GameObject.Find("cylinder head(Clone)");

                #endregion

                #region Carb install set up/check/inject

                // Carb install set up check inject
                head.transform.Find("Triggers Carbs/trigger_carburator").gameObject.FsmInject("Assemble", checkRoutinesRunning);
				head.transform.Find("Triggers Carbs/trigger_carburator_racing").gameObject.FsmInject("Assemble", checkRoutinesRunning);
				GameObject.Find("carburator(Clone)").FsmInject("Remove part", checkRoutinesRunning);

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
				starter.injectAction("Starter", "Start engine",  PlayMakerExtentions.injectEnum.append, onEngineCrankUp);

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
				if (cInput.GetButton("Use"))
					drivetrain.transmission = Drivetrain.Transmissions.RWD;
				drivetrain.clutchTorqueMultiplier = 400;

                #endregion
            }
            catch (Exception ex) 
			{
				ModConsole.LogError(ex.ToString());
			}
		}
		public void turboCondCheck()
		{
			GUIuse.Value = true;
			turbofan.transform.localEulerAngles = new Vector3(turbospin, 0f, 0f);
			if (isFiltered)
			{
				if (cInput.GetButtonUp("Use") && downPipe.installed)
				{
					Interacttext.Value = "Can't check with the filter on!";
					SWEAR.SendEvent("SWEARING");
				}
			}
			else
			{
				if (turboRPM > 200)
				{
					Interacttext.Value = "That looks dangerous!";
				}
				if (cInput.GetButtonUp("Use"))
				{
					if (!engineOn)
					{
						turbospin += 10f;
						if (wearMult > 99 || turboDestroyed)
						{
							Interacttext.Value = "There's nothing left to check...";
							SWEAR.SendEvent("SWEARING");
						}
						else if (wearMult > 65)
						{
							Interacttext.Value = "Feels worn out, a bit of shaft play";
							MasterAudio.PlaySound3DAndForget("Motor", turbo.transform, variationName: "damage_bearing");
						}
						else if (wearMult > 30)
						{
							Interacttext.Value = "A little used, seems fine";
							MasterAudio.PlaySound3DAndForget("Motor", turbo.transform, variationName: "valve_knock");
						}
						else
						{
							Interacttext.Value = "Looks brand new";
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
		public void deathByTurbo()
		{
			GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera").GetComponents<PlayMakerFSM>().FirstOrDefault((PlayMakerFSM x) => x.Fsm.Name == "Death").SendEvent("DEATH");
			GameObject.Find("Systems").transform.Find("Death").GetComponent<PlayMakerFSM>().FsmVariables.GetFsmBool("RunOver").Value = true;
			GameObject.Find("Systems").transform.Find("Death").GetComponent<PlayMakerFSM>().FsmVariables.GetFsmBool("Crash").Value = false;
			turbo.transform.Find("handgrind").gameObject.SetActive(true);
			GameObject.Find("Systems").transform.Find("Death/GameOverScreen/Paper/HitAndRun/TextEN").GetComponent<TextMesh>().text = "Boy's \n hand eaten \n by\n turbo";
			GameObject.Find("Systems").transform.Find("Death/GameOverScreen/Paper/HitAndRun/TextFI").GetComponent<TextMesh>().text = "Pojan käsi tuhoutui \n turboahtimella";
		}
		public void wasteGateAdjust()
		{
			wastegatePSI = wastegateRPM / RPM2PSI;
			Interacttext.Value = "Wastegate pressure: " + wastegatePSI.ToString("0.00") + " PSI";
			if (Input.mouseScrollDelta.y > 0f & wastegateRPM <= 194925f & adjustTime >= 1)
			{
				adjustTime = 0;
				wastegateRPM += WASTEGATE_ADJUST_INTERVAL;
				MasterAudio.PlaySound3DAndForget("CarBuilding", act.transform, variationName: "bolt_screw");
			}
			if (Input.mouseScrollDelta.y < 0f & wastegateRPM >= MIN_WASTEGATE_RPM & adjustTime >= 1)
			{
				adjustTime = 0f;
				wastegateRPM -= WASTEGATE_ADJUST_INTERVAL;
				MasterAudio.PlaySound3DAndForget("CarBuilding", act.transform, variationName: "bolt_screw");
			}
			bool flag3 = cInput.GetButtonDown("Finger") & wastegateRPM <= MAX_WASTEGATE_RPM & adjustTime >= 1;
			if (flag3)
			{
				adjustTime = 0;
				wastegateRPM = MAX_WASTEGATE_RPM;
				MasterAudio.PlaySound3DAndForget("CarBuilding", act.transform, variationName: "bolt_screw");
			}
		}
		public void partCheck()
		{
						
			if (wearMult > 55f & !TURBOMESH_D.activeInHierarchy)
			{
				TURBOMESH_D.SetActive(true);
				TURBOMESH.SetActive(false);
			}
			else if (!TURBOMESH.activeInHierarchy)
			{
				TURBOMESH.SetActive(true);
				TURBOMESH_D.SetActive(false);
			}
		}

		public void updateExhaust()
		{
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
					}
					else
					{
						exhaust_fromMuffler.SetActive(false);
						exhaust_fromEngine.SetActive(false);
						exhaust_fromHeaders.SetActive(false);
						exhaust_fromPipe.SetActive(true);
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
				if (headersinstalled.Value || raceheadersinstalled.Value)
					return;

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
		public void exhaustCrackle()
		{
			if (PSI > 3.0 & Random.Range(0f, PSI + 10f) > 20f)
			{
				BACKFIRE.SendEvent("TIMINGBACKFIRE");
			}
		}
		public void turboSounds()
		{
			soundSpool.SetActive(engineOn);

			turboWhistle.pitch =  PSI / wastegatePSI * whistleModifier;
			turboWhistle.volume = turboRPM / 200000f * 0.05f;
			turboWhistle.volume *= isFiltered ? 0.7f : 1.25f;
			if (drivetrain.throttle < 0.2f & isPiped)
			{
				exhaustCrackle();
			}
			if (PSI > 4.400000095367432 & THROTPEDAL.Value / 8f < 0.1f & isPiped & !drivetrain.revLimiterTriggered)
			{
				soundFlutter.SetActive(true);
				turboFlutter.volume = turboWhistle.volume * (isFiltered ? 16 : 26);
				turboFlutter.pitch = flutterModifier;
				turboFlutter.loop = false;
				turboFlutter.mute = false;
				bovtimesince = 0f;
			}
			if (bovtimesince < 160)
			{
				bovtimesince += 1f;
			}
			if (bovtimesince > 30 & soundFlutter.activeInHierarchy)
			{
				soundFlutter.SetActive(false);
				STRESS.Value -= 0.2f;
			}
		}
		public void boostGaugeNeedle()
		{
			turboNeedleBoost = turboRPM;
			vacsmooth = THROTPEDAL.Value / 8f * 117f;
			vacsmooth = Mathf.Clamp(vacsmooth, drivetrain.idlethrottle * 117f, 117f);
			vacthrot = Mathf.SmoothDamp(vacthrot, vacsmooth, ref throtsmooth, 0.05f);
			boostneedle = 133f + -vacthrot + -turboNeedleBoost / 1600f;
		}
		public void turboDestroyEvent()
		{
			wearMult = 100;
			turboDestroyed = true;
			MasterAudio.PlaySound3DAndForget("Motor", turbo.transform, variationName: "damage_oilpan");
			if (engineOn)
			{
				BLOWCHANCE = Random.Range(0f, drivetrain.rpm / 800f);
				if (BLOWCHANCE >  Random.Range(1, 5))
				{
					MOTORBLOW.SendEvent("Finished");
				}
			}
			SWEAR.SendEvent("SWEARING");
			turboFanCheck();
		}
		public void turboRepairEvent()
		{
			wearMult = Random.Range(0.5f, 9f);
			turboDestroyed = false;
			turboFanCheck();
		}
		public turboSimulationSaveData getSave() 
		{
			return new turboSimulationSaveData() { turboDestroyed = turboDestroyed, turboWear = wearMult, wastegatePsi = wastegatePSI };
		}

		#endregion

		#region IEnumerators

		private IEnumerator pipeFunction()
		{
			ModConsole.Log("Pipe Simulation: Started");
			while (canPipeWork)
			{
				boostGaugeNeedle();
				updateCarbSetup();
				//onCalculateDensity(true);
				//updateCalculateDensityClamp(true);
				//updateCaculateMixutureOperation(true);
				//onCalculateMixture();
				//onCheckMixture(true);
				if (PSI > maxBoostFuel)
				{
					fuelStarveEvent();
				}
				drivetrain.powerMultiplier *= maxBoostRpm * boostMultiplier + PSI / 10;
				if (PSI > 1.0)
				{
					engineTemp.Value += drivetrain.powerMultiplier * 4f * drivetrain.throttle * coolMult;
					drivetrain.revLimiterTime = 0.11f - (PSI / 1000);
				}
				else
				{
					drivetrain.revLimiterTime = 0.2f;
				}
				yield return null;
			}
			//onCalculateDensity(false);
			//updateCalculateDensityClamp(false);
			//updateCaculateMixutureOperation(false);
			//onCheckMixture(false);
			boostGaugeNeedleReset();
			pipeRoutine = null;
			ModConsole.Log("Pipe Simulation: Finished");
		}
		private IEnumerator turboFunction()
		{
			ModConsole.Log("Turbo Simulation: Started");
			while (canTurboWork)
			{
				try
				{
					PSI = turboRPM / RPM2PSI;

					float rpmInitial = Mathf.Clamp(drivetrain.rpm - initialRpm, 0, drivetrain.rpm);

					turboSpool = rpmInitial * 1.75f * (drivetrain.throttle + 0.05f) + rpmInitial * 0.08f + drivetrain.torque / 650f / 10f * drivetrain.throttle;
					if (drivetrain.rpm > maxBoostRpm)
					{
						turboSpool *= maxBoostRpm / 2650f - rpmInitial / (maxBoostRpm * 0.61f);
					}
					if (initialRpm > drivetrain.rpm)
					{
						turboSpool *= initialRpm / 14200f + rpmInitial / (initialRpm * 0.95f);
					}
					if (act.installed && turboTargetRPM != wastegateRPM)
					{
						turboTargetRPM = wastegateRPM;
					}
					turboRPM = Mathf.Clamp(turboRPM, 0f, turboTargetRPM * 5f);
					frictionmult = 120f + wearMult * 1.2f;
					wgspool = wastegateRPM / 22f - frictionmult * 3f;
					turboSpool = Mathf.Clamp(turboSpool, 120f, turboRPM > turboTargetRPM * 0.975 && act.installed ? wgspool : 55555);
					turbofan.transform.localEulerAngles = new Vector3(turbospin, 0f, 0f);
					turboRPM += turboSpool;
					turboRPM -= turboFriction;
					turboFriction = frictionmult * turboRPM / 1500f * 0.5f;
					turbospin += turboRPM;
					turboSounds();
				}
				catch (Exception ex)
				{
					ModConsole.Log(ex);
				}
				yield return null;
			}

			if (soundSpool.activeInHierarchy)
			{
				soundSpool.SetActive(false);
				turboRPM = 0f;
			}
			if (turboDestroyed)
			{
				turboDestroyEvent();
			}

			turboRoutine = null;
			ModConsole.Log("Turbo Simulation: Finished");
		}
		private IEnumerator oilCoolerFunction()
		{
			ModConsole.Log("Oil Cooler: Started");
			while (canOilCoolerWork)
			{;
				if (engineTemp.Value > oilCoolerThermostatRating)
				{
					engineTemp.Value -= oilCoolerCoolingRate;
				}
				yield return null;
			}
			oilCoolerRoutine = null;
			ModConsole.Log("Oil Cooler: Finished");
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

		private void fuelStarveEvent() 
		{
			timeBoost += Time.deltaTime;
			if (timeBoost > Random.Range(3f, 12f))
			{
				drivetrain.revLimiterTriggered = true;
				BACKFIRE.SendEvent("TIMINGBACKFIRE");
				if (wear)
					wearMult += Random.Range(0.3f, 3f);
				timeBoost = 0f;
			}
		}

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

		public const float PASCAL = 6894.76f;
		public const float KELVIN = 274.15f;
		public const float DRY_GAS_CONSTANT = 287.058f;
		public const float WET_GAS_CONSTANT = 461.495f;
		internal float calculateAirDensity(float pressure) 
		{
			// FORMULA: ρ = (pd / (Rd * T)) + (pv / (Rv * T))

			float RH = 0.20534f; // Relative humidity | Ranges between 0% (dry) and 100% (completly saturated with water vapour)
			float Tc = engineTemp.Value;//AMBIENTTEMPERATURE.Value;  // temp in c
			float Tk = Tc + KELVIN; // temperature in kelvin

			float pv = calculateSaturationVapourPressure(Tc) * RH; // water vapour in C
			float pd = pressure - pv; // pressure in pounds pre square inch (psi)

			pv *= PASCAL; // water vapour in pascals
			pd *= PASCAL; // pressure in pascals

			float result = (pd / (DRY_GAS_CONSTANT * Tk)) + (pv / (WET_GAS_CONSTANT * Tk)); // in kilogram per meter cubic (Kg/m3)

			return result;

			//return result / 1000; // in gram per meter cubic (g/m3
		}
		internal void onCheckMixture(bool pipeWorking)
		{
			if (pipeWorking)
			{
				//checkMixtureAirFuelMixture.float2 = 12.5f;
				checkMixtureLean.float2 = 15;
				checkMixtureRich.float2 = 11.6f;
				checkMixtureSuperRich.float2 = 10;
				checkMixtureSputter.float2 = 9.4f;
				checkMixtureOff.float2 = 7.27f;
			}
			else
			{
				//checkMixtureAirFuelMixture.float2 = 14.7f;
				checkMixtureLean.float2 = 16;
				checkMixtureRich.float2 = 14.05f;
				checkMixtureSuperRich.float2 = 12.7f;
				checkMixtureSputter.float2 = 10;
				checkMixtureOff.float2 = 8;
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
		internal void onCalculateDensity(bool pipeWorking)
		{
			calDensityFloatOp.float1.Value = 0.36f;
			if (pipeWorking)
				calDensityFloatOp.float1.Value += calculateAirDensity(PSI);
		}
		internal void onCalculateMixture()
		{
			calMixtureFloatOp.float2.Value = AIRDENSE.Value - calculateAirDensity(PSI / 10);
		}
		private void exhaustCheck() 
		{
			updateExhaust();
			checkRoutinesRunning();
		}

		private FloatCompare checkMixtureLean;
		private FloatCompare checkMixtureRich;
		private FloatCompare checkMixtureSuperRich;
		private FloatCompare checkMixtureSputter;
		private FloatCompare checkMixtureOff;
		private FloatOperator checkMixtureAirFuelMixture;
		private FloatOperator calMixtureFloatOp;
		private FloatOperator calDensityFloatOp;
		private FloatClamp calDensityFloatClamp;
		private void onEngineCrankUp() 
		{
			string message = $"[Engine Cranked Over {(engineCrankFirstTime ? "First Time" : "")}]";

			if (!engineCrankFirstTime)
			{
				engineCrankFirstTime = true;

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
				exhaust.injectAction("Logic", "Engine", PlayMakerExtentions.injectEnum.insert, updateExhaust, index: 5);

				turbo.onAssemble += exhaustCheck;
				turbo.onDisassemble += exhaustCheck;
				headers.onAssemble += exhaustCheck;
				headers.onDisassemble += exhaustCheck;
				downPipe.onAssemble += exhaustCheck;
				downPipe.onDisassemble += exhaustCheck;
			}
			checkRoutinesRunning();
			
			TurboMod.print(message);
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
				maxSafeTorque = 185;

				initialRpm = 2500;
				maxBoostRpm = 6400;
				maxBoostFuel = 18 * (Fuelpumpefficiency.Value * 60 + 0.25f);
			}
			if (carbInstall)
			{
				maxSafeTorque = 130;

				initialRpm = 2500;
				maxBoostRpm = 6000;
				maxBoostFuel = 36 * (Fuelpumpefficiency.Value * 60 + 0.25f);
			}
		}
		private void updateBoostMultiplier()
		{
			if (isFiltered)
			{
				if (highFlowFilter.installed)
					boostMultiplier = 0.0001275f;
				if (filter.installed)
					boostMultiplier = 0.000125f;
			}
			else
			{
				boostMultiplier = 0.0013f;
			}
		}
		private void onRaceCarbPurchased()
		{
			GameObject.Find("racing carburators(Clone)").FsmInject("Remove part", checkRoutinesRunning);
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
			turbofan.SetActive(!turboDestroyed || !(isFiltered && downPipe.installed && turbo.installed && headers.installed));
		}

		#endregion
	}
}
