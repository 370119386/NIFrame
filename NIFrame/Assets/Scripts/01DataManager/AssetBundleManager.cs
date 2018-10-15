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
        protected string baseUrl = string.Empty;
        protected AssetBundleManifest mBundleManifest;
        protected Dictionary<string, BundleInfo> mLoadedBundles = new Dictionary<string, BundleInfo>();

        public void Initialize(object argv,string baseUrl)
        {
            gameHandle = argv as GameFrameWork;
            this.baseUrl = baseUrl;
        }

        public AssetBundle getAssetBundle(string bundleName)
        {
            if(mLoadedBundles.ContainsKey(bundleName))
            {
                return mLoadedBundles[bundleName].bundle;
            }
            return null;
        }

        public bool IsBundleExist(string bundleName)
        {
            if(!mLoadedBundles.ContainsKey(bundleName))
            {
                return false;
            }

            if(null == mLoadedBundles[bundleName])
            {
                return false;
            }

            if(null == mLoadedBundles[bundleName].bundle)
            {
                return false;
            }

            return true;
        }

        public IEnumerator LoadAssetBundle(string bundleName,UnityAction onSucceed,UnityAction onFailed,bool bLoadAssetBundleFromStreamingAssets)
        {
            if (IsBundleExist(bundleName))
            {
                LoggerManager.Instance().LogFormat("DownLoadAssetBundle {0} Failed , this bundle has already loaded ...", bundleName);
                if (null != onSucceed)
                {
                    onSucceed.Invoke();
                }
                yield break;
            }

            var bundleUrl = CommonFunction.getAssetBundleSavePath(string.Format("{0}/{1}",CommonFunction.getPlatformString(), bundleName),true, bLoadAssetBundleFromStreamingAssets);
            LoggerManager.Instance().LogProcessFormat("[LoadAssetBundle]: bundleName = {0} bundleUrl = {1}", bundleName, bundleUrl);

            using (UnityWebRequest www = UnityWebRequest.Get(bundleUrl))
            {
                DownloadHandlerAssetBundle handler = new DownloadHandlerAssetBundle(www.url, 0);
                www.downloadHandler = handler;

                yield return www.Send();

                if (www.error != null)
                {
                    LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundle Failed:{0} url={1}", www.error, bundleUrl);
                    if (null != onFailed)
                    {
                        onFailed.Invoke();
                    }
                }
                else
                {
                    AssetBundle bundle = handler.assetBundle;
                    if (null == bundle)
                    {
                        LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundle Failed: Bundle Downloaded is null ...");
                        yield break;
                    }

                    if (!mLoadedBundles.ContainsKey(bundleName))
                    {
                        mLoadedBundles.Add(bundleName, new BundleInfo
                        {
                            bundle = bundle,
                        });
                    }
                    else
                    {
                        if (null == mLoadedBundles[bundleName])
                        {
                            mLoadedBundles[bundleName] = new BundleInfo { bundle = bundle };
                        }
                        else
                        {
                            mLoadedBundles[bundleName].bundle = bundle;
                        }
                    }

                    if (null != onSucceed)
                    {
                        onSucceed.Invoke();
                    }
                }
            }
        }

        public void UnLoadAssetBundle(string bundleName)
        {
            if (mLoadedBundles.ContainsKey(bundleName))
            {
                var bundleInfo = mLoadedBundles[bundleName];
                if(null != bundleInfo && null != bundleInfo.bundle)
                {
                    bundleInfo.bundle.Unload(true);
                    bundleInfo.bundle = null;
                }
            }
        }

        IEnumerator DownLoadAssetBundleByBuffer(string url, UnityEngine.Events.UnityAction<byte[]> cb, UnityEngine.Events.UnityAction onFailed)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                DownloadHandlerBuffer handler = new DownloadHandlerBuffer();
                www.downloadHandler = handler;
                yield return www.Send();

                if (www.isError)
                {
                    LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundleByBuffer Failed:{0} url={1}", www.error, url);

                    if (null != onFailed)
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

        public IEnumerator DownLoadAssetBundles(string url, List<string> bundles, UnityAction onSucceed, UnityAction onFailed)
        {
            bool succeed = true;
            for (int i = 0; i < bundles.Count; ++i)
            {
                var bundleName = bundles[i];
                var assetBundleUrl = string.Format("{0}{1}", url, bundleName);

                yield return DownLoadAssetBundleByBuffer(assetBundleUrl, (byte[] datas) =>
                {
                    var savePath = CommonFunction.getAssetBundleSavePath(CommonFunction.getPlatformString() + "/" + bundleName, false);
                    SaveFile(savePath, datas, () => { succeed = false; });
                },
                () =>
                {
                    succeed = false;
                    LoggerManager.Instance().LogErrorFormat("DownLoad AssetBundle Failed ... For bundleName = [{0}] ...", bundleName);
                });

                if (!succeed)
                {
                    if (null != onFailed)
                    {
                        onFailed.Invoke();
                    }
                    yield break;
                }
            }

            if (null != onSucceed)
            {
                onSucceed.Invoke();
            }
        }

        public IEnumerator LoadAssetBundleFromPkg(string mBundleName, UnityAction onSucceed, UnityAction onFailed)
        {
            bool loadFromStreamingAssets = AssetLoaderManager.Instance().NeedLoadFromStreamingAssets(mBundleName);
            yield return LoadAssetBundle(mBundleName, onSucceed, onFailed, loadFromStreamingAssets);
        }

        public IEnumerator LoadAssetBundleFromModule(int moduleId, UnityAction onSucceed, UnityAction onFailed)
        {
            var moduleItem = TableManager.Instance().GetTableItem<ModuleTable>(moduleId);
            if (null == moduleItem)
            {
                if(null != onFailed)
                {
                    onFailed.Invoke();
                }
                yield break;
            }

            bool succeed = true;
            for(int i = 0; i < moduleItem.RequiredBundles.Count; ++i)
            {
                yield return LoadAssetBundleFromPkg(moduleItem.RequiredBundles[i], null, ()=>
                {
                    succeed = false;
                });
                if(!succeed)
                {
                    LoggerManager.Instance().LogErrorFormat("Load {0} AssetBundleFailed ...", moduleItem.RequiredBundles[i]);
                    break;
                }
            }

            if(succeed)
            {
                if(null != onSucceed)
                {
                    onSucceed.Invoke();
                }
            }
            else
            {
                if(null != onFailed)
                {
                    onFailed.Invoke();
                }
            }
        }
    }
}