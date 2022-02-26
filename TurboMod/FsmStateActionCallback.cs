using HutongGames.PlayMaker;
using System;

namespace TommoJProductions.TurboMod
{
    public class FsmStateActionCallback : FsmStateAction
    {
        private event Action onEnterCallback;
        private bool finish;

        public FsmStateActionCallback(Action action, string actionName, bool finish)
        {
            Name = actionName;
            onEnterCallback += action;
            this.finish = finish;
        }

        public override void OnEnter()
        {
            onEnterCallback?.Invoke();
            if (finish)
                Finish();
        }
    }
}
