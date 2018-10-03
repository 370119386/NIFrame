using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Scripts.UI;

namespace NI
{
    public class LogFrame : ClientFrame
    {
        Scripts.UI.ComUIListScript mLogger;
        UnityEngine.UI.Button mbtnClose;
        UnityEngine.UI.Toggle mFilter_0;
        UnityEngine.UI.Toggle mFilter_1;
        UnityEngine.UI.Toggle mFilter_2;
        UnityEngine.UI.Toggle mFilter_3;
        UnityEngine.UI.Text mGeneratorText;

        protected override string GetPrefabPath()
        {
            return @"UI/Prefabs/LogFrame";
        }

        protected override void _InitScriptBinder()
        {
            mLogger = mScriptBinder.GetObject("Logger") as Scripts.UI.ComUIListScript;
            mbtnClose = mScriptBinder.GetObject("btnClose") as UnityEngine.UI.Button;
            mFilter_0 = mScriptBinder.GetObject("Filter_0") as UnityEngine.UI.Toggle;
            mFilter_1 = mScriptBinder.GetObject("Filter_1") as UnityEngine.UI.Toggle;
            mFilter_2 = mScriptBinder.GetObject("Filter_2") as UnityEngine.UI.Toggle;
            mFilter_3 = mScriptBinder.GetObject("Filter_3") as UnityEngine.UI.Toggle;
            mGeneratorText = mScriptBinder.GetObject("GeneratorText") as UnityEngine.UI.Text;
        }

        protected void _InitLogList()
        {
            if (null != mLogger)
            {
                UIListTemplate.Initialize<ComLogItem>(mLogger);

                mLogger.onItemVisiable = (ComUIListElementScript item) =>
                {
                    var datas = LoggerManager.Instance().LogItems;
                    if (null != item && item.m_index >= 0 && item.m_index < datas.Count)
                    {
                        ComLogItem logItem = item.gameObjectBindScript as ComLogItem;
                        if (null != logItem)
                        {
                            logItem.OnItemVisible(datas[item.m_index]);
                        }
                    }
                };
            }
        }

        TextGenerator mGenerator;
        TextGenerationSettings mGenerationSetting;
        protected void _InitGenerator(Text t)
        {
            mGenerator = t.cachedTextGeneratorForLayout;
            mGenerationSetting = t.GetGenerationSettings(new Vector2(t.rectTransform.sizeDelta.x,0));
        }

        protected void _UnInitGenerator()
        {
            mGenerator = null;
        }

        protected void _UpdateLogList()
        {
            if (null != mLogger)
            {
                List<Vector2> elementsSize = new List<Vector2>();
                for (int i = 0; i < LoggerManager.Instance().LogItems.Count; ++i)
                {
                    var logItem = LoggerManager.Instance().LogItems[i];
                    Vector2 size = Vector2.zero;
                    if(null != logItem)
                    {
                        float fw = mGenerator.GetPreferredWidth(logItem.log, mGenerationSetting)/ mGeneratorText.pixelsPerUnit;
                        float fh = mGenerator.GetPreferredHeight(logItem.log, mGenerationSetting) / mGeneratorText.pixelsPerUnit;
                        size.x = fw;
                        size.y = fh;
                        Debug.LogErrorFormat("size={0}", size);
                    }
                    elementsSize.Add(size);
                }
                mLogger.SetElementAmount(LoggerManager.Instance().LogItems.Count,elementsSize);
            }
        }

        protected void _InitFilters()
        {
            for (int i = 0; i < 4; ++i)
            {
                var toggle = mScriptBinder.GetObject("Filter_" + i) as UnityEngine.UI.Toggle;
                if (null != toggle)
                {
                    int flag = (1 << i);
                    bool isOn = (LoggerManager.Instance().Filter & flag) == 0;
                    toggle.onValueChanged.RemoveAllListeners();
                    toggle.isOn = isOn;
                    toggle.onValueChanged.AddListener((bool bValue) =>
                    {
                        if (bValue)
                        {
                            LoggerManager.Instance().RemoveFilter(flag);
                        }
                        else
                        {
                            LoggerManager.Instance().AddFilter(flag);
                        }
                    });
                }
            }
        }

        protected override void OnOpenFrame()
        {
            _InitGenerator(mGeneratorText);
            _InitLogList();
            _UpdateLogList();
            _InitFilters();

            if (null != mbtnClose)
            {
                mbtnClose.onClick.AddListener(CloseByManager);
            }

            EventManager.Instance().RegisterEvent(Event.Event_LogItemChanged, _OnLogItemChanged);
            EventManager.Instance().RegisterEvent(Event.Event_LogFilterChanged, _OnLogFilterChanged);
        }

        protected override void OnCloseFrame()
        {
            if (null != mLogger)
            {
                UIListTemplate.UnInitialize<ComLogItem>(mLogger);
                mLogger = null;
            }
            _UnInitGenerator();

            EventManager.Instance().UnRegisterEvent(Event.Event_LogItemChanged, _OnLogItemChanged);
            EventManager.Instance().UnRegisterEvent(Event.Event_LogFilterChanged, _OnLogFilterChanged);
        }

        protected void _OnLogItemChanged(object argv)
        {
            _UpdateLogList();
        }

        protected void _OnLogFilterChanged(object argv)
        {
            _UpdateLogList();
        }
    }
}