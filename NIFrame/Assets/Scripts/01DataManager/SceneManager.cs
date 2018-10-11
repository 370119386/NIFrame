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

        public void Clear()
        {
            argv = null;
            begin = null;
            end = null;
            loadingTask = null;
        }
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

            _current = Create(iId);

            object argv = null;
            if(null != _param)
            {
                argv = _param.argv;
            }

            if(null == _current || !_current.Create(argv))
            {
                LoggerManager.Instance().LogErrorFormat("Create Scene Failed For Id = {0}", iId);
                return;
            }

            StartCoroutine(AnsySwitchScene());
        }

        protected Scene Create(int iId)
        {
            var sceneItem = TableManager.Instance().GetTableItem<ProtoTable.SceneTable>(iId);
            if(null != sceneItem)
            {
                return new Scene(sceneItem);
            }

            return null;
        }

        protected IEnumerator AnsySwitchScene()
        {
            var ansyOperation = Resources.UnloadUnusedAssets();
            while(!ansyOperation.isDone)
            {
                yield return null;
            }

            System.GC.Collect();

#if UNITY_TEST_ALIVED_OBJECT
            AssetLoaderManager.Instance().ReportAlivedObject();
#endif

            if (null != _param && null != _param.loadingTask)
            {
                yield return _param.loadingTask;
            }

            if(_current.HasAnsyTask)
            {
                yield return _current.LoadAnsyTask();
            }

            _current.Enter();

            if (null != _param && null != _param.end)
            {
                _param.end.Invoke();
            }

            if(null != _param)
            {
                _param.Clear();
                _param = null;
            }
        }
    }
}