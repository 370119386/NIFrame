using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    public enum LogType
    {
        LT_NORMAL = 0,
        LT_WARNING,
        LT_ERROR,
        LT_PROCESS,
    }

    [System.Serializable]
    public class LogItem
    {
        public LogType eLogType;
        public string log;
    }

    public class LoggerManager : Singleton<LoggerManager>
    {
        protected List<LogItem> mLogItems = new List<LogItem>(128);
        protected List<LogItem> mShowLogItems = new List<LogItem>(128);

        public List<LogItem> LogItems
        {
            get
            {
                if(0 == Filter)
                {
                    return mLogItems;
                }

                return mShowLogItems;
            }
        }

        protected int _filter = 0;
        public int Filter
        {
            get
            {
                return _filter;
            }

            set
            {
                _filter = value;

                mShowLogItems.Clear();
                for (int i = 0; i < mLogItems.Count; ++i)
                {
                    int flag = (1 << ((int)mLogItems[i].eLogType));
                    if (0 == (flag & Filter))
                    {
                        mShowLogItems.Add(mLogItems[i]);
                    }
                }

                EventManager.Instance().SendEvent(Event.Event_LogFilterChanged);
            }
        }

        public void AddFilter(int flag)
        {
            Filter |= flag;
        }

        public void RemoveFilter(int flag)
        {
            Filter &= ~flag;
        }

        public void Log(string log)
        {
            var logItem = new LogItem { eLogType = LogType.LT_NORMAL, log = log };
            Debug.LogFormat(logItem.log);
            mLogItems.Add(logItem);
            int flag = (1 << ((int)LogType.LT_NORMAL));
            if (0 == (flag & Filter))
            {
                mShowLogItems.Add(logItem);
            }
            SendLogItemChangedEvent();
        }

        public void LogFormat(string fmt,params object[] argv)
        {
            var logItem = new LogItem { eLogType = LogType.LT_NORMAL, log = string.Format(fmt, argv) };
            Debug.LogFormat(logItem.log);
            mLogItems.Add(logItem);
            int flag = (1 << ((int)LogType.LT_NORMAL));
            if (0 == (flag & Filter))
            {
                mShowLogItems.Add(logItem);
            }
            SendLogItemChangedEvent();
        }

        public void LogWarning(string log)
        {
            var logItem = new LogItem { eLogType = LogType.LT_WARNING, log = log };
            Debug.LogWarningFormat(logItem.log);
            mLogItems.Add(logItem);
            int flag = (1 << ((int)LogType.LT_WARNING));
            if (0 == (flag & Filter))
            {
                mShowLogItems.Add(logItem);
            }
            SendLogItemChangedEvent();
        }

        public void LogWarningFormat(string fmt, params object[] argv)
        {
            var logItem = new LogItem { eLogType = LogType.LT_WARNING, log = string.Format(fmt, argv) };
            Debug.LogWarningFormat(logItem.log);
            mLogItems.Add(logItem);
            int flag = (1 << ((int)LogType.LT_WARNING));
            if (0 == (flag & Filter))
            {
                mShowLogItems.Add(logItem);
            }
            SendLogItemChangedEvent();
        }

        public void LogError(string log)
        {
            var logItem = new LogItem { eLogType = LogType.LT_ERROR, log = log };
            Debug.LogErrorFormat(logItem.log);
            mLogItems.Add(logItem);
            int flag = (1 << ((int)LogType.LT_ERROR));
            if (0 == (flag & Filter))
            {
                mShowLogItems.Add(logItem);
            }
            SendLogItemChangedEvent();
        }

        public void LogErrorFormat(string fmt, params object[] argv)
        {
            var logItem = new LogItem { eLogType = LogType.LT_ERROR, log = string.Format(fmt, argv) };
            Debug.LogErrorFormat(logItem.log);
            mLogItems.Add(logItem);
            int flag = (1 << ((int)LogType.LT_ERROR));
            if (0 == (flag & Filter))
            {
                mShowLogItems.Add(logItem);
            }
            SendLogItemChangedEvent();
        }

        public void LogProcess(string log)
        {
            var logItem = new LogItem { eLogType = LogType.LT_PROCESS, log = log };
            Debug.LogFormat(logItem.log);
            mLogItems.Add(logItem);
            int flag = (1 << ((int)LogType.LT_PROCESS));
            if (0 == (flag & Filter))
            {
                mShowLogItems.Add(logItem);
            }
            SendLogItemChangedEvent();
        }

        public void LogProcessFormat(string fmt, params object[] argv)
        {
            var logItem = new LogItem { eLogType = LogType.LT_PROCESS, log = string.Format(fmt, argv) };
            Debug.LogFormat(logItem.log);
            mLogItems.Add(logItem);
            int flag = (1 << ((int)LogType.LT_PROCESS));
            if (0 == (flag & Filter))
            {
                mShowLogItems.Add(logItem);
            }
            SendLogItemChangedEvent();
        }

        protected void SendLogItemChangedEvent()
        {
            EventManager.Instance().SendEvent(Event.Event_LogItemChanged);
        }
    }
}