using System;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public class SatsumaEngineCallback : MonoBehaviour
    {
        // Written, 19.09.2021

        /// <summary>
        /// Represents a on enable callback to unity runtime.
        /// </summary>
        public event Action onEnable;
        /// <summary>
        /// Represents a on disable callback to unity runtime.
        /// </summary>
        public event Action onDisable;
        /// <summary>
        /// Represents a on first time enable callback to unity runtime.
        /// </summary>
        public event Action onFirstTimeEnable;

        private bool firstTimeEnabled = false;

        void OnEnable() 
        {
            if  (!firstTimeEnabled) 
            {
                firstTimeEnabled = true;
                onFirstTimeEnable?.Invoke();
            }
            onEnable?.Invoke();
        }
        void OnDisable()
        {
            onDisable?.Invoke();
        }
    }
}
