using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

using System.Text.RegularExpressions;
using System.Text;

using System.Reflection;
using System;
using System.Threading;

using UnityEngine.Events;
using UnityEditor;

namespace npoi
{
    enum ErrCode
    {
        EC_SUCCEED = 0,
        EC_NO_FILE,
        EC_NO_SHEET,
        EC_SHEET_NAME_INVALID,
        EC_NO_PROTO_HEAD_ROW,
        EC_PROTO_HEAD_MATCH_FAILED,
        EC_NO_PROTO_VAR_TYPE_ROW,
        EC_PROTO_VAR_TYPE_MATCH_FAILED,
        EC_NO_PROTO_VAR_NAME_ROW,
        EC_PROTO_VAR_NAME_MATCH_FAILED,
        EC_NO_SERVER_CLIENT_FLAG_ROW,
        EC_NO_ANNOTATION_ROW,
        EC_CONVERT_PROTO_FAILED,
        EC_WRITE_PROTO_FAILED,
        EC_ENUM_DECLARE_CONVERT_IS_EMPTY,
		EC_NO_INITIALIZE,
		EC_VAR_TYPE_CONVERT_FAILED,
		EC_VAR_VALUE_CONVERT_FAILED,
		EC_ASSEMBLY_CLASS_CREATE_INSTANCE_FAILED,
		EC_HAS_NO_COLOUM,
		EC_PROTO_NOT_MATCH_CS,
        EC_COUNT,
    }

	enum VarType
	{
		VT_SINT32 = 0,
		VT_FLOAT,
		VT_STRING,
		VT_UNION,
		VT_BOOL,
		VT_ENUM,
		VT_COUNT,
	}

    class ExcelColoumValue
    {
        public int iIndex;
        public string head;
        public string type;
        public string name;
		public string propertyName;
        public bool isValidToSearver = false;
        public string declare;
		public VarType eVarType = VarType.VT_COUNT;
		public bool bUnionFloat = false;
		public List<int> enumValues = null;
		public bool hasEnumValue(int iValue)
		{
			return null != enumValues && enumValues.Contains (iValue);
		}

		public object toValue(string value)
		{
			if (eVarType != VarType.VT_COUNT) 
			{
				if (eVarType == VarType.VT_STRING) 
				{
					return value;
				}

				if (eVarType == VarType.VT_BOOL) 
				{
					value = value.Trim ();
					int iValue = 0;
					if (int.TryParse (value, out iValue)) 
					{
						return (bool)(iValue != 0);
					}
				}

				if (eVarType == VarType.VT_FLOAT) 
				{
					value = value.Trim ();
					int iValue = 0;
					float fValue = 0.0f;
					if (float.TryParse (value, out fValue)) 
					{
						iValue = (int)(fValue * 1000.0f);
						return (int)iValue;
					}
				}

				if (eVarType == VarType.VT_SINT32) 
				{
					value = value.Trim ();
					int iValue = 0;
					if (int.TryParse (value, out iValue)) 
					{
						return (int)iValue;
					}
				}

				if (eVarType == VarType.VT_ENUM) 
				{
					value = value.Trim ();
					int iValue = 0;
					if (int.TryParse (value, out iValue)) 
					{
						if (hasEnumValue (iValue)) 
						{
							return iValue;
						}
					}
				}

				if (eVarType == VarType.VT_UNION) 
				{
					var retValue = _constructUnionCell (value, bUnionFloat);
					return retValue;
				}
			}
			return null;
		}

		private int _convertToInt(string value, bool isFloat = false)
		{
			int iValue = 0;
			try
			{
				iValue = isFloat ? (int)(Convert.ToDouble(value) * 1000) : Convert.ToInt32(value);
			}
			catch(Exception e) 
			{
				Debug.LogErrorFormat ("value = {0} isFloat = {1}", value,isFloat);
				Debug.LogErrorFormat (e.ToString ());
			}
			return iValue;
		}

		private object _constructUnionCell(string content, bool isFloat = false)
		{
			const string FIX_EVERY_SPLIT = ",";
			const string FIX_GROW_SPLIT = ";";
			if (string.IsNullOrEmpty (content) || content.Equals ("-")) {
				content = 0.ToString ();
			}

			ProtoTable.UnionCell unionCell = new ProtoTable.UnionCell();

			if (content.Contains(FIX_EVERY_SPLIT))
			{
				unionCell.valueType = ProtoTable.UnionCellType.union_everyvalue;
				ProtoTable.EveryValue values = new ProtoTable.EveryValue();

				var stringValues = content.Split(FIX_EVERY_SPLIT[0]);
				foreach(var sv in stringValues)
				{
					values.everyValues.Add(_convertToInt(sv, isFloat));
				}

				unionCell.eValues = values;
			}
			else if (content.Contains(FIX_GROW_SPLIT))
			{
				unionCell.valueType = ProtoTable.UnionCellType.union_fixGrow;

				var stringValues = content.Split(FIX_GROW_SPLIT[0]);
				if (stringValues.Length != 2)
				{
					Debug.LogErrorFormat("[GenerateText] union format error {0}", content);
					return null;
				}

				unionCell.fixInitValue = _convertToInt(stringValues[0], isFloat);
				unionCell.fixLevelGrow = _convertToInt(stringValues[1], isFloat);
			}
			else
			{
				unionCell.valueType = ProtoTable.UnionCellType.union_fix;
				unionCell.fixValue = _convertToInt(content, isFloat);
			}

