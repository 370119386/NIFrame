using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ProtoTable;
using UnityEngine.Events;

namespace NI
{
    public enum BundleStatus
    {
        BS_INVALID = -1,
        BS_DOWNLOADING,
        BS_EXISTED,
    }

    public class BundleInfo
    {
        public AssetBundle bundle;
    }

    public class AssetBundleManager : Singleton<AssetBundleManager>
    {
        protected GameFrameWork gameHandle;
        protected AssetBundleManifest mBundleManifest;
        protected Dictionary<string, BundleInfo> mLoadedBundles = new Dictionary<string, BundleInfo>();

        public void Initialize(object argv)
        {
            gameHandle = argv as GameFrameWork;
        }

        public AssetBundle getAssetBundle(string bundleName)
        {
            if(mLoadedBundles.ContainsKey(bundleName))
            {
                return mLoadedBundles[bundleName].bundle;
            }
            return null;
        }

        protected string getVersionUrl(VersionConfigTable version)
        {
            string url = string.Empty;
#if UNITY_IOS
            url = version.IosUrl;
#elif UNITY_ANDROID
            url = version.AndroidUrl;
#endif
            return url;
        }

        protected string getManifestName()
        {
            string manifest = string.Empty;
#if UNITY_IOS
            manifest = "iOS";
#elif UNITY_ANDROID
            manifest = "Android";
#endif
            return manifest;
        }

        public IEnumerator LoadAssetBundle(string bundleName,UnityAction onSucceed,UnityAction onFailed)
        {
            if (mLoadedBundles.ContainsKey(bundleName))
            {
                LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundle {0} Failed , this bundle has already loaded ...", bundleName);
                if (null != onSucceed)
                {
                    onSucceed.Invoke();
                }
                yield break;
            }

            var bundleUrl = CommonFunction.getAssetBundleSavePath(bundleName,true);
            UnityWebRequest www = UnityWebRequest.Get(bundleUrl);
            DownloadHandlerAssetBundle handler = new DownloadHandlerAssetBundle(www.url, 0);
            www.downloadHandler = handler;
            yield return www.Send();

            if (www.isError)
            {
                LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundle Failed:{0} url={1}",www.error, bundleUrl);
                if(null != onFailed)
                {
                    onFailed.Invoke();
                }
            }
            else
            {
                AssetBundle bundle = handler.assetBundle;
                if(null == bundle)
                {
                    LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundle Failed: Bundle Downloaded is null ...");
                    yield break;
                }

                mLoadedBundles.Add(bundleName, new BundleInfo
                {
                    bundle = bundle,
                });

                if (null != onSucceed)
                {
                    onSucceed.Invoke();
                }
            }
        }

        IEnumerator DownLoadAssetBundleByBuffer(string url, UnityEngine.Events.UnityAction<byte[]> cb, UnityEngine.Events.UnityAction onFailed)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            DownloadHandlerBuffer handler = new DownloadHandlerBuffer();
            www.downloadHandler = handler;
            yield return www.Send();

            if (www.isError)
            {
                LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundleByBuffer Failed:{0} url={1}", www.error, url);

                if(null != onFailed)
                {
                    onFailed.Invoke();
                }
            }
            else
            {
                LoggerManager.Instance().LogFormat("DownLoadAssetBundleByBuffer Succeed : Length = {0} ...", handler.data.Length);

                if (null != cb)
                {
                    cb.Invoke(www.downloadHandler.data);
                }
            }
        }

        protected void SaveFile(string path,byte[] datas,UnityEngine.Events.UnityAction onFailed = null)
        {
            var bundleName = System.IO.Path.GetFileName(path);
            var dir = System.IO.Path.GetDirectoryName(path);

            try
            {
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                System.IO.File.WriteAllBytes(path, datas);

                LoggerManager.Instance().LogProcessFormat("TrySave {0} To {1} Succeed ...", bundleName, path);
            }
            catch (System.Exception e)
            {
                LoggerManager.Instance().LogErrorFormat("TrySave {0} To {1} Failed ...", bundleName, path);
                LoggerManager.Instance().LogErrorFormat(e.ToString());
                if(null != onFailed)
                {
                    onFailed.Invoke();
                }
            }
        }

