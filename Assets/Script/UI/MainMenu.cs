using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI按钮引用")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("面板引用")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    private SettingsMenuUI inputSystemSettingsMenuUI;
    private void Awake()
    {
        if (playButton == null) playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (settingsButton == null) settingsButton = GameObject.Find("SettingsButton")?.GetComponent<Button>();
        if (exitButton == null) exitButton = GameObject.Find("ExitButton")?.GetComponent<Button>();

        // 自动查找面板
        if (mainMenuPanel == null) mainMenuPanel = GameObject.Find("MainMenuPanel");
        if (settingsPanel == null) settingsPanel = GameObject.Find("SettingsPanel");

        // 自动查找设置UI控制器
        if (inputSystemSettingsMenuUI == null)
            inputSystemSettingsMenuUI = FindObjectOfType<SettingsMenuUI>();

        // 检查按钮是否找到
        if (playButton == null || settingsButton == null || exitButton == null)
        {
            Debug.LogError("MainMenu: 一个或多个按钮引用未找到！");
            return;
        }
        if (mainMenuPanel == null || settingsPanel == null)
        {
            Debug.LogError("MainMenu: 主菜单面板或设置面板引用未找到！");
            return;
        }

        if (inputSystemSettingsMenuUI == null)
        {
            Debug.LogError("MainMenu: SettingsMenuUI 组件未找到！");
            return;
        }

        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void OnDestroy()
    {
        // 清理事件监听器
        playButton?.onClick.RemoveListener(OnPlayButtonClicked);
        settingsButton?.onClick.RemoveListener(OnSettingsButtonClicked);
        exitButton?.onClick.RemoveListener(OnExitButtonClicked);
    }

    void OnPlayButtonClicked()
    {
        // 转换到SelectScene选择角色
        if (SceneController.Instance != null)
        {
            SceneController.Instance.TransitionToSelectScene();
        }
        else
        {
            Debug.LogError("SceneController实例未找到！");
        }
    }

    void OnSettingsButtonClicked()
    {
        Debug.Log("打开设置菜单");

        if (mainMenuPanel != null && settingsPanel != null && inputSystemSettingsMenuUI != null)
        {
            mainMenuPanel.SetActive(false);  // 隐藏主菜单
            settingsPanel.SetActive(true);   // 显示设置面板
            inputSystemSettingsMenuUI.OpenSettings();  // 初始化设置UI
        }
        else
        {
            Debug.LogError("MainMenu: 无法打开设置菜单，缺少必要的引用！");
        }
    }

    void OnExitButtonClicked()
    {
        Application.Quit();
        Debug.Log("退出游戏");
    }

    // 公共方法，供设置菜单调用以返回主菜单
    public void ReturnToMainMenu()
    {
        if (mainMenuPanel != null && settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }
}
