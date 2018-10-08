using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using ProtoTable;

namespace NI
{
    public class GameFrameWork : MonoBehaviour
    {
        public UILayer[] mLayers = new UILayer[0];
        public string mBaseConfigUrl = @"https://resourcekids.66uu.cn/kids/TestAds/";
        public string mBundleName = @"base_tables";
        public string mNativeBaseConfigUrl = @"Data/Table/";

        public string getConfigUrl()
        {
            return mBaseConfigUrl + CommonFunction.getPlatformString() + "/" + mBundleName;
        }

        // Use this for initialization
        void Start()
        {
            SystemManager.Instance().Initialize(this);
            UIManager.Instance().Initialize(mLayers);

            AssetLoaderManager.Instance().Initialize(new AssetLoaderData
            {
                frameHandle = this,
                BaseConfigUrl = mBaseConfigUrl,
                localResourcesInfoTable = TableManager.Instance().ReadTableFromResourcesFile<ResourceInfoTable>(@"Data/Table"),
                remoteResourcesInfoTable = null,
            });

            GameObject.DontDestroyOnLoad(this);

            StartCoroutine(CheckVersion());
        }

        protected enum GameStartErrorCode
        {
            GSEC_SUCCEED = 0,
            GSEC_REMOTE_VERSITION_FAILED = 1,
            GSEC_NATIVE_VERSITION_FAILED = 2,
            CSEC_FETCH_VERSION_CHANGED_FAILED = 3,
            CSEC_QUERY_VERSION_ITEM_FAILED = 4,
            CSEC_NEED_REDOWNLOAD = 5,
            CSEC_VERSION_UPDATE_FAILED = 6,
            CSEC_DOWNLOAD_MANIFEST_BUNDLE_FAILED = 7,
        }
        protected GameStartErrorCode mGameStartErrorCode = GameStartErrorCode.GSEC_SUCCEED;
        protected Dictionary<int, object> mRemoteVersionTable = null;
        protected VersionConfigTable mRemoteVersionItem = null;
        protected Dictionary<int, object> mNativeVersionTable = null;
        protected VersionConfigTable mNativeVersionItem = null;

        IEnumerator CheckVersion()
        {
            mGameStartErrorCode = GameStartErrorCode.GSEC_SUCCEED;

            yield return LoadRemoteVersionInfo(getConfigUrl());
            if(mGameStartErrorCode == GameStartErrorCode.GSEC_SUCCEED)
            {
                LoggerManager.Instance().LogProcessFormat("[Succeed]:拉取远端版本号 = [{0}] ... Id = [{1}]", mRemoteVersionItem.Desc, mRemoteVersionItem.ID);
            }
            else
            {
                ReportError(mGameStartErrorCode);
                yield break;
            }

            yield return LoadNativeVersionInfo();
            if (mGameStartErrorCode == GameStartErrorCode.GSEC_SUCCEED)
            {
                LoggerManager.Instance().LogProcessFormat("[Succeed]:拉取本地版本号 = [{0}] ... Id = [{1}]", mNativeVersionItem.Desc, mNativeVersionItem.ID);
            }
            else
            {
                ReportError(mGameStartErrorCode);
                yield break;
            }

            if (mNativeVersionItem.ID > mRemoteVersionItem.ID)
            {
                mGameStartErrorCode = GameStartErrorCode.CSEC_FETCH_VERSION_CHANGED_FAILED;
                ReportError(mGameStartErrorCode);
                yield break;
            }

            if (mNativeVersionItem.ID < mRemoteVersionItem.ID)
            {
                bool needUpdateAll = false;
                for(int i = mNativeVersionItem.ID; i < mRemoteVersionItem.ID; ++i)
                {
                    int iId = i;
                    if(!mRemoteVersionTable.ContainsKey(iId))
                    {
                        mGameStartErrorCode = GameStartErrorCode.CSEC_QUERY_VERSION_ITEM_FAILED;
                        ReportError(mGameStartErrorCode);
                        yield break;
                    }

                    var versionItem = mRemoteVersionTable[iId] as ProtoTable.VersionConfigTable;
                    if(null == versionItem)
                    {
                        mGameStartErrorCode = GameStartErrorCode.CSEC_QUERY_VERSION_ITEM_FAILED;
                        ReportError(mGameStartErrorCode);
                        yield break;
                    }

                    if(0 != versionItem.NeedUpdateAll)
                    {
                        needUpdateAll = true;
                        break;
                    }
                }

                if(needUpdateAll)
                {
                    mGameStartErrorCode = GameStartErrorCode.CSEC_NEED_REDOWNLOAD;
                    ReportError(mGameStartErrorCode);
                    yield break;
                }

                yield return AssetBundleManager.Instance().DownLoadCurrentVersionBundles(mRemoteVersionTable[mRemoteVersionItem.ID] as VersionConfigTable);

                //yield return FetchFilesFromRemote(mRemoteVersionTable[mRemoteVersionItem.ID] as VersionConfigTable);

                if(mGameStartErrorCode == GameStartErrorCode.GSEC_SUCCEED)
                {
                    LoggerManager.Instance().LogProcessFormat("[Succeed]:更新客户端版本成功 = [{0}|{1}] ... [{2}|{3}]", 
                        mNativeVersionItem.Desc, mNativeVersionItem.ID, mRemoteVersionItem.Desc, mRemoteVersionItem.ID);
                }
                else
                {
                    ReportError(mGameStartErrorCode);
                    yield break;
                }
            }
            else
            {
                LoggerManager.Instance().LogProcessFormat("[Succeed]:校验版本号成功 = [{0}] ... Id = [{1}]", mRemoteVersionItem.Desc, mRemoteVersionItem.ID);
            }

            yield return StartGame();
        }

