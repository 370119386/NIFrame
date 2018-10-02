using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.UI
{
    public class UIListTemplate
    {
        public static void Initialize<T>(ComUIListScript comUIListScript) where T : Component
        {
            if (null != comUIListScript)
            {
                comUIListScript.Initialize();
                comUIListScript.onBindItem = (GameObject go) =>
                {
                    if (null != go)
                    {
                        return go.GetComponent<T>();
                    }
                    return null;
                };
            }
        }

        public static void UnInitialize<T>(ComUIListScript comUIListScript) where T : Component
        {
            if (null != comUIListScript)
            {
                comUIListScript.onBindItem = null;
                comUIListScript.onItemVisiable = null;
                comUIListScript.onItemSelected = null;
                comUIListScript.onItemChageDisplay = null;
                comUIListScript.OnItemRecycle = null;
            }
        }
    }
}