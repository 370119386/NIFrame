using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NI
{
    public delegate void OnBeginLoading();
    public delegate void OnEndLoading();

    public class SceneParam
    {
        public object argv;
        public OnBeginLoading begin;
        public OnEndLoading end;
        public IEnumerator loadingTask;
    }

    public class SceneManager : MonoBehaviour
    {
        protected static SceneManager _Instance = null;
        protected int _sceneId = -1;
        protected SceneParam _param = null;
        protected Scene _current = null;

        void Start()
        {
            DontDestroyOnLoad(this);
        }

        public static void Create()
        {
            if(null == _Instance)
            {
                var goHandle = new GameObject("SceneManager",typeof(SceneManager));
                _Instance = goHandle.GetComponent<SceneManager>();
            }
        }

        public static SceneManager Instance()
        {
            return _Instance;
        }

        public void SwitchScene(int iId, SceneParam param = null)
        {
            _sceneId = iId;
            _param = param;

            StopAllCoroutines();

            if(null != _current)
            {
                _current.Exit();
                _current = null;
            }

            if (null != param && null != param.begin)
            {
                param.begin.Invoke();
                param.begin = null;
            }

            System.GC.Collect();

            _current = Create(iId);

            if(null == _current || !_current.Create(iId))
            {
                LoggerManager.Instance().LogErrorFormat("Create Scene Failed For Id = {0}", iId);
                return;
            }

            StartCoroutine(AnsySwitchScene());
        }

        protected Scene Create(int iId)
        {
            return new Scene();
        }

        protected IEnumerator AnsySwitchScene()
        {
            if(null != _param && null != _param.loadingTask)
            {
                yield return _param.loadingTask;
            }

            if(_current.HasAnsyTask)
            {
                yield return _current.LoadAnsyTask();
            }

            _current.Enter();

            if (null != _param.end)
            {
                _param.end.Invoke();
            }
        }
    }
}