using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HutongGames.PlayMaker;

using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public class AirSim
    {
        // Written, 16.10.2022

        #region Constants

        /// <summary>
        /// Represents 1 pascal.
        /// </summary>
        public const float PASCAL = 6894.76f;
        /// <summary>
        /// Represents 1 kelvin.
        /// </summary>
        public const float KELVIN = 274.15f;
        /// <summary>
        /// Represents the dry gas constant.
        /// </summary>
        public const float DRY_GAS_CONSTANT = 287.058f;
        /// <summary>
        /// Represents the wet gas constant.
        /// </summary>
        public const float WET_GAS_CONSTANT = 461.495f;

        /// <summary>
        /// Represents the height reference level in meters.
        /// </summary>
        public const float ALTITUDE_REFERENCE_LEVEL = 0; // meters
        /// <summary>
        /// Represents the pressure at <see cref="ALTITUDE_REFERENCE_LEVEL"/> in psi.
        /// </summary>
        public const float PRESSURE_REFERENCE_LEVEL = 14.7f; // psi
        /// <summary>
        /// Represents gravity force in meters squared.
        /// </summary>
        public const float GRAVITY = 9.80665f; // m/s²
        /// <summary>
        /// Represents the molar mass or air on earth in kg/mol.
        /// </summary>
        public const float MOLAR_MASS_OF_AIR = 0.0289644f; // kg/mol
        /// <summary>
        /// Represents the universal gas constant
        /// </summary>
        public const float UNIVERSAL_GAS_CONSTANT = 8.31432f; // N·m/(mol·K)

        #endregion

        public readonly FsmFloat engineTemp;
        public readonly FsmFloat ambientTempAtSeaLevel;

        private float previousEngineTemp;

        public float relativeHumidity = 0.20534f;
        public float intakeToCompressorDepression = 0.8f;
        public float compressorToManifoldDepression = 1.15f;

        public float absoluteInletPressure => atmosphericPressure - intakeToCompressorDepression;
        public float absoluteOutletPressure => pressure + absoluteInletPressure + compressorToManifoldDepression;
        public float gaugePressure => absoluteOutletPressure - atmosphericPressure;
        public float pressureRatio => absoluteOutletPressure / absoluteInletPressure;

        public float ambientTemp { get; private set; }
        public float intakeManifoldTemp { get; private set; }
        public float engineHeatDelta { get; private set; }
        public float airDensity { get; private set; } = 1;
        public float atmosphericPressure { get; private set; }
        public float waterVapourPressure { get; private set; }
        public float pressure { get; private set; }
        public float pressureDelta { get; private set; }

        public PID intakeHeatTransferPid;
        public PID pressurePid;

        private float dT;

        public AirSim() 
        {
            engineTemp = FsmVariables.GlobalVariables.FindFsmFloat("EngineTemp");
            ambientTempAtSeaLevel = PlayMakerGlobals.Instance.Variables.FindFsmFloat("AmbientTemperature");

            intakeManifoldTemp = 24;

            intakeHeatTransferPid = new PID()
            {
                kp = 0.1f,
                ki = 0.085f,
                n = 1,
            };

            pressurePid = new PID()
            {
                kp = 0.1f,
                ki = 20,
                n = 1,
            };

            dT = Time.fixedDeltaTime;
        }

        public void update(float pressure, float altitude)
        {
            // Written, 16.10.2022

            calculateAmbientTemp(altitude);
            calculateAtmosphericPressure(altitude, ambientTemp);
            calculateHeatTransfer();

            pressureDelta = pressure - this.pressure;

            this.pressure = pressurePid.Update(pressure + pressureDelta, this.pressure, dT);

            waterVapourPressure = calculateSaturationVapourPressure(intakeManifoldTemp) * relativeHumidity;
            airDensity = calculateAirDensity(absoluteOutletPressure, waterVapourPressure);
        }

        private void calculateAmbientTemp(float altitude)
        {
            // Written, 16.10.2022

            ambientTemp = ambientTempAtSeaLevel.Value + approximateTempChangeAtAltitude(altitude);
        }

        private void calculateHeatTransfer()
        {
            // Written, 09.10.2022

            engineHeatDelta = engineTemp.Value - previousEngineTemp;
            previousEngineTemp = engineTemp.Value;

            intakeManifoldTemp = intakeHeatTransferPid.Update(engineTemp.Value + engineHeatDelta, intakeManifoldTemp, dT);

        }
        /// <summary>
        /// Calculates air density at a given pressure (psi) 
        /// </summary>
        /// <param name="pressure">The pressure to calculate air density at. (psi)</param>
        /// <param name="waterVapourPressure">Water Vapour pressure (psi</param>
        private float calculateAirDensity(float pressure, float waterVapourPressure)
        {
            // Modified, 27.02.2022

            // FORMULA: ρ = (pd / (Rd * T)) + (pv / (Rv * T))

            if (!float.IsNaN(pressure))
            {
                float tempKelvin = intakeManifoldTemp + KELVIN;

                float dryPressure = pressure - waterVapourPressure;

                float waterVapourPascals = waterVapourPressure * PASCAL; // water vapour in pascals
                float dryPressurePascals = dryPressure * PASCAL; // pressure in pascals

                return (dryPressurePascals / (DRY_GAS_CONSTANT * tempKelvin)) + (waterVapourPascals / (WET_GAS_CONSTANT * tempKelvin)); // in kilogram per meter cubic (Kg/m3)
            }
            else
            {
                return 1;
            }
        }
        /// <summary>
        /// Calculates saturation vapour pressure at a given temperature. (c)
        /// </summary>
        /// <param name="tempC">The temperature</param>
        private float calculateSaturationVapourPressure(float tempC)
        {
            // Modified, 27.02.2022

            // Tetens equation
            // p = 0.61078exp(17.27 * T / (T + 237.3)) in kilopascals for greater then 0 temp c
            // p = 0.61078exp(21.875 * T / (T + 265.5)) in kilopascals for less then 0 temp c

            float resultKiloPascals;
            if (tempC > 0)
                resultKiloPascals = 0.61078f * Mathf.Log10(17.27f * tempC / (tempC + 237.3f));
            else
                resultKiloPascals = 0.61078f * Mathf.Log10(21.875f * tempC / (tempC + 265.5f));

            return resultKiloPascals * 6.89476f; // convert kilopascal to psi
        }
        /// <summary>        
        /// Calculates atmospheric pressure at the turbo's altitude.         
        /// </summary>
        public void calculateAtmosphericPressure(float altitude, float tempC)
        {
            // Written, 15.01.2022
            //barometric formula: P = P₀ exp(-gM(h - h₀) / (RT))

            atmosphericPressure = PRESSURE_REFERENCE_LEVEL * (float)Math.Exp(-GRAVITY * MOLAR_MASS_OF_AIR * (altitude - ALTITUDE_REFERENCE_LEVEL) / (UNIVERSAL_GAS_CONSTANT * tempC));
        }

        /// <summary>
        /// approximates temp loss due to altitude change. in temp c.
        /// </summary>
        /// <param name="altitude">the current altitude.</param>
        public float approximateTempChangeAtAltitude(float altitude) 
        {
            // Written, 31.10.2022

            // approx temp change due to elevation change.
            // ( 1.2 x Change in elevation in meters)/100 = temp loss due to elevation change in c

            return (1.2f * altitude) / 10;
        }

        public float calculateCfm(float engineRpm, float engineDisplacementCI) => engineDisplacementCI * engineRpm / 3456;

        public float calculateMassFlow(float cfm, float airDensity) => cfm * airDensity;
        public float calculateMassFlow(float cfm) => cfm * airDensity;

    }
}
