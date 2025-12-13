using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YY.RPGgame
{
    public enum SceneType
    {
        MainMenu,
        WorldMap,
        TestWorld
    }
    public class SceneManager : Singleton<SceneManager>
    {
        [SerializeField] private string loadingSceneName;
        [SerializeField] private float minLoadingTime = 5f;

        private SceneType currentSceneType;
        private string targetSceneName;
        private float loadingStartTime;
        private bool isLoading = false;

        //场景加载事件
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<float> OnLoadingProgress;

        protected override void Initialize()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void LoadSceneAsync(string sceneName,SceneType sceneType, object sceneData = null)
        {
            if (isLoading) return;
            isLoading = true;

            targetSceneName = sceneName;
            currentSceneType = sceneType;

            // 通知UI系统显示加载界面
            //UIManager.Instance?.ShowLoadingScreen();

            // 开始加载流程
            StartCoroutine(LoadingCoroutine(sceneName, sceneData));
        }

        private IEnumerator LoadingCoroutine(string sceneName, object sceneData)
        {
            loadingStartTime = Time.time;

            // 通知场景加载开始
            OnSceneLoadStarted?.Invoke(sceneName);

            //if (!string.IsNullOrEmpty(loadingSceneName))
            //{
            //    var loadOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(loadingSceneName);
            //    yield return WaitForLoading(loadOp);
            //}

            // 异步加载目标场景
            var targetLoadOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            targetLoadOp.allowSceneActivation = false;

            // 模拟最小加载时间，确保加载界面可见
            while (targetLoadOp.progress < 0.9f ||
                   (Time.time - loadingStartTime) < minLoadingTime)
            {
                float progress = Mathf.Clamp01(targetLoadOp.progress / 0.9f);
                float timeProgress = (Time.time - loadingStartTime) / minLoadingTime;
                float totalProgress = Mathf.Min(progress, timeProgress);

                OnLoadingProgress?.Invoke(totalProgress);
                yield return null;
            }

            //场景数据传递
            if (sceneData != null)
            {
                // 可以存储到GameManager或直接传递给场景控制器
                //GameManager.Instance?.SetSceneData(sceneData);
            }

            // 激活场景
            targetLoadOp.allowSceneActivation = true;
            yield return new WaitUntil(() => targetLoadOp.isDone);

            // 场景加载完成后的初始化
            //yield return InitializeScene();

            // 通知完成
            isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);

            // 隐藏加载界面（通过UI系统）
            //UIManager.Instance?.HideLoadingScreen();
        }
        private IEnumerator WaitForLoading(AsyncOperation op)
        {
            while (!op.isDone)
            {
                yield return null;
            }
        }

        //private IEnumerator InitializeScene()
        //{
        //    // 根据场景类型执行不同的初始化逻辑
        //    switch (currentSceneType)
        //    {
        //        case SceneType.MainMenu:
        //            // 初始化主菜单
        //            UIManager.Instance?.ShowMainMenu();
        //            break;

        //        case SceneType.WorldMap:
        //            // 初始化世界地图
        //            UIManager.Instance?.ShowHUD();
        //            GameManager.Instance?.InitializeWorldMap();
        //            break;

        //        case SceneType.Battle:
        //            // 初始化战斗
        //            UIManager.Instance?.ShowBattleUI();
        //            GameManager.Instance?.InitializeBattle();
        //            break;

        //        case SceneType.Town:
        //            // 初始化城镇
        //            UIManager.Instance?.ShowHUD();
        //            GameManager.Instance?.InitializeTown();
        //            break;
        //    }

        //    yield return null;
        //}

        /// <summary>
        /// 重新加载当前场景
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            LoadSceneAsync(currentScene, currentSceneType);
        }

        /// <summary>
        /// 获取当前场景类型
        /// </summary>
        public SceneType GetCurrentSceneType()
        {
            return currentSceneType;
        }
    }
}
