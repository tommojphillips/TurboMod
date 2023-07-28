using System.Linq;

using MSCLoader;

using TommoJProductions.ModApi;
using TommoJProductions.ModApi.Database;

using UnityEngine;
using UnityEngine.UI;

using static TommoJProductions.ModApi.ModClient;

namespace TommoJProductions.TurboMod
{
    public class SimulationDebug : MonoBehaviour
    {
        internal Simulation sim;

#if DEBUG
        private bool guiDebug = true;
#else 
        private bool guiDebug = false;
#endif
        private readonly int fontSize = Screen.width / 135;
        private readonly int editsWidth = 240;
        private readonly int valuesWidth = 180;
        private readonly int height = Screen.height;
        private readonly int top = 200;
        private readonly int left = 50;
        private readonly int playerStatsOffSet = 240;
        private GUIStyle guiStyle;

        private bool drawTurboDebugValues = true;
        private bool drawDrivetrainDebugValues = true;
        private bool drawPushStartDebugEdits = true;
        private bool drawTurboDebugEdits = true;

        private void Update() 
        {
            // Written, 13.11.2022

            guiToggle();
        }
        private void OnGUI()
        {
            if (guiDebug)
            {
                guiStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(0, 0, 0, 0)
                };
                guiStyle.normal.textColor = Color.white;
                guiStyle.hover.textColor = Color.blue;
                guiStyle.fontSize = fontSize;

                drawDebugValues();
                drawDebugValues2();
                drawDebugEdits();
                drawDebugEdits2();
            }
        }

