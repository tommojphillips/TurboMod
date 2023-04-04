using MSCLoader;

using TommoJProductions.ModApi;
using TommoJProductions.ModApi.Attachable;

using UnityEngine;

using static TommoJProductions.TurboMod.Simulation;

namespace TommoJProductions.TurboMod
{
    public class WastegateAdjust : MonoBehaviour
    {
        internal Simulation sim;

        private bool wastegateInteracted;
        private bool wastegateAdjusted;

        private Part act => sim.act;

        // Update is called once per frame
        void Update()
        {
            wasteGateAdjust();
        }

        private void wasteGateAdjust()
        {
            if (act.installed && !act.inherentlyPickedUp)
            {
                if (act.isPlayerLookingAt())
                {
                    wastegateAdjusted = false;

                    if (Input.mouseScrollDelta.y > 0 && sim.wastegateRPM <= MAX_WASTEGATE_RPM * 0.96f)
                    {
                        sim.wastegatePsi = sim.wastegatePsi + WASTEGATE_ADJUST_INTERVAL;
                        wastegateAdjusted = true;
                    }
                    if (Input.mouseScrollDelta.y < 0 && sim.wastegateRPM >= MIN_WASTEGATE_RPM)
                    {
                        sim.wastegatePsi = sim.wastegatePsi - WASTEGATE_ADJUST_INTERVAL;
                        wastegateAdjusted = true;
                    }
                    if (cInput.GetButtonDown("Finger") && sim.wastegateRPM <= MAX_WASTEGATE_RPM)
                    {
                        sim.wastegatePsi = MAX_WASTEGATE_RPM / RPM2PSI;
                        wastegateAdjusted = true;
                    }
                    if (wastegateAdjusted)
                    {
                        ModClient.playSoundAtInterupt(transform, "CarBuilding", "bolt_screw");
                    }
                    wastegateInteracted = true;
                    ModClient.guiInteract("Wastegate Pressure: " + sim.wastegatePsi.ToString("0.00") + " PSI");
                }
                else if (wastegateInteracted)
                {
                    wastegateInteracted = false;
                    ModClient.guiInteract("", GuiInteractSymbolEnum.None);
                }
            }
        }
    }
}