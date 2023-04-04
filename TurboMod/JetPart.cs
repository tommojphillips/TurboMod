using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TommoJProductions.ModApi.Attachable;

namespace TommoJProductions.TurboMod
{
    public class JetPart : Part
    {
        // Written, 10.11.2022

        public int jetSize { get; private set; }

        public JetPart(int jetSize)
        {
            this.jetSize = jetSize;
        }
    }
}
