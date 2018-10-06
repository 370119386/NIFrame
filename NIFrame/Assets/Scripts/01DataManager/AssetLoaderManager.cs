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
        public Dictionary<int, object> localResourcesInfoTable;
    }

    public enum AssetType
    {
        AT_INVALID = -1,
        AT_PREFAB = 1,
        AT_SPRITE = 2,
        AT_ASSETS = 3,
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

        protected Dictionary<string, AssetBundle> mAlivedBundles = new Dictionary<string, AssetBundle>();

        public void Initialize(object argv)
        {
            var data = argv as AssetLoaderData;
            frameHandle = data.frameHandle;
            VersionUrl = data.BaseConfigUrl;
            mLocalResourcesInfoTable.Clear();
            if(null != data.localResourcesInfoTable)
            {
                var iter = data.localResourcesInfoTable.GetEnumerator();
                while (iter.MoveNext())
                {
                    var resItem = iter.Current.Value as ResourceInfoTable;
                    if(null != resItem)
                    {
                        int iHashCode = 0;
                        if(!string.IsNullOrEmpty(resItem.PathHashKey))
                        {
                            iHashCode = resItem.PathHashKey.GetHashCode();
                        }

                        if(0 != iHashCode)
                        {
                            mLocalResourcesInfoTable.Add(iHashCode, resItem);
                        }
                    }
                }
            }
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

        protected void _AddOrUpdateAlivedObject(ResourceInfoTable resInfoItem,int iHashCode,UnityEngine.Object objRef)
        {
            AssetType eAssetType = (AssetType)resInfoItem.TypeId;
            if(null == resInfoItem)
            {
                LoggerManager.Instance().LogErrorFormat("AddOrUpdateAlivedObject resInfoItem is null ...");
                return;
            }

            if(null == objRef)
            {
                LoggerManager.Instance().LogErrorFormat("AddOrUpdateAlivedObject objRef is null ... resId = {0}", resInfoItem.ID);
                return;
            }

            AssetInstance assetInstance = null;
            if (mAlivedObjects.ContainsKey(iHashCode))
            {
                assetInstance = mAlivedObjects[iHashCode];
                assetInstance.objectRef.Target = objRef;
                assetInstance.resInfo = resInfoItem;
                assetInstance.eAssetType = eAssetType;
            }
            else
            {
                assetInstance = new AssetInstance
                {
                    iHashCode = iHashCode,
                    resInfo = resInfoItem,
                    objectRef = new WeakReference(objRef),
                    eAssetType = eAssetType,
                };
                mAlivedObjects.Add(iHashCode, assetInstance);
            }
        }

        protected T _LoadResFromAssetBundle<T>(ResourceInfoTable resInfoItem, AssetBundle assetBundle, int iHashCode) where T : UnityEngine.Object
        {
            AssetType eAssetType = (AssetType)resInfoItem.TypeId;

            if(eAssetType == AssetType.AT_PREFAB)
            {
                var assetInst = assetBundle.LoadAsset<T>(resInfoItem.Path[1]);
                if(null == assetInst)
                {
                    LoggerManager.Instance().LogErrorFormat("Load Prefab From AssetBundle Failed ... ResId = {0} PathHashKey = {1} ...", resInfoItem.ID, resInfoItem.PathHashKey);
                    return default(T);
                }

                _AddOrUpdateAlivedObject(resInfoItem, iHashCode, assetInst);

                return UnityEngine.Object.Instantiate(assetInst) as T;
            }
            else if(eAssetType == AssetType.AT_SPRITE)
            {
                Sprite assetInst = null;

                if(resInfoItem.Path.Count == 3)
                {
                    LoggerManager.Instance().LogErrorFormat("Load Sprite From AssetBundle Failed ... need 3 argvs ...");
                    return default(T);
                }

                var sprites = assetBundle.LoadAssetWithSubAssets(resInfoItem.Path[1], typeof(Sprite));
                if (null != sprites)
                {
                    for (int i = 0; i < sprites.Length; ++i)
                    {
                        if (sprites[i].name == resInfoItem.Path[2])
                        {
                            assetInst = sprites[i] as Sprite;
                            break;
                        }
                    }
                }

                if(null == assetInst)
                {
                    LoggerManager.Instance().LogErrorFormat("Load Sprite From AssetBundle Failed ... ResId = {0} PathHashKey = {1} ...", resInfoItem.ID, resInfoItem.PathHashKey);
                    return default(T);
                }

                _AddOrUpdateAlivedObject(resInfoItem, iHashCode, assetInst);

                return assetInst as T;
            }
            else if(eAssetType == AssetType.AT_ASSETS)
            {
                var assetInst = assetBundle.LoadAsset<T>(resInfoItem.Path[1]);
                if(null == assetInst)
                {
                    LoggerManager.Instance().LogErrorFormat("Load Asset From AssetBundle Failed ... ResId = {0} PathHashKey = {1} ... type={2}", resInfoItem.ID, resInfoItem.PathHashKey,typeof(T).Name);
                    return default(T);
                }

                _AddOrUpdateAlivedObject(resInfoItem, iHashCode, assetInst);

                return assetInst as T;
            }

            return default(T);
        }

        protected T _LoadResFromStreamingAssetsBundle<T>(ResourceInfoTable resInfoItem, AssetType eAssetType, int iHashCode) where T : UnityEngine.Object
        {
            if(resInfoItem.Path.Count > 1)
            {
                AssetBundle assetBundle = null;
                if(!mAlivedBundles.ContainsKey(resInfoItem.Path[0]))
                {
                    var bundlePath = CommonFunction.getStreamingAssetsPath(resInfoItem.Path[0]);
                    assetBundle = AssetBundle.LoadFromFile(bundlePath);
                    if (null != assetBundle)
                    {
                        mAlivedBundles.Add(bundlePath,assetBundle);
                    }
                }
                else
                {
                    assetBundle = mAlivedBundles[resInfoItem.Path[0]];
                }

                if(null == assetBundle)
                {
                    LoggerManager.Instance().LogProcessFormat("Load assetbundle failed name = {0} ...", resInfoItem.Path[0]);
                    return default(T);
                }

                return _LoadResFromAssetBundle<T>(resInfoItem,assetBundle,iHashCode);
            }

            LoggerManager.Instance().LogErrorFormat("_LoadResFromStreamingAssetsBundle failed ... path = {0} type = {1} argc = {2} error", resInfoItem.PathHashKey, typeof(T).Name,resInfoItem.Path.Count);
            return default(T);
        }

        protected T _LoadResFromResourcesFolder<T>(ResourceInfoTable resInfoItem, AssetType eAssetType,int iHashCode) where T : UnityEngine.Object
        {
            if (eAssetType == AssetType.AT_PREFAB)
            {
                var assetInst = Resources.Load<T>(resInfoItem.Path[0]);
                if (null == assetInst)
                {
                    LoggerManager.Instance().LogErrorFormat("LoadGameObject with path = {0} failed ...", resInfoItem.PathHashKey);
                    return null;
                }

                _AddOrUpdateAlivedObject(resInfoItem, iHashCode, assetInst);

                return UnityEngine.Object.Instantiate(assetInst) as T;
            }
            else if (eAssetType == AssetType.AT_SPRITE)
            {
                Sprite assetInst = null;
                Sprite[] spriteArray = Resources.LoadAll<Sprite>(resInfoItem.Path[0]);
                for (int i = 0; i < spriteArray.Length; ++i)
                {
                    if (spriteArray[i].name == resInfoItem.Path[1])
                    {
                        assetInst = spriteArray[i];
                        break;
                    }
                }

                if (null == assetInst)
                {
                    LoggerManager.Instance().LogErrorFormat("Load Sprite with path = {0} failed ...", resInfoItem.PathHashKey);
                    return default(T);
                }

                _AddOrUpdateAlivedObject(resInfoItem, iHashCode, assetInst);

                return assetInst as T;
            }
            else if (eAssetType == AssetType.AT_ASSETS)
            {
                T assetInst = Resources.Load<T>(resInfoItem.Path[0]);
                if (null == assetInst)
                {
                    LoggerManager.Instance().LogErrorFormat("Load Asset from path = {0} with type = {1} failed ...", resInfoItem.PathHashKey, typeof(T).Name);
                    return null;
                }

                _AddOrUpdateAlivedObject(resInfoItem, iHashCode, assetInst);

                return assetInst;
            }

            LoggerManager.Instance().LogErrorFormat("LoadResFromResourcesFolder failed ... path = {0} type = {1}", resInfoItem.PathHashKey, typeof(T).Name);

            return default(T);
        }

        public T LoadResources<T>(string path, AssetType eAssetType) where T : UnityEngine.Object
        {
            int iHashCode = path.GetHashCode();

            var resInfoItem = getResourceInfo(iHashCode);
            if (null == resInfoItem || resInfoItem.Path.Count <= 0)
            {
                LoggerManager.Instance().LogErrorFormat("LoadResources with path = {0} eAssetType = {1} failed ,can not be found in ResourceInfoTable ...",
                    path,eAssetType);
                return default(T);
            }

            if(eAssetType != (AssetType)resInfoItem.TypeId)
            {
                LoggerManager.Instance().LogErrorFormat("LoadResources failed assetType verify failed eAssetType = {0} while assetType in ResourceInfoTable = {1}", (int)eAssetType,resInfoItem.TypeId);
                return default(T);
            }

            if (mAlivedObjects.ContainsKey(iHashCode))
            {
                var assetInst = mAlivedObjects[iHashCode];
                if (null != assetInst && assetInst.objectRef.IsAlive)
                {
                    if (eAssetType == AssetType.AT_PREFAB)
                    {
                        return UnityEngine.Object.Instantiate(assetInst.objectRef.Target as T);
                    }
                    else if(eAssetType == AssetType.AT_SPRITE)
                    {
                        return assetInst.objectRef.Target as T;
                    }
                    else if(eAssetType == AssetType.AT_ASSETS)
                    {
                        return assetInst.objectRef.Target as T;
                    }
                }
            }

            //LoadFromResources
            if(resInfoItem.LoadType == ResourceInfoTable.eLoadType.LoadFromResources)
            {
                return _LoadResFromResourcesFolder<T>(resInfoItem, eAssetType, iHashCode);
            }
            else if(resInfoItem.LoadType == ResourceInfoTable.eLoadType.LoadFromBundle)
            {
                return _LoadResFromStreamingAssetsBundle<T>(resInfoItem, eAssetType, iHashCode);
            }

            LoggerManager.Instance().LogErrorFormat("LoadResources failed path={0} eAssetType={1} type={2}", path, eAssetType, typeof(T).Name);

            return default(T);
        }
    }
}