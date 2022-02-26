using System;

namespace TommoJProductions.TurboMod
{
    public static class TurboSimulationExtentions
    {
        public static float round(this float _float, int decimalPlace = 0)
        {
            // Written, 15.01.2022

            return (float)Math.Round(_float, decimalPlace);
        }
    }
}
