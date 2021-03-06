﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProtoTable;
using System;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace NI
{
    public class AssetLoaderData
    {
        public GameFrameWork frameHandle;
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

        protected Dictionary<int, ResourceInfoTable> mLocalResourcesInfoTable = new Dictionary<int, ResourceInfoTable>();
        protected Dictionary<int, AssetInstance> mAlivedObjects = new Dictionary<int, AssetInstance>();

        protected Dictionary<string,string> mRemoteFileMD5Dic = new Dictionary<string, string>(32);
        protected Dictionary<string, string> mLocalFileMD5Dic = new Dictionary<string, string>(32);

        public bool IsMD5FileLoadSucceed
        {
            get
            {
                return mRemoteFileMD5Dic.Count > 0;
            }
        }

        byte[] mLocalVersion = new byte[4];
        byte[] mRemoteVersion = new byte[4];
        public bool IsVersionOK
        {
            get;private set;
        }
        public string RemoteVersion
        {
            get
            {
                return string.Format("{0}.{1}.{2}.{3}", mRemoteVersion[0], mRemoteVersion[1], mRemoteVersion[2], mRemoteVersion[3]);
            }
        }

        public bool HasLargeUpdate
        {
            get
            {
                return mLocalVersion[0] != mRemoteVersion[0] || mLocalVersion[1] != mRemoteVersion[1];
            }
        }

        public bool HasSmallUpdate
        {
            get
            {
                return mLocalVersion[2] != mRemoteVersion[2] || mLocalVersion[3] != mRemoteVersion[3];
            }
        }

        protected bool SetVersion(string version,ref byte[] vv)
        {
            if(string.IsNullOrEmpty(version))
            {
                return false;
            }

            var tokens = version.Split('.');
            if(tokens.Length != 4)
            {
                return false;
            }

            for(int i = 0; i < tokens.Length; ++i)
            {
                if(string.IsNullOrEmpty(tokens[i]))
                {
                    return false;
                }

                if(!byte.TryParse(tokens[i],out vv[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool SetLocalVersion(string version)
        {
            return SetVersion(version, ref mLocalVersion);
        }

        public IEnumerator LoadRemoteVersion(string url)
        {
            IsVersionOK = false;

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.Send();

                if (www.isError)
                {
                    LoggerManager.Instance().LogErrorFormat(www.error);
                }
                else
                {
                    IsVersionOK = SetVersion(www.downloadHandler.text, ref mRemoteVersion);
                }
            }
        }

        public bool SetLocalVersionMD5(string content)
        {
            mLocalFileMD5Dic.Clear();
            if (!string.IsNullOrEmpty(content))
            {
                var tokens = content.Split(new char[] { '\r', '\n' });
                for (int i = 0; i < tokens.Length; ++i)
                {
                    var token = tokens[i].Split('|');
                    if (2 == token.Length && !string.IsNullOrEmpty(token[0]) && !string.IsNullOrEmpty(token[1]))
                    {
                        if (!mLocalFileMD5Dic.ContainsKey(token[0]))
                        {
                            mLocalFileMD5Dic.Add(token[0], token[1]);
                        }
                    }
                }
            }
            return mLocalFileMD5Dic.Count > 0;
        }

        public IEnumerator LoadRemoteVersionMD5Files(string url, UnityAction onSucceed, UnityAction onFailed)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.Send();

                if (www.isError)
                {
                    LoggerManager.Instance().LogErrorFormat(www.error);
                    if (null != onFailed)
                    {
                        onFailed.Invoke();
                    }
                }
                else
                {
                    mRemoteFileMD5Dic.Clear();
                    if (!string.IsNullOrEmpty(www.downloadHandler.text))
                    {
                        var tokens = www.downloadHandler.text.Split(new char[] { '\r', '\n' });
                        for (int i = 0; i < tokens.Length; ++i)
                        {
                            var token = tokens[i].Split('|');
                            if (2 == token.Length && !string.IsNullOrEmpty(token[0]) && !string.IsNullOrEmpty(token[1]))
                            {
                                if (!mRemoteFileMD5Dic.ContainsKey(token[0]))
                                {
                                    mRemoteFileMD5Dic.Add(token[0], token[1]);
                                }
                            }
                        }
                    }

                    if (mRemoteFileMD5Dic.Count == 0)
                    {
                        LoggerManager.Instance().LogErrorFormat(@"Load Remote MD5File Failed ...");
                        if (null != onFailed)
                        {
                            onFailed.Invoke();
                        }
                    }
                    else
                    {
                        if (null != onSucceed)
                        {
                            onSucceed.Invoke();
                        }
                    }
                }
            }
        }

        public void GetNeedDownLoadModule(string[] bundles,List<string> needDownLoadBundles)
        {
            for (int i = 0; i < bundles.Length; ++i)
            {
                var bundle = bundles[i];
                string localMd5 = string.Empty;
                string remoteMd5 = string.Empty;

                if(mLocalFileMD5Dic.ContainsKey(bundle))
                {
                    localMd5 = mLocalFileMD5Dic[bundle];
                }

                if(mRemoteFileMD5Dic.ContainsKey(bundle))
                {
                    remoteMd5 = mRemoteFileMD5Dic[bundle];
                }

                if(localMd5.Equals(remoteMd5))
                {
                    continue;
                }

                var localNewMd5 = CommonFunction.GetMD5HashFromFile(CommonFunction.getAssetBundleSavePath(CommonFunction.getPlatformString() + "/" + bundle, false,false));
                if(!string.IsNullOrEmpty(localNewMd5))
                {
                    Debug.LogFormat("localNewMd5 = {0}", localNewMd5);
                }

                if(localNewMd5 == remoteMd5)
                {
                    continue;
                }

                needDownLoadBundles.Add(bundle);
            }
        }
        public bool NeedLoadFromStreamingAssets(string mBundleName)
        {
            if(mRemoteFileMD5Dic.ContainsKey(mBundleName))
            {
                if(!mLocalFileMD5Dic.ContainsKey(mBundleName))
                {
                    return false;
                }

                return mRemoteFileMD5Dic[mBundleName].Equals(mLocalFileMD5Dic[mBundleName]);
            }

            return true;
        }

        public bool Initialize(object argv)
        {
            var data = argv as AssetLoaderData;
            frameHandle = data.frameHandle;
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
                            if(!mLocalResourcesInfoTable.ContainsKey(iHashCode))
                            {
                                mLocalResourcesInfoTable.Add(iHashCode, resItem);
                            }
                            else
                            {
                                var orgItem = mLocalResourcesInfoTable[iHashCode];
                                mLocalResourcesInfoTable[iHashCode] = resItem;
                                LoggerManager.Instance().LogErrorFormat("mLocalResourcesInfoTable hash key repeated for id = {0} and id = {1}", orgItem.ID,resItem.ID);
                            }
                        }
                    }
                }
            }

            return mLocalResourcesInfoTable.Count > 0;
        }

        protected ResourceInfoTable getResourceInfo(int iHashCode)
        {
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
                if (resInfoItem.Path.Count != 2)
                {
                    LoggerManager.Instance().LogErrorFormat("Load Prefab From AssetBundle Failed ... need 2 argvs ... now is {0}",resInfoItem.Path.Count);
                    return default(T);
                }

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

                if(resInfoItem.Path.Count != 3)
                {
                    LoggerManager.Instance().LogErrorFormat("Load Sprite From AssetBundle Failed ... need 3 argvs ...now is {0}",resInfoItem.Path.Count);
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
                AssetBundle assetBundle = AssetBundleManager.Instance().getAssetBundle(resInfoItem.Path[0]);

                if(null == assetBundle)
                {
                    LoggerManager.Instance().LogProcessFormat("get assetbundle failed name = {0} ...", resInfoItem.Path[0]);
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

        public void ReportAlivedObject()
        {
            var iter = mAlivedObjects.GetEnumerator();
            while(iter.MoveNext())
            {
                var assetInst = iter.Current.Value;
                if(null != assetInst && assetInst.objectRef.IsAlive)
                {
                    LoggerManager.Instance().LogFormat("[alivedRes]:<color=#00ff00>[Path={0}][LoadType={1}]</color>", assetInst.resInfo.Path, assetInst.resInfo.LoadType);
                }
            }
        }
    }
}