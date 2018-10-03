using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProtoTable;

namespace NI
{
    public enum FrameStatus
    {
        FS_INVALID = -1,
        FS_OPEN = 1,
        FS_CLOSED = 2,
    }

    public class ClientFrame : IFrame
    {
        protected int mState = -1;
        protected int mLayer = 1;
        protected int mFrameId = -1;
        protected int mTypeId = -1;
        protected int mModuleId = -1;
        protected object userData = null;
        protected GameObject gameObject = null;
        protected FrameTypeTable frameItem = null;
        protected ComScriptBinder mScriptBinder = null;

        protected virtual string GetPrefabPath()
        {
            return string.Empty;
        }

        protected virtual void _InitScriptBinder()
        {
            
        }

        public int GetLayer()
        {
            return mLayer;
        }

        public void Create(object argv)
        {
            this.userData = argv;
        }

        public void Open(int typeId,int frameId,int moduleId = -1,int layer = 1,GameObject root = null)
        {
            if(this.mState == (int)FrameStatus.FS_OPEN)
            {
                Debug.LogErrorFormat("this frame has opened ...");
                return;
            }

            this.mLayer = layer;
            this.mTypeId = typeId;
            this.mFrameId = frameId;
            this.mModuleId = moduleId;
            this.mState = (int)FrameStatus.FS_INVALID;

            var path = GetPrefabPath();

            if(!string.IsNullOrEmpty(path))
            {
                gameObject = AssetLoaderManager.Instance().LoadResources<GameObject>(path, AssetType.AT_PREFAB);
            }
            else
            {
                frameItem = TableManager.Instance().GetTableItem<ProtoTable.FrameTypeTable>(mTypeId);
                if(null == frameItem)
                {
                    Debug.LogErrorFormat("query frameItem failed for id = {0}, class = {1}", mTypeId,GetType().Name);
                    return;
                }

                //gameObject = AssetBundleManager.LoadAsset<GameObject>((Consts.UGame)moduleId, frameItem.Prefab);
            }

            if (null == gameObject)
            {
                Debug.LogErrorFormat("load frame prefab failed for moduleId = {0} path = {1}", moduleId, path);
                return;
            }

            GameObject parent = root;
            if (parent == null)
            {
                var uiLayer = UIManager.Instance().GetLayer(mLayer);
                if(null != uiLayer)
                {
                    parent = uiLayer.goLayer;
                }
            }

            if(null == parent)
            {
                Debug.LogErrorFormat("parent is null , create frame failed , typeId = {0}", mTypeId);
                return;
            }

            mScriptBinder = gameObject.GetComponent<ComScriptBinder>();
            if(null != mScriptBinder)
            {
                _InitScriptBinder();
            }

            gameObject.transform.SetParent(parent.transform,false);


            this.mState = (int)FrameStatus.FS_OPEN;

            OnOpenFrame();
        }

        public void Close()
        {
            if(this.mState != (int)FrameStatus.FS_OPEN)
            {
                return;
            }

            OnCloseFrame();

            if(null != mScriptBinder)
            {
                mScriptBinder.DestroyWithFrame();
                mScriptBinder = null;
            }

            userData = null;
            mFrameId = -1;
            mTypeId = -1;
            mLayer = 0;
            mModuleId = -1;
            if(null != gameObject)
            {
                Object.Destroy(gameObject);
                gameObject = null;
            }
            this.mState = (int)FrameStatus.FS_CLOSED;
        }

        protected void StartCoroutine(IEnumerator enumerator)
        {
            if(null != mScriptBinder)
            {
                mScriptBinder.StartCoroutine(enumerator);
            }
        }

        protected virtual void OnOpenFrame()
        {
            
        }

        protected virtual void OnCloseFrame()
        {
            
        }

        public void CloseByManager()
        {
            UIManager.Instance().CloseFrame(mTypeId, mFrameId);
        }
    }   
}