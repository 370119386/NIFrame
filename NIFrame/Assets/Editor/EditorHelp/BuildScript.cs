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

    [MenuItem("AssetBundles/Build AssetBundles")]
    static public void Menu_BuildAssetBundles()
    {
        BuildAssetBundles();
    }

    [MenuItem("AssetBundles/DeleteLocalAssetBundles")]
    static public void DeleteLocalAssetBundles()
    {
        var Dir = CommonFunction.getAssetBundleSavePath(string.Empty, false);
        if(Directory.Exists(Dir))
        {
            DelectDir(Dir);
        }
    }

    public static void DelectDir(string srcPath)
    {
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