using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

using MSCLoader;

using TommoJProductions.ModApi;
using TommoJProductions.ModApi.Database;

using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public class VehiclePushStartLogic : MonoBehaviour
    {
        private Drivetrain drivetrain;
        private CarDynamics carDynamics;
        private CarController carController;

        private GameObject engine;
        private PlayMakerFSM starterFsm;

        private float wheelRotationalInertia;
        private float wheelAngularVelocity;
        private float wheelImpulse;
        private float wheelSpeed;
        private float clutchSpeed;
        public static float clutchDragImplulse;

        /// <summary>
        /// if true, enables gear reset to netural when engine starts.
        /// </summary>
        private bool starterHelpActive = false;

        private GameObject electrics;
        private PlayMakerFSM electricsFsm;
        private FsmBool electricsOk;
        private FsmBool batteryInstalled;
        private FsmFloat batteryCharge;
        private FsmBool batteryPlusTerminalBolted;
        private FsmBool batteryMinusTerminalBolted;
        private FsmBool starting;
        private FsmBool acc;
        private FsmBool fuelGaugeInstalled2;
        private FsmBool fuelPrimed;
        public static bool engineWillStart = true;
        public static bool pushStartLogicEnabled = true;
        private FsmState starterStallEngineState;
        private FsmBool shutOff;
        private PlayMakerFSM backfire;
        internal static float engineSpeed;
        internal static bool pushStarting;
        private bool keyBanging;
        private int unburnedFuel;

        private bool engineOn => engine.activeInHierarchy;
        private bool starterMotorHasPower => batteryInstalledAndBolted;
        private bool batteryInstalledAndBolted => batteryInstalled.Value && batteryMinusTerminalBolted.Value && batteryPlusTerminalBolted.Value;

        #region Unity Runtime

        void Start()
        {
            // Written, 13.11.2022

            Satsuma satsuma = Database.databaseVehicles.satsuma;

            engine = satsuma.engine;
            drivetrain = satsuma.drivetrain;
            carDynamics = satsuma.carDynamics;
            carController = satsuma.carDynamics.carController;

            Transform starter = satsuma.carSimulation.transform.Find("Car/Starter");
            starterFsm = starter.GetPlayMaker("Starter");

            electrics = satsuma.carSimulation.transform.Find("Car/Electrics").gameObject;
            electricsFsm = electrics.GetPlayMaker("Electrics");
            electricsOk = electricsFsm.FsmVariables.FindFsmBool("ElectricsOK");

            FsmState startHelpState = starter.GetPlayMaker("StartHelp").GetState("State 1");

            FsmState batteryState = electricsFsm.GetState("Battery");
            FsmBool battery = electricsFsm.FsmVariables.FindFsmBool("Battery");
            FsmBool installed1 = electricsFsm.FsmVariables.FindFsmBool("Installed1");
            FsmBool installed2 = electricsFsm.FsmVariables.FindFsmBool("Installed2");

            FsmBool installed3 = starterFsm.FsmVariables.FindFsmBool("Installed3");

            FsmState wiringState = electricsFsm.GetState("Wiring");
            BoolAllTrue _action;
            List<FsmBool> fsmbools;

            FsmState wiringStarterState = starterFsm.GetState("Wiring");

            FsmState fuelMixtureState = starterFsm.GetState("Fuel Mixture");

            GameObject ignitionGo = satsuma.gameObject.transform.Find("Dashboard/Steering/steering_column2/IgnitionSatsuma").gameObject;
            PlayMakerFSM ignitionUse = ignitionGo.GetComponent<PlayMakerFSM>();
            FsmState ignitionFsm = ignitionUse.GetState("Test");
            acc = ignitionUse.FsmVariables.FindFsmBool("ACC");

            GameObject dbWiring = GameObject.Find("Database/DatabaseWiring");
            batteryInstalled = GameObject.Find("Database/PartsStatus/Battery").GetPlayMaker("Data").FsmVariables.FindFsmBool("Installed");
            batteryCharge = GameObject.Find("Database/PartsStatus/Battery").GetPlayMaker("Data").FsmVariables.FindFsmFloat("Charge");
            batteryMinusTerminalBolted = dbWiring.transform.Find("WiringBatteryMinus").GetPlayMaker("Data").FsmVariables.FindFsmBool("Bolted");
            batteryPlusTerminalBolted = dbWiring.transform.Find("WiringBatteryPlus").GetPlayMaker("Data").FsmVariables.FindFsmBool("Bolted");

            starting = starterFsm.FsmVariables.FindFsmBool("Starting");
            FsmState accFsm = starterFsm.GetState("ACC");

            GameObject fuelGauge = satsuma.gameObject.transform.Find("Dashboard/pivot_dashboard/dashboard(Clone)/pivot_meters/dashboard meters(Clone)/Gauges/Fuel").gameObject;
            PlayMakerFSM fuelGaugeFsm = fuelGauge.GetPlayMaker("Fuel");
            FsmState fuelGaugeTestState = fuelGaugeFsm.GetState("Test");
            fuelGaugeInstalled2 = fuelGaugeFsm.FsmVariables.FindFsmBool("Installed2");

            fuelPrimed = starterFsm.FsmVariables.FindFsmBool("FuelOK");

            FsmState crankUpState = starterFsm.GetState("Crank up");

            FsmState starterWaitState = starterFsm.GetState("Wait");

            starterStallEngineState = starterFsm.GetState("Stall engine");
            shutOff = starterFsm.FsmVariables.FindFsmBool("ShutOff");

            backfire = engine.transform.Find("Symptoms").GetComponent<PlayMakerFSM>();

            // start help
            startHelpState.replaceAction(starterHelp, 0, CallbackTypeEnum.onUpdate, true);
            startHelpState.RemoveAction(1);

            batteryState.replaceAction(electricsBatteryCheck, 1);

            // removing required bools for the wiring state in electrics fsm
            _action = wiringState.Actions[5] as BoolAllTrue;
            fsmbools = _action.boolVariables.ToList();
            fsmbools.Remove(battery);
            fsmbools.Remove(installed1);
            fsmbools.Remove(installed2);
            _action.boolVariables = fsmbools.ToArray();
            wiringState.Actions[5] = _action;

            // removing required bools for the wiring state in starter fsm
            _action = wiringStarterState.Actions[4] as BoolAllTrue;
            fsmbools = _action.boolVariables.ToList();
            fsmbools.Remove(installed3);
            _action.boolVariables = fsmbools.ToArray();
            wiringStarterState.Actions[4] = _action;

            fuelMixtureState.RemoveAction(10);
            fuelMixtureState.replaceAction(crankEngine, 11, CallbackTypeEnum.onUpdate, true);

            ignitionFsm.replaceAction(ignition, 0);

            accFsm.replaceAction(starterMotorCheck, 1);

            // fuel gauge battery check
            fuelGaugeTestState.replaceAction(fuelGaugeBatteryCheck, 1); 

            starterWaitState.RemoveAction(5); // sets drivetrain.rpm to 0.

            (starterWaitState.Actions[18] as Wait).time.Value = 0.1f;

            injectCheckIfKeyAccTurnedOnWhenEngineStalling();

            // Remove engineFrictionFactor assignment to 3.
            starterStallEngineState.RemoveAction(6);

            modifyFrictionMaterials();
        }


        void Update()
        {
            // Written, 13.11.2022

            if (pushStartLogicEnabled)
            {
                wheelRotationalInertia = 0;
                wheelAngularVelocity = 0;
                wheelImpulse = 0;
                for (int i = 0; i < drivetrain.poweredWheels.Length; i++)
                {
                    wheelRotationalInertia += drivetrain.poweredWheels[i].rotationalInertia;
                    wheelAngularVelocity += drivetrain.poweredWheels[i].angularVelocity;
                    wheelImpulse += drivetrain.poweredWheels[i].wheelImpulse;
                }
                wheelSpeed = wheelAngularVelocity * drivetrain.angularVelo2RPM;
                clutchSpeed = wheelSpeed * drivetrain.ratio;
                clutchDragImplulse = getClutchDragImpluse(engineSpeed);

                if (!engineOn)
                {
                    if (!pushStarting)
                    {
                        pushStarting = true;
                        engineSpeed = 0;
                    }
                    else
                    {
                        if (carController.clutchInput < 0.5f)
                        {
                            engineSpeed = Mathf.Clamp(engineSpeed += clutchSpeed * Time.deltaTime, -clutchSpeed, clutchSpeed);

                            if (engineSpeed > drivetrain.minRPM)
                            {
                                pushStartEngine();
                                pushStarting = false;
                            }
                        }
                    }
                }
                else
                {
                    if (pushStarting)
                    {
                        pushStarting = false;
                    }
                }
            }
        }

        #endregion

        #region Methods

        private void modifyFrictionMaterials()
        {
            MyPhysicMaterial[] pm = carDynamics.physicMaterials;

            for (int i = 0; i < pm.Length; i++)
            {

                carDynamics.physicMaterials[i].physicMaterial.frictionCombine = PhysicMaterialCombine.Multiply;
                carDynamics.physicMaterials[i].physicMaterial.bounceCombine = PhysicMaterialCombine.Maximum;
            }
            carDynamics.physicMaterials[0].grip = 1.2f; // track
            carDynamics.physicMaterials[1].grip = 0.91f; // offroad
            carDynamics.physicMaterials[2].grip = 0.81f; // grass
        }

        private float getClutchDragImpluse(float engineRpm) 
        {
            return drivetrain.clutch.GetDragImpulse(
                engineRpm * drivetrain.RPM2angularVelo,
                wheelAngularVelocity * drivetrain.ratio,
                drivetrain.engineInertia * drivetrain.powerMultiplier * drivetrain.externalMultiplier,
                drivetrain.drivetrainInertia * drivetrain.powerMultiplier * drivetrain.externalMultiplier + wheelRotationalInertia,
                drivetrain.ratio,
                wheelImpulse,
                (drivetrain.torque - drivetrain.frictionTorque + drivetrain.startTorque) * Time.deltaTime);
        }

        private void injectCheckIfKeyAccTurnedOnWhenEngineStalling()
        {
            // Written, 07.12.2022

            List<FsmTransition> transitions = starterStallEngineState.Transitions.ToList();
            transitions.Add(new FsmTransition() { FsmEvent = starterFsm.FsmEvents[10], ToState = "Running" });
            starterStallEngineState.Transitions = transitions.ToArray();

            starterStallEngineState.insertNewAction(checkIfAccTurnedOnWhenEngineStalling, 7, CallbackTypeEnum.onUpdate, true);
        }
        private void exhaustCrackle()
        {
            backfire.SendEvent("TIMINGBACKFIRE");
        }

        #endregion

        #region Playmaker Injects

        private void pushStartEngine()
        {
            starterFsm.SendEvent("PUSHSTART");
            electrics.SetActive(true);
        }
        private void starterHelp()
        {
            // Written, 13.11.2022

            if (starterHelpActive)
            {
                carController.clutchInput = 1;
                drivetrain.gear = 1;
            }
        }
        private void electricsBatteryCheck()
        {
            // Written, 13.11.2022

            electricsFsm.SendEvent("PROCEED");
        }
        private void starterMotorCheck()
        {
            // Written, 13.11.2022

            if (starterMotorHasPower)
            {
                if (batteryCharge.Value > 80)
                {
                    starterFsm.SendEvent("START ENGINE");
                }
                else
                {
                    // play solenoid click sound
                }
            }
        }
        private void ignition()
        {
            // Written, 14.11.2022

            if (!acc.Value)
            {
                if (batteryInstalledAndBolted && batteryCharge.Value > 80)
                {
                    electrics.SetActive(true);
                }
                electricsOk.Value = true;
            }
        }
        private void fuelGaugeBatteryCheck()
        {
            // Written, 18.11.2022

            fuelGaugeInstalled2.Value = batteryInstalledAndBolted && batteryCharge.Value > 80;
        }
        private void crankEngine()
        {
            // Written, 18.11.2022

            if (fuelPrimed.Value && engineWillStart)
            {
                starterFsm.SendEvent("START ENGINE");
            }
        }
        private bool checkIfAccTurnedOnWhenEngineStalling()
        {
            // Written, 10.12.2022

            if (drivetrain.rpm > drivetrain.minRPM)
            {
                if (acc.Value)
                {
                    shutOff.Value = false;
                    starterFsm.SendEvent("RUN");

                    if (keyBanging && unburnedFuel > 10)
                    {
                        exhaustCrackle();
                    }
                }
                else
                {
                    if (!keyBanging)
                    {
                        keyBanging = true;
                    }
                    else
                    {
                        unburnedFuel += 10;
                    }
                }
            }
            else
            {
                keyBanging = false;
                unburnedFuel = 0;
                return true;
            }

            return false;
        }

        #endregion
    }
}
