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
            AssetBundleManager.Instance().Initialize(this,mBaseConfigUrl + CommonFunction.getPlatformString() + "/");

            GameObject.DontDestroyOnLoad(this);

            StartCoroutine(StartCheckVersion());
        }

        IEnumerator StartCheckVersion()
        {
            if(!AssetLoaderManager.Instance().SetLocalVersion(Application.version))
            {
                LoggerManager.Instance().LogErrorFormat("SetLocalVersion Failed V = {0}", Application.version);
                yield break;
            }
            Debug.LogFormat("SetLocalVersion Succeed ... [{0}]", Application.version);

            var platFormUrl = mBaseConfigUrl + CommonFunction.getPlatformString();

            var versionurl = platFormUrl + "/Version.txt";
            var md5url = platFormUrl + "/VersionMd5File.txt";

            yield return AssetLoaderManager.Instance().LoadRemoteVersion(versionurl);
            if(!AssetLoaderManager.Instance().IsVersionOK)
            {
                LoggerManager.Instance().LogErrorFormat("SetRemoteVersion Failed ...");
                yield break;
            }

            Debug.LogFormat("SetRemoteVersion Succeed ... [{0}]",AssetLoaderManager.Instance().RemoteVersion);

            if(AssetLoaderManager.Instance().HasLargeUpdate)
            {
                LoggerManager.Instance().LogProcessFormat("需要进行大版本更新，请下载最新版本 !!!");
                yield break;
            }

            if(true)
            {
                bool succeed = true;

                TextAsset versionText = Resources.Load<TextAsset>(@"Data/MD5/VersionMd5File");
                if (null == versionText)
                {
                    LoggerManager.Instance().LogErrorFormat("加载本地 VersionMd5File 失败 !!!", mBundleName);
                    yield break;
                }

                succeed = AssetLoaderManager.Instance().SetLocalVersionMD5(versionText.text);
                if (!succeed)
                {
                    LoggerManager.Instance().LogErrorFormat("解析本地 VersionMd5File 失败 !!!");
                    yield break;
                }

                LoggerManager.Instance().LogFormat("加载本地 VersionMd5File 成功!!!");
            }

            yield return AssetLoaderManager.Instance().LoadRemoteVersionMD5Files(md5url);
            if (!AssetLoaderManager.Instance().IsMD5FileLoadSucceed)
            {
                LoggerManager.Instance().LogErrorFormat("加载远程 VersionMd5File 失败 !!!");
                yield break;
            }
            LoggerManager.Instance().LogFormat("加载远程 VersionMd5File 成功!!!");

            if (AssetLoaderManager.Instance().HasSmallUpdate)
            {
                bool succeed = true;

                var baseBundleUrl = string.Format("{0}{1}/", mBaseConfigUrl, CommonFunction.getPlatformString());
                yield return AssetBundleManager.Instance().DownLoadAssetBundles(baseBundleUrl, new List<string>() { mBundleName }, null, () =>
                {
                    succeed = false;
                });

                if (!succeed)
                {
                    LoggerManager.Instance().LogErrorFormat("从远程加载[{0}] Bundle 失败 !!!", mBundleName);
                    yield break;
                }

                yield return AssetBundleManager.Instance().LoadAssetBundle(mBundleName, null, () =>
                  {
                      succeed = false;
                  }, false);

                if (!succeed)
                {
                    LoggerManager.Instance().LogErrorFormat("从本地加载 刚下载的[{0}] Bundle 失败 !!!", mBundleName);
                    yield break;
                }
                LoggerManager.Instance().LogFormat("加载刚下载的[{0}] Bundle 成功 !!!", mBundleName);

                var baseBundle = AssetBundleManager.Instance().getAssetBundle(mBundleName);
                if(null == baseBundle)
                {
                    LoggerManager.Instance().LogFormat("加载[{0}] Bundle 失败 !!!", mBundleName);
                    yield break;
                }

                //加载模块表
                TableManager.Instance().LoadTableFromAssetBundle<ProtoTable.ModuleTable>(baseBundle);
                var moduleTable = TableManager.Instance().GetTable<ModuleTable>();
                if (null == moduleTable)
                {
                    LoggerManager.Instance().LogErrorFormat("加载模块表 ModuleTable 失败 !!!");
                    yield break;
                }
                LoggerManager.Instance().LogFormat("加载模块表 ModuleTable 成功!!!");

                //加载
                int baseModuleId = 1;
                var moduleItem = TableManager.Instance().GetTableItem<ModuleTable>(baseModuleId);
                if(null == moduleTable)
                {
                    LoggerManager.Instance().LogErrorFormat("加载模块表项 ModuleTable 失败 ID={0}!!!", baseModuleId);
                    yield break;
                }
                List<string> needDownLoadBundles = new List<string>(32);
                AssetLoaderManager.Instance().GetNeedDownLoadModule(moduleItem.RequiredBundles.ToArray(),needDownLoadBundles);
                AssetLoaderManager.Instance().GetNeedDownLoadModule(new string[] 
                {
                    CommonFunction.getPlatformString()
                }, needDownLoadBundles);

                AssetBundleManager.Instance().UnLoadAssetBundle(mBundleName);

                if (needDownLoadBundles.Count > 0)
                {
                    for (int i = 0; i < needDownLoadBundles.Count; ++i)
                    {
                        Debug.LogFormat("Bundle [{0}] 需要下载 ...", needDownLoadBundles[i]);
                    }

                    var bundleUrl = string.Format("{0}{1}/", mBaseConfigUrl, CommonFunction.getPlatformString());
                    yield return AssetBundleManager.Instance().DownLoadAssetBundles(bundleUrl, needDownLoadBundles, null, () =>
                    {
                        succeed = false;
                        LoggerManager.Instance().LogErrorFormat("DownLoadAssetBundles Failed ...");
                    });

                    if(!succeed)
                    {
                        yield break;
                    }

                    LoggerManager.Instance().LogFormat("更新基础模块AssetBundles Succeed !!!");
                }

                LoggerManager.Instance().LogFormat("文件校验成功!!!");
            }

            yield return LoadGameBaseModule();
        }

        IEnumerator LoadGameBaseModule()
        {
            bool succeed = true;
            yield return AssetBundleManager.Instance().LoadAssetBundleFromPkg(mBundleName,null, 
            ()=>
            {
                succeed = false;
            });
            if(!succeed)
            {
                LoggerManager.Instance().LogErrorFormat("load BaseBundle {0} Failed ...", mBundleName);
                yield break;
            }

            TableManager.Instance().LoadTableFromAssetBundle<ProtoTable.ModuleTable>(AssetBundleManager.Instance().getAssetBundle(mBundleName));
            var moduleTable = TableManager.Instance().GetTable<ProtoTable.ModuleTable>();
            if(null == moduleTable)
            {
                LoggerManager.Instance().LogErrorFormat("load ModuleTable Failed ...");
                yield break;
            }

            yield return AssetBundleManager.Instance().LoadAssetBundleFromModule(1,null,()=>
            {
                succeed = false;
            });

            if(!succeed)
            {
                LoggerManager.Instance().LogErrorFormat("load baseModule Failed ...");
                yield break;
            }

            TableManager.Instance().LoadTableFromAssetBundle<ProtoTable.ResourceInfoTable>(AssetBundleManager.Instance().getAssetBundle(mBundleName));
            var localResourcesInfoTable = TableManager.Instance().GetTable<ProtoTable.ResourceInfoTable>();
            if(null == localResourcesInfoTable)
            {
                LoggerManager.Instance().LogErrorFormat("加载游戏资源表失败...");
                yield break;
            }
            var resourceInfoTable = AssetLoaderManager.Instance().Initialize(new AssetLoaderData
            {
                frameHandle = this,
                localResourcesInfoTable = localResourcesInfoTable,
            });
            LoggerManager.Instance().LogFormat("加载游戏基础模块成功!!!");
        }
    }
}