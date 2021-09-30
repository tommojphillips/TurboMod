using HutongGames.PlayMaker;
using System;

namespace TommoJProductions.TurboMod
{
    public class FsmStateActionCallback : FsmStateAction
    {
		private event Action onEnterCallback;
		private bool actionOnExit;
		private bool finish;

		public FsmStateActionCallback(Action action, bool onExit, bool finish)
		{
			Name = nameof(action);
			onEnterCallback += action;
			actionOnExit = onExit;
			this.finish = finish;
		}

		public override void OnEnter()
		{
			if (actionOnExit)
				return;
			onEnterCallback?.Invoke();
			if (finish)
				Finish();
		}

		public override void OnExit()
		{
			if (!actionOnExit)
				return;
			onEnterCallback?.Invoke();
			if (finish)
				Finish();
		}
	}
}
