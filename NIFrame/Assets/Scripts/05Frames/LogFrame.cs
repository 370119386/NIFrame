using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scripts.UI;

namespace NI
{
    public class LogFrame : ClientFrame
    {
        Scripts.UI.ComUIListScript mLogger;
        UnityEngine.UI.Button mbtnClose;

        protected override string GetPrefabPath()
        {
            return @"UI/Prefabs/LogFrame";
        }

        protected override void _InitScriptBinder()
        {
            mLogger = mScriptBinder.GetObject("Logger") as Scripts.UI.ComUIListScript;
            mbtnClose = mScriptBinder.GetObject("btnClose") as UnityEngine.UI.Button;
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

        protected void _UpdateLogList()
        {
            if (null != mLogger)
            {
                mLogger.SetElementAmount(LoggerManager.Instance().LogItems.Count);
            }
        }

        protected void _InitFilters()
        {
            for(int i = 0; i < 4; ++i)
            {
                var toggle = mScriptBinder.GetObject("Filter_" + i) as UnityEngine.UI.Toggle;
                if(null != toggle)
                {
                    int flag = (1 << i);
                    bool isOn = (LoggerManager.Instance().Filter & flag) == 0;
                    toggle.onValueChanged.RemoveAllListeners();
                    toggle.isOn = isOn;
                    toggle.onValueChanged.AddListener((bool bValue) =>
                    {
                        if(bValue)
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