using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    [System.Serializable]
    public class UILayer
    {
        public GameObject goLayer;
        public string desc;
    }

    public class UIManager : Singleton<UIManager>
    {
        protected UILayer[] mLayers = new UILayer[0];

        public UILayer GetLayer(int layerIndex)
        {
            if(null != mLayers && layerIndex >= 0 && layerIndex < mLayers.Length)
            {
                return mLayers[layerIndex];
            }
            return null;
        }

        public void Initialize(object argv)
        {
            mLayers = argv as UILayer[];
        }

        Dictionary<int, IFrame> mKey2Frames = new Dictionary<int, IFrame>();

        public IFrame OpenFrame(int typeId, object userData = null, int frameId = -1, GameObject root = null)
        {
            var frameItem = TableManager.Instance().GetTableItem<ProtoTable.FrameTypeTable>(typeId);
            if (null == frameItem)
            {
                Debug.LogErrorFormat("OpenFrame Failed for typeId = {0} can not be found in FrameTypeTable ...", frameItem);
                return null;
            }

            int key = GetFrameKey(typeId, frameId);
            if (mKey2Frames.ContainsKey(key))
            {
                IFrame frame = mKey2Frames[key] as IFrame;
                if (null != frame)
                {
                    frame.Close();
                    frame.Create(userData);
                    frame.Open(typeId, frameId, frameItem.ModuleId, frameItem.Layer, root);
                    return frame;
                }
                else
                {
                    Debug.LogErrorFormat("open frame failed typeId = {0} frameId = {1}", typeId, frame);
                    return null;
                }
            }
            else
            {
                var assembly = typeof(ClientFrame).Assembly;
                var frame = assembly.CreateInstance(frameItem.ClassName) as IFrame;
                if (null != frame)
                {
                    frame.Create(userData);
                    frame.Open(typeId, frameId, frameItem.ModuleId, frameItem.Layer, root);
                    mKey2Frames.Add(key, frame);
                }
                return frame;
            }
        }

        public T OpenFrame<T>(int typeId,int layer,object userData = null, int frameId = -1) where T : ClientFrame, new()
        {
            int key = GetFrameKey(typeId, frameId);
            T frame = null;

            if (!mKey2Frames.ContainsKey(key))
            {
                frame = new T();
                frame.Create(userData);
                frame.Open(typeId, frameId, -1, layer);
                mKey2Frames.Add(key, frame);
            }
            else
            {
                frame = mKey2Frames[key] as T;
                if (null != frame)
                {
                    frame.Close();
                    frame.Create(userData);
                    frame.Open(typeId, frameId, -1, layer);
                }
                else
                {
                    Debug.LogErrorFormat("open frame failed typeId = {0} frameId = {1}", typeId, frame);
                }
            }

            return frame;
        }

        public T OpenFrame<T>(int moduleId, int typeId, int layer, object userData = null, int frameId = -1) where T : ClientFrame, new()
        {
            int key = GetFrameKey(typeId, frameId);
            T frame = null;

            if (!mKey2Frames.ContainsKey(key))
            {
                frame = new T();
                frame.Create(userData);
                frame.Open(typeId, frameId, moduleId, layer);
                mKey2Frames.Add(key, frame);
            }
            else
            {
                frame = mKey2Frames[key] as T;
                if (null != frame)
                {
                    frame.Close();
                    frame.Create(userData);
                    frame.Open(typeId, frameId, moduleId, layer);
                }
                else
                {
                    Debug.LogErrorFormat("open frame failed typeId = {0} frameId = {1}", typeId, frame);
                }
            }

            return frame;
        }

        public void CloseFrame(int typeId, int frameId)
        {
            int key = GetFrameKey(typeId, frameId);
            if (mKey2Frames.ContainsKey(key))
            {
                var frame = mKey2Frames[key];
                if (null != frame)
                {
                    frame.Close();
                }
                mKey2Frames.Remove(key);
            }
        }

        public int GetFrameKey(int typeId, int frameId)
        {
            return (typeId & 0xFFFF) | ((frameId & 0xFFFF) << 16);
        }

        public void CloseAllFrames()
        {
            var iter = mKey2Frames.GetEnumerator();
            while (iter.MoveNext())
            {
                IFrame frame = iter.Current.Value as IFrame;
                if (null != frame)
                {
                    frame.Close();
                }
            }
            mKey2Frames.Clear();
        }
    }
}