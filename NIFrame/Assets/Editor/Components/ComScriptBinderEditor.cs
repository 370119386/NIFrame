using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NI;
using System.Reflection;
using System;
using System.Linq;
using System.Text;

[CustomEditor(typeof(ComScriptBinder))]
public class ComScriptBinderEditor : Editor
{
    protected SerializedProperty components = null;
    protected SerializedProperty labelSpace = null;
    protected SerializedProperty scriptStatus = null;
    protected SerializedProperty labelTypeID = null;
    protected string mInitializeCode = string.Empty;
    protected List<string> mInitializeCodeGUI = new List<string>();
    protected List<string> mDeclareCodeGUI = new List<string>();

    protected string getDeclareCode()
    {
        string ret = string.Empty;
        StringBuilder stringBuilder = StringBuilderCache.Acquire();
        for (int i = 0; i < mDeclareCodeGUI.Count; ++i)
        {
            stringBuilder.Append(mDeclareCodeGUI[i]);
            if (i != mDeclareCodeGUI.Count - 1)
            {
                stringBuilder.Append("\r\n");
            }
        }
        ret = stringBuilder.ToString();
        StringBuilderCache.Release(stringBuilder);
        return ret;
    }

    protected string getInitializeCode()
    {
        string ret = string.Empty;
        StringBuilder stringBuilder = StringBuilderCache.Acquire();
        stringBuilder.Append("protected override void _InitScriptBinder()\n");
        stringBuilder.Append("{\n");
        for (int i = 0; i < mInitializeCodeGUI.Count; ++i)
        {
            stringBuilder.Append("\t");
            stringBuilder.Append(mInitializeCodeGUI[i]);
            if(i != mInitializeCodeGUI.Count - 1)
            {
                stringBuilder.Append("\r\n");
            }
        }
        stringBuilder.Append("}\n");
        ret = stringBuilder.ToString();
        StringBuilderCache.Release(stringBuilder);
        return ret;
    }

    public void OnEnable()
    {
        components = serializedObject.FindProperty("scriptItems");
        labelSpace = serializedObject.FindProperty("labelSpace");
        scriptStatus = serializedObject.FindProperty("scriptStatus");
        labelTypeID = serializedObject.FindProperty("labelTypeId");
        createInitializeCodes();
        createDeclareCodes();
    }

