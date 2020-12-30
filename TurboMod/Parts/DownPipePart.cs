using ModApi.Attachable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TommoJProductions.TurboMod.Parts
{
    class DownPipePart : Part
    {
        // Written, 31.10.2020

        public override PartSaveInfo defaultPartSaveInfo => new PartSaveInfo()
        {
            installed = false,
            position = new Vector3(1559.62f, 5, 730),
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

        internal Quaternion downPipeUpRot = Quaternion.Euler(180, 0, 0);
        internal Quaternion downPipeDownRot = Quaternion.Euler(Vector3.zero);
        internal Vector3 downPipeDownPos = new Vector3(0.1434f, -0.0273f, 0.0732f);
        internal Vector3 downPipeUpPos = new Vector3(0.143f, 0.0302f, -0.0561f);
        internal bool isInDownPosition;
        internal Vector3 downPipeExhaustParticlesPos = new Vector3(0.0002f, -0.0902f, -0.0041f);
        internal Quaternion downPipeExhaustParticlesRot = Quaternion.Euler(Vector3.zero);

        public DownPipePart(PartSaveInfo inPartSaveInfo, GameObject inPart, GameObject inParent, Trigger inPartTrigger, Vector3 inPartPosition, Quaternion inPartRotation) : base(inPartSaveInfo, inPart, inParent, inPartTrigger, inPartPosition, inPartRotation)
        {
        }

        protected override void assemble(bool inStartup = false)
        {
            // Written, 04.11.2020

            TurboSimulation.checkExhaust = true;
            this.isInDownPosition = this.activePart.transform.rotation.eulerAngles.y >= 0 && this.activePart.transform.rotation.eulerAngles.y < 180;
            if (this.isInDownPosition)
            {
                this.rigidPart.transform.localRotation = this.downPipeDownRot;
                this.rigidPart.transform.localPosition = this.downPipeDownPos;
            }
            else
            {
                this.rigidPart.transform.localRotation = this.downPipeUpRot;
                this.rigidPart.transform.localPosition = this.downPipeUpPos;
            }
            base.assemble(inStartup);
        }
        protected override void disassemble(bool inStartup = false)
        {
            // Written, 04.11.2020

            TurboSimulation.checkExhaust = true;
            base.disassemble(inStartup);
        }

    }
}
