using ModApi.Attachable;
using UnityEngine;

namespace TommoJProductions.TurboMod.Parts
{
    class HeadersPart : Part
    {
        // Written, 26.10.2020

        #region Properties

        public override PartSaveInfo defaultPartSaveInfo => new PartSaveInfo()
        {
            installed = false,
            position = new Vector3(1561.49f, 5, 730),
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

        internal TurboPart turbo { get { return TurboMod.instance.turboParts.turboPart; } }
        #endregion

        #region Constructors

        public HeadersPart(PartSaveInfo inPartSaveInfo, GameObject inPart, GameObject inParent, Trigger inPartTrigger, Vector3 inPartPosition, Quaternion inPartRotation) : base(inPartSaveInfo, inPart, inParent, inPartTrigger, inPartPosition, inPartRotation)
        {
        }

        #endregion

        #region Methods

        protected override void assemble(bool inStartup = false)
        {
            if (!inStartup)
            {
                this.turbo.updatePartAndTriggerParent(this.rigidPart.transform, this.turbo.installedPos, this.turbo.installedRot);
                this.turbo.airFilter.updatePartAndTriggerParent(this.turbo.rigidPart.transform);
                this.turbo.wastegate.updatePartAndTriggerParent(this.turbo.rigidPart.transform);
            }
            base.assemble(inStartup);
        }

        protected override void disassemble(bool inStartup = false)
        {
            if (!inStartup)
            {
                this.turbo.updatePartAndTriggerParent(this.activePart.transform, this.turbo.installedPos, this.turbo.installedRot);
                this.turbo.airFilter.updatePartAndTriggerParent(this.turbo.rigidPart.transform);
                this.turbo.wastegate.updatePartAndTriggerParent(this.turbo.rigidPart.transform);
            }
            base.disassemble(inStartup);
        }

        #endregion
    }
}
