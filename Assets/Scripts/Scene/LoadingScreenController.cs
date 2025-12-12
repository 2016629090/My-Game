using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI引用")]
    public Image loadingImage;
    public Material loadingMaterial;
    public Slider progressBar;

    [Header("加载设置")]
    public float minLoadingTime = 5f;
    public string sceneToLoad = "MainScene";

    private AsyncOperation loadingOperation;

    void Start()
    {
        // 创建材质实例（避免修改原始材质）
        Material materialInstance = new Material(loadingMaterial);
        loadingImage.material = materialInstance;

        // 开始加载
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // 开始异步加载
        loadingOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneToLoad);
        loadingOperation.allowSceneActivation = false; // 不自动跳转

        float timer = 0f;
        float displayProgress = 0f;

        while (timer < minLoadingTime || displayProgress < 0.95f)
        {
            timer += Time.deltaTime;

            // 计算显示进度（比实际稍快，体验更好）
            float targetProgress = Mathf.Min(timer / minLoadingTime, loadingOperation.progress / 0.9f);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.deltaTime * 0.5f);

            // 更新Shader进度
            if (loadingImage.material != null)
            {
                loadingImage.material.SetFloat("_Progress", displayProgress);

                // 动态调整参数
                float desaturate = Mathf.Lerp(0.9f, 0.2f, displayProgress);
                loadingImage.material.SetFloat("_Desaturate", desaturate);

                float contrast = Mathf.Lerp(0.7f, 1.2f, displayProgress);
                loadingImage.material.SetFloat("_Contrast", contrast);
            }

            // 更新UI进度条
            if (progressBar != null)
                progressBar.value = displayProgress;

            yield return null;
        }

        // 加载完成，等待玩家输入
        displayProgress = 1f;
        loadingImage.material.SetFloat("_Progress", 1f);

        // 显示"按任意键继续"
        yield return StartCoroutine(WaitForPlayerInput());

        // 激活场景
        loadingOperation.allowSceneActivation = true;
    }

    IEnumerator WaitForPlayerInput()
    {
        // 这里添加"按任意键继续"的UI显示逻辑
        GameObject continuePrompt = new GameObject("ContinuePrompt");
        // ... 创建UI文字

        while (!Input.anyKeyDown)
        {
            // 勇士发光脉动效果
            float pulse = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
            loadingImage.material.SetFloat("_WarriorGlow", pulse);

            yield return null;
        }
    }

    void OnDestroy()
    {
        // 清理材质实例
        if (loadingImage.material != null)
            Destroy(loadingImage.material);
    }
}