			return (object)(unionCell);
		}

		public void loadVarType()
		{
			switch (type) 
			{
			case "sint32":
				{
					eVarType = VarType.VT_SINT32;
				}
				break;
			case "float":
				{
					eVarType = VarType.VT_FLOAT;
				}
				break;
			case "string":
				{
					eVarType = VarType.VT_STRING;
				}
				break;
			case "enum":
				{
					eVarType = VarType.VT_ENUM;
				}
				break;
			case "bool":
				{
					eVarType = VarType.VT_BOOL;
				}
				break;
			case "union":
				{
					eVarType = VarType.VT_UNION;
				}
				break;
			default:
				{
					eVarType = VarType.VT_COUNT;
				}
				break;
			}
		}
    }

    class ExcelUnit
    {
        static string[] m_errMsg = new string[(int)ErrCode.EC_COUNT]
        {
            string.Empty,
            @"file not exist !",
            @"m_workbook is empty , has no sheet !",
            @"sheet name is invalid, is null or empty !",
            @"proto head line not exist !",
            @"proto head just support {0}",
            @"proto var type line not exist !",
            @"proto var type just support {0}",
            @"proto var name line not exist !",
            @"proto var name just support {0}",
            @"no server_client_flag row !",
            @"no annotation row !",
            @"convert proto content failed !",
            @"write proto {0} failed !",
            @"enum declare convert failed !",
			@"initialize has not been called !",
			@"var type={0} int table [{1}] convert failed !",
			@"var type={0} value={1} convert failed !",
			@"assembly create instance cls={0} failed!",
			@"has no coloum {0} {1}:{2}!",
			@"proto no matched cs {0} !",
        };

        void _PrintErrMsg(ErrCode eErrCode,params object[] param)
        {
            this.m_eErrCode = eErrCode;
			if (eErrCode > ErrCode.EC_SUCCEED && eErrCode < ErrCode.EC_COUNT)
            {
				Debug.LogErrorFormat(m_errMsg[(int)eErrCode],param);
            }
        }

        static int PROTO_HEAD_ROW = 0;
        static int PROTO_VAR_TYPE_ROW = 1;
        static int PROTO_VAR_NAME_ROW = 2;
        static int SERVER_CLIENT_FLAG_ROW = 3;
        static int ANNOTATION_ROW = 4;
        static int CONTENT_START_ROW = 5;
        static Regex ms_proto_head_reg = new Regex(@"(^\s*required\s*$|^\s*repeated\s*$)",RegexOptions.Singleline);
        static Regex ms_proto_var_type_reg = new Regex(@"(sint32|string|enum|bool|float|union|union\(float\))", RegexOptions.Singleline);
        static Regex ms_proto_var_name_reg = new Regex(@"(^\s*[a-zA-Z_][a-zA-Z_0-9]*\s*$|^\s*[a-zA-Z_][a-zA-Z_0-9]*:[0-9]*\s*$)", RegexOptions.Singleline);
        //static Regex ms_server_client_reg = new Regex(@"^\s*([1])\s*$",RegexOptions.Singleline);
        static Regex ms_enum_prop_reg = new Regex(@"([a-zA-Z_][0-9a-zA-Z_]*):(\-*\d+):\s*([^\s]+)\s*", RegexOptions.Singleline);

        FileStream m_fileStream;
        HSSFWorkbook m_workbook;
        ISheet m_sheet;
        IRow headLineRow = null;
        IRow protoVarTypeRow = null;
        IRow protoVarNameRow = null;
        IRow serverClientFlagRow = null;
        IRow annotationRow = null;
        bool m_bHasUnion = false;
        ErrCode m_eErrCode = ErrCode.EC_SUCCEED;
        ExcelColoumValue[] coloumValues = null;
		int m_iValidColoumCounts = 0;
		int m_iConvertedRows = 0;
		object[] mDatas = null;
		string name = string.Empty;
        string path = string.Empty;
		bool m_bInitialized = false;

		public int ConvertedRows 
		{
			get 
			{
				return m_iConvertedRows;
			}
		}

		public string FileName 
		{
			get { return name; }
		}

		public string SheetName
		{
			get 
			{
				if (null == m_sheet) 
				{
					return string.Empty;
				}
				return m_sheet.SheetName;
			}
		}

        public ExcelUnit(string path,FileAccess accessMode = FileAccess.Read)
        {
            if(File.Exists(path))
            {
                m_fileStream = new FileStream(path, FileMode.Open, accessMode, FileShare.ReadWrite);
            }

            if(null == m_fileStream)
            {
                _PrintErrMsg(ErrCode.EC_NO_FILE);
				return;
            }

			name = Path.GetFileNameWithoutExtension (path);
            this.path = path;
        }

