using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GG
{
	[System.Serializable]
	public class Stage
	{
		public int key;
		public UnityEvent action;
	};

	public class ComStateMachine : MonoBehaviour 
	{
		public Stage[] status = new Stage[0];
		int key = 0;

		public int Key
		{
			set 
			{
				key = value;
                ApplyStatus();
            }
			get 
			{
				return key;
			}
		}

		void ApplyStatus()
		{
			int iCurrentPos = -1;
			for (int i = 0; i < status.Length; ++i) 
			{
				if (null != status [i] && status [i].key == key) 
				{
					iCurrentPos = i;
					break;
				}
			}
			if (-1 != iCurrentPos) 
			{
				if (null != status [iCurrentPos] && null != status [iCurrentPos].action) 
				{
					status [iCurrentPos].action.Invoke ();
				}
			}
		}

		public void next()
		{
			int iCurrentPos = -1;
			for (int i = 0; i < status.Length; ++i) 
			{
				if (null != status [i] && status [i].key == key) 
				{
					iCurrentPos = i;
					break;
				}
			}

			if (status.Length > 0) 
			{
				iCurrentPos = (iCurrentPos + 1 ) % status.Length;
				var curStatus = status [iCurrentPos];
				if (null != curStatus && null != curStatus.action) 
				{
					key = curStatus.key;
                    ApplyStatus();
                    UnityEngine.Debug.LogFormat ("<color=#00ff00>change status succeed !!!</color>");
				}
			}
		}
	}
}