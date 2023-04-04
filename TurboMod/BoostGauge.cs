using System;

using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public class BoostGauge : MonoBehaviour
    {
        internal Simulation sim;
        private float vaccummSmooth;
        private float vaccummThrottle;
        private float boostGaugeNeedle;
        private float throttleSmooth;
        private Vector3 boostNeedleVector;
        private Transform turboNeedleTransform;

        private void Start()
        {
            turboNeedleTransform = sim.boostGauge.transform.GetChild(2);
        }

        private void Update() 
        {
            boostGaugeNeedleUpdate();
        }

        private void boostGaugeNeedleUpdate()
        {
            if (sim.canPipeWork && sim.boostGauge.installed)
            {
                vaccummSmooth = Math.Round(sim.airSim.pressure) > 1 ? sim.onThrottlePedal ? 117 : sim.drivetrain.idlethrottle * 117 : 0;
                vaccummSmooth = Mathf.Clamp(vaccummSmooth, sim.drivetrain.idlethrottle * 117, 117);
                vaccummThrottle = Mathf.SmoothDamp(vaccummThrottle, vaccummSmooth, ref throttleSmooth, 0.05f);
                boostGaugeNeedle = 133 + -vaccummThrottle + -(sim.airSim.pressure * Simulation.RPM2PSI) / 1600;
                boostNeedleVector.z = boostGaugeNeedle;
            }
            else
            {
                boostNeedleVector.z = 18;
            }
            turboNeedleTransform.transform.localEulerAngles = boostNeedleVector;
        }
    }
}