        public bool succeed
        {
            get
            {
                return m_eErrCode == ErrCode.EC_SUCCEED;
            }
        }

        //public bool SaveGlobalQueryTable(Dictionary<int, ProtoTable.GlobalResQueryTable> data)
        //{
        //    if(null == data)
        //    {
        //        return false;
        //    }

        //    if(null == m_sheet)
        //    {
        //        return false;
        //    }

        //    try
        //    {
        //        var enumerator = data.GetEnumerator();
        //        int index = 0;
        //        while (enumerator.MoveNext())
        //        {
        //            var queryItem = enumerator.Current.Value;
        //            int _index = index + CONTENT_START_ROW;
        //            var row = m_sheet.GetRow(_index);
        //            if (null == row)
        //            {
        //                row = m_sheet.CreateRow(_index);
        //            }
        //            if (null == row)
        //            {
        //                return false;
        //            }

        //            int _col_count = 5;
        //            for (int j = 0; j < _col_count; ++j)
        //            {
        //                var cell = row.GetCell(j);
        //                if (null == cell)
        //                {
        //                    cell = row.CreateCell(j);
        //                }

        //                if (j == 0)
        //                    cell.SetCellValue(queryItem.ID);
        //                else if (j == 1)
        //                    cell.SetCellValue(queryItem.assetPackageIndex);
        //                else if (j == 2)
        //                    cell.SetCellValue(queryItem.AssetBundlePath);
        //                else if (j == 3)
        //                    cell.SetCellValue(queryItem.assetName);
        //                else if (j == 4)
        //                    cell.SetCellValue(queryItem.fileMd5);
        //            }

        //            if (row.Cells.Count > _col_count)
        //            {
        //                row.Cells.RemoveRange(_col_count, row.Cells.Count - _col_count);
        //            }
        //            ++index;
        //        }

        //        List<IRow> _remove_rows = new List<IRow>();
        //        for (int i = CONTENT_START_ROW + data.Count; i <= m_sheet.LastRowNum; ++i)
        //        {
        //            IRow row = m_sheet.GetRow(i);
        //            if (null != row)
        //            {
        //                _remove_rows.Add(row);
        //            }
        //        }
        //        for(int i = 0; i < _remove_rows.Count; ++i)
        //        {
        //            m_sheet.RemoveRow(_remove_rows[i]);
        //        }

        //        int count = m_sheet.LastRowNum - CONTENT_START_ROW + 1;
        //        if(count != data.Count)
        //        {
        //            return false;
        //        }

        //        UnityEngine.Debug.LogFormat("<color=#ffff00>modify data <color=#00ff00>  count = {0}</color> succeed !!!</color>", count);

        //        try
        //        {
        //            var fileStream = new FileStream(path, FileMode.Open,FileAccess.Write, FileShare.ReadWrite);
        //            m_workbook.Write(fileStream);
        //            UnityEngine.Debug.LogFormat("<color=#ffff00>save <color=#00ff00>GlobalResQueryTable</color> succeed !!!</color>", count);
        //        }
        //        catch(Exception e)
        //        {
        //            UnityEngine.Debug.LogErrorFormat("<color=#ffff00>save <color=#ff0000>GlobalResQueryTable</color> failed !!!</color>", count);
        //            UnityEngine.Debug.LogErrorFormat("<color=#ff0000>exception:{0}</color>", e.ToString());
        //            return false;
        //        }

        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        UnityEngine.Debug.LogErrorFormat("modify data error !!! {0}", e.ToString());
        //    }
        //    return false;
        //}

        public bool Init()
        {
			if (m_bInitialized) 
			{
				return succeed;
			}
			m_bInitialized = true;

            if (null != m_fileStream)
            {
                m_workbook = new HSSFWorkbook(m_fileStream);
                m_sheet = m_workbook.GetSheetAt(0);
                m_bHasUnion = false;

                if (null == m_sheet)
                {
                    _PrintErrMsg(ErrCode.EC_NO_SHEET);
                    return false;
                }

                if (string.IsNullOrEmpty(m_sheet.SheetName))
                {
                    _PrintErrMsg(ErrCode.EC_SHEET_NAME_INVALID);
                    return false;
                }

                if(!succeed)
                {
                    Debug.LogErrorFormat("[npoi] loadsheet=<color=#ff0000>[{0}]</color> falied !", m_sheet.SheetName);
                    return false;
                }
				//Debug.LogFormat ("sheet = {0}", m_sheet.SheetName);
            }
            return succeed;
        }

		public bool LoadProtoBase()
		{
			_loadProtoHeadLine(m_sheet);
			if (!succeed)
			{
				Debug.LogErrorFormat("[npoi] loadProtoHeadLine falied !");
				return false;
			}

			_loadProtoVarTypeLine(m_sheet);
			if (!succeed)
			{
				Debug.LogErrorFormat("[npoi] loadProtoVarTypeLine falied !");
				return false;
			}

			_loadProtoVarNameLine(m_sheet);
			if (!succeed)
			{
				Debug.LogErrorFormat("[npoi] loadProtoVarNameLine falied !");
				return false;
			}

			_loadServerClientFlag(m_sheet);
			if(!succeed)
			{
				Debug.LogErrorFormat("[npoi] _loadServerClientFlag falied !");
				return false;
			}

			_loadAnnontation(m_sheet);
			if(!succeed)
			{
				Debug.LogErrorFormat("[npoi] _loadAnnontation falied !");
				return false;
			}

			return true;
		}

