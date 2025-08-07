using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneAudioTrigger : MonoBehaviour
{
    [Header("������Ч����")]
    [SerializeField] private string sceneBGM;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float fadeTime = 2f;

    private void Start()
    {
        if (playOnStart && !string.IsNullOrEmpty(sceneBGM))
        {
            // �ӳ�һִ֡�У�ȷ��AudioManager�Ѿ���ʼ��
            StartCoroutine(PlaySceneBGM());
        }

        // ע�᳡���л��¼�
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private System.Collections.IEnumerator PlaySceneBGM()
    {
        yield return null; // �ȴ�һ֡

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