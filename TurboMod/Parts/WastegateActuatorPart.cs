using ModApi.Attachable;
using UnityEngine;

namespace TommoJProductions.TurboMod.Parts
{
    class WastegateActuatorPart : Part
    {
        // Written, 26.10.2020

        #region Properties

        public override PartSaveInfo defaultPartSaveInfo => new PartSaveInfo()
        {
            installed = false,
            position = new Vector3(1558.18f, 5, 730),
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

        internal WastegateAdjustMono wastegateAdjust { get; private set; }

        #endregion

        #region Constructors

        public WastegateActuatorPart(PartSaveInfo inPartSaveInfo, GameObject inPart, GameObject inParent, Trigger inPartTrigger, Vector3 inPartPosition, Quaternion inPartRotation) : base(inPartSaveInfo, inPart, inParent, inPartTrigger, inPartPosition, inPartRotation)
        {
            // Written, 28.10.2020

            this.wastegateAdjust = this.rigidPart.AddComponent<WastegateAdjustMono>();
       }

        #endregion

        #region Methods

        protected override void assemble(bool inStartup = false)
        {
            base.assemble(inStartup);
        }

        protected override void disassemble(bool inStartup = false)
        {
            base.disassemble(inStartup);
        }

        protected override void onTriggerExit(Collider inCollider)
        {
            base.onTriggerExit(inCollider);
        }

        protected override void onTriggerStay(Collider inCollider)
        {
            base.onTriggerStay(inCollider);
        }

        #endregion
    }
}
