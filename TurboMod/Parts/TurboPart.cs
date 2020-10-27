using ModApi.Attachable;
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

        internal WastegateActuatorPart wastegate { get { return TurboMod.instance.turboParts.wastegateActuatorPart; } }
        internal AirFilterPart airFilter { get { return TurboMod.instance.turboParts.airFilterPart; } }

        #endregion

        #region Fields

        private AudioSource flutterSource;
        private AudioSource handGrindSource;
        private AudioSource spoolSource;

        #endregion

        #region Constructors

        public TurboPart(PartSaveInfo inPartSaveInfo, GameObject inPart, GameObject inParent, Trigger inPartTrigger, Vector3 inPartPosition, Quaternion inPartRotation) : base(inPartSaveInfo, inPart, inParent, inPartTrigger, inPartPosition, inPartRotation)
        {
            // Written, 27.10.2020

            // destorying new looking turbo mesh from parts
            Object.DestroyImmediate(this.activePart.transform.FindChild("turbomesh").gameObject);
            Object.DestroyImmediate(this.rigidPart.transform.FindChild("turbomesh").gameObject);
            // Assigning audio source.
            this.flutterSource = inPart.transform.FindChild("flutter").gameObject.GetComponent<AudioSource>();
            this.handGrindSource = inPart.transform.FindChild("handgrind").gameObject.GetComponent<AudioSource>();
            this.spoolSource = inPart.transform.FindChild("turbospool").gameObject.GetComponent<AudioSource>();
            // Destorying audio source from parts.
            Object.DestroyImmediate(this.activePart.transform.FindChild("flutter").gameObject);
            Object.DestroyImmediate(this.activePart.transform.FindChild("handgrind").gameObject);
            Object.DestroyImmediate(this.activePart.transform.FindChild("turbospool").gameObject);
            Object.DestroyImmediate(this.rigidPart.transform.FindChild("flutter").gameObject);
            Object.DestroyImmediate(this.rigidPart.transform.FindChild("handgrind").gameObject);
            Object.DestroyImmediate(this.rigidPart.transform.FindChild("turbospool").gameObject);

            this.activePart.AddComponent<CheckTurboConditionMono>();
            //Assigning active
            this.installedPos = inPartPosition;
            this.installedRot = inPartRotation;
        }

        #endregion

        #region Methods

        protected override void assemble(bool inStartup = false)
        {
            // Written, 28.10.2020


            this.setActiveRecursively(false);
            this.setActiveRecursively(true, false);
            if (!inStartup)
            {
                this.wastegate.updatePartAndTriggerParent(this.rigidPart.transform);
                this.airFilter.updatePartAndTriggerParent(this.rigidPart.transform);
            }
            base.assemble(inStartup);
        }

        protected override void disassemble(bool inStartup = false)
        {
            this.setActiveRecursively(true);
            this.setActiveRecursively(false, false);
            if (!inStartup)
            {
                this.wastegate.updatePartAndTriggerParent(this.activePart.transform);
                this.airFilter.updatePartAndTriggerParent(this.activePart.transform);
            }
            base.disassemble(inStartup);
        }

        #endregion
    }
}
