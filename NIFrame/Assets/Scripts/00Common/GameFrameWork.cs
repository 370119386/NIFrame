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

        public string getConfigUrl()
        {
            return mBaseConfigUrl + CommonFunction.getPlatformString() + "/" + mBundleName;
        }

        void Awake()
        {
            SceneManager.Create();
        }

        // Use this for initialization
        void Start()
        {
            SystemManager.Instance().Initialize(this);
            UIManager.Instance().Initialize(mLayers);
            TableManager.Instance().Initialize(@"Data/Table/");

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
            CSEC_DOWNLOAD_LATEST_VERSION_FAILED = 7,
            CSEC_CHECK_VERSION_MD5_FAILED = 8,
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
                for(int i = mNativeVersionItem.ID + 1; i <= mRemoteVersionItem.ID; ++i)
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

                yield return AssetBundleManager.Instance().DownLoadCurrentVersionBundles(mRemoteVersionTable[mRemoteVersionItem.ID] as VersionConfigTable,null,()=>
                {
                    mGameStartErrorCode = GameStartErrorCode.CSEC_DOWNLOAD_LATEST_VERSION_FAILED;
                });

                if(mGameStartErrorCode != GameStartErrorCode.GSEC_SUCCEED)
                {
                    ReportError(mGameStartErrorCode);
                    yield break;
                }

                LoggerManager.Instance().LogProcessFormat("[Succeed]:更新客户端版本成功 = [{0}|{1}] ... [{2}|{3}]",
                    mNativeVersionItem.Desc, mNativeVersionItem.ID, mRemoteVersionItem.Desc, mRemoteVersionItem.ID);

                yield return AssetBundleManager.Instance().CheckVersionFileMD5(mRemoteVersionTable[mRemoteVersionItem.ID] as VersionConfigTable, null, () =>
                {
                    mGameStartErrorCode = GameStartErrorCode.CSEC_CHECK_VERSION_MD5_FAILED;
                });

                if (mGameStartErrorCode != GameStartErrorCode.GSEC_SUCCEED)
                {
                    ReportError(mGameStartErrorCode);
                    yield break;
                }

                LoggerManager.Instance().LogProcessFormat("[Succeed]:校验文件MD5成功 ...");

                yield return StartGame(mRemoteVersionTable[mRemoteVersionItem.ID] as VersionConfigTable,false);
            }
            else
            {
                LoggerManager.Instance().LogProcessFormat("[Succeed]:校验版本号成功 = [{0}] ... Id = [{1}]", mRemoteVersionItem.Desc, mRemoteVersionItem.ID);

                yield return AssetBundleManager.Instance().CheckVersionFileMD5(mNativeVersionTable[mNativeVersionItem.ID] as VersionConfigTable, null, () =>
                {
                    mGameStartErrorCode = GameStartErrorCode.CSEC_CHECK_VERSION_MD5_FAILED;
                },0 != mNativeVersionItem.NeedUpdateAll);

                if (mGameStartErrorCode != GameStartErrorCode.GSEC_SUCCEED)
                {
                    ReportError(mGameStartErrorCode);
                    LoggerManager.Instance().LogProcessFormat("[Failed]:校验文件MD5失败 ...");
                    yield break;
                }

                LoggerManager.Instance().LogProcessFormat("[Succeed]:校验文件MD5成功 ...");

                yield return StartGame(mNativeVersionTable[mNativeVersionItem.ID] as VersionConfigTable,0 != mNativeVersionItem.NeedUpdateAll);
            }
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
                    case GameStartErrorCode.GSEC_NATIVE_VERSITION_FAILED:
                        {
                            LoggerManager.Instance().LogErrorFormat("down load native version table failed ...");
                            break;
                        }
                    case GameStartErrorCode.CSEC_FETCH_VERSION_CHANGED_FAILED:
                        {
                            LoggerManager.Instance().LogErrorFormat("native version error ...");
                            break;
                        }
                    case GameStartErrorCode.CSEC_QUERY_VERSION_ITEM_FAILED:
                        {
                            LoggerManager.Instance().LogErrorFormat("version item query failed ...");
                            break;
                        }
                    case GameStartErrorCode.CSEC_NEED_REDOWNLOAD:
                        {
                            LoggerManager.Instance().LogErrorFormat("game need redownloaded ... has global updated ...");
                            break;
                        }
                    case GameStartErrorCode.CSEC_VERSION_UPDATE_FAILED:
                        {
                            LoggerManager.Instance().LogErrorFormat("version update failed ...");
                            break;
                        }
                    case GameStartErrorCode.CSEC_DOWNLOAD_LATEST_VERSION_FAILED:
                        {
                            LoggerManager.Instance().LogErrorFormat("download latest version failed ...");
                            break;
                        }
                    case GameStartErrorCode.CSEC_CHECK_VERSION_MD5_FAILED:
                        {
                            LoggerManager.Instance().LogErrorFormat("check version failed ... need redownload ...");
                            break;
                        }
                }
            }
        }

        protected IEnumerator StartGame(VersionConfigTable version,bool bLoadAssetBundleFromStreamingAssets)
        {
            LoggerManager.Instance().LogProcessFormat("[Succeed]:开始加载游戏[BaseAssetBundles] ... 版本号 = [{0}]",version.Desc);

            bool succeed = true;
            var platformBundleName = CommonFunction.getPlatformString();
            yield return AssetBundleManager.Instance().LoadAssetBundle(platformBundleName, null,()=>
            {
                LoggerManager.Instance().LogErrorFormat("[Failed]:加载 [{0}] platform assetbundle failed", platformBundleName);
                succeed = false;
            }, bLoadAssetBundleFromStreamingAssets);

            if(!succeed)
            {
                yield break;
            }
            LoggerManager.Instance().LogProcessFormat("[Succeed]:加载 [{0}] platform assetbundle succeed", platformBundleName);

            for(int i = 0; i < version.BaseAssetBundles.Count; ++i)
            {
                var bundleName = version.BaseAssetBundles[i];
                if (!string.IsNullOrEmpty(bundleName))
                {
                    yield return AssetBundleManager.Instance().LoadAssetBundle(bundleName, null, () =>
                    {
                        LoggerManager.Instance().LogErrorFormat("[Failed]:加载 [{0}] general assetbundle failed", bundleName);
                        succeed = false;
                    }, bLoadAssetBundleFromStreamingAssets);
                    if(!succeed)
                    {
                        yield break;
                    }
                    LoggerManager.Instance().LogProcessFormat("[Succeed]:加载 [{0}] general assetbundle succeed", bundleName);
                }
            }

            LoggerManager.Instance().LogProcessFormat("[Succeed]:开始进入游戏 ... 版本号 = [{0}]", version.Desc);

            if(!AssetLoaderManager.Instance().Initialize(new AssetLoaderData
            {
                frameHandle = this,
                localResourcesInfoTable = TableManager.Instance().ReadTableFromAssetBundle<ResourceInfoTable>(AssetBundleManager.Instance().getAssetBundle(mBundleName)),
            }))
            {
                LoggerManager.Instance().LogErrorFormat("AssetLoaderManager Initialize Failed ...");
                yield break;
            }

            LoggerManager.Instance().LogProcessFormat("[Succeed]:AssetLoaderManager Initialize Succeed ...");
            TableManager.Instance().LoadTable<SceneTable>();

            SceneManager.Instance().SwitchScene(1);
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

        protected IEnumerator LoadNativeVersionFromAssetsBundle()
        {
            var url = CommonFunction.getAssetBundleSavePath(mBundleName,true);
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
            yield return LoadNativeVersionFromAssetsBundle();

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