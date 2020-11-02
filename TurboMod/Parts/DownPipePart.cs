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
        public DownPipePart(PartSaveInfo inPartSaveInfo, GameObject inPart, GameObject inParent, Trigger inPartTrigger, Vector3 inPartPosition, Quaternion inPartRotation) : base(inPartSaveInfo, inPart, inParent, inPartTrigger, inPartPosition, inPartRotation)
        {
        }
    }
}
