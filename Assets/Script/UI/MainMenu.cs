using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI��ť����")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("�������")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    private SettingsMenuUI inputSystemSettingsMenuUI;
    private void Awake()
    {
        if (playButton == null) playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (settingsButton == null) settingsButton = GameObject.Find("SettingsButton")?.GetComponent<Button>();
        if (exitButton == null) exitButton = GameObject.Find("ExitButton")?.GetComponent<Button>();

        // �Զ��������
        if (mainMenuPanel == null) mainMenuPanel = GameObject.Find("MainMenuPanel");
        if (settingsPanel == null) settingsPanel = GameObject.Find("SettingsPanel");

        // �Զ���������UI������
        if (inputSystemSettingsMenuUI == null)
            inputSystemSettingsMenuUI = FindObjectOfType<SettingsMenuUI>();

        // ��鰴ť�Ƿ��ҵ�
        if (playButton == null || settingsButton == null || exitButton == null)
        {
            Debug.LogError("MainMenu: һ��������ť����δ�ҵ���");
            return;
        }
        if (mainMenuPanel == null || settingsPanel == null)
        {
            Debug.LogError("MainMenu: ���˵����������������δ�ҵ���");
            return;
        }

        if (inputSystemSettingsMenuUI == null)
        {
            Debug.LogError("MainMenu: SettingsMenuUI ���δ�ҵ���");
            return;
        }

        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void OnDestroy()
    {
        // �����¼�������
        playButton?.onClick.RemoveListener(OnPlayButtonClicked);
        settingsButton?.onClick.RemoveListener(OnSettingsButtonClicked);
        exitButton?.onClick.RemoveListener(OnExitButtonClicked);
    }

    void OnPlayButtonClicked()
    {
        // ת����SelectSceneѡ���ɫ
        if (SceneController.Instance != null)
        {
            SceneController.Instance.TransitionToSelectScene();
        }
        else
        {
            Debug.LogError("SceneControllerʵ��δ�ҵ���");
        }
    }

    void OnSettingsButtonClicked()
    {
        Debug.Log("�����ò˵�");

        if (mainMenuPanel != null && settingsPanel != null && inputSystemSettingsMenuUI != null)
        {
            mainMenuPanel.SetActive(false);  // �������˵�
            settingsPanel.SetActive(true);   // ��ʾ�������
            inputSystemSettingsMenuUI.OpenSettings();  // ��ʼ������UI
        }
        else
        {
            Debug.LogError("MainMenu: �޷������ò˵���ȱ�ٱ�Ҫ�����ã�");
        }
    }

    void OnExitButtonClicked()
    {
        Application.Quit();
        Debug.Log("�˳���Ϸ");
    }

    // ���������������ò˵������Է������˵�
    public void ReturnToMainMenu()
    {
        if (mainMenuPanel != null && settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }
}