		int _GetMaxColoumValues(IRow row)
		{
			int iRet = 0;
			if (null != row) 
			{
				for (int j = 0; j < row.Cells.Count; ++j) 
				{
					if (row.Cells[j].CellType == CellType.String && !string.IsNullOrEmpty (row.Cells[j].StringCellValue)) 
					{
						iRet = Math.Max (iRet, row.Cells [j].ColumnIndex + 1);
					}
				}
			}
			return iRet;
		}

        void _loadProtoHeadLine(ISheet sheet)
        {
            if(null != sheet)
            {
                headLineRow = m_sheet.GetRow(PROTO_HEAD_ROW);
                if (null == headLineRow)
                {
                    _PrintErrMsg(ErrCode.EC_NO_PROTO_HEAD_ROW);
                    return;
                }
					
				coloumValues = new ExcelColoumValue[_GetMaxColoumValues(headLineRow)];
				m_iValidColoumCounts = 0;

				//Debug.LogErrorFormat ("sheetName={0} HeadLines = {1}", sheet.SheetName, coloumValues.Length);

                for (int i = 0; i < headLineRow.Cells.Count; ++i)
                {
					int iColoumIndex = headLineRow.Cells [i].ColumnIndex;

					if(_IsHeadLineValid(i))
                    {
                        var match = ms_proto_head_reg.Match(headLineRow.Cells[i].StringCellValue);
                        if (!match.Success)
                        {
                            Debug.LogErrorFormat("ROW={0},COL={1},VALUE={2}", PROTO_HEAD_ROW, i + 1,headLineRow.Cells[i].StringCellValue);
                            _PrintErrMsg(ErrCode.EC_PROTO_HEAD_MATCH_FAILED, ms_proto_head_reg);
                            break;
                        }

						coloumValues[iColoumIndex] = new ExcelColoumValue
                        {
							iIndex = iColoumIndex,
                            head = headLineRow.Cells[i].StringCellValue,
                        };

						m_iValidColoumCounts += 1;
                    }
                }
            }
        }

        void _loadProtoVarTypeLine(ISheet sheet)
        {
            if(null != sheet)
            {
                protoVarTypeRow = m_sheet.GetRow(PROTO_VAR_TYPE_ROW);
                if(null == protoVarTypeRow)
                {
                    _PrintErrMsg(ErrCode.EC_NO_PROTO_VAR_TYPE_ROW);
                    return;
                }

                for (int i = 0; i < protoVarTypeRow.Cells.Count; ++i)
                {
					int iColoumIndex = protoVarTypeRow.Cells [i].ColumnIndex;

					if(!_IsColoumValid(iColoumIndex))
                    {
                        continue;
                    }

					var coloumValue = coloumValues [iColoumIndex];

                    var match = ms_proto_var_type_reg.Match(protoVarTypeRow.Cells[i].StringCellValue);
                    if (!match.Success)
                    {
                        Debug.LogErrorFormat(protoVarTypeRow.Cells[i].StringCellValue);
                        _PrintErrMsg(ErrCode.EC_PROTO_VAR_TYPE_MATCH_FAILED,ms_proto_var_type_reg);
                        return;
                    }

					coloumValue.type = match.Groups [1].Value;
					coloumValue.loadVarType ();
					coloumValue.type = _generateVarType(match.Groups[1].Value);
					if (coloumValue.eVarType == VarType.VT_UNION)
					{
						coloumValue.bUnionFloat = protoVarTypeRow.Cells[i].StringCellValue.EndsWith ("(float)");
					} 
					else 
					{
						coloumValue.bUnionFloat = false;
					}

					if (coloumValue.eVarType == VarType.VT_COUNT) 
					{
						_PrintErrMsg (ErrCode.EC_VAR_TYPE_CONVERT_FAILED, coloumValue.type,sheet.SheetName);
						return;
					}

                    if (!m_bHasUnion)
                    {
						if (coloumValue.eVarType == VarType.VT_UNION) 
						{
							m_bHasUnion = true;
						}
                    }
                }
            }
        }

