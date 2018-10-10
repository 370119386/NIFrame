using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    public class Scene : IScene
    {
        protected int iId;
        protected List<IEnumerator> ansyTasks = new List<IEnumerator>(32);

        protected void AddTask(IEnumerator iter)
        {
            ansyTasks.Add(iter);
        }

        public bool HasAnsyTask
        {
            get
            {
                return ansyTasks.Count > 0;
            }
        }

        public IEnumerator LoadAnsyTask()
        {
            for(int i = 0; i < ansyTasks.Count; ++i)
            {
                yield return ansyTasks[i];
            }

            ansyTasks.Clear();
        }

        public bool Create(int iId)
        {
            OnCreate();
            return true;
        }

        public virtual void OnCreate()
        {
            //TODO: Add IEnumerator Here .
        }

        public void Enter()
        {

        }

        public void Exit()
        {
            UIManager.Instance().CloseAllFrames();
            EventManager.Instance().ClearAllEvents();
            InvokeManager.Instance().Clear();
        }

        public int ID()
        {
            return iId;
        }
    }
}