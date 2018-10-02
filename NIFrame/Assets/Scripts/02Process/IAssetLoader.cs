using System.Collections;
using System.Collections.Generic;

namespace GG
{
    public enum AssetType
    {
        AT_INVALID = -1,
        AT_PREFAB,
        AT_IMAGE,
        AT_ASSET,
        AT_SPRITE,
        AT_COUNT,
    }

    public interface IAssetLoader
    {
        UnityEngine.Object LoadRes(string path, System.Type type, AssetType eAssetType, string subRes);
        UnityEngine.Object LoadRes(string path, System.Type type, AssetType eAssetType, bool bForceLoadFromNative = false);
    }
}