    void _menuFunction(object value)
    {
        var argv = value as object[];
        if(null != argv && argv.Length == 2)
        {
            try
            {
                ScriptBinderItem component = argv[1] as ScriptBinderItem;
                if(null != component)
                {
                    component.component = argv[0] as UnityEngine.Object;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogErrorFormat(ex.ToString());
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.color = new Color32(0x6a,0xff,0x5e,0xFF);
        GUILayout.BeginVertical("GroupBox");

        GUI.color = Color.gray;
        GUILayout.BeginVertical("GroupBox");
        GUI.color = Color.magenta;
        EditorGUILayout.LabelField("ClientScriptBinder(神器)", GUILayout.MinWidth(100));
        GUILayout.EndVertical();

        GUI.color = Color.gray;
        GUILayout.BeginVertical("GroupBox");
        GUI.color = Color.white;
        base.OnInspectorGUI();
        GUILayout.EndVertical();

        EditorGUI.BeginChangeCheck();
        OnScriptItemGUI();
        OnDeclareCodeGUI();
        OnInitializedCodeGUI();
        OnScriptStatusGUI();

        GUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            createInitializeCodes();
            createDeclareCodes();
            serializedObject.ApplyModifiedProperties();
        }
    }

    protected void OnScriptItemGUI()
    {
        for (int i = 0; i < components.arraySize; ++i)
        {
            var scriptBindItem = components.GetArrayElementAtIndex(i);
            if (null != scriptBindItem)
            {
                GUI.color = Color.gray;
                GUILayout.BeginVertical("GroupBox");

                //ScriptBinderItem
                SerializedProperty hashCode = scriptBindItem.FindPropertyRelative("iHashCode");
                SerializedProperty component = scriptBindItem.FindPropertyRelative("component");
                SerializedProperty varName = scriptBindItem.FindPropertyRelative("varName");
                SerializedProperty locked = scriptBindItem.FindPropertyRelative("locked");
                ScriptBinderItem scriptItem = null;
                if (i < (target as ComScriptBinder).scriptItems.Length)
                {
                    scriptItem = (target as ComScriptBinder).scriptItems[i];
                }

                EditorGUILayout.BeginHorizontal();
                GUI.color = Color.white;
                locked.boolValue = EditorGUILayout.Toggle(locked.boolValue);
                GUI.enabled = locked.boolValue;
                varName.stringValue = EditorGUILayout.TextField(varName.stringValue);
                GUI.enabled = true;
                hashCode.intValue = varName.stringValue.GetHashCode();
                GUI.color = Color.white;
                EditorGUILayout.LabelField(hashCode.intValue.ToString(), GUILayout.MaxWidth(80));

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                component.objectReferenceValue = EditorGUILayout.ObjectField(component.objectReferenceValue, typeof(UnityEngine.Object), true) as UnityEngine.Object;
                if (null != component.objectReferenceValue)
                {
                    GameObject gameObject = component.objectReferenceValue as GameObject;
                    if (null == gameObject)
                    {
                        if (component.objectReferenceValue as Component)
                        {
                            gameObject = (component.objectReferenceValue as Component).gameObject;
                        }
                    }

                    if (GUILayout.Button("Select Component", "GV Gizmo DropDown",GUILayout.MaxWidth(120)))
                    {
                        Component[] coms = gameObject.GetComponents<Component>();

                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("GameObject"), component.objectReferenceValue is GameObject, _menuFunction, new object[] { gameObject, scriptItem });
                        if (null != coms)
                        {
                            for (int j = 0; j < coms.Length; ++j)
                            {
                                menu.AddItem(new GUIContent(coms[j].GetType().Name), component.objectReferenceValue == coms[j], _menuFunction, new object[] { coms[j], scriptItem });
                            }
                        }
                        menu.ShowAsContext();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("insert"))
                {
                    if (i > 0)
                    {
                        components.InsertArrayElementAtIndex(i - 1);
                    }
                    else
                    {
                        components.InsertArrayElementAtIndex(i);
                    }
                }
                if (GUILayout.Button("append"))
                {
                    components.InsertArrayElementAtIndex(i);
                }
                GUI.enabled = !string.IsNullOrEmpty(varName.stringValue);
                if (GUILayout.Button("getcode"))
                {
                    string codeInfo = getCopyString(component, varName,false);
                    if (!string.IsNullOrEmpty(codeInfo))
                    {
                        GUIUtility.systemCopyBuffer = codeInfo;
                        UnityEngine.Debug.LogErrorFormat("<color=#00ff00>copy succeed : {0}</color>", codeInfo);
                    }
                }
                GUI.enabled = true;
                if (GUILayout.Button("  -  "))
                {
                    components.DeleteArrayElementAtIndex(i);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
        }

        GUI.color = Color.gray;
        EditorGUILayout.BeginVertical("GroupBox");
        GUI.color = Color.white;
        EditorGUILayout.BeginHorizontal();
        GUI.color = Color.green;
        EditorGUILayout.LabelField("Object TotalCount:", GUILayout.MinWidth(60));
        GUI.color = Color.white;
        GUI.enabled = false;
        EditorGUILayout.IntField(components.arraySize, GUILayout.MinWidth(60));
        GUI.enabled = true;
        if (GUILayout.Button("  +   ", GUILayout.MinWidth(60)))
        {
            components.InsertArrayElementAtIndex(components.arraySize);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    protected string getCopyString(SerializedProperty component, SerializedProperty varName,bool bDeclare)
    {
        if(null != component && null != varName && null != component.objectReferenceValue)
        {
            string enumVarName = varName.stringValue;
            string componentName = component.objectReferenceValue.GetType().FullName;
            string fmtContent = string.Empty;
            if(!bDeclare)
            {
                fmtContent = "m{0} = mScriptBinder.GetObject(\"{0}\") as {1};";
            }
            else
            {
                fmtContent = "{1} m{0};";
            }
            fmtContent = string.Format(fmtContent, varName.stringValue,componentName);
            return fmtContent;
        }
        return string.Empty;
    }

    protected void OnScriptStatusGUI()
    {
        if (scriptStatus.arraySize > 0)
        {
            for (int i = 0; i < scriptStatus.arraySize; ++i)
            {
                var component = scriptStatus.GetArrayElementAtIndex(i);
                if (null != component)
                {
                    GUI.color = Color.gray;
                    EditorGUILayout.BeginVertical("GroupBox");
                    GUI.color = Color.white;

                    var hashCode = component.FindPropertyRelative("iHashCode");
                    var statusName = component.FindPropertyRelative("statusName");
                    var action = component.FindPropertyRelative("action");
                    var locked = component.FindPropertyRelative("locked");

                    GUI.color = Color.white;
                    EditorGUILayout.BeginHorizontal();
                    locked.boolValue = EditorGUILayout.Toggle(locked.boolValue);
                    GUI.enabled = locked.boolValue;
                    statusName.stringValue = EditorGUILayout.TextField(statusName.stringValue);
                    GUI.enabled = true;
                    hashCode.intValue = statusName.stringValue.GetHashCode();
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField(hashCode.intValue.ToString(), GUILayout.MaxWidth(80));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(action);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("insert"))
                    {
                        if (i > 0)
                            scriptStatus.InsertArrayElementAtIndex(i - 1);
                        else
                            scriptStatus.InsertArrayElementAtIndex(i);
                    }
                    if (GUILayout.Button("append"))
                    {
                        scriptStatus.InsertArrayElementAtIndex(i);
                    }
                    if (GUILayout.Button("execute action"))
                    {
                        var script = (target as ComScriptBinder);
                        script._SetAction(statusName.stringValue);
                    }
                    if (GUILayout.Button("-"))
                    {
                        scriptStatus.DeleteArrayElementAtIndex(i);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }
        }

        GUI.color = Color.gray;
        EditorGUILayout.BeginVertical("GroupBox");
        GUI.color = Color.white;
        EditorGUILayout.BeginHorizontal();
        GUI.color = Color.green;
        EditorGUILayout.LabelField("Status TotalCount:", GUILayout.MinWidth(60));
        GUI.color = Color.white;
        GUI.enabled = false;
        EditorGUILayout.IntField(scriptStatus.arraySize, GUILayout.MinWidth(60));
        GUI.enabled = true;
        if (GUILayout.Button("  +   ", GUILayout.MinWidth(60)))
        {
            scriptStatus.InsertArrayElementAtIndex(scriptStatus.arraySize);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    protected void OnDeclareCodeGUI()
    {
        if (mDeclareCodeGUI.Count > 0)
        {
            GUI.color = Color.gray;
            EditorGUILayout.BeginVertical("GroupBox");
            GUI.color = Color.green;
            for (int i = 0; i < mDeclareCodeGUI.Count; ++i)
            {
                EditorGUILayout.LabelField(mDeclareCodeGUI[i]);
            }
            GUI.color = Color.white;
            if (GUILayout.Button("copy declaration code"))
            {
                GUIUtility.systemCopyBuffer = string.Empty;
                var repeatedValue = string.Empty;
                if (!checkVarNameRepeated(ref repeatedValue))
                {
                    GUIUtility.systemCopyBuffer = getDeclareCode();
                    UnityEngine.Debug.LogErrorFormat("<color=#00ff00>copy succeed !</color>");
                }
                else
                {
                    GUIUtility.systemCopyBuffer = string.Format("copy failed repeated name = [{0}]!", repeatedValue);
                    UnityEngine.Debug.LogErrorFormat("<color=#ff0000>copy failed repeated name = [<color=#00ff00>{0}</color>]!</color>", repeatedValue);
                }
            }
            EditorGUILayout.EndVertical();
            GUI.color = Color.white;
        }
    }

    protected void OnInitializedCodeGUI()
    {
        if(mInitializeCodeGUI.Count > 0)
        {
            GUI.color = Color.gray;
            EditorGUILayout.BeginVertical("GroupBox");
            GUI.color = Color.green;
            for (int i = 0; i < mInitializeCodeGUI.Count; ++i)
            {
                EditorGUILayout.LabelField(mInitializeCodeGUI[i]);
            }
            GUI.color = Color.white;
            if (GUILayout.Button("copy initialize code"))
            {
                GUIUtility.systemCopyBuffer = string.Empty;
                var repeatedValue = string.Empty;
                if (!checkVarNameRepeated(ref repeatedValue))
                {
                    GUIUtility.systemCopyBuffer = getInitializeCode();
                    UnityEngine.Debug.LogErrorFormat("<color=#00ff00>copy succeed !</color>");
                }
                else
                {
                    GUIUtility.systemCopyBuffer = string.Format("copy failed repeated name = [{0}]!", repeatedValue);
                    UnityEngine.Debug.LogErrorFormat("<color=#ff0000>copy failed repeated name = [<color=#00ff00>{0}</color>]!</color>", repeatedValue);
                }
            }
            EditorGUILayout.EndVertical();
            GUI.color = Color.white;
        }
    }

    protected bool checkVarNameRepeated(ref string repeatValue)
    {
        List<string> varNames = new List<string>();
        for (int i = 0; i < components.arraySize; ++i)
        {
            var scriptBindItem = components.GetArrayElementAtIndex(i);
            if (null != scriptBindItem)
            {
                SerializedProperty varName = scriptBindItem.FindPropertyRelative("varName");
                if (!string.IsNullOrEmpty(varName.stringValue))
                {
                    if(varNames.Contains(varName.stringValue))
                    {
                        repeatValue = varName.stringValue;
                        return true;
                    }

                    varNames.Add(varName.stringValue);
                }
            }
        }
        return false;
    }

    protected void createInitializeCodes()
    {
        mInitializeCodeGUI.Clear();
        for (int i = 0; i < components.arraySize; ++i)
        {
            var scriptBindItem = components.GetArrayElementAtIndex(i);
            if (null != scriptBindItem)
            {
                SerializedProperty component = scriptBindItem.FindPropertyRelative("component");
                SerializedProperty varName = scriptBindItem.FindPropertyRelative("varName");
                if (!string.IsNullOrEmpty(varName.stringValue))
                {
                    var copyString = getCopyString(component, varName, false);
                    if(!string.IsNullOrEmpty(copyString))
                    {
                        mInitializeCodeGUI.Add(copyString);
                    }
                }
            }
        }
    }

    protected void createDeclareCodes()
    {
        mDeclareCodeGUI.Clear();
        for (int i = 0; i < components.arraySize; ++i)
        {
            var scriptBindItem = components.GetArrayElementAtIndex(i);
            if (null != scriptBindItem)
            {
                SerializedProperty component = scriptBindItem.FindPropertyRelative("component");
                SerializedProperty varName = scriptBindItem.FindPropertyRelative("varName");
                if (!string.IsNullOrEmpty(varName.stringValue))
                {
                    var copyString = getCopyString(component, varName, true);
                    if(!string.IsNullOrEmpty(copyString))
                    {
                        mDeclareCodeGUI.Add(copyString);
                    }
                }
            }
        }
    }
}