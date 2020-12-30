using ModApi.Attachable;
using MSCLoader;
using UnityEngine;

namespace TommoJProductions.TurboMod.Parts
{
    internal class TurboPart : Part
    {
        // Written, 26.10.2020

        #region Properties

        public override PartSaveInfo defaultPartSaveInfo => new PartSaveInfo()
        {
            installed = false,
            position = new Vector3(1560.55f, 5, 730),
            rotation = new Quaternion()
        };

        public override GameObject rigidPart
        {
            get;
            set;
        }

        public override GameObject activePart
        {
            get;
            set;
        }

        internal Vector3 installedPos { get; private set; }
        internal Quaternion installedRot { get; private set; }

        internal AudioSource turboBackfire { get; private set; }
        internal ModAudio turboBackfireModAudio { get; private set; }
        internal WastegateActuatorPart wastegate { get { return TurboMod.instance.turboParts.wastegateActuatorPart; } }
        internal AirFilterPart airFilter { get { return TurboMod.instance.turboParts.airFilterPart; } }
        internal HighFlowAirFilterPart highFlowAirFilter { get { return TurboMod.instance.turboParts.highFlowAirFilterPart; } }
        internal DownPipePart downPipe { get { return TurboMod.instance.turboParts.downPipePart; } }

        internal Vector3 turboExhaustParticlesPos = new Vector3(0.0706f, 0, 0);
        internal Quaternion turboExhaustParticlesRot = Quaternion.Euler(Vector3.zero);

        #endregion

        #region Constructors

        public TurboPart(PartSaveInfo inPartSaveInfo, GameObject inPart, GameObject inParent, Trigger inPartTrigger, Vector3 inPartPosition, Quaternion inPartRotation) : base(inPartSaveInfo, inPart, inParent, inPartTrigger, inPartPosition, inPartRotation)
        {
            // Written, 27.10.2020

            // destorying new looking turbo mesh from parts
            Object.DestroyImmediate(this.activePart.transform.FindChild("turbomesh").gameObject);
            Object.DestroyImmediate(this.rigidPart.transform.FindChild("turbomesh").gameObject);
            // Destorying audio source from parts.
            Object.DestroyImmediate(this.rigidPart.transform.FindChild("handgrind").gameObject);
            Object.DestroyImmediate(this.activePart.transform.FindChild("flutter").gameObject);
            Object.DestroyImmediate(this.activePart.transform.FindChild("handgrind").gameObject);
            Object.DestroyImmediate(this.activePart.transform.FindChild("turbospool").gameObject);
            // Assigning turbo related monos 
            this.rigidPart.AddComponent<TurboSimulation>();
            this.activePart.AddComponent<CheckTurboConditionMono>();
            //Assigning active installed pos and rot
            this.installedPos = inPartPosition;
            this.installedRot = inPartRotation;
            // Audio
            this.initializeAudio();
        }

        #endregion

        #region Methods
        private void initializeAudio()
        {
            // Written, 03.11.2020

            this.turboBackfire = this.rigidPart.AddComponent<AudioSource>();
            this.turboBackfireModAudio = new ModAudio();
            this.turboBackfireModAudio.audioSource = this.turboBackfire;
            this.turboBackfireModAudio.LoadAudioFromFile(System.IO.Path.Combine(ModLoader.GetModAssetsFolder(TurboMod.instance), "backFire_once.wav"), true, false);
            this.turboBackfire.minDistance = 1f;
            this.turboBackfire.maxDistance = 10f;
            this.turboBackfire.spatialBlend = 1f;
            this.turboBackfire.volume = 1;
            //this.turboBackfire.pitch = 1.33f;
        }
        protected override void assemble(bool inStartup = false)
        {
            // Written, 28.10.2020

            TurboSimulation.checkExhaust = true;
            this.setActiveRecursively(false);
            this.setActiveRecursively(true, false);
            if (!inStartup)
            {
                this.wastegate.updatePartAndTriggerParent(this.rigidPart.transform);
                this.airFilter.updatePartAndTriggerParent(this.rigidPart.transform);
                this.highFlowAirFilter.updatePartAndTriggerParent(this.rigidPart.transform);
                this.downPipe.updatePartAndTriggerParent(this.rigidPart.transform);
            }
            base.assemble(inStartup);
        }

        protected override void disassemble(bool inStartup = false)
        {
            // Written, 04.11.2020

            TurboSimulation.checkExhaust = true;
            this.setActiveRecursively(true);
            this.setActiveRecursively(false, false);
            if (!inStartup)
            {
                this.wastegate.updatePartAndTriggerParent(this.activePart.transform);
                this.airFilter.updatePartAndTriggerParent(this.activePart.transform);
                this.highFlowAirFilter.updatePartAndTriggerParent(this.activePart.transform);
                this.downPipe.updatePartAndTriggerParent(this.activePart.transform);
            }
            base.disassemble(inStartup);
        }

        #endregion
    }
}
