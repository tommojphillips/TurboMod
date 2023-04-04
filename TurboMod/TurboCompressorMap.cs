using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

namespace TommoJProductions.TurboMod
{
    public class TurboCompressorMap
    {
        internal Graph compressorGraph;

        public TurboCompressorMap(GraphData[] graphData)
        {
            compressorGraph = createCompressorGraph(graphData);
        }

        private Graph createCompressorGraph(GraphData[] graphData)
        {
            return new Graph(graphData);
        }

        public float cal(float pressureRatio) 
        {
            return compressorGraph.cal(pressureRatio);
        }
    }    
}
