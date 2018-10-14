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
                    var targetPath = System.IO.Path.GetFullPath(dstBundlePath + moduleItem.RequiredBundles[i]);
                    targetPath = Path.GetDirectoryName(targetPath);
                    if(!Directory.Exists(targetPath))
                    {
                        Directory.CreateDirectory(targetPath);
                    }
                    System.IO.File.Copy(srcBundlePath + moduleItem.RequiredBundles[i], dstBundlePath + moduleItem.RequiredBundles[i]);
                    System.IO.File.Copy(srcBundlePath + moduleItem.RequiredBundles[i] + ".manifest", dstBundlePath + moduleItem.RequiredBundles[i] + ".manifest");
                    Debug.LogFormat("<color=#00ff00>copy {0} succeed ...</color>", moduleItem.RequiredBundles[i]);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("复制AssetBundles To StreamingAssets 文件夹失败...");
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
}