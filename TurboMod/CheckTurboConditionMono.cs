using ModApi;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
    internal class CheckTurboConditionMono : MonoBehaviour
    {
        // Written, 27.10.2020

        private GameObject turboBlades;
        private bool previousTurboBladesHit;

        void Start() 
        {
            // Written, 27.10.2020

            // setting up turbo blades
            this.turboBlades = this.gameObject.transform.FindChild("motor_turbocharger_blades").gameObject;
            SphereCollider sc = this.turboBlades.AddComponent<SphereCollider>();
            sc.radius = 0.015f;
            sc.isTrigger = true;
        }
        void Update()
        {
            // Written, 27.10.2020

            RaycastHit hit;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1f, 1 << this.turboBlades.layer))
            {
                ModClient.guiUse = hit.collider?.gameObject == this.turboBlades;
                if (ModClient.guiUse)
                {
                    this.previousTurboBladesHit = true;
                    if (cInput.GetButtonDown("Use"))
                        this.checkTurboCondition();
                }
            }
            else if (this.previousTurboBladesHit)
            {
                this.previousTurboBladesHit = false;
                ModClient.guiInteract(inGuiInteractSymbol: GuiInteractSymbolEnum.None);
            }
        }

        private void checkTurboCondition() 
        {
            // Written, 27.10.2020


            this.turboBlades.transform.localEulerAngles += new Vector3(10, 0f, 0f);
            ModClient.guiInteract("Looks alright");
            MasterAudio.PlaySound3DAndForget("Motor", this.transform, false, 1, null, 0f, "valve_knock");
        }
    }
}