        void _loadProtoVarNameLine(ISheet sheet)
        {
            if(null != sheet)
            {
                protoVarNameRow = m_sheet.GetRow(PROTO_VAR_NAME_ROW);
                if (null == protoVarNameRow)
                {
                    _PrintErrMsg(ErrCode.EC_NO_PROTO_VAR_NAME_ROW);
                    return;
                }

                for (int i = 0; i < protoVarNameRow.Cells.Count; ++i)
                {
					int iColoumIndex = protoVarNameRow.Cells [i].ColumnIndex;

					if (!_IsColoumValid(iColoumIndex))
                    {
                        continue;
                    }

					var coloumValue = coloumValues [iColoumIndex];

                    var match = ms_proto_var_name_reg.Match(protoVarNameRow.Cells[i].StringCellValue);
                    if (!match.Success)
                    {
                        Debug.LogErrorFormat(protoVarNameRow.Cells[i].StringCellValue);
                        _PrintErrMsg(ErrCode.EC_PROTO_VAR_NAME_MATCH_FAILED,ms_proto_var_name_reg);
                        break;
                    }

					coloumValue.name = _generateName (protoVarNameRow.Cells [i].StringCellValue);

					if (coloumValue.eVarType == VarType.VT_ENUM) 
					{
						coloumValue.propertyName = _generateEnumVarType (coloumValue.name);
					} 
					else 
					{
						coloumValue.propertyName = _generateName (coloumValue.name);
					}
                }
            }
        }

        void _loadServerClientFlag(ISheet sheet)
        {
            if(null != sheet)
            {
                serverClientFlagRow = m_sheet.GetRow(SERVER_CLIENT_FLAG_ROW);
                if(null == serverClientFlagRow)
                {
                    return;
                }

                for(int i = 0; i < serverClientFlagRow.Cells.Count; ++i)
                {
					int iColoumIndex = serverClientFlagRow.Cells [i].ColumnIndex;

					if(!_IsColoumValid(iColoumIndex))
                    {
                        continue;
                    }

					var coloumValue = coloumValues[iColoumIndex];

                    coloumValue.isValidToSearver = _IsValidToServer(i);
                }
            }
        }

		private object _getCellValueObject(ICell cell, string defaultStr)
		{
			object obj = defaultStr;

			if (cell != null)
			{
				switch (cell.CellType)
				{
				case CellType.Numeric:
					obj = cell.NumericCellValue;
					break;
				case CellType.String:
					obj = cell.StringCellValue;
					break;
				case CellType.Blank:
					obj = defaultStr;
					break;
				case CellType.Formula:
					try
					{
						obj = (int)cell.NumericCellValue;
					}
					catch 
					{
						Debug.LogErrorFormat("[GenerateText] 公式转换出错");
						Debug.LogErrorFormat("[GenerateText] cell type is not valid {0}, {1}, [{2}行, {3}列]", m_sheet.SheetName, cell.CellType.ToString(), cell.RowIndex + 1, cell.ColumnIndex);
					}
					break;
				default:
					Debug.LogErrorFormat("[GenerateText] 数据非法 {0}, {1}, [{2}行, {3}列]", m_sheet.SheetName, cell.CellType.ToString(), cell.RowIndex + 1, cell.ColumnIndex);

					break;
				}
			}
			return obj;
		}

		bool _IsValidRow(IRow row)
		{
			if (null == row || row.Cells.Count <= 0) {
				return false;
			}

			if (row.Cells [0].ColumnIndex != 0) {
				return false;
			}

			var rowObject = _getCellValueObject (row.Cells [0], string.Empty);
			if (string.IsNullOrEmpty (rowObject.ToString ())) 
			{
				return false;
			}

			return true;
		}

		object _loadContentRow(ISheet sheet,IRow row)
		{
			if (!_IsValidRow (row)) 
			{
				return null;
			}

			var classname = sheet.SheetName;
			Assembly ass = typeof(ProtoTable.FrameTypeTable).Assembly;
			object tableunit = ass.CreateInstance (string.Format("ProtoTable.{0}",classname));
			if (null == tableunit) 
			{
				_PrintErrMsg (ErrCode.EC_ASSEMBLY_CLASS_CREATE_INSTANCE_FAILED, classname);
				return null;
			}

			Type type = tableunit.GetType();
			object obj = new object();

