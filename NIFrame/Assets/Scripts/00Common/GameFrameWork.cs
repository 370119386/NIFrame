using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProtoTable;

namespace NI
{
    public class GameFrameWork : MonoBehaviour
    {
        public UILayer[] mLayers = new UILayer[0];
        public string mBaseConfigUrl = @"https://resourcekids.66uu.cn/kids/TestAds/";
        public string mBundleName = @"base_tables";

        void Awake()
        {
            SceneManager.Create();
        }

        // Use this for initialization
        void Start()
        {
            Application.targetFrameRate = 30;

            SystemManager.Instance().Initialize(this);
            UIManager.Instance().Initialize(mLayers);
            TableManager.Instance().Initialize(@"Data/Table/");
            AssetBundleManager.Instance().Initialize(this,mBaseConfigUrl + CommonFunction.getPlatformString() + "/");

            Object.DontDestroyOnLoad(this);

            StartCoroutine(StartCheckVersion());
        }

        protected enum GMError
        {
            GME_SUCCEED = 0,
            GME_LoadVersionFailed,
            GME_LoadFileMD5Failed,
            GME_DownLoadBaseModuleFailed,
            GME_LoadBaseModuleFailed,
        }
        protected GMError mError = GMError.GME_SUCCEED;

        protected bool ReportError()
        {
            if(mError == GMError.GME_SUCCEED)
            {
                return false;
            }

            return true;
        }

        IEnumerator LoadVersionInfo()
        {
            if (!AssetLoaderManager.Instance().SetLocalVersion(Application.version))
            {
                mError = GMError.GME_LoadVersionFailed;
                LoggerManager.Instance().LogErrorFormat("SetLocalVersion Failed V = {0}", Application.version);
                yield break;
            }

            var versionUrl = string.Format("{0}{1}/Version.txt", mBaseConfigUrl, CommonFunction.getPlatformString());
            //var md5url = platformUrl + "/VersionMd5File.txt";

            yield return AssetLoaderManager.Instance().LoadRemoteVersion(versionUrl);
            if (!AssetLoaderManager.Instance().IsVersionOK)
            {
                mError = GMError.GME_LoadVersionFailed;
                LoggerManager.Instance().LogErrorFormat("SetRemoteVersion Failed ...");
                yield break;
            }
        }

        IEnumerator LoadFileMD5Info()
        {
            TextAsset versionText = Resources.Load<TextAsset>(@"Data/MD5/VersionMd5File");
            if (null == versionText)
            {
                mError = GMError.GME_LoadFileMD5Failed;
                LoggerManager.Instance().LogErrorFormat("加载本地 VersionMd5File 失败 !!!", mBundleName);
                yield break;
            }

            var succeed = AssetLoaderManager.Instance().SetLocalVersionMD5(versionText.text);
            if (!succeed)
            {
                mError = GMError.GME_LoadFileMD5Failed;
                LoggerManager.Instance().LogErrorFormat("解析本地 VersionMd5File 失败 !!!");
                yield break;
            }

            var md5url = string.Format("{0}{1}/{2}/VersionMd5File.txt",mBaseConfigUrl,CommonFunction.getPlatformString(),
                                       AssetLoaderManager.Instance().RemoteVersion);
                                       
            yield return AssetLoaderManager.Instance().LoadRemoteVersionMD5Files(md5url,null,()=>
            {
                succeed = false;
            });

            if (!succeed)
            {
                mError = GMError.GME_LoadFileMD5Failed;
                LoggerManager.Instance().LogErrorFormat("加载远程 VersionMd5File 失败 !!!");
                yield break;
            }
        }

