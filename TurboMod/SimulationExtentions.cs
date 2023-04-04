namespace TommoJProductions.TurboMod
{
    public class SimulationExtentions
    {
        // Written, 17.10.2022

        /// <summary>
        /// Checks the value for NaN. if NaN, returns 0.
        /// </summary>
        /// <param name="_float">The float to check NaN</param>
        public static float safeFloat(float _float)
        {
            if (float.IsNaN(_float))
                return 0;
            return _float;
        }

        /// <summary>
        /// Converts 'Cubic feet per minute' to pound mass (abbreviated as lbm or just lb)
        /// </summary>
        /// <param name="cfm">The value in cubic feet per minute</param>
        public static float toLbm(float cfm) => cfm * 44.93f;
        /// <summary>
        /// Convets 'Cubic feet per minute' to liters per minute. LPM is an abbreviation of litres per minute (l/min)
        /// </summary>
        /// <param name="cfm">The value in cubic feet per minute</param>
        public static float toLpm(float cfm) => cfm * 28.316f;
        /// <summary>
        /// Converts Cubic centimeters (cc) to Cubic Inch (abbreviated as CI)
        /// </summary>
        /// <param name="cubicCentimeter">The value in cubic centimeters</param>
        public static float toCubicInch(float cubicCentimeter) => cubicCentimeter / 16.387f;
    }
}
