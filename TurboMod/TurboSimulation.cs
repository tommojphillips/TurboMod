using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TommoJProductions.TurboMod.Parts;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
    internal class TurboSimulation : MonoBehaviour
    {
		// Written, 28.10.2020

		private GameObject turboBlades;
		private int rpmAtMaxBoost = 7500;
		private int initialRpm = 1600;
		private float turboCoolerModifier = 0.007f;
		internal TurboParts turboParts { get { return TurboMod.instance.turboParts; } }
		internal bool throttleUsed { get { return this.drivetrain.idlethrottle > 0; } }
		internal bool throttleButtonUsed { get { return cInput.GetKey("Throttle"); } }
		internal bool anyThrottleUsed { get { return this.throttleButtonUsed || this.throttleUsed; } }
		internal float turboSpoolDelayTime { get; private set; }
		internal float turboSpoolDelayModifier { get; private set; }
		internal float turboRpm { get; private set; }
		internal bool canBlowOff { get; private set; }
		internal float airFuelRatioModifier { get; private set; }
		internal float whistlePitchModifier { get; private set; } = 0.00018f;
		internal float flutterPitch { get; private set; } = 1.33f;
		internal float calculatedBoost { get; private set; }
		internal float wastegatePsi { get; private set; }
		internal float turboDelayModifier { get; private set; } = 0.55f;
		internal float appliedPowerMultiplier { get; private set; }
		internal float boostMultiplier { get; private set; } = 0.00013f;

		#region Data fields

		private FsmFloat fuelPumpEfficiency;
        private FsmFloat enginePowerMultiplier;
        private FsmFloat engineTemp;
        private FsmFloat fuel;
        private FsmFloat coolingAirRateMultiplier;
        private FsmFloat rpm;
		private FsmFloat playerStressLevel;
		private FsmFloat afr;
		private FsmString currentVehicle;
        //gameobjects
        private GameObject satuma;
		private Drivetrain drivetrain;
        private GameObject carSimulation;
		private GameObject engine;
		private GameObject engineFuel;
		//Playmaker
		private PlayMakerFSM carCoolingFSM;
		private PlayMakerFSM backfireEvent;
		private PlayMakerFSM fuelFSM;
		// Audio
		private AudioSource whistleSound;
		private AudioSource flutterSound;

        #endregion

        #region Unity Runtime Methods

        void Start() 
        {
			// Written, 28.10.2020

			this.turboBlades = this.gameObject.transform.FindChild("motor_turbocharger_blades").gameObject;
			this.setValues(); 
			this.turboParts.wastegateActuatorPart.wastegateAdjust.turboSimulation = this;
			if (this.turboParts.wastegateActuatorPart.wastegateAdjust.wastegatePsiToAdd != default)
			{
				this.addWastegatePsi(this.turboParts.wastegateActuatorPart.wastegateAdjust.wastegatePsiToAdd);
				this.turboParts.wastegateActuatorPart.wastegateAdjust.wastegatePsiToAdd = default;
			}

		}
		void Update() 
		{
			// Written, 02.11.2020

			this.debugKeys();

			this.turboSimulation();
		}
		void LateUpdate() 
        {
			// Written, 02.11.2020

			this.turboSimulationLate();
        }
		void OnGUI() 
		{
			// Written, 30.10.2020

			if (this.debugGuiKey.GetKeybindDown())
				this.showDebugGUI = !this.showDebugGUI;
			if (this.showDebugGUI)
			{
				string text = string.Empty;
				try
				{
					string turboStatus = string.Empty;
					if (this.turboParts.isCorePartsInstalled())
						turboStatus = "+ Core parts installed: ";
					else
						turboStatus = "+ Core parts not installed:\n";
					float _calculatedBoost = this.calculateMaxBoost();
					turboStatus += _calculatedBoost + " psi\nDelayed:(" + this.calculatedBoost + ") psi";
					turboStatus += "\n differance: " + (Mathf.Abs(_calculatedBoost - this.calculatedBoost));
					if (this.turboParts.carbPipePart.installed)
						turboStatus += "\n+ Carburator pipe installed";
					else
						turboStatus += "\n+ Carburator pipe not installed";
					if (this.turboParts.oilCoolerPart.installed)
						turboStatus += "\n+ Oil cooler installed";
					else
						turboStatus += "\n+ Oil cooler not installed";
					if (this.turboParts.wastegateActuatorPart.installed)
						turboStatus += "\n+ Wastegate installed";
					else
						turboStatus += "\n+ Wastegate not installed";
					turboStatus += " (" + this.wastegatePsi + "psi)";
					if (this.turboParts.airFilterPart.installed)
						turboStatus += "\n+ Air filter installed";
					else if (this.turboParts.highFlowAirFilterPart.installed)
						turboStatus += "\n+ High flow air filter installed";
					else
						turboStatus += "\n+ No air filter installed";
					turboStatus += "()";
					turboStatus += "\nWhistle pitch modifier (rpm*modifier): " + this.whistlePitchModifier;
					turboStatus += "\nFlutter pitch absolute: " + this.flutterPitch;
					turboStatus += "\nTurbo lag: " + this.turboDelayModifier;
					turboStatus += "\nTurbo spool delay modifier: " + this.turboSpoolDelayModifier;
					turboStatus += "\nTurbo spool delay time: " + this.turboSpoolDelayTime;
					turboStatus += "\nEditing Choice: " + this.adjustWhat;
					turboStatus += "\npotential power modifier: " + this.appliedPowerMultiplier;
					if (this.turboParts.isCorePartsInstalled() && this.turboParts.carbPipePart.installed)
						turboStatus += " (";
					else
						turboStatus += " (NOT";
					turboStatus += " APPLIED)";
					string satsumaStats = string.Format("HP: {0}\nPower modifier: {1}\nTorque: {2}\nEngine temp: {3}",
						Math.Round(this.drivetrain.currentPower, 2), Math.Round(this.enginePowerMultiplier.Value, 2),
						Math.Round(this.drivetrain.torque, 2), Math.Round(this.engineTemp.Value, 2));
					text = string.Format(
						"------------------------------------------------------------" +
						"\nTurbo Stats:\n" + turboStatus + "\n------------------------------------------------------------" +
						"\nSatsuma Stats:\n" + satsumaStats + "\n------------------------------------------------------------");
				}
				catch (Exception ex)
				{
					text = "ERROR {0}";
					TurboMod.print(text , ex.StackTrace);
				}
				finally 
				{
					GUI.Label(new Rect(20, 20, 500, 500), text);
				}
			}
		}

        #endregion

        #region Debug methods

        private bool showDebugGUI = true;
		private int adjustWhat = 0;
		private int numOfChoices = 3;//flutter pitch (0), whistle pitch (1), turbo delay (2)
		private Keybind debugGuiKey = new Keybind("turbo_debugGui", "turbo mod gui", KeyCode.KeypadEnter);
		private Keybind modifierKey = new Keybind("turbo_modifier", "turbo mod modifier", KeyCode.LeftControl);
		private Keybind increaseKey = new Keybind("turbo_increase", "turbo mod increase", KeyCode.KeypadPlus);
		private Keybind decreaseKey = new Keybind("turbo_decrease", "turbo mod decrease, ", KeyCode.KeypadMinus);
		private void debugKeys()
		{
			// Written, 31.10.2020

			if (this.debugGuiKey.GetKeybindDown())
				this.showDebugGUI = !this.showDebugGUI;
			if (this.modifierKey.GetKeybindDown())
				if (adjustWhat < numOfChoices)
					adjustWhat++;
				else if (adjustWhat > 0)
					adjustWhat--;
			float increaseBy = 0;
			if (this.increaseKey.GetKeybind())
				increaseBy = 0.0001f;
			else if (this.decreaseKey.GetKeybind())
				increaseBy = -0.001f;
			if (increaseBy != 0)
				switch (this.adjustWhat)
				{
					case 0: // flutter
						this.flutterPitch += increaseBy;
						break;
					case 1: // whistle
						this.whistlePitchModifier += increaseBy;
						break;
					case 2: // turbo delay (lag)
						this.turboDelayModifier += increaseBy;
						break;
				}
		}

		#endregion

		internal void addWastegatePsi(float inWastegatePsi)
		{
			// Written, 31.10.2020

			this.wastegatePsi += inWastegatePsi;
		}
		private void turboBlowOff() 
		{
			// Written, 30.10.2020

			this.turboSpoolDelayTime /= 2;
			this.canBlowOff = false;
			this.flutterSound.pitch = this.flutterPitch;
			if (!this.flutterSound.isPlaying)
				this.flutterSound.Play();
			if (this.turboParts.airFilterPart.installed)
				this.flutterSound.volume = 0.35f;
			else if (this.turboParts.highFlowAirFilterPart.installed)
				this.flutterSound.volume = 0.65f;
			else
				this.flutterSound.volume = 0.85f;
			this.playerStressLevel.Value -= 3f;
		}
        private void exhaustCrackle(bool inForce = false)
        {
			// Written, 28.10.2020

			if ((UnityEngine.Random.Range(0f, this.calculatedBoost + 10f) > 20 && this.calculatedBoost > 3) || inForce)
				this.backfireEvent.SendEvent("TIMINGBACKFIRE");
        }
		private void setValues()
		{
			// Written, 28.10.2020

			try
			{
				// GameObjects
				this.satuma = GameObject.Find("SATSUMA(557kg, 248)");
				this.drivetrain = this.satuma.GetComponent<Drivetrain>();
				this.drivetrain.clutchTorqueMultiplier = 10f;
				this.carSimulation = this.satuma.transform.FindChild("CarSimulation").gameObject;
				this.engine = this.carSimulation.transform.FindChild("Engine").gameObject;
				this.whistleSound = this.gameObject.transform.FindChild("turbospool").gameObject.GetComponent<AudioSource>();
				this.whistleSound.minDistance = 1;
				this.whistleSound.maxDistance = 10;
				this.whistleSound.spatialBlend = 1;
				this.whistleSound.loop = true; 
				this.flutterSound = this.gameObject.transform.FindChild("flutter").gameObject.GetComponent<AudioSource>();
				this.flutterSound.minDistance = 1;
				this.flutterSound.maxDistance = 10;
				this.flutterSound.spatialBlend = 1;
				this.flutterSound.loop = false;
				this.flutterSound.mute = false;
				// PlaymakerFSM
				this.carCoolingFSM = this.carSimulation.transform.FindChild("Car/Cooling").gameObject.GetComponent<PlayMakerFSM>();
				this.backfireEvent = this.engine.transform.FindChild("Symptoms").gameObject.GetComponent<PlayMakerFSM>();
				this.engineFuel = this.engine.transform.FindChild("Fuel").gameObject;
				this.fuelFSM = this.engineFuel.gameObject.GetComponent<PlayMakerFSM>();
				this.afr = this.engineFuel.GetComponents<PlayMakerFSM>()[1].FsmVariables.FindFsmFloat("Mixture");
				this.fuelPumpEfficiency = this.fuelFSM.FsmVariables.GetFsmFloat("FuelPumpEfficiency");
				this.coolingAirRateMultiplier = this.carCoolingFSM.FsmVariables.FindFsmFloat("CoolingAirRateModifier");
				this.playerStressLevel = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerStress");
				//this.engineOilFSM = this.oil
				this.enginePowerMultiplier = PlayMakerGlobals.Instance.Variables.FindFsmFloat("EnginePowerMultiplier");
				this.engineTemp = PlayMakerGlobals.Instance.Variables.FindFsmFloat("EngineTemp");
				this.fuel = PlayMakerGlobals.Instance.Variables.FindFsmFloat("Fuel");
				this.currentVehicle = PlayMakerGlobals.Instance.Variables.FindFsmString("PlayerCurrentVehicle");
				this.rpm = PlayMakerGlobals.Instance.Variables.FindFsmFloat("RPM");
			}
			catch (Exception ex)
			{
				TurboMod.print("ERROR: {0}", ex.StackTrace);
				throw ex;
			}
		}
		private void boostCalculation() 
		{
			// Written, 02.11.2020

			float _calBoost = this.calculateMaxBoost();
			float _delayedBoost = this.turboDelay(_calBoost, this.turboDelayModifier, 1);
			this.calculatedBoost = _delayedBoost;
		}
		private float calculateMaxBoost() 
		{
			// Written, 02.11.2020

			if (this.rpm.Value < this.initialRpm)
				return this.wastegatePsi * (this.initialRpm / this.drivetrain.maxRPM);
			else if (this.rpm.Value < this.rpmAtMaxBoost)
				return this.wastegatePsi * (this.rpm.Value / this.drivetrain.maxRPM);
			else
				return this.wastegatePsi * (this.rpmAtMaxBoost / this.drivetrain.maxRPM);
		}
		private void adjustAFR() 
		{
			// Written, 31.10.2020


		}
		private void turboSounds()
		{
			// Written, 30.10.2020

			// Whistle
			if (!this.whistleSound.isPlaying)
				this.whistleSound.Play();
			if (this.turboParts.airFilterPart.installed)
			{
				this.whistleSound.volume = this.rpm.Value * 3E-05f;
				this.whistleSound.pitch = (this.rpm.Value - 500) * 0.0003f;
			}
			else if (this.turboParts.highFlowAirFilterPart.installed)
			{
				this.whistleSound.volume = this.rpm.Value * 3E-05f;
				this.whistleSound.pitch = (this.rpm.Value - 500) * 0.000575f;
			}
			else
			{
				this.whistleSound.volume = this.rpm.Value * 2.6E-05f;
				this.whistleSound.pitch = (this.rpm.Value - 250) * this.whistlePitchModifier;//0.00045f;
			}
			//
			// Logic
			this.whistleSound.volume *= this.calculatedBoost / this.wastegatePsi;
			if (this.anyThrottleUsed)
			{
				if (this.turboSpoolDelayTime >= 1)
					this.canBlowOff = true;
			}
			else 
			{
				if (this.canBlowOff && this.calculatedBoost > 1)
					this.turboBlowOff();
				this.exhaustCrackle();
			}
		}
		private float turboDelay(float calculatedBoost, float delay, float cut)
		{
			// Written, 02.11.2020

			float delayedBoost = calculatedBoost * Mathf.Lerp(0, 1, this.turboSpoolDelayTime);
			float deltaTimeDelay = Time.deltaTime * delay;
			if (this.throttleButtonUsed)
			{
				if (this.turboSpoolDelayTime <= cut)
					this.turboSpoolDelayTime += deltaTimeDelay;
			}
			else
			{
				if (this.turboSpoolDelayTime >= 0)
					this.turboSpoolDelayTime -= deltaTimeDelay;
			}
			return delayedBoost;
		}
		private void spinTurboBlades() 
		{
			// Written, 31.10.2020

			if (string.IsNullOrEmpty(this.currentVehicle.Value))
				this.turboBlades.transform.Rotate(new Vector3(this.rpm.Value / 500, 0f, 0f));
		}
		private void coolOil() 
		{
			// Written, 31.10.2020

			if (this.engineTemp.Value > 65)
				this.engineTemp.Value -= this.turboCoolerModifier;
			this.coolingAirRateMultiplier.Value = this.turboCoolerModifier;
		}
		private void wastegateCheck()
		{
			// Written, 01.11.2020

			if (!this.turboParts.wastegateActuatorPart.installed && this.wastegatePsi != this.turboParts.wastegateActuatorPart.wastegateAdjust.wastegateMinPsi)
			{
				this.wastegatePsi = this.turboParts.wastegateActuatorPart.wastegateAdjust.wastegateMinPsi;
			}
			else if (this.wastegatePsi == default)
				this.wastegatePsi = this.turboParts.wastegateActuatorPart.wastegateAdjust.wastegateMinPsi;
		}
		private void applyBoost() 
		{
			// Written, 31.10.2020

			this.appliedPowerMultiplier = this.rpmAtMaxBoost * this.boostMultiplier + this.calculatedBoost / this.turboParts.wastegateActuatorPart.wastegateAdjust.wastegateMaxPsi;
			if (this.appliedPowerMultiplier > 0)
			this.enginePowerMultiplier.Value = this.appliedPowerMultiplier;
		}
		private void turboSimulation()
		{
			// Written, 30.10.2020

			if (this.engine.activeInHierarchy)
			{
				this.wastegateCheck();
				if (this.turboParts.oilCoolerPart.installed)
					this.coolOil();
				if (this.turboParts.isCorePartsInstalled())
				{
					//this.calculateTurboRpm();
					this.spinTurboBlades();
					this.boostCalculation();
					this.turboSounds();					
				}
			}
		}
		private void turboSimulationLate() 
		{
			// Written, 02.11.2020

			if (this.engine.activeInHierarchy)
				if (this.turboParts.isCorePartsInstalled())
					if (this.turboParts.carbPipePart.installed)
					{
						this.adjustAFR();
						this.applyBoost();
					}

		}
		private void calculateTurboRpm()
		{
			// Written, 31.10.2020

			if (this.rpm.Value < this.initialRpm)
				this.turboRpm = this.initialRpm / 14200f + this.rpm.Value / (this.initialRpm * 0.95f);
			else if (this.rpm.Value > this.rpmAtMaxBoost)
				this.turboRpm = (this.rpmAtMaxBoost * 1.75f * (this.drivetrain.throttle + 0.05f) + this.rpmAtMaxBoost * 0.08f + this.drivetrain.currentPower / 650f / 10f * this.drivetrain.throttle) * this.wastegatePsi;
			else
				this.turboRpm = (this.rpm.Value * 1.75f * (this.drivetrain.throttle + 0.05f) + this.rpm.Value * 0.08f + this.drivetrain.currentPower / 650f / 10f * this.drivetrain.throttle) * this.wastegatePsi;
		}

		// Not Mine
		/*public void MainSim()
		{
			// Written, 28.10.2020

			try
			{
				if (this.engine.activeInHierarchy && this.turboParts.isCorePartsInstalled())
				{
					/*if (this.rpm.Value <= this.rpmAtMaxBoost)
						this.calculatedBoost = this.calculateBoost();

					if (this.turboParts.oilCoolerPart.installed)
					{
						if (this.engineTemp.Value > 95)
							this.engineTemp.Value -= turboCoolerModifier;
						this.coolingAirRateMultiplier.Value = turboCoolerModifier;
					}
					if (this.rpm.Value > this.rpmAtMaxBoost)
						this.turboSpool *= this.rpmAtMaxBoost / 2650f - this.rpm.Value / (this.rpmAtMaxBoost * 0.61f);
					else if (this.rpm.Value < initialRpm)
						this.turboSpool *= initialRpm / 14200f + this.rpm.Value / (initialRpm * 0.95f);
					else
						this.turboSpool = this.rpm.Value * 1.75f * (this.drivetrain.throttle + 0.05f) + this.rpm.Value * 0.08f + this.drivetrain.currentPower / 650f / 10f * this.drivetrain.throttle;//this.rpm.Value * 1.75f * (this.carController.throttle + 0.05f) + this.rpm.Value * 0.08 + this.enginePower.Value / 660 * this.carController.throttle;
					if (this.turboParts.wastegateActuatorPart.installed & this.turboTargetRpm != this.wastegateRpm)
						this.turboTargetRpm = this.wastegateRpm;
					this.turboRpm = Mathf.Clamp(this.turboRpm, 0f, this.turboTargetRpm * 5f);
					this.wastegateSpool = this.wastegateRpm / 22f - this.friction * 3f;
					if (this.turboRpm > this.turboTargetRpm * 0.975 && this.turboParts.wastegateActuatorPart.installed)
						this.turboSpool = Mathf.Clamp((float)this.turboSpool, 120f, this.wastegateSpool);
					else
						this.turboSpool = Mathf.Clamp((float)this.turboSpool, 120f, 50000f);
					if (string.IsNullOrEmpty(this.currentVehicle.Value))
						this.turboBlades.transform.localEulerAngles = new Vector3(this.turboSpin, 0f, 0f);
					this.turboRpm += (float)this.turboSpool;
					this.turboSpin += this.turboRpm;
					this.turboSounds();
					if (this.turboParts.carbPipePart.installed)
					{
						this.airFuelRatioModifier = this.afr.Value * this.drivetrain.throttle * 14.7f;
						this.maxBoostFuel = 13f * (this.fuelPumpEfficiency.Value * 60f + 0.25f);
						if (this.calculatedBoost > this.maxBoostFuel)
						{
							this.timeBoosting += Time.deltaTime;
							if (this.timeBoosting > UnityEngine.Random.Range(3, 12))
							{
								this.exhaustCrackle();
								this.timeBoosting = 0;
							}
							this.enginePowerMultiplier.Value = this.rpmAtMaxBoost * this.boostMultiplier + calculatedBoost / 22f;
						}
						if (this.calculatedBoost > 1)
							this.drivetrain.revLimiterTime = 0.1f;
					}
				}
				else if (this.whistleSound.isPlaying)
				{
					this.whistleSound.Stop();
				}
			}
			catch (Exception ex)
			{
				TurboMod.print("[MainSimulation] An error occured\nError: {0}", ex.StackTrace);
			}
			/*this.turbosounds();
			bool flag10 = this.isPiped;
			if (flag10)
			{
				this.afrFactor = this.AFR.Value * this.drivetrain.throttle * 14.7f;
				bool flag11 = this.PSIRound > (double)this.maxboostfuel;
				if (flag11)
				{
					this.timeBoost += Time.deltaTime;
					bool flag12 = this.timeBoost > Random.Range(3f, 12f);
					if (flag12)
					{
						this.drivetrain.revLimiterTriggered = true;
						this.BACKFIRE.SendEvent("TIMINGBACKFIRE");
						this.wearMult += Random.Range(0.3f, 3f);
						this.timeBoost = 0f;
					}
				}
				this.drivetrain.powerMultiplier *= this.rpmAtMaxBoost * this.boostMultiplier + this.PSI / 22f;
				(this.FUELevent.FsmStates.First((FsmState state) => state.Name == "Calculate mixture").Actions[4] as FloatOperator).float2 = this.AIRDENSE.Value - this.PSIRART;
				bool flag13 = this.PSIRound > 1.0;
				if (flag13)
				{
					this.engineTemp.Value += this.drivetrain.powerMultiplier * 4f * this.drivetrain.throttle * this.CoolMult;
					this.drivetrain.revLimiterTime = 0.1f;
				}
				else
				{
					this.drivetrain.revLimiterTime = 0.2f;
				}
				bool flag14 = turbosim.filter.isFitted & this.boostMultiplier != 0.000125f;
				if (flag14)
				{
					this.boostMultiplier = 0.000125f;
				}
				bool flag15 = turbosim.HKSfilter.isFitted & this.boostMultiplier != 0.0001275f;
				if (flag15)
				{
					this.boostMultiplier = 0.0001275f;
					this.wearMult += this.turbowearrate / 12f;
				}
				bool flag16 = !this.isFiltered & this.boostMultiplier != 0.00013f;
				if (flag16)
				{
					this.boostMultiplier = 0.00013f;
					this.wearMult += this.turbowearrate / 2f;
				}
				this.Pistonwear = this.PISTON1wear.Value + this.PISTON2wear.Value + this.PISTON3wear.Value + this.PISTON4wear.Value;
			}
			bool flag17 = !turbosim.oil_line.isFitted || this.OilLevel.Value < 1.5f;
			if (flag17)
			{
				this.wearMult += this.turbowearrate * 20f;
			}
			else
			{
				this.wearMult += this.turbowearrate;
			}
			bool flag18 = this.drivetrain.torque > this.maxsafeTorque || this.afrFactor > 15f;
			if (flag18)
			{
				bool flag19 = !this.PINGING.activeInHierarchy;
				if (flag19)
				{
					this.PINGING.SetActive(true);
				}
				this.motorStress += this.drivetrain.torque - this.maxsafeTorque / 20000f;
				this.motorStressint = this.motorStress / 2.5f;
				bool flag20 = this.motorStressint >= 24500f;
				if (flag20)
				{
					bool flag21 = (float)Random.Range(0, 1) > 0.5f;
					if (flag21)
					{
						this.FIRE.SetActive(true);
					}
					this.MOTORBLOW.SendEvent("FINISHED");
					this.BACKFIRE.SendEvent("TIMINGBACKFIRE");
				}
				bool flag22 = this.motorStressint >= 3350f;
				if (flag22)
				{
					this.HEADGASKETwear.Value -= 0.01f;
				}
				bool flag23 = this.motorStressint >= 6500f;
				if (flag23)
				{
					bool flag24 = this.PISTON1wear.Value > 9.5f;
					if (flag24)
					{
						this.PISTON1wear.Value -= 0.009f;
					}
					bool flag25 = this.PISTON2wear.Value > 9.5f;
					if (flag25)
					{
						this.PISTON2wear.Value -= 0.009f;
					}
					bool flag26 = this.PISTON3wear.Value > 9.5f;
					if (flag26)
					{
						this.PISTON3wear.Value -= 0.009f;
					}
					bool flag27 = this.PISTON4wear.Value > 9.5f;
					if (flag27)
					{
						this.PISTON4wear.Value -= 0.009f;
					}
				}
			}
			else
			{
				this.motorStress *= 0.99f;
			}
		}
		else
		{
			bool activeInHierarchy = this.soundSpool.activeInHierarchy;
			if (activeInHierarchy)
			{
				this.soundSpool.SetActive(false);
				this.turboRPM = 0f;
			}
		}
		}*/
	}
}
