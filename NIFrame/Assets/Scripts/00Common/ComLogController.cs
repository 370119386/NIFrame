using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    public class ComLogController : MonoBehaviour
    {
        public void OnOpenLogpanel()
        {
            UIManager.Instance().OpenFrame<LogFrame>(1, 1);
        }
    }
}