using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YY.RPGgame
{
    /// <summary>
    /// 场景管理器，处理加载场景，玩家传送
    /// </summary>
    public class SceneManager : Singleton<SceneManager>
    {
        [SerializeField] private string loadingSceneName;
        [SerializeField] private float minLoadingTime = 5f;

        private string targetSceneName;
        private float loadingStartTime;
        private bool isLoading = false;
        private CancellationTokenSource _loadingCts;

        //场景加载事件
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<float> OnLoadingProgress;

        protected (Vector3 postion, Quaternion rotation)? m_nextSceneCoordinates;

        protected override void Initialize()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 设置下一场景中玩家的位置和旋转
        /// </summary>
        /// <param name="postion">世界空间中的位置</param>
        /// <param name="rotation">世界空间中的旋转</param>
        public virtual void SetNextSceneCoordinates(Vector3 postion, Vector3 rotation)
        {
            m_nextSceneCoordinates = new()
            {
                postion = postion,
                rotation = Quaternion.Euler(rotation)
            };
        }

        /// <summary>
        /// 将玩家传送到下一个场景中的坐标
        /// </summary>
        public virtual void TeleportPlayer()
        {

        }

        public void LoadSceneAsync(string sceneName, object sceneData = null)
        {
            if (isLoading) return;
            isLoading = true;

            targetSceneName = sceneName;

            // 取消之前的加载（如果有）
            CancelCurrentLoading();
            _loadingCts = new CancellationTokenSource();

            // 通知UI系统显示加载界面
            //UIManager.Instance?.ShowLoadingScreen();

            // 开始加载流程（使用 UniTask 替换协程）
            LoadingUniTask(sceneName, sceneData, _loadingCts.Token).Forget();
        }

        private async UniTaskVoid LoadingUniTask(string sceneName, object sceneData, CancellationToken cancellationToken)
        {
            loadingStartTime = Time.time;

            // 通知场景加载开始
            OnSceneLoadStarted?.Invoke(sceneName);

            //if (!string.IsNullOrEmpty(loadingSceneName))
            //{
            //    var loadOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(loadingSceneName);
            //    await loadOp.ToUniTask(cancellationToken: cancellationToken);
            //}

            // 异步加载目标场景
            var targetLoadOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            targetLoadOp.allowSceneActivation = false;

            // 模拟最小加载时间，确保加载界面可见
            while (targetLoadOp.progress < 0.9f ||
                   (Time.time - loadingStartTime) < minLoadingTime)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    isLoading = false;
                    return;
                }

                float progress = Mathf.Clamp01(targetLoadOp.progress / 0.9f);
                float timeProgress = (Time.time - loadingStartTime) / minLoadingTime;
                float totalProgress = Mathf.Min(progress, timeProgress);

                OnLoadingProgress?.Invoke(totalProgress);

                // 替换 yield return null
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            //场景数据传递
            if (sceneData != null)
            {
                // 可以存储到GameManager或直接传递给场景控制器
                //GameManager.Instance?.SetSceneData(sceneData);
            }

            // 激活场景
            targetLoadOp.allowSceneActivation = true;

            // 替换 yield return new WaitUntil(() => targetLoadOp.isDone);
            await targetLoadOp.ToUniTask(cancellationToken: cancellationToken);

            // 场景加载完成后的初始化
            // await InitializeScene();

            // 通知完成
            isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);

            // 隐藏加载界面（通过UI系统）
            //UIManager.Instance?.HideLoadingScreen();
        }

        /// <summary>
        /// 取消当前加载
        /// </summary>
        private void CancelCurrentLoading()
        {
            if (_loadingCts != null && !_loadingCts.IsCancellationRequested)
            {
                _loadingCts.Cancel();
                _loadingCts.Dispose();
            }
            _loadingCts = null;
        }

        /// <summary>
        /// 重新加载当前场景
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            LoadSceneAsync(currentScene);
        }

        void OnDestroy()
        {
           CancelCurrentLoading();
        }
    }
}