			for (int j = 0; j < row.Cells.Count; ++j)
			{
				ICell cell = row.Cells[j];
				int iColoumIndex = cell.ColumnIndex;

				if(!_IsColoumValid(iColoumIndex))
				{
					continue;	
				}

				var coloumValue = coloumValues [iColoumIndex];

				obj = _getCellValueObject(cell, string.Empty);
				PropertyInfo proInfo = type.GetProperty(coloumValue.name, BindingFlags.Public | BindingFlags.Instance);

				if (coloumValue.head.Equals ("repeated"))
				{
					object theList = proInfo.GetValue(tableunit, null);
					foreach (string item in obj.ToString().Split('|'))
					{
						//objList.Add();
						try
						{
							var addObj = coloumValue.toValue(item);
							//Debug.LogFormat("<color=#00ff00>List<{1}> ak{0} Add({2})</color>",coloumValue.propertyName,coloumValue.type,obj.ToString());
							theList.GetType().InvokeMember("Add", BindingFlags.Public | BindingFlags.InvokeMethod, null, theList, new object[] { addObj });
						}
						catch (Exception)
						{
							Debug.LogErrorFormat("[GenerateText] 转表失败，请保证协议(*.cs)和表格数据(*.xls)相对应");
							Debug.LogErrorFormat("[GenerateText] {0} : {1}, {2}, {3}", coloumValue.head, item, coloumValue.type, coloumValue.eVarType);
							Debug.LogErrorFormat("[GenerateText] {0} {1}:{2} key: {3}({4}) (type:{5}) ERROR DATA", sheet.SheetName, row.RowNum + 1, cell.ColumnIndex + 1, coloumValue.type, coloumValue.name, coloumValue.type);
							_PrintErrMsg (ErrCode.EC_PROTO_NOT_MATCH_CS,sheet.SheetName);
							return null;
						}
					}
				}
				else if (coloumValue.head.Equals ("required")) 
				{
					try
					{
						obj = coloumValue.toValue(obj.ToString());
						proInfo.SetValue(tableunit, obj, null);
						//Debug.LogFormat("<color=#00ff00>NAME={0} TYPE={1} VALUE={2}</color>",coloumValue.propertyName,coloumValue.type,obj.ToString());
					}
					catch(Exception e) 
					{
						Debug.LogErrorFormat (e.ToString ());
						Debug.LogErrorFormat("{0}",coloumValue.propertyName);
						_PrintErrMsg (ErrCode.EC_PROTO_NOT_MATCH_CS,sheet.SheetName);
						return null;
					}
				}
				else 
				{
					Debug.LogErrorFormat ("unknown proto head = {0} {1}!!!", coloumValue.head,coloumValue.type);
					return null;
				}
			}

			return tableunit;
		}

		class loadContenRowParam
		{
			public delegate void OnTaskOver(int iCount,string txtPath);
			public ExcelUnit excelUnit;
			public ISheet sheet;
			public int iStartRowIndex;
			public int iEndRowIndex;
			public object[] mDatas;
			public string textPath;
			public object loadLock;
			public OnTaskOver callback;

			public static void _ConvertRow(object param)
			{
				var curParam = param as loadContenRowParam;
				if (null != curParam) 
				{
					try
					{
						var beginTime = System.DateTime.Now.Ticks;

						int iValidCount = 0;

						for (int i = curParam.iStartRowIndex; i < curParam.iEndRowIndex; ++i) 
						{
							var tableUnit = curParam.excelUnit._loadContentRow (curParam.sheet, curParam.sheet.GetRow(i));
							curParam.mDatas[i] = tableUnit;
							if(null != tableUnit)
							{
								++iValidCount;
							}
						}

						var deltaTime = System.DateTime.Now.Ticks - beginTime;
						beginTime = System.DateTime.Now.Ticks;
						Debug.LogFormat("[convert row contents] <color=#00ff00>[{0}]</color>[{1}-{2}][{3}/{4}]<color=#00ff00>[time = {5}ms]</color>",curParam.sheet.SheetName,
							curParam.iStartRowIndex,curParam.iEndRowIndex - 1,
							iValidCount,curParam.iEndRowIndex - curParam.iStartRowIndex,
							deltaTime/10000);

						deltaTime = System.DateTime.Now.Ticks - beginTime;
						Debug.LogFormat("[push row contents] <color=#00ff00>[{0}] [{1}-{2}]</color>",curParam.sheet.SheetName,curParam.iStartRowIndex,curParam.iEndRowIndex);

						lock(curParam.loadLock)
						{
							if(null != curParam.callback)
							{
								curParam.callback.Invoke(curParam.iEndRowIndex - curParam.iStartRowIndex,curParam.textPath);
							}
						}
					}
					catch(Exception e) 
					{
						Debug.LogErrorFormat (e.ToString ());
						Debug.LogFormat("<color=#ff0000>[{0}] [{1}|{2}]</color>",curParam.sheet.SheetName,curParam.iStartRowIndex,curParam.iEndRowIndex);
						lock(curParam.loadLock)
						{
							if(null != curParam.callback)
							{
								curParam.callback.Invoke(curParam.iEndRowIndex - curParam.iStartRowIndex,curParam.textPath);
							}
						}
					}
				}
			}
		}

		void _OnTaskOver(int iCount,string txtPath)
		{
            m_iTotalTaskCount -= iCount;
            if (0 == m_iTotalTaskCount)
            {
                var beginTime = System.DateTime.Now.Ticks;
                TextDataStream ws = new TextDataStream(txtPath);
                for (int i = 0; i < mDatas.Length; ++i)
                {
                    if (null != mDatas[i])
                    {
                        ws.Write(mDatas[i]);
                    }
                }
                ++m_iConvertedRows;
                ws.Flush();
                ws.Close();
                var deltaTime = System.DateTime.Now.Ticks - beginTime;
                var name = Path.GetFileNameWithoutExtension(txtPath);
                Debug.LogFormat("<color=#00ff00>[write file][{0}][time={1}ms]</color>", name, deltaTime / 10000);
            }
        }
			
