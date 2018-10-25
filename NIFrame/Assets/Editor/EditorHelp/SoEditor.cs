using UnityEngine;
using System.IO;
using System.Collections;
using UnityEditor;

namespace NI
{
    public class Scriptablity : MonoBehaviour
    {
        public static T Create<T>(string _path, string _name) where T : ScriptableObject
        {
            if(!Directory.Exists(_path))
            {
                Debug.LogError("can't create asset, path not found");
                return null;
            }

            if (string.IsNullOrEmpty(_name))
            {
                Debug.LogError("can't create asset, the name is empty");
                return null;
            }
            string assetPath = Path.Combine(_path, _name + ".asset");

            if(File.Exists(assetPath))
            {
                File.Delete(assetPath);
            }

            T newT = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(newT, assetPath);
            Selection.activeObject = newT;

            return newT;
        }

        public static void Create<T>() where T : ScriptableObject
        {

            string assetName = "New " + typeof(T).Name;
            string assetPath = "Assets";
            if (Selection.activeObject)
            {
                assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (Path.GetExtension(assetPath) != "")
                {
                    assetPath = Path.GetDirectoryName(assetPath);
                }
            }

            bool doCreate = true;
            string path = Path.Combine(assetPath, assetName + ".asset");
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                doCreate = EditorUtility.DisplayDialog(assetName + " already exists.",
                                                        "Do you want to overwrite the old one?",
                                                        "Yes", "No");
            }
            if (doCreate)
            {
                T T_info = Create<T>(assetPath, assetName);
                Selection.activeObject = T_info;
            }
        }

        public static void Create()
        {
            Debug.LogError("You should call 'Create' method like this : Create<ExampleData>();");
        }
    }
}