using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public struct GraphData
    {
        // Written, 09.10.2022

        public float end;
        public float to;

        public GraphData(float end, float to)
        {
            this.end = end;
            this.to = to;
        }

        public float graphCal(float current, float start, float from)
        {
            return Mathf.Lerp(start, end, (current - from) / (to - from));
        }
    }
}