        void ReportError(GameStartErrorCode error)
        {
            if(error != GameStartErrorCode.GSEC_SUCCEED)
            {
                switch (error)
                {
                    case GameStartErrorCode.GSEC_REMOTE_VERSITION_FAILED:
                        {
                            LoggerManager.Instance().LogErrorFormat("down load remote version table failed ...");
                            break;
                        }
                }
            }
        }

        protected IEnumerator StartGame()
        {
            LoggerManager.Instance().LogProcessFormat("[Succeed]:开始进入游戏 ...");
            yield return null;
        }

        protected IEnumerator FetchFilesFromRemote(VersionConfigTable version)
        {
            string url = string.Empty;
#if UNITY_IOS
            url = version.IosUrl;
#elif UNITY_ANDROID
            url = version.AndroidUrl;
#endif
            LoggerManager.Instance().LogProcessFormat("[Try]:准备提取远端版本号...{0},url={1}", version.Desc, url);

            yield return DownLoadManifestBundle(url);
        }

        protected IEnumerator DownLoadManifestBundle(string url)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            DownloadHandlerAssetBundle handler = new DownloadHandlerAssetBundle(www.url, 0);
            www.downloadHandler = handler;
            yield return www.Send();

            if (www.isError)
            {
                LoggerManager.Instance().LogError(www.error);
                mGameStartErrorCode = GameStartErrorCode.CSEC_DOWNLOAD_MANIFEST_BUNDLE_FAILED;
            }
            else
            {
                AssetBundle bundle = handler.assetBundle;
                if (null == bundle)
                {
                    mGameStartErrorCode = GameStartErrorCode.CSEC_DOWNLOAD_MANIFEST_BUNDLE_FAILED;
                    yield break;
                }

                System.IO.File.WriteAllBytes(bundle.name, handler.data);

                //1 load remote version config table
                AssetBundleManifest manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                if(null != manifest)
                {
                    LoggerManager.Instance().LogProcessFormat("down load manifest succeed ...");
                }

                bundle.Unload(true);
            }
        }

