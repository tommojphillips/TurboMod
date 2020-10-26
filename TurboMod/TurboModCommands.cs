using MSCLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TommoJProductions.TurboMod
{
    class TurboModCommands : ConsoleCommand
    {
        // Written, 26.10.2020

        public override string Name => "tlog";

        public override string Help => "displays the turbo mod log in the modloader console.";

        public override void Run(string[] args)
        {
            // Written, 26.10.2020

            ModConsole.Print(TurboMod.instance.getLog());
        }
    }
}
