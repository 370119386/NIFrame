using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    public class SystemManager : Singleton<SystemManager>
    {
        public GameFrameWork FrameHandle
        {
            get;private set;
        }

        public void Initialize(object argv)
        {
            FrameHandle = argv as GameFrameWork;
        }
    }
}