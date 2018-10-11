using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    public delegate void CBInvoke();
    class InvokeManager : Singleton<InvokeManager>
    {
        static int ms_handle_id = 0;

        const int FLAG_REPEAT_FOREVER = (1 << 1);
        const int FLAG_GLOBAL = (1 << 2);
        const int FLAG_REMOVE = (1 << 3);

        int m_fps = 30;

        class InvokeItem
        {
            public int flag = 0;
            public int iId = -1;
            public int repeat = 1;
            public int delay = 0;
            public int interval = 0;
            public int repeat_interval = 0;
            public CBInvoke cbInvoke = null;
            public void Reset()
            {
                flag = 0;
                iId = -1;
                repeat = 1;
                delay = 0;
                interval = 0;
                repeat_interval = 0;
                cbInvoke = null;
            }
        }
        List<InvokeItem> m_actives = new List<InvokeItem>(32);
        List<InvokeItem> m_recycled = new List<InvokeItem>(32);

        public void Clear(bool bGlobal = false)
        {
            LoggerManager.Instance().LogProcessFormat("Clear All Invokes ... bGlobal = {0}", bGlobal);

            if (bGlobal)
            {
                for (int i = 0; i < m_actives.Count; ++i)
                {
                    m_actives[i].Reset();
                    m_recycled.Add(m_actives[i]);
                }
                m_actives.Clear();
            }
            else
            {
                for (int i = 0; i < m_actives.Count; ++i)
                {
                    var item = m_actives[i];
                    if((item.flag & FLAG_GLOBAL) == 0)
                    {
                        item.Reset();
                        m_actives.RemoveAt(i--);
                        m_recycled.Add(item);
                    }
                }
            }
        }

        public void Initialize(int fps = 30)
        {
            Clear(false);
            this.m_fps = fps;
        }

        public int InvokeOnce(float delay, CBInvoke cbInvoke)
        {
            if(null != cbInvoke)
            {
                InvokeItem item = null;
                if(m_recycled.Count > 0)
                {
                    item = m_recycled[0];
                    m_recycled.RemoveAt(0);
                }
                else
                {
                    item = new InvokeItem();
                }
                item.flag = 0;
                item.iId = ++ms_handle_id;
                item.repeat = 1;
                item.interval = 0;
                item.repeat_interval = 0;
                item.delay = (int)delay * m_fps;
                item.cbInvoke = cbInvoke;
                m_actives.Add(item);
                return item.iId;
            }
            return -1;
        }

        public int InvokeRepeate(float delay, float interval,CBInvoke cbInvoke,int repeat = -1,bool global = false)
        {
            if (null != cbInvoke)
            {
                InvokeItem item = null;
                if (m_recycled.Count > 0)
                {
                    item = m_recycled[0];
                    m_recycled.RemoveAt(0);
                }
                else
                {
                    item = new InvokeItem();
                }
                item.flag = 0;
                if(repeat < 0)
                {
                    item.flag |= FLAG_REPEAT_FOREVER;
                }
                if (global)
                {
                    item.flag |= FLAG_GLOBAL;
                }
                item.iId = ++ms_handle_id;
                item.repeat = repeat;
                item.delay = (int)delay * m_fps;
                item.interval = (int)interval * m_fps;
                item.repeat_interval = 0;
                item.cbInvoke = cbInvoke;
                m_actives.Add(item);
                return item.iId;
            }
            return -1;
        }

        public void Update()
        {
            for (int i = 0; i < m_actives.Count; ++i)
            {
                var item = m_actives[i];

                if((item.flag & FLAG_REMOVE) == FLAG_REMOVE)
                {
                    continue;
                }

                if(item.delay > 0)
                {
                    --item.delay;
                    continue;
                }

                if(item.repeat_interval > 0)
                {
                    --item.repeat_interval;
                    continue;
                }

                if((item.flag & FLAG_REPEAT_FOREVER) == FLAG_REPEAT_FOREVER)
                {
                    if(null != item.cbInvoke)
                    {
                        item.cbInvoke();
                    }
                    item.repeat_interval = item.interval;
                }
                else
                {
                    if(item.repeat > 0)
                    {
                        if (null != item.cbInvoke)
                        {
                            item.cbInvoke();
                        }
                        item.repeat_interval = item.interval;
                        --item.repeat;
                    }
                    if(item.repeat == 0)
                    {
                        item.flag |= FLAG_REMOVE;
                    }
                }
            }

            for(int i = 0; i < m_actives.Count; ++i)
            {
                var item = m_actives[i];
                if((item.flag & FLAG_REMOVE) == FLAG_REMOVE)
                {
                    m_actives.RemoveAt(i--);
                    item.Reset();
                    m_recycled.Add(item);
                }
            }
        }
    }
}