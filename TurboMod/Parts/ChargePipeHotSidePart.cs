using ModApi.Attachable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TommoJProductions.TurboMod.Parts
{
    internal class ChargePipeHotSidePart : Part
    {
        public ChargePipeHotSidePart(PartSaveInfo inPartSaveInfo, GameObject inPart, GameObject inParent, Trigger inPartTrigger, Vector3 inPartPosition, Quaternion inPartRotation) : base(inPartSaveInfo, inPart, inParent, inPartTrigger, inPartPosition, inPartRotation)
        {
            
        }

        public override PartSaveInfo defaultPartSaveInfo => new PartSaveInfo() 
        {
            installed = false,
            position = new Vector3(),
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


    }
}
