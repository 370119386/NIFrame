using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Diagnostics;

namespace npoi
{
	class ExcelConvertEditorWindow : EditorWindow 
	{
		[MenuItem ("NI/TableConvert/Window")]
		static void AddWindow ()
		{       
			//创建窗口
			Rect  wr = new Rect (0,0,316,420);
			ExcelConvertEditorWindow window = (ExcelConvertEditorWindow)EditorWindow.GetWindowWithRect (typeof (ExcelConvertEditorWindow),wr,true,"客户端-转表");	
			window.Show();
		}

		[Serializable]
		public class ConvertInfo
		{
			public bool bSelected = false;
			public string name = string.Empty;
		}

		protected string filter = string.Empty;
		protected List<ConvertInfo> fileNames = new List<ConvertInfo>();

		Vector2 _scrollPos = Vector2.zero;

		protected void OnEnable()
		{
			
		}

		void _ListAllExcelFiles()
		{
			fileNames.Clear ();
			var dir = Path.GetFullPath (Application.dataPath + ExcelConfig.XLSX_PATH);
			string[] strList = Directory.GetFiles (dir, "*.xls", SearchOption.TopDirectoryOnly);
			for (int i = 0; i < strList.Length; ++i) 
			{
				var name = Path.GetFileNameWithoutExtension (strList[i]);
				fileNames.Add (new ConvertInfo
					{
						bSelected = false,
						name = name,
					});
			}
		}

		protected void _LoadShellCmd(string shell,string argv)
		{
			Process process = new Process();
			process.StartInfo.FileName = shell;
			process.StartInfo.Arguments = argv;
			process.StartInfo.CreateNoWindow = false;
			process.StartInfo.ErrorDialog = true;
			process.StartInfo.UseShellExecute = false;

			//重新定向标准输入，输入，错误输出
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.Start ();

			string strRst = process.StandardOutput.ReadToEnd();
			if(!string.IsNullOrEmpty(strRst))
			{
				UnityEngine.Debug.LogFormat ("<color=#00ff00>{0}</color>",strRst);
			}
			strRst = process.StandardError.ReadToEnd();
			if (!string.IsNullOrEmpty (strRst)) 
			{
				UnityEngine.Debug.LogFormat ("<color=#ff0000>{0}</color>",strRst);
			}
            process.WaitForExit();
            process.Close();
        }

        protected void OnGUI()
		{
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("过滤器",GUILayout.Width(100));
			filter = EditorGUILayout.TextField (filter,GUILayout.Width(100));
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			if(GUILayout.Button("刷新",GUILayout.Width(100)))
			{
				_ListAllExcelFiles ();
			}
			if (GUILayout.Button ("全选", GUILayout.Width (100))) 
			{
				for (int i = 0; i < fileNames.Count; ++i) 
				{
					fileNames [i].bSelected = true;
				}
			}
			if (GUILayout.Button ("反选", GUILayout.Width (100))) 
			{
				for (int i = 0; i < fileNames.Count; ++i) 
				{
					fileNames [i].bSelected = !fileNames[i].bSelected;
				}
			}
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("转表-PROTO", GUILayout.Width (100))) 
			{
				for (int i = 0; i < fileNames.Count; ++i) 
				{
					if (fileNames [i].bSelected) 
					{
						if (ExcelManager.Instance ().Convert (Application.dataPath, fileNames [i].name, ExcelHelper.ConvertType.CT_PROTO))
						{
							UnityEngine.Debug.LogFormat ("<color=#00ff00>convert <color=#ffff00>{0}.proto</color> succeed !!</color>", fileNames [i].name);
						} 
						else 
						{
							UnityEngine.Debug.LogFormat ("<color=#ff0000>convert <color=#ffff00>{0}.proto</color> succeed !!</color>", fileNames [i].name);
						}
					}
				}
			}
			if (GUILayout.Button ("转表-CS", GUILayout.Width (100))) 
			{
				for (int i = 0; i < fileNames.Count; ++i) 
				{
					if (fileNames [i].bSelected) 
					{
						var dir = Path.GetFullPath (Application.dataPath + ExcelConfig.XLSX_PATH + fileNames [i].name + ".xls");
						var excelUnit = new ExcelUnit (dir);
						excelUnit.Init ();
						if (excelUnit.succeed) 
						{
							string proto_path = Path.GetFullPath(Application.dataPath + ExcelConfig.PROTO_PATH + excelUnit.SheetName + ".proto");
							string out_path = Path.GetFullPath(Application.dataPath + ExcelConfig.TABLE_SCRIPTS_PATH + excelUnit.SheetName + ".cs");
							string ccode_path = Path.GetFullPath(Application.dataPath + ExcelConfig.TABLE_SCRIPTS_CCODE_PATH + excelUnit.SheetName + ".cc");

							ProtoBuf.CodeGenerator.CommandLineOptions.BuildXsxl2Cs(proto_path, out_path, ccode_path);
						}
						else 
						{
							UnityEngine.Debug.LogErrorFormat ("表{0}转cs文件失败!", fileNames [i]);
						}
						excelUnit.Close ();
					}
				}
			}
			if (GUILayout.Button ("转表-TXT", GUILayout.Width (100))) 
			{
				for (int i = 0; i < fileNames.Count; ++i) 
				{
					if (fileNames [i].bSelected) 
					{
						var dir = Path.GetFullPath (Application.dataPath + ExcelConfig.XLSX_PATH + fileNames [i].name + ".xls");
						var excelUnit = new ExcelUnit (dir);
						excelUnit.Init ();
						excelUnit.LoadProtoBase ();
						excelUnit.generateText (Application.dataPath);
						if (excelUnit.succeed) 
						{
							UnityEngine.Debug.LogFormat ("<color=#00ff00>convert <color=#ffff00>{0}.txt</color> succeed !!</color>",excelUnit.SheetName);
							ExcelHelper.ConvertAsset (excelUnit.SheetName + ".txt");
						} 
						else
						{
							UnityEngine.Debug.LogFormat ("<color=#ff0000>convert <color=#ffff00>{0}.txt</color> succeed !!</color>",excelUnit.SheetName);
						}
                        var txtPath = Path.GetFullPath(Application.dataPath + ExcelConfig.TXT_SAVE_PATH + excelUnit.SheetName + ".txt");
                        if (File.Exists(txtPath))
                        {
                            File.Delete(txtPath);
                        }
                        excelUnit.Close();
                    }
				}
			}
			EditorGUILayout.EndHorizontal ();

			//excel-table-name
			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
			EditorGUILayout.BeginVertical ();
			var defColor = GUI.color;
			for (int i = 0; i < fileNames.Count; ++i) 
			{
				if (fileNames [i].name.StartsWith(filter)) 
				{
					EditorGUILayout.BeginHorizontal ();
					if (i % 2 == 0) 
					{
						GUI.color = Color.yellow;
					}
					else 
					{
						GUI.color = Color.cyan;
					}
					EditorGUILayout.LabelField (fileNames [i].name);
					fileNames [i].bSelected = EditorGUILayout.Toggle (fileNames [i].bSelected);
					EditorGUILayout.EndHorizontal ();
				}
			}
			GUI.color = defColor;
			EditorGUILayout.EndVertical ();
			EditorGUILayout.EndScrollView ();
		}
	}
}