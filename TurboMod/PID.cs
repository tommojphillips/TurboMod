using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public class PID
    {
        public float kp; // proportional
        public float ki; // integral
        public float kd; // derivative
        public float n; // filter cofficient

        public float outputLowerLimit;
        public float outputUpperLimit;

        public bool useLimits = false;

        public bool enabled = true;

        public float k { get; private set; }
        public float b0 { get; private set; }
        public float b1 { get; private set; }
        public float b2 { get; private set; }
        public float a0 { get; private set; }
        public float a1 { get; private set; }
        public float a2 { get; private set; }          
        public float y0 { get; private set; }   // Current output
        public float y1 { get; private set; }   // Output one iteration old
        public float y2 { get; private set; }   // Output two iterations old
        public float e0 { get; private set; }   // Current error
        public float e1 { get; private set; }   // Error one iteration old
        public float e2 { get; private set; }   // Error two iterations old

        public PID() { }
        public PID(PID pid)
        {
            kp = pid.kp;
            ki = pid.ki;
            kd = pid.kd;
            n = pid.n;
            useLimits = pid.useLimits;
            outputLowerLimit = pid.outputLowerLimit;
            outputUpperLimit = pid.outputUpperLimit;
        }

        public float Update(float setpoint, float processValue, float dt)
        {
            if (enabled)
            {
                // Calculate rollup parameters
                k = 2 / dt;
                b0 = Mathf.Pow(k, 2) * kp + k * ki + ki * n + k * kp * n + Mathf.Pow(k, 2) * kd * n;
                b1 = 2 * ki * n - 2 * Mathf.Pow(k, 2) * kp - 2 * Mathf.Pow(k, 2) * kd * n;
                b2 = Mathf.Pow(k, 2) * kp - k * ki + ki * n - k * kp * n + Mathf.Pow(k, 2) * kd * n;
                a0 = Mathf.Pow(k, 2) + n * k;
                a1 = -2 * Mathf.Pow(k, 2);
                a2 = Mathf.Pow(k, 2) - k * n;

                // Age errors and output history
                e2 = e1;                        // Age errors one iteration
                e1 = e0;                        // Age errors one iteration
                e0 = setpoint - processValue;   // Compute new error
                y2 = y1;                        // Age outputs one iteration
                y1 = y0;                        // Age outputs one iteration
                y0 = -a1 / a0 * y1 - a2 / a0 * y2 + b0 / a0 * e0 + b1 / a0 * e1 + b2 / a0 * e2; // Calculate current output

                if (useLimits)
                {
                    // Clamp output if needed
                    if (y0 > outputUpperLimit)
                    {
                        y0 = outputUpperLimit;
                    }
                    else if (y0 < outputLowerLimit)
                    {
                        y0 = outputLowerLimit;
                    }
                }
            }
            return y0;        
        }
    }
}