        private void guiToggle()
        {
            // Written, 15.01.2022

            if (cInput.GetButtonDown("DrivingMode") && cInput.GetButton("Finger"))
            {
                guiDebug = !guiDebug;
            }
        }
        private void drawDebugValues()
        {
            using (new GUILayout.AreaScope(new Rect(left, top - 150, valuesWidth, height - top + 150), "", guiStyle))
            {
                drawProperty("VALUES");

                if (drawTurboDebugValues)
                {
                    drawProperty("Target Rpm", sim.targetRPM.round(2));
                    drawProperty("Turbo Rpm", sim.turboRpm.round(2));
                    drawProperty("Turbo Spool", sim.turboSpool.round(2));

                    drawProperty($"Throttle: {sim.drivetrain.throttle.round(2)}\nIdle: {sim.drivetrain.idlethrottle.round(2)}\nPedal: {sim.throttlePedalPosition.round(2)}");

                    drawProperty("Air flow", sim.currentAirFlow.round(2));
                    drawProperty("- Manifold", sim.airFlowManifold.round(2));
                    drawProperty("- Volumetric efficiency", sim.volumetricEfficiency.round(2));

                    drawProperty("Pressure", sim.airSim.pressure.round(2));
                    drawProperty("Pressure Delta", sim.airSim.pressureDelta.round(3));

                    drawProperty("COMPRESSOR");
                    drawProperty("Absolute inlet pressure", sim.airSim.absoluteInletPressure.round(2));
                    drawProperty("Absolute outlet pressure", sim.airSim.absoluteOutletPressure.round(2));
                    drawProperty("Pressure ratio", sim.airSim.pressureRatio.round(2));
                    drawProperty("Air density", sim.airSim.airDensity.round(2));

                    drawProperty("Engine heat delta", sim.compressorMapCal().round(2));

                    drawProperty("Engine temp", sim.airSim.engineTemp.Value.round(2));
                    drawProperty("Intake temp", sim.airSim.intakeManifoldTemp.round(2));
                    drawProperty("Ambient Temp", sim.airSim.ambientTemp.round(2));
                    drawProperty("Engine heat delta", sim.airSim.engineHeatDelta.round(2));

                    drawProperty("Rev Limiter Time", sim.drivetrain.revLimiterTime.round(2));

                    drawProperty("Afr", sim.afrAbsolute.round(2));
                }
            }
        }
        private void drawDebugValues2()
        {
            using (new GUILayout.AreaScope(new Rect((left * 2) + valuesWidth, playerStatsOffSet, valuesWidth, height - top + 150), "", guiStyle))
            {
                drawProperty("VALUES 2");

                if (drawDrivetrainDebugValues)
                {   
                    drawProperty("-----------------------------------");

                    drawProperty(sim.surgeRoutine == null ? "OK" : "Surging");
                    drawProperty("Calculated surge rate", sim.calculatedSurgeRate.round(2));
                }
            }
        }
        private void drawDebugEdits()
        {
            using (new GUILayout.AreaScope(new Rect((left * 3) + (valuesWidth * 2), playerStatsOffSet, editsWidth, height - playerStatsOffSet), "", guiStyle))
            {
                drawPropertyBool("Draw Turbo Debug Values", ref drawTurboDebugValues);
                drawPropertyBool("Draw Turbo Debug Edits", ref drawTurboDebugEdits);
                drawPropertyBool("Draw Drivetrain Debug Values", ref drawDrivetrainDebugValues);
                drawPropertyBool("Draw Push Start Debug Edits", ref drawPushStartDebugEdits);

                if (drawTurboDebugEdits)
                {
                    drawProperty("-----------------------------------");
                    drawPID(ref sim.airSim.intakeHeatTransferPid, "engine-intake heat transfer");
                    drawPID(ref sim.airSim.pressurePid, "pressure");
                    drawProperty("-----------------------------------");
                    GUILayout.Space(5);

                    drawProperty("EDITS");
                    drawProperty("Flutter audio ");
                    drawPropertyEdit("# of flutters", ref sim.flutterI);
                    drawPropertyEdit("wait from", ref sim.flutterAudioWaitFrom);
                    drawPropertyEdit("wait to", ref sim.flutterAudioWaitTo);
                    drawPropertyEdit("flutter pitch max t", ref sim.flutterAudioPitchMaxT);
                    drawPropertyEdit("flutter min pitch", ref sim.flutterAudioMinPitch);
                    drawPropertyEdit("flutter max pitch", ref sim.flutterAudioMaxPitch);
                    GUILayout.Space(5);
                    drawProperty("Surge");
                    drawPropertyEdit("surge co eff", ref sim.turboSurgeCoEff);
                }
            }
        }
        private void drawDebugEdits2()
        {
            using (new GUILayout.AreaScope(new Rect((left * 4) + (valuesWidth * 3), top - 150, editsWidth, height - playerStatsOffSet), "", guiStyle))
            {
                if (drawPushStartDebugEdits)
                {
                    //drawPropertyBool("Will engine start", ref VehiclePushStartLogic.engineWillStart);
                    //drawPropertyBool("Push start logic enabled", ref VehiclePushStartLogic.pushStartLogicEnabled);
                    drawProperty("Start Torque", sim.drivetrain.startTorque);
                    drawProperty("Torque", sim.drivetrain.torque);
                    drawProperty("Clutch pos", sim.drivetrain.clutch.GetClutchPosition());
                    //drawProperty("Clutch Drag Impluse", VehiclePushStartLogic.clutchDragImplulse);
                    drawProperty("Engine Friction Factor", sim.drivetrain.engineFrictionFactor);
                }

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


        private void drawPID(ref PID pid, string pidName)
        {
            // Written, 03.10.2022

            using (new GUILayout.VerticalScope())
            {
                drawProperty("PID", pidName);
                if (GUILayout.Button("Reset"))
                {
                    pid = resetPid(pid);
                }

                drawPropertyBool("Enabled", ref pid.enabled);
                drawPropertyEdit("kp", ref pid.kp);
                drawPropertyEdit("ki", ref pid.ki);
                drawPropertyEdit("kd", ref pid.kd);
                drawPropertyEdit("n", ref pid.n);
                drawPropertyBool("Use limits", ref pid.useLimits);
                if (pid.useLimits)
                {
                    drawPropertyEdit("lower limit", ref pid.outputLowerLimit);
                    drawPropertyEdit("upper limit", ref pid.outputUpperLimit);
                }
            }
        }
        private PID resetPid(PID pid)
        {
            return new PID(pid) { enabled = false };
        }
    }
}