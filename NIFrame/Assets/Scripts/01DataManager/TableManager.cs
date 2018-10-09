using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;

namespace NI
{
    public class TableManager
    {
        static TableManager ms_instance = null;
        public static TableManager Instance()
        {
            if(null == ms_instance)
            {
                ms_instance = new TableManager();
            }
            return ms_instance;
        }

        Dictionary<System.Type, Dictionary<int,object>> mTables = new Dictionary<System.Type, Dictionary<int, object>>();

        string mPath = string.Empty;

        public void Initialize(string path)
        {
            this.mPath = path;
            mTables.Clear();
        }

        public void Clear()
        {
            this.mPath = string.Empty;
            mTables.Clear();
        }

        object convertTableObject(AssetBinary asset, System.Type type)
        {
            if (asset == null || null == type)
            {
                return null;
            }

            var IDMap = type.GetProperty("ID").GetGetMethod();
            if (null == IDMap)
            {
                return null;
            }

            Dictionary<int, object> table = new Dictionary<int, object>();
            bool bCanParse = ProtoBuf.Serializer.CanParse(type);
            byte[] data = asset.m_DataBytes;

            for (int i = 0; i < data.Length;)
            {
                int len = 0;
                for (int j = i; j < i + 8; ++j)
                {
                    if (data[j] > 0)
                        len = len * 10 + (data[j] - '0');
                }

                i += 8;

                MemoryStream mDataStream = new MemoryStream(data, i, len);

                try
                {
                    object tableData = null;

                    if (bCanParse)
                    {
                        tableData = ProtoBuf.Serializer.ParseEx(type, mDataStream);
                    }
                    else
                    {
                        tableData = ProtoBuf.Serializer.DeserializeEx(type, mDataStream);
                    }

                    if (tableData == null)
                    {
                        Debug.LogErrorFormat("table data is nil {0}, {1}", type.Name, i);
                    }
                    else
                    {
                        var id = (int)IDMap.Invoke(tableData, null);
                        if (!table.ContainsKey(id))
                        {
                            table.Add(id, tableData);
                        }
                        else
                        {
                            Debug.LogErrorFormat("table {0} key repeated id = {1}", type.Name, id);
                            return null;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogErrorFormat("{0} : *.cs don't match the *.xls, delete the *.proto, regenerate the *.cs", type.Name);
                    Debug.LogErrorFormat("error deserialize at line {0}, with error {1}", i + 1, e.ToString());

                    string ErrorMsg = "表格：" + type.Name + " 加载失败，原因：" + e.Message;

                    Debug.LogErrorFormat("【读表错误!】 {0}", ErrorMsg);

                    return null;
                }

                i += len;
            }

            return table;
        }

        public void LoadTable<T>() where T : class,global::ProtoBuf.IExtensible, global::ProtoBuf.IParseable,new()
        {
            LoadTable(typeof(T));
        }

        public void LoadTable(System.Type type)
        {
            var beginTime = System.DateTime.Now.Ticks;

            var path = mPath + type.Name;

            if (mTables.ContainsKey(type))
            {
                return;
            }

            AssetBinary res = null;

            //res = AssetBundleManager.LoadAsset<AssetBinary>(Consts.UGame.CommonSharedRes, path);

            if (null == res)
            {
                return;
            }

            var table = convertTableObject(res, type) as Dictionary<int, object>;
            if (null == table)
            {
                Debug.LogErrorFormat("load failed table [<color=#ff0000>{0}</color>] failed !!!", type.Name);
                return;
            }
            mTables.Add(type, table);

            var deltaTime = System.DateTime.Now.Ticks - beginTime;
            //Debug.LogErrorFormat("load {0} cost <color=#00ff00>{1}</color> ms!", type.Name, deltaTime / 10000);
        }

        public void LoadTableFromMemory<T>(byte[] datas) where T : class, global::ProtoBuf.IExtensible, global::ProtoBuf.IParseable, new()
        {
            AssetBinary res = new AssetBinary();
            res.m_DataBytes = datas;

            var type = typeof(T);

            if (null == res)
            {
                return;
            }

            var table = convertTableObject(res, type) as Dictionary<int, object>;
            if (null == table)
            {
                Debug.LogErrorFormat("load failed table [<color=#ff0000>{0}</color>] failed !!!", type.Name);
                return;
            }

            if(mTables.ContainsKey(type))
            {
                mTables.Remove(type);
            }

            mTables.Add(type, table);
        }

        public Dictionary<int,object> ReadTableFromResourcesFile<T>(string filePath) where T : class, global::ProtoBuf.IExtensible, global::ProtoBuf.IParseable, new()
        {
            var type = typeof(T);
            var beginTime = System.DateTime.Now.Ticks;

            var path = System.IO.Path.Combine(filePath, type.Name);

            AssetBinary res = Resources.Load<AssetBinary>(path);

            if (null == res)
            {
                return null;
            }

            var table = convertTableObject(res, type) as Dictionary<int, object>;
            if (null == table)
            {
                Debug.LogErrorFormat("load failed table [<color=#ff0000>{0}</color>] failed !!!", type.Name);
                return null;
            }

            var deltaTime = System.DateTime.Now.Ticks - beginTime;
            //Debug.LogErrorFormat("load {0} cost <color=#00ff00>{1}</color> ms!", type.Name, deltaTime / 10000);

            return table;
        }

        public Dictionary<int,object> ReadTableFromAssetBundle<T>(AssetBundle bundle) where T : class, global::ProtoBuf.IExtensible, global::ProtoBuf.IParseable, new()
        {
            if (null != bundle)
            {
                var beginTime = System.DateTime.Now.Ticks;
                AssetBinary res = null;

                var type = typeof(T);
                res = bundle.LoadAsset<AssetBinary>(type.Name);
                if (null == res)
                {
                    return null;
                }

                var table = convertTableObject(res, type) as Dictionary<int, object>;

                var deltaTime = System.DateTime.Now.Ticks - beginTime;
                //Debug.LogErrorFormat("load {0} cost <color=#00ff00>{1}</color> ms!", type.Name, deltaTime / 10000);

                return table;
            }
            return null;
        }

        public void LoadTableFromAssetBundle<T>(AssetBundle bundle) where T : class, global::ProtoBuf.IExtensible, global::ProtoBuf.IParseable, new()
        {
            if(null != bundle)
            {
                var beginTime = System.DateTime.Now.Ticks;
                AssetBinary res = null;

                var type = typeof(T);

                res = bundle.LoadAsset<AssetBinary>(type.Name);
                if (null == res)
                {
                    return;
                }

                var table = convertTableObject(res,type) as Dictionary<int, object>;
                if (null == table)
                {
                    Debug.LogErrorFormat("load failed table [<color=#ff0000>{0}</color>] failed !!!", type.Name);
                    return;
                }

                if (mTables.ContainsKey(type))
                    mTables.Remove(type);
                mTables.Add(type, table);

                var deltaTime = System.DateTime.Now.Ticks - beginTime;
                //Debug.LogErrorFormat("load {0} cost <color=#00ff00>{1}</color> ms!", type.Name, deltaTime / 10000);
            }
        }

        public Dictionary<int,object> GetTable<T>() where T : class,global::ProtoBuf.IExtensible, global::ProtoBuf.IParseable,new()
        {
            var type = typeof(T);
            if(mTables.ContainsKey(type))
            {
                return mTables[type];
            }
            return null;
        }

        public T GetTableItem<T>(int iId) where T : class,global::ProtoBuf.IExtensible, global::ProtoBuf.IParseable,new ()
        {
            var table = GetTable<T>();
            if(null != table && table.ContainsKey(iId))
            {
                return table[iId] as T;
            }
            return default(T);
        }

        public IEnumerator LoadTables(System.Type[] tables)
        {
            for(int i = 0; i < tables.Length; ++i)
            {
                LoadTable(tables[i]);
                yield return null;
            }
            yield return null;
        }
    }
}