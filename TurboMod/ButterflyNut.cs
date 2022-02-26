using System;
using TommoJProductions.ModApi;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public class ButterflyNut : MonoBehaviour
    {
        // Written, 05.11.2021

        #region Constraints

        public const float MAX_TIGHTNESS = 8;

        #endregion

        #region Events

        public event Action onScrew;
        public event Action onLoose;
        public event Action onTight;
        public event Action outLoose;
        public event Action outTight;

        #endregion 

        #region Fields

        public float posStep = 0.0005f;
        public float rotStep = 30;
        public float tightnessStep = 1;
        public Vector3 posDirection = Vector3.forward;
        public Vector3 rotDirection = Vector3.forward;

        private Vector3 startPosition;
        private Vector3 startEulerAngles;

        #endregion

        #region Properties

        public int tightness { get; internal set; }
        public bool tight => tightness >= MAX_TIGHTNESS;
        public bool loose => tightness <= 0;

        private Vector3 positionDelta => posDirection * posStep;
        private Vector3 rotationDelta => rotDirection * rotStep;

        #endregion

        #region Unity Runtime Invoked Methods

        private void Awake()
        {
            startPosition = transform.localPosition;
            startEulerAngles = transform.localEulerAngles;
        }
        private void Start()
        {
            updateNutPosRot();
        }
        private void Update()
        {
            if (gameObject.isPlayerLookingAt())
            {
                float scrollInput = Input.mouseScrollDelta.y;

                if (Mathf.Abs(scrollInput) > 0)
                {
                    int tempTightness = (int)Mathf.Clamp(tightness + (scrollInput * tightnessStep), 0, MAX_TIGHTNESS);
                    if (tempTightness != tightness)
                    {
                        if (tight)
                            outTight?.Invoke();
                        else if (loose)
                            outLoose?.Invoke();
                        tightness = tempTightness;
                        updateNutPosRot();
                        if (tight)
                            onTight?.Invoke();
                        else if (loose)
                            onLoose?.Invoke();
                        else
                            onScrew?.Invoke();
                        MasterAudio.PlaySound3DAndForget("CarBuilding", transform, variationName: "bolt_screw");
                    }
                }
            }
        }

        #endregion

        #region Methods

        private void updateNutPosRot()
        {
            transform.localPosition = startPosition + positionDelta * tightness;
            transform.localEulerAngles = startEulerAngles + rotationDelta * tightness;
        }

        #endregion
    }
}
