using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProtoTable;

namespace NI
{
    public class Scene : IScene
    {
        public Scene(SceneTable sceneItem)
        {
            this.sceneItem = sceneItem;
            this.iId = sceneItem.ID;
        }

        protected SceneTable sceneItem;
        protected int iId;
        protected object userData;
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

        public bool Create(object argv)
        {
            LoggerManager.Instance().LogProcessFormat("Create Scene Name = {0}", sceneItem.Name);

            userData = argv;

            for(int i = 0; i < sceneItem.AssetBundles.Count; ++i)
            {
                var bundleName = sceneItem.AssetBundles[i];
                if (!string.IsNullOrEmpty(bundleName))
                {
                    AddTask(AssetBundleManager.Instance().LoadAssetBundle(bundleName,null,()=>
                    {
                        LoggerManager.Instance().LogErrorFormat("LoadAssetBundleFailed Name = {0} SceneName = {1}", bundleName, sceneItem.Name);
                    }));
                }
            }

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

        public virtual void OnExit()
        {

        }

        public void Exit()
        {
            LoggerManager.Instance().LogProcessFormat("Exit Scene Name = {0}", sceneItem.Name);

            OnExit();

            ansyTasks.Clear();
            userData = null;
            iId = -1;

            UIManager.Instance().CloseAllFrames();
            EventManager.Instance().Clear();
            InvokeManager.Instance().Clear();

            for(int i = 0; i < sceneItem.AssetBundles.Count; ++i)
            {
                if(!string.IsNullOrEmpty(sceneItem.AssetBundles[i]))
                {
                    AssetBundleManager.Instance().UnLoadAssetBundle(sceneItem.AssetBundles[i]);
                }
            }
            sceneItem = null;
        }

        public int ID()
        {
            return iId;
        }
    }
}