        IEnumerator DownLoadBaseModule()
        {
            bool succeed = true;

            var baseBundleUrl = string.Format("{0}{1}/{2}/", mBaseConfigUrl, CommonFunction.getPlatformString(),
                                             AssetLoaderManager.Instance().RemoteVersion);

            yield return AssetBundleManager.Instance().DownLoadAssetBundles(baseBundleUrl, new List<string>() { mBundleName }, null, () =>
            {
                succeed = false;
            });

            if (!succeed)
            {
                mError = GMError.GME_DownLoadBaseModuleFailed;
                LoggerManager.Instance().LogErrorFormat("从远程加载[{0}] Bundle 失败 !!!", mBundleName);
                yield break;
            }

            yield return AssetBundleManager.Instance().LoadAssetBundle(mBundleName, null, () =>
            {
                succeed = false;
            }, false);

            if (!succeed)
            {
                mError = GMError.GME_DownLoadBaseModuleFailed;
                LoggerManager.Instance().LogErrorFormat("从本地加载 刚下载的[{0}] Bundle 失败 !!!", mBundleName);
                yield break;
            }

            var baseBundle = AssetBundleManager.Instance().getAssetBundle(mBundleName);
            if (null == baseBundle)
            {
                mError = GMError.GME_DownLoadBaseModuleFailed;
                LoggerManager.Instance().LogFormat("加载[{0}] Bundle 失败 !!!", mBundleName);
                yield break;
            }

            //加载模块表
            TableManager.Instance().LoadTableFromAssetBundle<ProtoTable.ModuleTable>(baseBundle);
            var moduleTable = TableManager.Instance().GetTable<ModuleTable>();
            if (null == moduleTable)
            {
                mError = GMError.GME_DownLoadBaseModuleFailed;
                LoggerManager.Instance().LogErrorFormat("加载模块表 ModuleTable 失败 !!!");
                yield break;
            }

            //加载
            int baseModuleId = 1;
            var moduleItem = TableManager.Instance().GetTableItem<ModuleTable>(baseModuleId);
            if (null == moduleTable)
            {
                mError = GMError.GME_DownLoadBaseModuleFailed;
                LoggerManager.Instance().LogErrorFormat("加载模块表项 ModuleTable 失败 ID={0}!!!", baseModuleId);
                yield break;
            }

            List<string> needDownLoadBundles = new List<string>(32);
            AssetLoaderManager.Instance().GetNeedDownLoadModule(moduleItem.RequiredBundles.ToArray(), needDownLoadBundles);
            AssetLoaderManager.Instance().GetNeedDownLoadModule(new string[]
            {
                    CommonFunction.getPlatformString()
            }, needDownLoadBundles);

            AssetBundleManager.Instance().UnLoadAssetBundle(mBundleName);

            if (needDownLoadBundles.Count > 0)
            {
                for (int i = 0; i < needDownLoadBundles.Count; ++i)
                {
                    LoggerManager.Instance().LogProcessFormat("Bundle [{0}] 需要下载 ...", needDownLoadBundles[i]);
                }

                var bundleUrl = string.Format("{0}{1}/{2}/", mBaseConfigUrl, CommonFunction.getPlatformString(),
                                              AssetLoaderManager.Instance().RemoteVersion);

                yield return AssetBundleManager.Instance().DownLoadAssetBundles(bundleUrl, needDownLoadBundles, null, () =>
                {
                    succeed = false;
                    LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundles Failed ...");
                });

                if (!succeed)
                {
                    mError = GMError.GME_DownLoadBaseModuleFailed;
                    yield break;
                }
                LoggerManager.Instance().LogFormat("DownLoadBaseModule Succeed !");
            }
            else
            {
                LoggerManager.Instance().LogProcessFormat("No Bundle Need Download ...");
            }
        }

