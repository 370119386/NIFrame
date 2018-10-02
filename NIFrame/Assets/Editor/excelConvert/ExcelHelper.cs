using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace npoi
{
    class ExcelHelper
    {
		//[MenuItem("NI/TableConvert/Xlsx2Proto")]
        public static void ConvertProtoFile()
        {
			_ConvertAll(ConvertType.CT_PROTO);
        }

		//[MenuItem("NI/TableConvert/Xlsx2Txt")]
		public static void ConvertText()
		{
			_ConvertAll(ConvertType.CT_TXT);
		}

		[MenuItem("NI/TableConvert/Txt2Asset")]
		public static void ConvertAsset()
		{
            var dir = Path.GetFullPath(Application.dataPath + ExcelConfig.TXT_SAVE_PATH);
            ConvertAsset(dir, "*.txt", ExcelConfig.TXT_SAVE_PATH);
		}

        public static bool ConvertAsset(string dir,string filter,string save_dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string[] strList = Directory.GetFiles(dir, filter, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < strList.Length; ++i)
            {
                var txtPath = strList[i];
                var name = Path.GetFileNameWithoutExtension(txtPath);
                try
                {
                    var assetPath = save_dir + name + ".asset";
                    AssetBinary asset = ScriptableSingleton<AssetBinary>.CreateInstance<AssetBinary>();
                    asset.m_DataBytes = File.ReadAllBytes(txtPath);

                    if (File.Exists(assetPath))
                    {
                        AssetBinary oldAsset = AssetDatabase.LoadAssetAtPath<AssetBinary>(assetPath);
                        oldAsset.m_DataBytes = asset.m_DataBytes;
                        EditorUtility.SetDirty(oldAsset);
                        AssetDatabase.SaveAssets();
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(asset, assetPath);
                    }
                    Debug.LogFormat("<color=#00ff00>convert <color=#ffff00>{0}.asset</color> succeed !</color>", name);
                }
                catch (System.Exception e)
                {
                    Debug.LogErrorFormat(e.ToString());
                    Debug.LogErrorFormat("<color=#ff0000>convert <color=#ffff00>{0}.asset</color> failed !</color>", name);
                    return false;
                }
            }
            return true;
        }

        [MenuItem("NI/PB/PbTxt2Asset")]
        public static void ConvertPbTxt2Asset()
        {
            ConvertDataPathTxt(ExcelConfig.PB_TXT_SRC_PATH, @"*.pb", ExcelConfig.PB_ASSET_DST_PATH);
        }
        
        public static bool ConvertDataPathTxt(string path, string filter,string save_dir)
        {
            var dir = Path.GetFullPath(Application.dataPath + path);
            return ConvertAsset(dir, filter,save_dir);
        }

		public static bool ConvertAsset(string filter)
		{
			var dir = Path.GetFullPath (Application.dataPath + ExcelConfig.TXT_SAVE_PATH);
            bool succeed = ConvertAsset(dir,filter, ExcelConfig.TXT_ASSET_PATH);
            return succeed;
        }

		[MenuItem("NI/TableConvert/LoadTableList")]
		public static void ConvertTableListScriptCS()
		{
			var name = "TableList.cs";
			var save_dir = Path.GetFullPath (Application.dataPath + ExcelConfig.TABLE_LIST_CS_PATH + name);
			var dir = Path.GetFullPath (Application.dataPath + ExcelConfig.PROTO_PATH);
			string[] strList = Directory.GetFiles (dir, "*.proto", SearchOption.TopDirectoryOnly);
			if (true)
			{
				var removeList = new List<string> ();
				removeList.Add ("Union");
				var datas = strList.ToList ();
				datas.RemoveAll (x => {
					var filename = Path.GetFileNameWithoutExtension(x);
					return removeList.Contains(filename);
				});
				strList = datas.ToArray();
			}
			StringBuilder kBuilder = new StringBuilder (2048);
			int autoTab = 0;
			kBuilder.Append("using System;\n");
			kBuilder.Append("using ProtoTable;\n");
			kBuilder.Append("\n");

			kBuilder.AppendFormat ("namespace GameClient\n");
			kBuilder.AppendFormat ("{0}{{\n", Tabs (autoTab));
			++autoTab;
			kBuilder.AppendFormat ("{0}class TableList\n", Tabs (autoTab));
			kBuilder.AppendFormat ("{0}{{\n", Tabs (autoTab));
			++autoTab;
			kBuilder.AppendFormat ("{0}protected static Type[] ms_table_types = new Type[{1}]\n",Tabs(autoTab),strList.Length);
			kBuilder.AppendFormat ("{0}{{\n", Tabs (autoTab));
			++autoTab;
			for (int i = 0; i < strList.Length; ++i) 
			{
				var fileName = Path.GetFileNameWithoutExtension(strList [i]);
				kBuilder.AppendFormat("{0}typeof({1}),\n",Tabs(autoTab),fileName);
			}
			--autoTab;
			kBuilder.AppendFormat ("{0}}};\n", Tabs (autoTab));

			kBuilder.AppendFormat ("{0}public static Type[] Values\n", Tabs (autoTab));
			kBuilder.AppendFormat ("{0}{{\n", Tabs (autoTab));
			++autoTab;
			kBuilder.AppendFormat ("{0}get {{ return ms_table_types; }}\n",Tabs(autoTab));
			--autoTab;
			kBuilder.AppendFormat ("{0}}}\n", Tabs (autoTab));

			--autoTab;
			kBuilder.AppendFormat ("{0}}};\n", Tabs (autoTab));
			--autoTab;
			kBuilder.AppendFormat ("{0}}};\n", Tabs (autoTab));
			var content = kBuilder.ToString ();
			File.WriteAllText(save_dir, content, Encoding.ASCII);
		}

		public static string Tabs(int n)
		{
			string ret = string.Empty;
			for (int i = 0; i < n; ++i) {
				ret += '\t';
			}
			return ret;
		}

		//[MenuItem("TableConvert/CheckTheSame")]
		public static void Check()
		{
			Regex regexNonBlank = new Regex (@"([^\s]+)");
			var dir = Path.GetFullPath (Application.dataPath + ExcelConfig.PROTO_PATH);
			string[] strList = Directory.GetFiles (dir, "*.proto", SearchOption.TopDirectoryOnly);
			for (int i = 0; i < strList.Length; ++i)
			{
				var name = Path.GetFileName (strList [i]);
				var cmp_path = Path.GetFullPath (Application.dataPath + ExcelConfig.CMP_PROTO_PATH + name);
				if (!File.Exists (strList [i]) || !File.Exists(cmp_path)) 
				{
					continue;
				}
				string content0 = File.ReadAllText (strList[i]);
				string content1 = File.ReadAllText (cmp_path);
				StringBuilder kBuilder = new StringBuilder (2048);
				StringBuilder kBuilder1 = new StringBuilder (2048);

				foreach (Match match in regexNonBlank.Matches(content0)) {
					kBuilder.Append (content0.Substring (match.Index, match.Length));
				}
				foreach (Match match in regexNonBlank.Matches(content1)) {
					kBuilder1.Append (content1.Substring (match.Index, match.Length));
				}

				var cur = kBuilder.ToString ();
				var cmp = kBuilder1.ToString ();
				if(cur == cmp)
				{
					Debug.LogFormat ("check <color=#00ff00>[{0}]</color> succeed !!!", name);
				}
				else 
				{
					Debug.LogErrorFormat ("check [{0}] failed !!!", name);
					for(int j = 0 ; j < cur.Length && j < cmp.Length; ++j)
					{
						if(cur[j] != cmp[j])
						{
							Debug.LogErrorFormat ("<color=#ffff00>{0}</color>", name);
							Debug.LogErrorFormat ("{0}", cur.Substring (j, cur.Length - j));
							Debug.LogErrorFormat("{0}",cmp.Substring(j,cmp.Length - j));
							break;
						}
					}
				}
			}
		}

		public enum ConvertType
		{
			CT_PROTO = 0,
			CT_TXT,
		}

        class ConvertParams
        {
            public string name;
            public int index;
            public int sum;
            public string applicationPath;
			public ConvertType eConvertType;
        }
			
        public static int ms_num = 0;
        public static bool ms_locked = false;
		static List<ConvertParams> contents = new List<ConvertParams>();
		public static long ms_beginTime = 0;
		static void _ConvertAll(ConvertType eConvertType)
        {
            if (ms_locked)
            {
                return;
            }
            ms_locked = true;

			ms_beginTime = System.DateTime.Now.Ticks;

            var dir = Path.GetFullPath(Application.dataPath + ExcelConfig.XLSX_PATH);
            string[] strList = Directory.GetFiles(dir, "Z-资源配置表.xls", SearchOption.AllDirectories);
            Debug.LogFormat("[npoi] start convert sum = {0}", strList.Length);
            ms_num = 0;
			contents.Clear ();
            for (int i = 0; i < strList.Length; ++i)
            {
                string name = Path.GetFileName(strList[i]);

                ConvertParams param = new ConvertParams
                {
                    name = name,
                    index = i,
                    sum = strList.Length,
                    applicationPath = Application.dataPath,
					eConvertType = eConvertType,
                };

				contents.Add (param);
            }

			int iThreadMax = 1;
			if (iThreadMax == 0) 
			{
				_ConvertTable (contents);
			}
			else 
			{
				int iMaxThread = iThreadMax <= contents.Count ? iThreadMax : contents.Count;
				Thread[] threads = new Thread[iMaxThread];
				for (int i = 0; i < threads.Length; ++i) 
				{
					threads[i] = new Thread(_ConvertTable);
					threads[i].Priority = System.Threading.ThreadPriority.Highest;
					threads[i].Start(contents);
				}
			}
        }

		static void _OnConvertDone()
		{
			var deltaTime = System.DateTime.Now.Ticks - ms_beginTime;
			Debug.LogFormat ("<color=#00ff00>[done][total][time={0}ms]</color>", deltaTime / 10000);
			ms_locked = false;
		}

        public static void _ConvertTable(object param)
        {
			while (true) 
			{
				ConvertParams curParam = null;
				int iNum = 0;
				lock (param) 
				{
					var contents = param as List<ConvertParams>;
					if (null != contents && contents.Count > 0) 
					{
						curParam = contents [0];
						contents.RemoveAt (0);
					}
				}

				if (null == curParam)
				{
					break;
				}

				if (ExcelManager.Instance().Convert(curParam.applicationPath,curParam.name,curParam.eConvertType))
				{
					lock (param)
					{
						iNum = ++ms_num;
					}
					Debug.LogFormat("[npoi] convert [{0}/{1}] <color=#00ff00>{2}</color> ok !!!", iNum, curParam.sum, curParam.name);
				}
				else
				{
					lock (param) 
					{
						iNum = ++ms_num;
					}
					Debug.LogErrorFormat("[npoi] convert [{0}/{1}] <color=#ffff00>{2}</color> failed !!!", iNum, curParam.sum, curParam.name);
				}

				if (iNum == curParam.sum) 
				{
					_OnConvertDone ();
				}
			}
        }
    }
}