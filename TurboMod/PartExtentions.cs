using ModApi;
using ModApi.Attachable;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
    internal static class PartExtentions
    {

        // Written, 27.10.2020

        internal static void setActiveRecursively(this Part inPart, bool setActive, bool onActivePart = true)
        {
            GameObject _obj = onActivePart ? inPart.activePart : inPart.rigidPart;
            foreach (Transform transform in _obj.transform)
                transform.gameObject.SetActive(setActive);
        }
        internal static void updatePartAndTriggerParent(this Part inPart, Transform inParent, Vector3 pos = default, Quaternion rot = default)
        {
            inPart.rigidPart.setParentAndPosition(inParent, pos == default ? inPart.partTrigger.triggerPosition : pos, rot == default ? inPart.partTrigger.triggerRotation : rot);
            inPart.rigidPart.SetActive(inPart.installed);
            inPart.activePart.SetActive(!inPart.installed);
            inPart.partTrigger.triggerGameObject.setParentAndPosition(inParent, inPart.partTrigger.triggerPosition, inPart.partTrigger.triggerRotation);
            inPart.partTrigger.triggerGameObject.SetActive(!inPart.installed);
        }
        internal static void setParentAndPosition(this GameObject inGameObject, Transform inParent, Vector3 inPos, Quaternion inRot)
        {
            // Written, 28.10.2020

            inGameObject.transform.SetParent(inParent);
            inGameObject.transform.localPosition = inPos;
            inGameObject.transform.localRotation = inRot;

        }
    }
}