        IEnumerator StartCheckVersion()
        {
            //加载版本信息
            yield return LoadVersionInfo();

            if(ReportError())
            {
                yield break;
            }
            LoggerManager.Instance().LogProcessFormat("LoadVersionInfo Succeed !");

            //检测大版本更新
            if (AssetLoaderManager.Instance().HasLargeUpdate)
            {
                LoggerManager.Instance().LogProcessFormat("NeedReDownLoadFrom AppleStore , Has Large Update !");
                yield break;
            }

            //加载文件MD5
            yield return LoadFileMD5Info();
            if(ReportError())
            {
                yield break;
            }
            LoggerManager.Instance().LogProcessFormat("Load VersionMd5File Succeed !");

            //检测小版本更新
            if (AssetLoaderManager.Instance().HasSmallUpdate)
            {
                //下载差异包
                LoggerManager.Instance().LogProcessFormat("DownLoad Package From Server!");
                yield return DownLoadBaseModule();
                if (ReportError())
                {
                    yield break;
                }
            }
            else
            {
                LoggerManager.Instance().LogProcessFormat("Need Not DownLoad Package From Server!");
            }

            //加载游戏基础模块
            yield return LoadGameBaseModule();
            if(ReportError())
            {
                yield break;
            }
            LoggerManager.Instance().LogProcessFormat("LoadGameBaseModule Succeed !");
        }

        IEnumerator LoadGameBaseModule()
        {
            bool succeed = true;

            if (AssetLoaderManager.Instance().HasSmallUpdate)
            {
                yield return AssetBundleManager.Instance().LoadAssetBundleFromPkg(mBundleName, null,
                () =>
                {
                    succeed = false;
                });
                if (!succeed)
                {
                    mError = GMError.GME_LoadBaseModuleFailed;
                    LoggerManager.Instance().LogErrorFormat("Load BaseBundle {0} From Pkg Failed ...", mBundleName);
                    yield break;
                }
            }
            else
            {
                yield return AssetBundleManager.Instance().LoadAssetBundle(mBundleName, null,
                () =>
                {
                    succeed = false;
                },true);


                if (!succeed)
                {
                    mError = GMError.GME_LoadBaseModuleFailed;
                    LoggerManager.Instance().LogErrorFormat("Load BaseBundle {0} From StreamingAssets Failed ...", mBundleName);
                    yield break;
                }
            }

            TableManager.Instance().LoadTableFromAssetBundle<ProtoTable.ModuleTable>(AssetBundleManager.Instance().getAssetBundle(mBundleName));
            var moduleTable = TableManager.Instance().GetTable<ProtoTable.ModuleTable>();
            if(null == moduleTable)
            {
                mError = GMError.GME_LoadBaseModuleFailed;
                LoggerManager.Instance().LogErrorFormat("load ModuleTable Failed ...");
                yield break;
            }
            LoggerManager.Instance().LogProcessFormat("Load ModuleTable Succeed !");

            yield return AssetBundleManager.Instance().LoadAssetBundleFromModule(1,null,()=>
            {
                succeed = false;
            });

            if(!succeed)
            {
                mError = GMError.GME_LoadBaseModuleFailed;
                LoggerManager.Instance().LogErrorFormat("load baseModule Failed ...");
                yield break;
            }

            TableManager.Instance().LoadTableFromAssetBundle<ProtoTable.ResourceInfoTable>(AssetBundleManager.Instance().getAssetBundle(mBundleName));
            var localResourcesInfoTable = TableManager.Instance().GetTable<ProtoTable.ResourceInfoTable>();
            if(null == localResourcesInfoTable)
            {
                mError = GMError.GME_LoadBaseModuleFailed;
                LoggerManager.Instance().LogErrorFormat("加载游戏资源表失败...");
                yield break;
            }

            TableManager.Instance().LoadTableFromAssetBundle<ProtoTable.FrameTypeTable>(AssetBundleManager.Instance().getAssetBundle(mBundleName));
            var frameTypeTable = TableManager.Instance().GetTable<ProtoTable.FrameTypeTable>();
            if (null == frameTypeTable)
            {
                mError = GMError.GME_LoadBaseModuleFailed;
                LoggerManager.Instance().LogErrorFormat("加载界面表失败...");
                yield break;
            }

            var resourceInfoTable = AssetLoaderManager.Instance().Initialize(new AssetLoaderData
            {
                frameHandle = this,
                localResourcesInfoTable = localResourcesInfoTable,
            });
        }
    }
}