		int m_iTotalTaskCount = 0;
		void _loadContent(ISheet sheet,string applicationPath)
		{
			m_iConvertedRows = 0;
			m_iTotalTaskCount = 0;
			mDatas = null;
			if (succeed) 
			{
				if (null == m_fileStream) 
				{
					_PrintErrMsg (ErrCode.EC_NO_INITIALIZE);
					return;
				}

				if (null == sheet) 
				{
					_PrintErrMsg (ErrCode.EC_NO_SHEET);
					return;
				}

				var txtPath = Path.GetFullPath (applicationPath + ExcelConfig.TXT_SAVE_PATH + sheet.SheetName + ".txt");
				if (File.Exists (txtPath)) 
				{
					File.Delete (txtPath);
				}

				int iTotal = (sheet.LastRowNum - CONTENT_START_ROW + 1);
				int iThreadCount = iTotal / 300;
				int iMaxThread = 1;
				iThreadCount = Math.Min (iThreadCount, iMaxThread);
				object loadLock = new object ();
				m_iTotalTaskCount = sheet.LastRowNum - CONTENT_START_ROW + 1;
				var beginTime = System.DateTime.Now.Ticks;
				mDatas = new object[sheet.LastRowNum + 1];
				var deltaTime = System.DateTime.Now.Ticks - beginTime;
				Debug.LogFormat ("[allocate memory] <color=#00ff00>[{0}]</color>[counts = {1}] <color=#ffff00>[time = {2}ms]</color>", sheet.SheetName, m_iTotalTaskCount, deltaTime/10000);

				if (iThreadCount <= 0) 
				{
					loadContenRowParam._ConvertRow (new loadContenRowParam {
						excelUnit = this,
						sheet = sheet,
						iStartRowIndex = CONTENT_START_ROW,
						iEndRowIndex = sheet.LastRowNum + 1,
						mDatas = mDatas,
						textPath = txtPath,
						loadLock = loadLock,
						callback = _OnTaskOver,
					});
				} 
				else 
				{
					int iAve = iTotal / (iThreadCount + 1);

					Thread[] threads = new Thread[iThreadCount];
					for (int i = 0; i < iThreadCount; ++i) 
					{
						threads[i] = new Thread(loadContenRowParam._ConvertRow);
						threads[i].Priority = System.Threading.ThreadPriority.Highest;
						threads[i].Start (new loadContenRowParam {
							excelUnit = this,
							sheet = sheet,
							iStartRowIndex = CONTENT_START_ROW + iAve * i,
							iEndRowIndex = CONTENT_START_ROW + iAve * (i + 1),
							mDatas = mDatas,
							textPath = txtPath,
							loadLock = loadLock,
							callback = _OnTaskOver,
						});
					}
						
					loadContenRowParam._ConvertRow (new loadContenRowParam {
						excelUnit = this,
						sheet = sheet,
						iStartRowIndex = CONTENT_START_ROW + iAve * iThreadCount,
						iEndRowIndex = sheet.LastRowNum + 1,
						mDatas = mDatas,
						textPath = txtPath,
						loadLock = loadLock,
						callback = _OnTaskOver,
					});

					for (int i = 0; i < threads.Length; ++i) 
					{
						threads [i].Join ();
					}
				}
			}
		}

        void _loadAnnontation(ISheet sheet)
        {
            if(null != sheet)
            {
                annotationRow = sheet.GetRow(ANNOTATION_ROW);
                if (null == annotationRow)
                {
                    _PrintErrMsg(ErrCode.EC_NO_ANNOTATION_ROW);
                    return;
                }

                for(int i = 0; i < annotationRow.Cells.Count; ++i)
                {
					int iColoumIndex = annotationRow.Cells[i].ColumnIndex;

					if(!_IsColoumValid(iColoumIndex))
                    {
                        continue;
                    }

					var coloumValue = coloumValues [iColoumIndex];

                    if(coloumValue.type.Equals("enum"))
                    {
                        coloumValue.declare = _generateEnumDeclare(coloumValue, annotationRow.Cells[i].StringCellValue);
                        if(string.IsNullOrEmpty(coloumValue.declare))
                        {
                            _PrintErrMsg(ErrCode.EC_ENUM_DECLARE_CONVERT_IS_EMPTY);
                            return;
                        }
                    }
                }
            }
        }

        bool _IsColoumValid(int iIndex)
        {
			if (null != coloumValues && iIndex >= 0 && iIndex < coloumValues.Length)
            {
				return null != coloumValues[iIndex];
            }
            return false;
        }

		bool _IsHeadLineValid(int iIndex)
		{
			if (null != headLineRow && iIndex >= 0 && iIndex < headLineRow.Cells.Count)
			{
				return headLineRow.Cells[iIndex].CellType == CellType.String && !string.IsNullOrEmpty(headLineRow.Cells[iIndex].StringCellValue);
			}
			return false;
		}

        ExcelColoumValue _GetColoumValue(int iIndex)
        {
            if(null != coloumValues)
            {
                if(iIndex >= 0 && iIndex < coloumValues.Length)
                {
                    return coloumValues[iIndex];
                }
            }
            return null;
        }

        bool _IsValidToServer(int iIndex)
        {
            if(null != serverClientFlagRow && iIndex >= 0 && iIndex < serverClientFlagRow.Cells.Count)
            {
				var cellObject = _getCellValueObject(serverClientFlagRow.Cells[iIndex],string.Empty);
				int iRes = 0;
				if (int.TryParse (cellObject.ToString(), out iRes)) 
				{
					return iRes == 1;
				}
            }
            return false;
        }

