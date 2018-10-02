using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GG
{
    public class AnsyLoadTask
    {
        public string title = string.Empty;
        public float process = 0.0f;
        public bool finished = false;
        public int power = 1;
        public DelegateAnsyLoadTask ansyLoadTask = null;
    }

    public delegate IEnumerator DelegateAnsyLoadTask(AnsyLoadTask handle);

    public interface ISceneLoader
    {
        void AddAnsyTask(AnsyLoadTask item);
    }
}