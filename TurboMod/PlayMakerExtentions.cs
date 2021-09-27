using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public static class PlayMakerExtentions
    {
		public enum injectEnum 
		{
			append,
			prepend,
			insert
		}

		public static void addNewGlobalTransition(this PlayMakerFSM fsm, FsmEvent _event, string stateName)
		{
			FsmTransition[] fsmGlobalTransitions = fsm.FsmGlobalTransitions;
			List<FsmTransition> temp = new List<FsmTransition>();
			foreach (FsmTransition t in fsmGlobalTransitions)
			{
				temp.Add(t);
			}
			temp.Add(new FsmTransition
			{
				FsmEvent = _event,
				ToState = stateName
			});
			fsm.Fsm.GlobalTransitions = temp.ToArray();
		}
		public static void appendNewAction(this FsmState state, FsmStateAction action)
		{
			FsmStateAction[] actions = state.Actions;
			List<FsmStateAction> temp = new List<FsmStateAction>();
			foreach (FsmStateAction v in actions)
			{
				temp.Add(v);
			}
			temp.Add(action);
			state.Actions = temp.ToArray();
		}
		public static void prependNewAction(this FsmState state, FsmStateAction action)
		{
			List<FsmStateAction> temp = new List<FsmStateAction>();
			temp.Add(action);
			foreach (FsmStateAction v in state.Actions)
			{
				temp.Add(v);
			}
			state.Actions = temp.ToArray();
		}
		public static void insertNewAction(this FsmState state, FsmStateAction action, int index)
		{
			List<FsmStateAction> temp = new List<FsmStateAction>();
			foreach (FsmStateAction v in state.Actions)
			{
				temp.Add(v);
			}
			temp.Insert(index, action);
			state.Actions = temp.ToArray();
		}
		public static void injectAction(this GameObject go, string fsmName, string stateName, injectEnum injectType, Action callback, bool onExit = false, int index = 0)
		{
			PlayMakerFSM fsm = go.GetPlayMaker(fsmName);
			FsmState state = go.GetPlayMakerState(stateName);
			FsmStateActionCallback _callback = new FsmStateActionCallback(callback, onExit);
			switch (injectType)
			{
				case injectEnum.append:
					state.appendNewAction(_callback);
					break;
				case injectEnum.prepend:
					state.prependNewAction(_callback);
					break;
				case injectEnum.insert:
					state.insertNewAction(_callback, index);
					break;
			}
			FsmEvent callbackEvent = state.Fsm.GetEvent(nameof(callback));
			fsm.addNewGlobalTransition(callbackEvent, nameof(callback));
		}
	}
}
