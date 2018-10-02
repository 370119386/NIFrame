using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProtoTable;
using System;

namespace NI
{
    public class AssetLoaderData
    {
        public GameFrameWork frameHandle;
        public string BaseConfigUrl;
    }

    public enum AssetType
    {
        AT_INVALID = -1,
        AT_PREFAB = 1,
    }

    public class AssetInstance
    {
        public int iHashCode;
        public AssetType eAssetType;
        public ResourceInfoTable resInfo;
        public WeakReference objectRef;
    }

    public class AssetLoaderManager : Singleton<AssetLoaderManager>
    {
        protected GameFrameWork frameHandle = null;
        protected string VersionUrl = string.Empty;

        protected Dictionary<int, ResourceInfoTable> mLocalResourcesInfoTable = new Dictionary<int, ResourceInfoTable>();
        protected Dictionary<int, ResourceInfoTable> mRemoteResourcesInfoTable = new Dictionary<int, ResourceInfoTable>();
        protected Dictionary<int, AssetInstance> mAlivedObjects = new Dictionary<int, AssetInstance>();

        public void Initialize(object argv)
        {
            var data = argv as AssetLoaderData;
            frameHandle = data.frameHandle;
            VersionUrl = data.BaseConfigUrl;

            mLocalResourcesInfoTable.Clear();
            mRemoteResourcesInfoTable.Clear();
        }

        public ResourceInfoTable getResourceInfo(int iHashCode)
        {
            if(mRemoteResourcesInfoTable.ContainsKey(iHashCode))
            {
                return mRemoteResourcesInfoTable[iHashCode];
            }

            if(mLocalResourcesInfoTable.ContainsKey(iHashCode))
            {
                return mLocalResourcesInfoTable[iHashCode];
            }

            return null;
        }

        public T LoadResources<T>(string path, AssetType eAssetType) where T : UnityEngine.Object
        {
            int iHashCode = path.GetHashCode();

            if (mAlivedObjects.ContainsKey(iHashCode))
            {
                var assetInstance = mAlivedObjects[iHashCode];
                if (null != assetInstance && assetInstance.objectRef.IsAlive)
                {
                    if (eAssetType == AssetType.AT_PREFAB)
                    {
                        return UnityEngine.Object.Instantiate(assetInstance.objectRef.Target as T);
                    }
                }
            }

            var resInfoItem = getResourceInfo(iHashCode);
            if (null == resInfoItem || resInfoItem.Path.Count == 0)
            {
                //LoadFromResources
                if (eAssetType == AssetType.AT_PREFAB)
                {
                    GameObject memoryInstance = Resources.Load<GameObject>(path);
                    if(null == memoryInstance)
                    {
                        LoggerManager.Instance().LogErrorFormat("LoadGameObject with path = {0} failed ...", path);
                        return null;
                    }

                    AssetInstance assetInstance = null;
                    if (mAlivedObjects.ContainsKey(iHashCode))
                    {
                        assetInstance = mAlivedObjects[iHashCode];
                        assetInstance.objectRef.Target = memoryInstance;
                        assetInstance.eAssetType = eAssetType;
                    }
                    else
                    {
                        assetInstance = new AssetInstance
                        {
                            iHashCode = iHashCode,
                            resInfo = resInfoItem,
                            objectRef = new WeakReference(memoryInstance),
                            eAssetType = eAssetType,
                        };
                    }

                    mAlivedObjects.Add(iHashCode,assetInstance);

                    return UnityEngine.Object.Instantiate(memoryInstance) as T;
                }
            }

            return default(T);
        }
    }
}