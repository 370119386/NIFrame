using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BuildScript
{
	const string kAssetBundlesOutputPath = "AssetBundles";

    public static string GetPlatformFolderForAssetBundles(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "Windows";
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
            case BuildTarget.StandaloneOSXUniversal:
                return "OSX";
            // Add more build targets for your own.
            // If you add more targets, don't forget to add the same platforms to GetPlatformFolderForAssetBundles(RuntimePlatform) function.
            default:
                return null;
        }
    }

    public static void BuildAssetBundles(string assetBundleOutPutPath = "AssetBundles")
	{
		// Choose the output path according to the build target.
		string outputPath = Path.Combine(assetBundleOutPutPath, GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) );
		if (!Directory.Exists(outputPath) )
			Directory.CreateDirectory (outputPath);

		BuildPipeline.BuildAssetBundles (outputPath, 0, EditorUserBuildSettings.activeBuildTarget);
	}

	public static void BuildPlayer()
	{
		var outputPath = EditorUtility.SaveFolderPanel("Choose Location of the Built Game", "", "");
		if (outputPath.Length == 0)
			return;

		string[] levels = GetLevelsFromBuildSettings();
		if (levels.Length == 0)
		{
			Debug.Log("Nothing to build.");
			return;
		}

		string targetName = GetBuildTargetName(EditorUserBuildSettings.activeBuildTarget);
		if (targetName == null)
			return;

		// Build and copy AssetBundles.
		BuildScript.BuildAssetBundles();
		BuildScript.CopyAssetBundlesTo(Path.Combine(Application.streamingAssetsPath, kAssetBundlesOutputPath));
		Debug.Log("Copy asset bundles to " + Path.Combine(Application.streamingAssetsPath, kAssetBundlesOutputPath));

		BuildOptions option = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;
		BuildPipeline.BuildPlayer(levels, outputPath + targetName, EditorUserBuildSettings.activeBuildTarget, option);
	}

	public static string GetBuildTargetName(BuildTarget target)
	{
		switch(target)
		{
		case BuildTarget.Android :
			return "/test.apk";
		case BuildTarget.StandaloneWindows:
		case BuildTarget.StandaloneWindows64:
			return "/test.exe";
		case BuildTarget.StandaloneOSXIntel:
		case BuildTarget.StandaloneOSXIntel64:
		case BuildTarget.StandaloneOSXUniversal:
			return "/test.app";
		case BuildTarget.iOS:
			return "/test.ipa";
			// Add more build targets for your own.
		default:
			Debug.Log(string.Format("Target{0} not implemented.", target.ToString()));
			return null;
		}
	}

	static void CopyAssetBundlesTo(string outputPath)
	{
		// Clear streaming assets folder.
		FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
		Directory.CreateDirectory(outputPath);

		string outputFolder = GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);

		// Setup the source folder for assetbundles.
		var source = Path.Combine(Path.Combine(System.Environment.CurrentDirectory, kAssetBundlesOutputPath), outputFolder);
		if (!System.IO.Directory.Exists(source) )
			Debug.Log("No assetBundle output folder, try to build the assetBundles first.");

		// Setup the destination folder for assetbundles.
		var destination = System.IO.Path.Combine(outputPath, outputFolder);
		if (System.IO.Directory.Exists(destination) )
			FileUtil.DeleteFileOrDirectory(destination);
		
		FileUtil.CopyFileOrDirectory(source, destination);
	}

	static string[] GetLevelsFromBuildSettings()
	{
		List<string> levels = new List<string>();
		for(int i = 0 ; i < EditorBuildSettings.scenes.Length; ++i)
		{
			if (EditorBuildSettings.scenes[i].enabled)
				levels.Add(EditorBuildSettings.scenes[i].path);
		}

		return levels.ToArray();
	}

    //[MenuItem("AssetBundles/Build AssetBundles")]
    static public void Menu_BuildAssetBundles()
    {
        BuildAssetBundles();
    }

    //[MenuItem("AssetBundles/BuildStreamingAssetBundles")]
    static public void BuildStreamingAssetBundles()
    {
        string outputPath = Application.dataPath + "/StreamingAssets/AssetBundles/" + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        BuildPipeline.BuildAssetBundles(outputPath, 0, EditorUserBuildSettings.activeBuildTarget);

        AssetDatabase.Refresh();
    }

    public static List<string> GetAllFiles(string dir, string searchPattern)
    {
        List<string> existFiles = new List<string>(32);
        if (Directory.Exists(dir))
        {
            var files = Directory.GetFiles(dir, searchPattern);
            existFiles.AddRange(files);

            var dirs = Directory.GetDirectories(dir);
            for(int i = 0; i < dirs.Length; ++i)
            {
                existFiles.AddRange(GetAllFiles(dirs[i], searchPattern));
            }
        }
        return existFiles;
    }

    //[MenuItem("AssetBundles/ComputePkgAssetBundlesMD5")]
    static public void ComputePkgAssetBundlesMD5()
    {
        var Dir = Application.dataPath + "/../AssetBundles/" + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) + "/";
        Dir = System.IO.Path.GetFullPath(Dir);
        ComputeAssetBundlesMD5(Dir, Dir);
        CreateUnityVersion();
    }

    static public void CopyFileMD5ToNative(string src,string dst)
    {
        if(File.Exists(dst))
        {
            File.Delete(dst);
        }
        System.IO.File.Copy(src, dst);
        if(File.Exists(dst))
        {
            Debug.LogFormat("<color=#00ff00>CopyFileMD5ToNative Succeed [{0}] </color>",System.IO.Path.GetFileNameWithoutExtension(dst));
        }
    }

    [MenuItem("AssetBundles/BuildPublicAsset")]
    static public void BuildPublicAsset()
    {
        //打包assetBundles
        BuildAssetBundles();
        //计算文件MD5
        var Dir = Application.dataPath + "/../AssetBundles/" + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) + "/";
        Dir = System.IO.Path.GetFullPath(Dir);
        ComputeAssetBundlesMD5(Dir, Dir);
        //生成MD5文件到本地
        CopyFileMD5ToNative(Dir + "VersionMd5File.txt", Application.dataPath + "/Resources/Data/MD5/VersionMd5File.txt");
        //生成UnityVersion 文件
        CreateUnityVersion();
        //生成基础bundles
        CreateStreamingAssetBundles();
    }

    static public void ComputeAssetBundlesMD5(string path,string target)
    {
        var filter = @"*.manifest";
        int endLength = filter.Length - 1;
        var manifests = GetAllFiles(path, filter);
        var builder = StringBuilderCache.Acquire(1024);
        for (int i = 0; i < manifests.Count; ++i)
        {
            var bundleName = manifests[i].Substring(path.Length, manifests[i].Length - path.Length).Replace('\\', '/');
            bundleName = bundleName.Remove(bundleName.Length - endLength, endLength);

            var fileName = System.IO.Path.GetFileNameWithoutExtension(manifests[i]);
            var filePath = System.IO.Path.GetDirectoryName(manifests[i]);

            var md5 = CommonFunction.GetMD5HashFromFile(System.IO.Path.Combine(filePath,fileName));
            if(i != manifests.Count - 1)
            {
                builder.AppendFormat("{0}|{1}\r\n", bundleName, md5);
            }
            else
            {
                builder.AppendFormat("{0}|{1}", bundleName, md5);
            }
            Debug.LogFormat("<color=#00ff00>{0}|{1}</color>", bundleName, md5);
        }
        var version = System.IO.Path.Combine(target, "VersionMd5File.txt");
        System.IO.File.WriteAllText(version, builder.ToString());
        StringBuilderCache.Release(builder);
    }

    static public void CreateUnityVersion()
    {
        try
        {
            System.IO.File.WriteAllText(Application.dataPath + "/../AssetBundles/" + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) + "/Version.txt", Application.version);
            Debug.LogFormat("<color=#00ff00>Version.txt</color>");
        }
        catch(System.Exception e)
        {
            Debug.LogErrorFormat(e.Message);
        }
    }

    [MenuItem("AssetBundles/DelNativeAssetBundles")]
    static public void DeleteLocalAssetBundles()
    {
        var Dir = CommonFunction.getAssetBundleSavePath(string.Empty, false,false);
        DelectDir(Dir);
    }

    [MenuItem("AssetBundles/DeleteStreamingAssetBundles")]
    static public void DeleteStreamingAssetBundles()
    {
        var Dir = CommonFunction.getAssetBundleSavePath(string.Empty, false, true);
        DelectDir(Dir);
        AssetDatabase.Refresh();
    }

    [MenuItem("AssetBundles/CreateStreamingAssetBundles")]
    static public void CreateStreamingAssetBundles()
    {
        var Dir = CommonFunction.getAssetBundleSavePath(string.Empty, false, true);
        DelectDir(Dir);

        var srcPath = Application.dataPath + "/Datas/Table/ModuleTable.asset";
        var dstPath = Application.dataPath + "/Resources/Data/Table/ModuleTable.asset";
        if (File.Exists(dstPath))
        {
            File.Delete(dstPath);
        }
        System.IO.File.Copy(srcPath, dstPath,true);

        var srcBundlePath = Application.dataPath + "/../AssetBundles/" + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) + "/";
        srcBundlePath = System.IO.Path.GetFullPath(srcBundlePath);
        var dstBundlePath = Dir + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) + "/";

        var moduleTable = NI.TableManager.Instance().ReadTableFromResourcesFile<ProtoTable.ModuleTable>(@"Data/Table/");

        int[] modules = new int[] { 1 };
        for(int i = 0; i < modules.Length; ++i)
        {
            if(!moduleTable.ContainsKey(modules[i]))
            {
                Debug.LogErrorFormat("打包模块不存在!!!");
                break;
            }

            var moduleItem = moduleTable[modules[i]] as ProtoTable.ModuleTable;
            if(null == moduleItem)
            {
                Debug.LogErrorFormat("打包模块不存在!!!");
                break;
            }

            try
            {
                for(int j = 0; j < moduleItem.RequiredBundles.Count; ++j)
                {
                    var targetPath = System.IO.Path.GetFullPath(dstBundlePath + moduleItem.RequiredBundles[j]);
                    targetPath = Path.GetDirectoryName(targetPath);
                    if(!Directory.Exists(targetPath))
                    {
                        Directory.CreateDirectory(targetPath);
                    }
                    System.IO.File.Copy(srcBundlePath + moduleItem.RequiredBundles[j], dstBundlePath + moduleItem.RequiredBundles[j]);
                    System.IO.File.Copy(srcBundlePath + moduleItem.RequiredBundles[j] + ".manifest", dstBundlePath + moduleItem.RequiredBundles[j] + ".manifest");
                    Debug.LogFormat("<color=#00ff00>copy {0} succeed ...</color>", moduleItem.RequiredBundles[j]);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("复制AssetBundles To StreamingAssets 文件夹失败...");
                Debug.LogErrorFormat("Error:{0}", e.Message);
                return;
            }
        }

        try
        {
            System.IO.File.Copy(srcBundlePath + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget), dstBundlePath + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget));
            System.IO.File.Copy(srcBundlePath + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) + ".manifest", dstBundlePath + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) + ".manifest");
        }
        catch (System.Exception e)
        {
            Debug.LogErrorFormat("复制Platform AssetBundles To StreamingAssets 文件夹失败...");
            return;
        }

        Debug.LogFormat("<color=#00ff00>CreateStreamingAssetBundles Succeed ...</color>");

        AssetDatabase.Refresh();
    }

    public static void DelectDir(string srcPath)
    {
        if (!Directory.Exists(srcPath))
        {
            return;
        }

        try
        {
            DirectoryInfo dir = new DirectoryInfo(srcPath);
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (FileSystemInfo i in fileinfo)
            {
                if (i is DirectoryInfo)
                {
                    DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                    subdir.Delete(true);
                }
                else
                {
                    File.Delete(i.FullName);
                }
            }
        }
        catch (System.Exception e)
        {
            throw;
        }
    }

    public static bool TokenVersion(string version, ref byte[] data)
    {
        System.Array.Clear(data, 0, data.Length);
        var tokens = version.Split('.');
        if (tokens.Length < 2 || tokens.Length > 4)
        {
            return false;
        }

        for (int i = 0; i < tokens.Length; ++i)
        {
            if (!byte.TryParse(tokens[i], out data[i]))
            {
                return false;
            }
        }
        return true;
    }

    static public void ComputeAssetBundlesMD5(string path, ref FileMd5Pair[] md5Pairs)
    {
        List<FileMd5Pair> pairs = new List<FileMd5Pair>(32);
        var filter = @"*.manifest";
        int endLength = filter.Length - 1;
        var manifests = GetAllFiles(path, filter);
        for (int i = 0; i < manifests.Count; ++i)
        {
            var bundleName = manifests[i].Substring(path.Length, manifests[i].Length - path.Length).Replace('\\', '/');
            bundleName = bundleName.Remove(bundleName.Length - endLength, endLength);

            var fileName = System.IO.Path.GetFileNameWithoutExtension(manifests[i]);
            var filePath = System.IO.Path.GetDirectoryName(manifests[i]);

            var md5 = CommonFunction.GetMD5HashFromFile(System.IO.Path.Combine(filePath, fileName));
            pairs.Add(new FileMd5Pair
            {
                key = bundleName,
                value = md5,
            });
        }
        md5Pairs = pairs.ToArray();

        for (int i = 0; i < md5Pairs.Length; ++i)
        {
            Debug.LogFormat("<color=#00ff00>{0}</color>\t<color=#ff00ffff>{1}</color>", md5Pairs[i].key, md5Pairs[i].value);
        }
    }

    static public void ComputeAssetBundlesDependency(string path, ref BundleDependency[] depends)
    {
        List<BundleDependency> dependsList = new List<BundleDependency>(32);

        var filter = @"*.manifest";
        int endLength = filter.Length - 1;
        var manifests = GetAllFiles(path, filter);
        var builder = StringBuilderCache.Acquire(1024);

        var platform = GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
        var platformBundlePath = CommonFunction.getAssetBundleSavePath(platform + "/" + platform, false, true);

        var assetBundle = AssetBundle.LoadFromFile(platformBundlePath);
        if (null != assetBundle)
        {
            var abManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (null != abManifest)
            {
                for (int i = 0; i < manifests.Count; ++i)
                {
                    var bundleName = manifests[i].Substring(path.Length, manifests[i].Length - path.Length).Replace('\\', '/');
                    bundleName = bundleName.Remove(bundleName.Length - endLength, endLength);

                    if (!string.Equals(bundleName, platform))
                    {
                        dependsList.Add(new BundleDependency
                        {
                            key = bundleName,
                            depends = abManifest.GetAllDependencies(bundleName),
                        });
                    }
                    else
                    {
                        dependsList.Add(new BundleDependency
                        {
                            key = bundleName,
                            depends = new string[0],
                        });
                    }
                }
            }
        }
        assetBundle.Unload(true);

        depends = dependsList.ToArray();
        for (int i = 0; i < depends.Length; ++i)
        {
            if (depends[i].depends.Length == 0)
            {
                Debug.LogFormat("<color=#00ff00>[{0}]</color>", depends[i].key);
            }
            else
            {
                string ret = string.Format("<color=#00ff00>[{0}]</color> <color=#ffff00ff>depends</color> ", depends[i].key);
                for (int j = 0; j < depends[i].depends.Length; ++j)
                {
                    ret += string.Format("<color=#FF00FFFF>[{0}]</color>", depends[i].depends[j]);
                }
                ret += string.Format("<color=#FFFF00FF>[{0}]</color>", depends[i].depends.Length);
                Debug.Log(ret);
            }
        }
    }

    [MenuItem("AssetBundles/MakeHotFixData")]
    public static void MakeHotFixData()
    {
        //创建版本Asset文件
        string storePath = @"Assets/Resources/Data/";
        if (!Directory.Exists(storePath))
        {
            Directory.CreateDirectory(storePath);
        }
        var hotFixData = NI.Scriptablity.Create<HotFixData>(storePath, "HotFixData");
        if (null == hotFixData)
        {
            Debug.LogErrorFormat("MakeHotFixData Failed ...");
            return;
        }

        //解析文件版本
        if (!TokenVersion(Application.version, ref hotFixData.datas))
        {
            return;
        }

        hotFixData.version = string.Format("{0}.{1}.{2}.{3}", hotFixData.datas[0], hotFixData.datas[1], hotFixData.datas[2], hotFixData.datas[3]);

        hotFixData.largeV = (hotFixData.datas[0] << 8) | hotFixData.datas[1];
        hotFixData.smallV = (hotFixData.datas[2] << 8) | hotFixData.datas[3];

        Debug.LogFormat("<color=#00ff00>[Version = {0} largeV = {1} smallV = {2}]</color>", hotFixData.version, hotFixData.largeV, hotFixData.smallV);

        //计算文件MD5
        var Dir = Application.dataPath + "/../AssetBundles/" + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) + "/";
        Dir = System.IO.Path.GetFullPath(Dir);
        ComputeAssetBundlesMD5(Dir, ref hotFixData.bundleName2Md5);

        //计算AB包依赖
        ComputeAssetBundlesDependency(Dir, ref hotFixData.depends);

        AssetDatabase.Refresh();
    }
}