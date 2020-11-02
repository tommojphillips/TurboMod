using ModApi;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
	internal class WastegateAdjustMono : MonoBehaviour
	{
		internal float wastegatePsiIncrease { get; set; } = 0.25f;
		internal float wastegateMinPsi { get; set; } = 2.25f;
		internal float wastegateMaxPsi { get; set; } = 22.25f;
		internal TurboSimulation turboSimulation { get; set; }
		/// <summary>
		/// Represents wastegate psi to add if turbo simulation instance is null.
		/// </summary>
		internal float wastegatePsiToAdd { get; set; }

		private bool previousWastegateHit;
		


		void Update()
		{
			// Written, 28.10.2020

			if (!this.transform.parent.gameObject.isPlayerHolding())
			{
				if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1f, 1 << this.gameObject.layer))
				{
					if (hit.collider.gameObject == this.gameObject)
					{
						this.previousWastegateHit = true;
						ModClient.guiInteract("Wastegate:" + (this.turboSimulation?.wastegatePsi ?? this.wastegatePsiToAdd).ToString("0.##") + "PSI");
						if (Input.mouseScrollDelta.y > 0)
						{
							if ((this.turboSimulation?.wastegatePsi ?? this.wastegatePsiToAdd) < this.wastegateMaxPsi)
							{
								if (this.turboSimulation == null)
									this.wastegatePsiToAdd += this.wastegatePsiIncrease;
								else
									this.turboSimulation.addWastegatePsi(this.wastegatePsiIncrease);
								MasterAudio.PlaySound3DAndForget("CarBuilding", this.gameObject.transform, false, 1f, null, 0f, "bolt_screw");
							}
						}
						else if (Input.mouseScrollDelta.y < 0)
						{
							if ((this.turboSimulation?.wastegatePsi ?? this.wastegatePsiToAdd) > this.wastegateMinPsi)
							{
								if (this.turboSimulation == null)
									this.wastegatePsiToAdd -= this.wastegatePsiIncrease;
								else
									this.turboSimulation.addWastegatePsi(-this.wastegatePsiIncrease);
								MasterAudio.PlaySound3DAndForget("CarBuilding", this.gameObject.transform, false, 1f, null, 0f, "bolt_screw");
							}
						}
					}
				}
			}
			else if (this.previousWastegateHit)
			{
				ModClient.guiInteract(inGuiInteractSymbol: GuiInteractSymbolEnum.None);
				this.previousWastegateHit = false;
			}
		}
	}
}