        protected IEnumerator LoadRemoteVersionInfo(string url)
        {
            LoggerManager.Instance().LogFormat("remote url = {0}", url);
            UnityWebRequest www = UnityWebRequest.Get(url);
            DownloadHandlerAssetBundle handler = new DownloadHandlerAssetBundle(www.url, 0);
            www.downloadHandler = handler;
            yield return www.Send();

            if (www.isError)
            {
                LoggerManager.Instance().LogError(www.error);
                mGameStartErrorCode = GameStartErrorCode.GSEC_REMOTE_VERSITION_FAILED;
            }
            else
            {
                AssetBundle bundle = handler.assetBundle;
                if(null == bundle)
                {
                    mGameStartErrorCode = GameStartErrorCode.GSEC_REMOTE_VERSITION_FAILED;
                    yield break;
                }

                //1 load remote version config table
                mRemoteVersionTable = TableManager.Instance().ReadTableFromAssetBundle<VersionConfigTable>(bundle);
                mRemoteVersionItem = getLatestVersionItem(mRemoteVersionTable);
                if (null == mRemoteVersionItem)
                {
                    mGameStartErrorCode = GameStartErrorCode.GSEC_REMOTE_VERSITION_FAILED;
                    yield break;
                }

                bundle.Unload(true);
            }
        }

        protected IEnumerator LoadTestAssetBundleFromFile()
        {
            var url = CommonFunction.getStreamingAssetsPath("ui/image/pck_ui_lobby_main_1");
            LoggerManager.Instance().LogFormat("native url = {0}", url);
            UnityWebRequest www = UnityWebRequest.Get(url);
            DownloadHandlerAssetBundle handler = new DownloadHandlerAssetBundle(www.url, 0);
            www.downloadHandler = handler;
            yield return www.Send();

            if (www.isError)
            {
                Debug.LogErrorFormat(www.error);
            }
            else
            {
                AssetBundle bundle = handler.assetBundle;
                if(null == bundle)
                {
                    Debug.LogErrorFormat("load test bundle failed ...");
                }
                else
                {
                    Debug.LogErrorFormat("Load assetbundle succeed ... for test asset bundle ...");
                }
            }
        }

        protected IEnumerator LoadNativeVersionFromStreamingAssetsBundle()
        {
            var url = CommonFunction.getStreamingAssetsPath(mNativeBaseConfigUrl);
            LoggerManager.Instance().LogFormat("native url = {0}", url);
            UnityWebRequest www = UnityWebRequest.Get(url);
            DownloadHandlerAssetBundle handler = new DownloadHandlerAssetBundle(www.url, 0);
            www.downloadHandler = handler;
            yield return www.Send();

            if(!www.isError && null != handler.assetBundle)
            {
                AssetBundle bundle = handler.assetBundle;
                mNativeVersionTable = TableManager.Instance().ReadTableFromAssetBundle<VersionConfigTable>(bundle);
                mNativeVersionItem = getLatestVersionItem(mNativeVersionTable);
                bundle.Unload(true);

                if(null != mNativeVersionItem)
                {
                    LoggerManager.Instance().LogFormat("load native version from streamingassets file succeed ...");
                }
                else
                {
                    LoggerManager.Instance().LogFormat("load native version from streamingassets file failed ...");
                }
            }
            else
            {
                LoggerManager.Instance().LogFormat("load native version from streamingassets file failed ...");
            }
        }

        protected IEnumerator LoadNativeVersionInfo()
        {
            yield return LoadNativeVersionFromStreamingAssetsBundle();

            if (null == mNativeVersionItem)
            {
                mNativeVersionTable = TableManager.Instance().ReadTableFromResourcesFile<VersionConfigTable>(@"Data/Table");
                if (null == mNativeVersionTable)
                {
                    mGameStartErrorCode = GameStartErrorCode.GSEC_NATIVE_VERSITION_FAILED;
                    yield break;
                }

                mNativeVersionItem = getLatestVersionItem(mNativeVersionTable);
                if (null == mNativeVersionItem)
                {
                    mGameStartErrorCode = GameStartErrorCode.GSEC_NATIVE_VERSITION_FAILED;
                    yield break;
                }
            }
        }

        protected VersionConfigTable getLatestVersionItem(Dictionary<int,object> versionConfigTable)
        {
            VersionConfigTable latestVersion = null;

            if (null != versionConfigTable)
            {
                var iter = versionConfigTable.GetEnumerator();
                while (iter.MoveNext())
                {
                    latestVersion = iter.Current.Value as VersionConfigTable;
                }
            }

            return latestVersion;
        }
    }
}