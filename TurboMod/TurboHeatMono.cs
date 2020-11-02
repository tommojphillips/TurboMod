using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
    internal class TurboHeatMono : MonoBehaviour
    {
        // Written, 28.10.2020

        private MeshRenderer renderer;
        internal GameObject turboMesh;

        private void Start() 
        {
            // Written, 28.10.2020

            this.renderer = this.turboMesh.GetComponent<MeshRenderer>();
            Material[] materials = new Material[] { this.renderer.material, TurboMod.instance.modAssets.turboGlowMat };
            //this.renderer.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            this.renderer.materials = materials;
        }

        private void Update() 
        {
            // Written, 28.10.2020

                            
            //this.renderer.material.SetColor("_EmissionColor", new Color32(240, 52, 52, 1));
        }
    }
}
