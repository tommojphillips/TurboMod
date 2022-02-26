using MSCLoader;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public class VehiclePushStartLogic : MonoBehaviour
    {
        public GameObject engine;
        public Drivetrain drivetrainReference;
        private PlayMakerFSM starterFsm;
        private bool engineOn => engine.activeInHierarchy;
        private int previousGear;
        void Start()
        {
            starterFsm = gameObject.GetPlayMaker("Starter");
        }
        void Update()
        {
            if (!engineOn)
            {
                if (drivetrainReference.velo > drivetrainReference.clutch.speedDiff)
                {
                    previousGear = drivetrainReference.gear;
                    pushStart();
                    drivetrainReference.gear = previousGear;
                }
            }
        }
        public void pushStart()
        {
            starterFsm.SendEvent("PUSHSTART");
        }
    }
}
