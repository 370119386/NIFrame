using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NI
{
    public class ComLogItem : MonoBehaviour
    {
        public Text mText;
        public ComColors mColors;

        public void OnItemVisible(LogItem value)
        {
            if(null != value)
            {
                if (null != mText)
                {
                    mText.text = value.log;
                }

                if (null != mColors)
                {
                    mColors.SetTextColor(mText,(int)value.eLogType);
                }
            }
        }
    }
}