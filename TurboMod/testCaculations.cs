using MSCLoader;

namespace TommoJProductions.TurboMod
{
    public class testCaculations : ConsoleCommand
    {
        public TurboSimulation turboSim;

        public override string Name => "testCals";

        public override string Help => "Tests air density related calculations\nCaluclation list:\n" +
            "1.) <color=yellow>airDensity</color> [<color=blue>psi</color>]\n" +
            "2.) <color=yellow>vapourPressure</color> [<color=blue>temperature C</color>]\n" +
            "3.) <color=yellow>dewPoint</color> [<color=blue>temperature C</color>] [<color=blue>humidity 0-100</color>]";
        public override void Run(string[] args)
        {
            int argsNum = args.Length;
            int argumentLevel = 1;
            int argsNumLevel = argsNum + argumentLevel;

            if (argsNum > 0)
            {
                string arg0 = args[0];
                //argumentLevel = 1;
                switch (arg0)
                {
                    case "airDensity":
                    case "ad":
                        if (argsNumLevel == 1)
                        {
                            if (float.TryParse(args[1], out float _rs))
                                ModConsole.Print($"{arg0}: {turboSim.calculateAirDensity(_rs, 24)}");
                            else
                                ModConsole.Warning($"{arg0} expects 1 argument oftype<FLOAT>, the pressure in pounds per square inch (PSI)");
                        }
                        else
                            ModConsole.Warning($"{arg0} expects 1 argument, the pressure in pounds per square inch (PSI)");
                        break;
                    case "vapourPressure":
                    case "vp":
                        if (argsNumLevel == 1)
                        {
                            if (float.TryParse(args[1], out float _rs))
                                ModConsole.Print($"{arg0}: {turboSim.calculateSaturationVapourPressure(_rs)}");
                            else
                                ModConsole.Warning($"{arg0} expects 1 argument oftype<FLOAT>, the temperature in C");
                        }
                        else
                            ModConsole.Warning($"{arg0} expects 1 argument, the temperature in C");
                        break;
                    case "dewPoint":
                    case "dp":
                        if (argsNumLevel == 2)
                        {
                            if (float.TryParse(args[1], out float _rs) && float.TryParse(args[2], out float _rs1))
                                ModConsole.Print($"{arg0}: {turboSim.calculateDewPoint(_rs, _rs1)}");
                            else
                                ModConsole.Warning($"{arg0} expects 2 arguments\noftype<FLOAT>, the temperature in C\noftype<FLOAT>, the relative humidity in percent (0-1)");
                        }
                        else
                            ModConsole.Warning($"{arg0} expects 2 arguments\noftype<FLOAT>, the temperature in C\noftype<FLOAT>, the relative humidity in percent (0-1)");
                        break;
                    case "atmosphericPressure":
                    case "atmPSI":
                    case "ap":
                        if (argsNumLevel == 0)
                        {
                            turboSim.calculateAtmosphericPressure();
                            ModConsole.Print($"{arg0}: {turboSim.atmosphericPressure}");
                        }
                        else
                            ModConsole.Warning($"{arg0} expects 0 arguments");
                        break;
                }
            }
        }

        public testCaculations(TurboSimulation inTurboSim)
        {
            turboSim = inTurboSim;
        }
    }
}
