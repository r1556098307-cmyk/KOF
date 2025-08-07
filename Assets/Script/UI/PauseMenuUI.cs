using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button resumeButton;

    [Header("设置")]
    [SerializeField] private bool disablePlayerInputWhenPaused = true;

    private PlayerInputControl inputControl;
    private bool isPaused = false;
    private ComboListUI comboListUI; // 如果出招表打开，需要先关闭

    // 用于记录暂停前的时间缩放
    private float previousTimeScale = 1f;

    void Awake()
    {
        // 初始化Input System
        inputControl = new PlayerInputControl();

        // 绑定ESC键事件
        inputControl.UI.Cancel.performed += OnEscapePressed;

        // 查找ComboListUI（可选）
        comboListUI = GetComponent<ComboListUI>();
        if (comboListUI == null)
            comboListUI = FindObjectOfType<ComboListUI>();

        // 绑定按钮事件
        SetupButtons();
    }

    void Start()
    {
        // 初始时隐藏暂停菜单
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);


        // 确保游戏开始时不是暂停状态
        isPaused = false;
    }

    void OnEnable()
    {
        inputControl?.Enable();
    }

    void OnDisable()
    {
        inputControl?.Disable();
    }

    void OnDestroy()
    {
        // 解绑事件
        if (inputControl != null)
        {
            inputControl.UI.Cancel.performed -= OnEscapePressed;
            inputControl.Dispose();
        }

        // 解绑按钮事件
        UnbindButtons();
    }

    void SetupButtons()
    {
        // 重新开始按钮
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        // 返回主菜单按钮
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        // 继续游戏按钮（取消）
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
    }

    void UnbindButtons()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveAllListeners();

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveAllListeners();

        if (resumeButton != null)
            resumeButton.onClick.RemoveAllListeners();
    }

    void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (comboListUI != null && comboListUI.IsComboListOpen())
        {
            return; 
        }


     

        // 切换暂停状态
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (pauseMenuPanel == null) return;

        isPaused = true;
        pauseMenuPanel.SetActive(true);

        // 记录并设置时间缩放
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // 切换输入模式
        if (disablePlayerInputWhenPaused)
        {
            DisableGameplayInput();
            DisablePlayerCombatSystems();
        }

        // 设置第一个按钮为选中状态（手柄支持）
        if (resumeButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    public void Resume()
    {
        if (pauseMenuPanel == null) return;

        isPaused = false;
        pauseMenuPanel.SetActive(false);

        // 恢复时间缩放
        Time.timeScale = previousTimeScale;

        // 恢复输入模式
        if (disablePlayerInputWhenPaused)
        {
            EnableGameplayInput();
            EnablePlayerCombatSystems();
        }
    }

    void OnRestartClicked()
    {
        // 恢复时间缩放
        Time.timeScale = 1f;

        SceneController.Instance.TransitionToGameScene();
    }

    void OnMainMenuClicked()
    {
        // 恢复时间缩放
        Time.timeScale = 1f;

        SceneController.Instance.TransitionToMenuScene();
    }

    void OnResumeClicked()
    {
        Resume();
    }

    void DisableGameplayInput()
    {
        // 禁用游戏输入
        if (inputControl != null)
        {
            inputControl.GamePlay.Disable();
            // UI输入保持启用以便导航菜单
            inputControl.UI.Enable();
        }
    }

    void EnableGameplayInput()
    {
        // 启用游戏输入
        if (inputControl != null)
        {
            inputControl.GamePlay.Enable();
        }
    }

    void DisablePlayerCombatSystems()
    {
        // 查找并禁用玩家的所有输入相关组件
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 禁用ComboSystem
            ComboSystem comboSystem = player.GetComponent<ComboSystem>();
            if (comboSystem != null)
            {
                comboSystem.enabled = false;
                // 清除已记录的输入，防止恢复后触发
                comboSystem.ClearInputHistory();
            }

            // 禁用PlayerController（如果有的话）
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // 禁用HumanInputProvider（如果有的话）
            HumanInputProvider humanInput = player.GetComponent<HumanInputProvider>();
            if (humanInput != null)
            {
                humanInput.enabled = false;
            }
        }
    }

    void EnablePlayerCombatSystems()
    {
        // 查找并启用玩家的所有输入相关组件
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 启用ComboSystem
            ComboSystem comboSystem = player.GetComponent<ComboSystem>();
            if (comboSystem != null)
            {
                comboSystem.enabled = true;
            }

            // 启用PlayerController
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // 启用HumanInputProvider
            HumanInputProvider humanInput = player.GetComponent<HumanInputProvider>();
            if (humanInput != null)
            {
                humanInput.enabled = true;
            }
        }
    }

    // 公共方法
    public bool IsPaused()
    {
        return isPaused;
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }
}