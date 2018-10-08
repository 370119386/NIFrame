using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ProtoTable;

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
                LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundle Failed:{0}",www.error);
            }
            else
            {
                AssetBundle bundle = handler.assetBundle;
                if(null == bundle)
                {
                    LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundle Failed: Bundle Downloaded is null ...");
                    yield break;
                }

                if(null != cb)
                {
                    cb.Invoke(bundle);
                }
            }
        }

        public IEnumerator DownLoadCurrentVersionBundles(ProtoTable.VersionConfigTable version)
        {
            if(null == version)
            {
                LoggerManager.Instance().LogErrorFormat("version is null when DownLoadCurrentVersionBundles ...");
                yield break;
            }

            var url = getVersionUrl(version);
            LoggerManager.Instance().LogProcessFormat("Start DownLoad Bundles Version = {0} Url = {1}",version.Desc,url);

            yield return DownLoadAssetBundle(url, (AssetBundle manifestBundle) =>
             {
                mBundleManifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
             });

            if(null == mBundleManifest)
            {
                LoggerManager.Instance().LogProcessFormat("DownLoad Manifest Failed ... For Version {0}",version.Desc);
                yield break;
            }

            LoggerManager.Instance().LogProcessFormat("DownLoad Manifest Succeed ... For Version = {0}", version.Desc);

            for (int i = 0; i < version.BaseAssetBundles.Count; ++i)
            {
                yield return DownLoadAssetBundle(string.Empty, null);
            }
        }
    }
}