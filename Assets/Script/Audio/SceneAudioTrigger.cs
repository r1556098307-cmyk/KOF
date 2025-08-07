using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneAudioTrigger : MonoBehaviour
{
    [Header("场景音效设置")]
    [SerializeField] private string sceneBGM;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float fadeTime = 2f;

    private void Start()
    {
        if (playOnStart && !string.IsNullOrEmpty(sceneBGM))
        {
            // 延迟一帧执行，确保AudioManager已经初始化
            StartCoroutine(PlaySceneBGM());
        }

        // 注册场景切换事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private System.Collections.IEnumerator PlaySceneBGM()
    {
        yield return null; // 等待一帧

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(sceneBGM, true, fadeTime);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnSceneChanged(scene.name);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}