        protected void DeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    LoggerManager.Instance().LogProcessFormat("Delete file Path = {0}", path);
                    System.IO.File.Delete(path);
                    LoggerManager.Instance().LogProcessFormat("Delete {0} Succeed",path);
                }
            }
            catch (System.Exception e)
            {
                LoggerManager.Instance().LogErrorFormat("Delete file Path = {0} Failed:Error={1}", path, e.Message);
            }
        }

        public IEnumerator CheckVersionFileMD5(ProtoTable.VersionConfigTable version, UnityAction onSucceed, UnityAction onFailed)
        {
            if (null == version)
            {
                LoggerManager.Instance().LogErrorFormat("CheckFileMD5 Failed version is null ...");
                if (null != onFailed)
                {
                    onFailed.Invoke();
                }
                yield break;
            }

            var versionUrl = getVersionUrl(version);
            var platformBundle = CommonFunction.getAssetBundleSavePath(CommonFunction.getPlatformString(),false);
            if(true)
            {
                var platformMd5 = CommonFunction.GetMD5HashFromFile(platformBundle);
                if(string.IsNullOrEmpty(platformMd5))
                {
                    if (null != onFailed)
                    {
                        onFailed.Invoke();
                    }
                    LoggerManager.Instance().LogErrorFormat("CheckMD5 Failed For {0} platformBundle ...", platformBundle);
                    DeleteFile(platformBundle);
                    yield break;
                }

                LoggerManager.Instance().LogProcessFormat("[Succeed:] CheckMD5 Succeed For {0} platformBundle ...<color=#00ff00>{1}</color>", platformBundle,platformMd5);
                yield return null;
            }

            for (int i = 0; i < version.BaseAssetBundles.Count; ++i)
            {
                var bundleName = version.BaseAssetBundles[i];
                var bundlePath = CommonFunction.getAssetBundleSavePath(bundleName,false);
                var fileMd5 = CommonFunction.GetMD5HashFromFile(bundlePath);
                if (string.IsNullOrEmpty(fileMd5))
                {
                    if (null != onFailed)
                    {
                        onFailed.Invoke();
                    }
                    LoggerManager.Instance().LogErrorFormat("CheckMD5 Failed For {0} generalBundle ...", bundlePath);
                    DeleteFile(platformBundle);
                    yield break;
                }

                LoggerManager.Instance().LogProcessFormat("[Succeed]: CheckMD5 Succeed For {0} generalBundle ...<color=#00ff00>{1}</color>", bundlePath,fileMd5);
                yield return null;
            }

            if (null != onSucceed)
            {
                onSucceed.Invoke();
            }
        }

        public IEnumerator DownLoadCurrentVersionBundles(ProtoTable.VersionConfigTable version,UnityAction onSucceed, UnityAction onFailed)
        {
            if(null == version)
            {
                LoggerManager.Instance().LogErrorFormat("version is null when DownLoadCurrentVersionBundles ...");
                if(null != onFailed)
                {
                    onFailed.Invoke();
                }
                yield break;
            }

            var versionUrl = getVersionUrl(version);
            var url = string.Format("{0}{1}", versionUrl, CommonFunction.getPlatformString());
            LoggerManager.Instance().LogProcessFormat("Start DownLoad Bundles Version = {0} Url = {1}",version.Desc,url);

            bool succeed = true;
            yield return DownLoadAssetBundleByBuffer(url, (byte[] datas) =>
            {
                LoggerManager.Instance().LogProcessFormat("DownLoad DownLoadAssetBundleByBuffer Succeed ... For DataLength = {0}", datas.Length);
                var savePath = CommonFunction.getAssetBundleSavePath(CommonFunction.getPlatformString(),false);
                SaveFile(savePath, datas,()=>
                {
                    succeed = false;
                });
            },
            ()=>
            {
                succeed = false;
            });

            if(!succeed)
            {
                LoggerManager.Instance().LogErrorFormat("DownLoad Manifest Failed ... For url = {0}", url);
                if (null != onFailed)
                {
                    onFailed.Invoke();
                }
                yield break;
            }

            LoggerManager.Instance().LogProcessFormat("DownLoad Manifest Succeed ... For Version = {0}", version.Desc);

            for (int i = 0; i < version.BaseAssetBundles.Count; ++i)
            {
                var bundleName = version.BaseAssetBundles[i];
                var assetBundleUrl = string.Format("{0}{1}", versionUrl, version.BaseAssetBundles[i]);

                yield return DownLoadAssetBundleByBuffer(assetBundleUrl, (byte[] datas) =>
                {
                    var savePath = CommonFunction.getAssetBundleSavePath(bundleName, false);
                    SaveFile(savePath, datas, () => { succeed = false; });
                },
                ()=> 
                {
                    succeed = false;
                    LoggerManager.Instance().LogErrorFormat("DownLoad AssetBundle Failed ... For bundleName = [{0}] ...", bundleName);
                });

                if(!succeed)
                {
                    if (null != onFailed)
                    {
                        onFailed.Invoke();
                    }
                    yield break;
                }
            }

            if(null != onSucceed)
            {
                onSucceed.Invoke();
            }
        }
    }
}