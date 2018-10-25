using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FileMd5Pair
{
    public string key;
    public string value;
}

[System.Serializable]
public class BundleDependency
{
    public string key;
    public string[] depends = new string[0];
}

[CreateAssetMenu]
public class HotFixData : ScriptableObject
{
    public string version;
    public byte[] datas = new byte[4];
    public int largeV;
    public int smallV;
    public FileMd5Pair[] bundleName2Md5 = new FileMd5Pair[0];
    public BundleDependency[] depends = new BundleDependency[0];
}