using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ProtoTable;
using UnityEngine.Events;

namespace NI
{
    public class AssetBundleManager : Singleton<AssetBundleManager>
    {
        protected GameFrameWork gameHandle;
        protected AssetBundleManifest mBundleManifest;

        public void Initialize(object argv)
        {
            gameHandle = argv as GameFrameWork;
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

        IEnumerator DownLoadAssetBundle(string url,UnityEngine.Events.UnityAction<AssetBundle> cb)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            DownloadHandlerAssetBundle handler = new DownloadHandlerAssetBundle(www.url, 0);
            www.downloadHandler = handler;
            yield return www.Send();

            if (www.isError)
            {
                LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundle Failed:{0} url={1}",www.error,url);
            }
            else
            {
                AssetBundle bundle = handler.assetBundle;
                if(null == bundle)
                {
                    LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundle Failed: Bundle Downloaded is null ...");
                    yield break;
                }

                if (null != cb)
                {
                    cb.Invoke(bundle);
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
                LoggerManager.Instance().LogProcessFormat("TrySave {0} To {1} Failed ...", bundleName, path);
                LoggerManager.Instance().LogProcessFormat(e.ToString());
                if(null != onFailed)
                {
                    onFailed.Invoke();
                }
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
                var savePath = CommonFunction.getAssetBundleSavePath(CommonFunction.getPlatformString());
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
                var savePath = CommonFunction.getAssetBundleSavePath(bundleName);
                yield return DownLoadAssetBundleByBuffer(assetBundleUrl, (byte[] datas) =>
                {
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