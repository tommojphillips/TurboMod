using ModApi.Attachable;
using System;
using UnityEngine;

namespace TommoJProductions.TurboMod.Parts
{
    internal class HighFlowAirFilterPart : Part
    {
        // Written, 30.10.2020

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
        public HighFlowAirFilterPart(PartSaveInfo inPartSaveInfo, GameObject inPart, GameObject inParent, Trigger inPartTrigger, Vector3 inPartPosition, Quaternion inPartRotation) : base(inPartSaveInfo, inPart, inParent, inPartTrigger, inPartPosition, inPartRotation)
        {
        }
    }
}