        bool _IsEnumValue(int iIndex)
        {
            if(null != protoVarTypeRow && iIndex >= 0 && iIndex < protoVarTypeRow.Cells.Count)
            {
                var match = ms_proto_var_type_reg.Match(protoVarTypeRow.Cells[iIndex].StringCellValue);
                if(match.Success && match.Groups[1].Value.Equals("enum"))
                {
                    return true;
                }
            }
            return false;
        }

        string m_proto_content = string.Empty;
        public void CreateProto(string path)
        {
            if(succeed)
            {
                var protoFilePath = Path.GetFullPath(path + m_sheet.SheetName + ".proto");
                try
                {
					StringBuilder kString = new StringBuilder(256);
                    if(m_bHasUnion)
                    {
                        kString.Append("import \"Union.proto\";\n\n");
                    }
                    kString.Append("package ProtoTable;\n");
                    kString.AppendFormat("message {0}\n", m_sheet.SheetName);
                    kString.Append("{\n");
                    int iCnt = 0;
                    for(int i = 0; i < coloumValues.Length; ++i)
                    {
						var coloumValue = coloumValues[i];
						if(null == coloumValue)
						{
							continue;
						}

                        if(coloumValue.type.Equals("enum"))
                        {
                            kString.Append(coloumValue.declare);
                            kString.AppendFormat("{0} {1} {2} = {3};\n", coloumValue.head, _generateEnumVarType(coloumValue.name), coloumValue.name, ++iCnt);
                        }
						else if(coloumValue.type.Equals("float"))
						{
							kString.AppendFormat("{0} {1} {2} = {3};\n", coloumValue.head,"sint32", coloumValue.name, ++iCnt);
						}
                        else
                        {
                            kString.AppendFormat("{0} {1} {2} = {3};\n", coloumValue.head,coloumValue.type, coloumValue.name, ++iCnt);
                        }
                    }
					kString.Append("};\n");
                    m_proto_content = kString.ToString();
					File.WriteAllText(protoFilePath, m_proto_content, Encoding.ASCII);
                }
                catch (System.Exception ex)
                {
                    Debug.LogErrorFormat(ex.ToString());
                    _PrintErrMsg(ErrCode.EC_WRITE_PROTO_FAILED, protoFilePath);
                }
            }
        }

        string _generateVarType(string content)
        {
            if (content.StartsWith("union"))
            {
                return "UnionCell";
            }

            return content;
        }

        string _generateName(string content)
        {
            var tokens = content.Split(':');
            if(tokens.Length > 0)
            {
				tokens [0] = tokens [0].Trim ();
                return tokens[0];
            }
            return content;
        }

        string _generateEnumVarType(string varName)
        {
            return string.Format("e{0}",varName);
        }

        string _generateEnumDeclare(ExcelColoumValue value,string content)
        {
            var tokens = content.Split(new char[] { '\r','\n'});
            List<Match> matches = new List<Match>();
            for(int i = 0; i < tokens.Length; ++i)
            {
                var match = ms_enum_prop_reg.Match(tokens[i]);
                if(match.Success)
                {
                    matches.Add(match);
                }
            }
			if (matches.Count > 0) 
			{
				if (null == value.enumValues) 
				{
					value.enumValues = new List<int> ();
				}
				value.enumValues.Clear ();
				for (int j = 0; j < matches.Count; ++j) 
				{
					int iRes = 0;
					if (int.TryParse (matches [j].Groups [2].Value, out iRes)) 
					{
						if (!value.enumValues.Contains (iRes)) 
						{
							value.enumValues.Add (iRes);
						}
					}
				}
			}
            if(matches.Count > 0)
            {
				StringBuilder kString = new StringBuilder(256);
                var ret = kString.ToString();
                kString.AppendFormat("enum {0}\n", _generateEnumVarType(value.name));
                kString.Append("{\n");
                for(int i = 0; i < matches.Count; ++i)
                {
                    kString.AppendFormat("\t{0} = {1};\t//{2}\n", matches[i].Groups[1].Value, matches[i].Groups[2].Value, matches[i].Groups[3].Value);
                }
                kString.Append("}\n");
                ret = kString.ToString();
                return ret;
            }
            return string.Empty;
        }

		public void generateText(string applicationPath)
		{
			if (succeed) 
			{
				_loadContent (m_sheet,applicationPath);
				if (!succeed) 
				{
					Debug.LogErrorFormat ("generate text failed ! [{0}]", m_sheet.SheetName);
				}
			}
		}

        public void Close()
        {
            if(null != m_fileStream)
            {
                headLineRow = null;
                protoVarTypeRow = null;
                protoVarNameRow = null;
                serverClientFlagRow = null;
                m_workbook = null;
                m_sheet = null;
                m_fileStream.Close();
                m_fileStream = null;
				name = string.Empty;
				m_bInitialized = false;
            }
        }
    }
}