using HutongGames.PlayMaker;
using System;

namespace TommoJProductions.TurboMod
{
    public class FsmStateActionCallback : FsmStateAction
    {
		private event Action onEnterCallback;
		private bool actionOnExit;

		public FsmStateActionCallback(Action action, bool onExit)
		{
			Name = nameof(action);
			onEnterCallback += action;
			actionOnExit = onExit;
		}

		public override void OnEnter()
		{
			if (actionOnExit)
				return;
			onEnterCallback?.Invoke();
				Finish();
		}

		public override void OnExit()
		{
			if (!actionOnExit)
				return;
			onEnterCallback?.Invoke();
				Finish();
		}
	}
}
