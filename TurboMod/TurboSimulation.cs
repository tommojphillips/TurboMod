using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MSCLoader;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using TommoJProductions.ModApi.v0_1_3_0_alpha.Attachable;

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
		private bool GUIdebug = false;
		private float afrFactor;
		private float BLOWCHANCE;
		private float timeBoost;
		private float turbospin;
		private float throtsmooth;
		private float wgspool;
		private float vacsmooth;
		private FsmFloat engineTemp;
		private FsmFloat THROTPEDAL;
		private FsmFloat AIRDENSE;
		private FsmFloat STRESS = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerStress");
		private FsmFloat SPEEDKMH = PlayMakerGlobals.Instance.Variables.FindFsmFloat("SpeedKMH");
		private FsmState calculateMixtureState;
		private GameObject turbofan;
		private GameObject n2otrigger;
		private GameObject stockfiltertrigger;
		private GameObject soundSpool;
		private GameObject turboneedleobject;
		private GameObject TURBOMESH;
		private GameObject TURBOMESH_D;
		private GameObject engine;
		private GameObject FIRE;
		private GameObject soundFlutter;
		private GameObject PINGING;
		private GameObject headertriggers;
		private GameObject exhaust_fromEngine;
		private GameObject exhaust_fromMuffler;
		private GameObject exhaust_fromPipe;
		private GameObject exhaust_fromHeaders;
		private GUIStyle guiStyle = new GUIStyle();

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
		internal float maxboostfuel;
		internal float turboNeedleBoost;
		internal float wastegatePSI;
		internal float PSI;

		//Public fields
		public bool carbinstall;
		public bool isPiped;
		public bool isFiltered;
		public bool isExhaust;
		public bool raceCarbinstall;
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
		public float maxsafeTorque;
		public float motorStress;
		public float turboFriction;
		public float turboSpool;
		public float turbowearrate;
		public float rpmAtMaxBoost;
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
		internal float PSIRART;

		internal turboSimulationSaveData loadedSaveData;
		internal turboSimulationSaveData defaultSaveData => new turboSimulationSaveData() { turboDestroyed = false, turboWear = Random.Range(75, 100) + (Random.Range(0, 100) * 0.001f), wastegatePsi = 8.25f };

		private Coroutine oilCoolerRoutine;
		private Coroutine turboRoutine;
		private Coroutine pipeRoutine;

		#endregion

		#region Properties

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
			carbPipe.onAssemble += updateStockCarbInstall;
			carbPipe.onDisassemble += updateStockCarbInstall;
			// race carb install check
			coldSidePipe.onAssemble += updateRaceCarbInstall;
			coldSidePipe.onDisassemble += updateRaceCarbInstall;
			hotSidePipe.onAssemble += updateRaceCarbInstall;
			hotSidePipe.onDisassemble += updateRaceCarbInstall;
			intercooler.onDisassemble += updateRaceCarbInstall;
			intercooler.onAssemble += updateRaceCarbInstall;

			// Boost gauge needle
			boostGauge.onAssemble += boostGaugeNeedleReset;
			boostGauge.onDisassemble += boostGaugeNeedleReset;

			// oil cooler pipes
			oilCooler.onAssemble += oilCoolOnAssemble;
			oilCooler.onDisassemble += oilCoolOnDisassemble;
		}
        private void OnDisable()
		{
			// filter boost multipier check
			filter.onAssemble -= updateBoostMultiplier;
			filter.onDisassemble -= updateBoostMultiplier;
			highFlowFilter.onAssemble -= updateBoostMultiplier;
			highFlowFilter.onDisassemble -= updateBoostMultiplier;

			// carb install check
			carbPipe.onAssemble -= updateStockCarbInstall;
			carbPipe.onDisassemble -= updateStockCarbInstall;
			// race carb install check
			coldSidePipe.onAssemble -= updateRaceCarbInstall;
			coldSidePipe.onDisassemble -= updateRaceCarbInstall;
			hotSidePipe.onAssemble -= updateRaceCarbInstall;
			hotSidePipe.onDisassemble -= updateRaceCarbInstall;
			intercooler.onDisassemble -= updateRaceCarbInstall;
			intercooler.onAssemble -= updateRaceCarbInstall;

			// Boost gauge needle
			boostGauge.onAssemble -= boostGaugeNeedleReset;
			boostGauge.onDisassemble -= boostGaugeNeedleReset;

			// oil cooler pipes
			oilCooler.onAssemble -= oilCoolerOnAssemble;
			oilCooler.onDisassemble -= oilCoolerOnDisassemble;

		}
		private void Awake()
		{
			initSimulation();
			initEngineState();
		}
		private void Update()
		{
			checkRoutinesRunning();
		}
		private void LateUpdate()
		{
			if (cInput.GetButton("DrivingMode") && cInput.GetButton("Finger") && adjustTime >= 4)
			{
				GUIdebug = !GUIdebug;
				if (!GUIdebug)
				{
					Interacttext.Value = "Turbo Debug UI: ENABLED";
					adjustTime = 0f;
					ModConsole.Log("[turbomod]: Debug GUI: Enabled");
				}
				else
				{
					Interacttext.Value = "Turbo Debug UI: DISABLED";
					adjustTime = 0f;
					ModConsole.Log("[turbomod]: Debug GUI: Disabled");
				}
			}
			if (adjustTime != 4)
			{
				adjustTime += 0.125f;
			}
			if (isPlayerLookingAt(turbofan.gameObject))
			{
				turboCondCheck();
			}
			if (isPlayerLookingAt(act.gameObject))
			{
				wasteGateAdjust();
			}
			partCheck();

			// boost gauge needle rotation update
			if (turboneedleobject.transform.localEulerAngles.z != boostneedle)
				turboneedleobject.transform.localEulerAngles = new Vector3(0f, 0f, boostneedle);
		}

        #region OnGUI & GUI Fields

        private string whistleModifierString = "";
		private string flutterModifierString = "";
		private float whistleModifier = 1.1f;
		private float flutterModifier = 1.3563f;

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
					GUILayout.Label($"Whistle Mod: {whistleModifier}", guiStyle, new GUILayoutOption[0]);
					whistleModifierString = GUILayout.TextField(whistleModifier.ToString(), 10, guiStyle, new GUILayoutOption[0]);
					float.TryParse(whistleModifierString, out whistleModifier);
					GUILayout.Label($"Flutter Mod: {flutterModifier}", guiStyle, new GUILayoutOption[0]);
					flutterModifierString = GUILayout.TextField(flutterModifier.ToString(), 10, guiStyle, new GUILayoutOption[0]);
					float.TryParse(flutterModifierString, out flutterModifier);
					GUILayout.Label("TURBO STATS", guiStyle, new GUILayoutOption[0]);
					GUILayout.Label($"TurboRPM:		   {Math.Round(turboRPM)}/{Math.Round(wastegateRPM)}", guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("WasegateRPM:      " + Math.Round(wastegateRPM, 2).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("PSI Boost:        " + PSI.ToString("0.00"), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("WasegatePSI:      " + Math.Round(wastegatePSI, 2).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("FuelStarve Boost: " + Math.Round(maxboostfuel, 2).ToString(), guiStyle, new GUILayoutOption[0]);
					GUILayout.Label("AFR factor:       " + Math.Round(afrFactor, 2).ToString(), guiStyle, new GUILayoutOption[0]);
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
					GUILayout.Label("AFR:             " + Math.Round(AFR.Value, 2).ToString(), guiStyle, new GUILayoutOption[0]);
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
		private bool isPlayerLookingAt(GameObject gameObject)
		{
			if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1f, 1 << gameObject.layer))
			{
				if (hit.collider.gameObject == gameObject)
					return true;
			}
			return false;
		}
		private void initEngineState()
		{
			checkAnyConflictingPart();
			partCheck();
			updateStockCarbInstall();
			updateRaceCarbInstall();
			updateBoostMultiplier();
			exhaustcheck();

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
				GameObject satsuma = GameObject.Find("SATSUMA(557kg, 248)");
				GameObject carSimulation = satsuma.transform.Find("CarSimulation").gameObject;
				engine = carSimulation.transform.Find("Engine").gameObject;
				GameObject exhaust = carSimulation.transform.Find("Exhaust").gameObject;
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
				FUEL = engine.transform.Find("Fuel").GetComponent<PlayMakerFSM>();
				FUELevent = engine.transform.Find("Fuel").GetComponents<PlayMakerFSM>()[1];
				calculateMixtureState = FUELevent.FsmStates.First(_state => _state.Name == "Calculate mixture");
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
				/*// Exhaust check inject
				GameObject.Find("headers(Clone)").FsmInject("Remove part", onHeaderRemove);
				GameObject.Find("steel headers(Clone)").FsmInject("Remove part", onHeaderRemove);
				head.transform.Find("Triggers Headers/trigger_headers").gameObject.FsmInject("Assemble", onHeaderAssemble);
				head.transform.Find("Triggers Headers/trigger_steel headers").gameObject.FsmInject("Assemble", onHeaderAssemble);
				GameObject.Find("SATSUMA(557kg, 248)/MiscParts/Triggers Exhaust Pipes/trigger_exhaust pipe").FsmInject("Assemble 2", exhaustcheck);
				GameObject.Find("SATSUMA(557kg, 248)/MiscParts/Triggers Exhaust Pipes/trigger_racing exhaust").FsmInject("Assemble 2", exhaustcheck);
				GameObject.Find("exhaust pipe(Clone)").FsmInject("Remove part", exhaustcheck);
				GameObject.Find("racing exhaust(Clone)").FsmInject("Remove part", exhaustcheck);
				GameObject.Find("SATSUMA(557kg, 248)/MiscParts/Triggers Mufflers/trigger_racing_muffler").FsmInject("Assemble 2", exhaustcheck);
				GameObject.Find("SATSUMA(557kg, 248)/MiscParts/Triggers Mufflers/trigger_exhaust_muffler").FsmInject("Assemble 2", exhaustcheck);
				GameObject.Find("racing muffler(Clone)").FsmInject("Remove part", exhaustcheck);
				GameObject.Find("exhaust muffler(Clone)").FsmInject("Remove part", exhaustcheck);*/

				// Carb install set up check inject
			 	head.transform.Find("Triggers Carbs/trigger_carburator").gameObject.FsmInject("Assemble", updateStockCarbInstall);
				head.transform.Find("Triggers Carbs/trigger_carburator_racing").gameObject.FsmInject("Assemble", updateRaceCarbInstall);
				GameObject.Find("carburator(Clone)").FsmInject("Remove part", updateStockCarbInstall);
				GameObject.Find("racing carburators(Clone)").FsmInject("Remove part", updateRaceCarbInstall);

				// rwd
				drivetrain.transmission = Drivetrain.Transmissions.RWD;
				drivetrain.differentialLockCoefficient = 100;
				drivetrain.clutchMaxTorque = 800;
				drivetrain.clutchTorqueMultiplier = 4;
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
			turbofan.SetActive(!turboDestroyed || !(isFiltered && downPipe.installed && turbo.installed && headers.installed));

			if (!headers.installed)
			{
				if (turboRPM > 0)
					turboRPM -= turboSpool;
				else if (turboRPM < 0)
					turboRPM = 0;
			}

			isFiltered = filter.installed || highFlowFilter.installed;
			
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
		public void exhaustcheck()
		{
			isExhaust = headers.installed && turbo.installed && downPipe.installed;
			raceheadersinstalled.Value = isExhaust;
		}
		public void exhaustCrackle()
		{
			if (PSI > 3.0 & Random.Range(0f, PSI + 10f) > 20f)
			{
				BACKFIRE.SendEvent("TIMINGBACKFIRE");
			}
		}
		public void turbosounds()
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
		}
		public void turboRepairEvent()
		{
			wearMult = Random.Range(0.5f, 9f);
			turboDestroyed = false;
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
				afrFactor = AFR.Value * drivetrain.throttle * 14.7f;
				if (PSI > maxboostfuel)
				{
					timeBoost += Time.deltaTime;
					if (timeBoost > Random.Range(3f, 12f))
					{
						drivetrain.revLimiterTriggered = true;
						BACKFIRE.SendEvent("TIMINGBACKFIRE");
						wearMult += Random.Range(0.3f, 3f);
						timeBoost = 0f;
					}
				}
				drivetrain.powerMultiplier *= rpmAtMaxBoost * boostMultiplier + PSI / 22;
				(calculateMixtureState.Actions[4] as FloatOperator).float2 = AIRDENSE.Value - PSIRART;
				//(FUELevent.FsmStates.First((FsmState state) => state.Name == "Calculate mixture").Actions[4] as FloatOperator).float2 = AIRDENSE.Value - PSIRART;
				if (PSI > 1.0)
				{
					engineTemp.Value += drivetrain.powerMultiplier * 4f * drivetrain.throttle * coolMult;
					drivetrain.revLimiterTime = 0.1f;
				}
				else
				{
					drivetrain.revLimiterTime = 0.2f;
				}
				//Pistonwear = PISTON1wear.Value + PISTON2wear.Value + PISTON3wear.Value + PISTON4wear.Value;

				yield return null;
			}
			if (boostGauge.installed)
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
					PSIRART = PSI / 100f;
					turboSpool = drivetrain.rpm * 1.75f * (drivetrain.throttle + 0.05f) + drivetrain.rpm * 0.08f + drivetrain.currentPower / 650f / 10f * drivetrain.throttle;
					if (drivetrain.rpm > rpmAtMaxBoost)
					{
						turboSpool *= rpmAtMaxBoost / 2650f - drivetrain.rpm / (rpmAtMaxBoost * 0.61f);
					}
					if (initialRpm > drivetrain.rpm)
					{
						turboSpool *= initialRpm / 14200f + drivetrain.rpm / (initialRpm * 0.95f);
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
					turbosounds();
					if (wear)
					{
						turboDestroyed = wearMult >= 100;

						turbowearrate = turboRPM / 140000000f * 0.12f;

						if (!oilLines.installed || OilLevel.Value < 1.5f)
						{
							wearMult += turbowearrate * 20f;
						}
						else
							wearMult += turbowearrate;

						if (turboDestroyed)
						{
							turboDestroyEvent();
						}
						else if (wearMult <= 1)
						{
							turboRepairEvent();
						}
					}
					if (drivetrain.torque > maxsafeTorque || afrFactor > 15)
					{
						if (!PINGING.activeInHierarchy)
							PINGING.SetActive(true);
						motorStress += drivetrain.torque - maxsafeTorque / 20000f;

						if (motorStress > 24500)
							BACKFIRE.SendEvent("TIMINGBACKFIRE");
						if (wear)
						{
							if (motorStress > 24500)
							{
								bool flag21 = Random.Range(0, 1) > 0.5f;
								if (flag21)
								{
									FIRE.SetActive(true);
								}
								BACKFIRE.SendEvent("TIMINGBACKFIRE");
								BACKFIRE.SendEvent("TIMINGBACKFIRE");
								BACKFIRE.SendEvent("TIMINGBACKFIRE");
								BACKFIRE.SendEvent("TIMINGBACKFIRE");
								MOTORBLOW.SendEvent("FINISHED");
								BACKFIRE.SendEvent("TIMINGBACKFIRE");
							}
							if (motorStress >= 3350)
							{
								HEADGASKETwear.Value -= 0.01f;
							}
							if (motorStress >= 6500)
							{
								if (PISTON1wear.Value > 9.5)
								{
									PISTON1wear.Value -= 0.009f;
								}
								if (PISTON2wear.Value > 9.5)
								{
									PISTON2wear.Value -= 0.009f;
								}
								if (PISTON3wear.Value > 9.5)
								{
									PISTON3wear.Value -= 0.009f;
								}
								if (PISTON4wear.Value > 9.5)
								{
									PISTON4wear.Value -= 0.009f;
								}
							}
						}
					}
					else
					{
						motorStress *= 0.99f;
					}
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
		private void oilCoolerOnAssemble() 
		{
			oilCooler.transform.GetChild(0).gameObject.SetActive(true);
		}
		private void oilCoolerOnDisassemble()
		{
			oilCooler.transform.GetChild(0).gameObject.SetActive(false);
		}
		private void updateCarbSetup() 
		{
			if (raceCarbinstall)
			{
				maxsafeTorque = 185;
				rpmAtMaxBoost = 6400;
				maxboostfuel = 18 * (Fuelpumpefficiency.Value * 60 + 0.25f);
			}
			if (carbinstall)
			{
				maxsafeTorque = 130;
				rpmAtMaxBoost = 6000;
				//maxboostfuel = 13 * (Fuelpumpefficiency.Value * 60 + 0.25f);
				maxboostfuel = 22 * (Fuelpumpefficiency.Value * 60 + 0.25f);
			}
		}
		private void updateRaceCarbInstall() 
		{
			raceCarbinstall = racecarbinstalled.Value && coldSidePipe.installed && intercooler.installed && hotSidePipe.installed;
			isPiped = carbinstall || raceCarbinstall;
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
		private void updateStockCarbInstall() 
		{
			carbinstall = stockcarbinstalled.Value && carbPipe.installed;
			isPiped = carbinstall || raceCarbinstall;
		}

		#endregion
	}
}
