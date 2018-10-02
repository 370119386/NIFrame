using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GG
{
    class ComFps : MonoBehaviour
    {
        public Text text = null;
        float mStart = 0.0f;
        int mFrames = 0;
        // Use this for initialization
        void Start()
        {
            mStart = Time.time;
            mFrames = 0;
        }

        // Update is called once per frame
        void Update()
        {
            ++mFrames;
            if(Time.time >= mStart + 1.0f)
            {
                if(null != text)
                {
                    text.text = string.Format("FPS = {0:F2}", 1.0f / ((Time.time - mStart) / mFrames));
                }
                mStart = Time.time;
                mFrames = 0;
            }
        }
    }
}