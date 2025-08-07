using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI����")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button resumeButton;

    [Header("����")]
    [SerializeField] private bool disablePlayerInputWhenPaused = true;

    private PlayerInputControl inputControl;
    private bool isPaused = false;
    private ComboListUI comboListUI; // ������б�򿪣���Ҫ�ȹر�

    // ���ڼ�¼��ͣǰ��ʱ������
    private float previousTimeScale = 1f;

    void Awake()
    {
        // ��ʼ��Input System
        inputControl = new PlayerInputControl();

        // ��ESC���¼�
        inputControl.UI.Cancel.performed += OnEscapePressed;

        // ����ComboListUI����ѡ��
        comboListUI = GetComponent<ComboListUI>();
        if (comboListUI == null)
            comboListUI = FindObjectOfType<ComboListUI>();

        // �󶨰�ť�¼�
        SetupButtons();
    }

    void Start()
    {
        // ��ʼʱ������ͣ�˵�
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);


        // ȷ����Ϸ��ʼʱ������ͣ״̬
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
        // ����¼�
        if (inputControl != null)
        {
            inputControl.UI.Cancel.performed -= OnEscapePressed;
            inputControl.Dispose();
        }

        // ���ť�¼�
        UnbindButtons();
    }

    void SetupButtons()
    {
        // ���¿�ʼ��ť
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        // �������˵���ť
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        // ������Ϸ��ť��ȡ����
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


     

        // �л���ͣ״̬
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

        // ��¼������ʱ������
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // �л�����ģʽ
        if (disablePlayerInputWhenPaused)
        {
            DisableGameplayInput();
            DisablePlayerCombatSystems();
        }

        // ���õ�һ����ťΪѡ��״̬���ֱ�֧�֣�
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

        // �ָ�ʱ������
        Time.timeScale = previousTimeScale;

        // �ָ�����ģʽ
        if (disablePlayerInputWhenPaused)
        {
            EnableGameplayInput();
            EnablePlayerCombatSystems();
        }
    }

    void OnRestartClicked()
    {
        // �ָ�ʱ������
        Time.timeScale = 1f;

        SceneController.Instance.TransitionToGameScene();
    }

    void OnMainMenuClicked()
    {
        // �ָ�ʱ������
        Time.timeScale = 1f;

        SceneController.Instance.TransitionToMenuScene();
    }

    void OnResumeClicked()
    {
        Resume();
    }

    void DisableGameplayInput()
    {
        // ������Ϸ����
        if (inputControl != null)
        {
            inputControl.GamePlay.Disable();
            // UI���뱣�������Ա㵼���˵�
            inputControl.UI.Enable();
        }
    }

    void EnableGameplayInput()
    {
        // ������Ϸ����
        if (inputControl != null)
        {
            inputControl.GamePlay.Enable();
        }
    }

    void DisablePlayerCombatSystems()
    {
        // ���Ҳ�������ҵ���������������
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // ����ComboSystem
            ComboSystem comboSystem = player.GetComponent<ComboSystem>();
            if (comboSystem != null)
            {
                comboSystem.enabled = false;
                // ����Ѽ�¼�����룬��ֹ�ָ��󴥷�
                comboSystem.ClearInputHistory();
            }

            // ����PlayerController������еĻ���
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // ����HumanInputProvider������еĻ���
            HumanInputProvider humanInput = player.GetComponent<HumanInputProvider>();
            if (humanInput != null)
            {
                humanInput.enabled = false;
            }
        }
    }

    void EnablePlayerCombatSystems()
    {
        // ���Ҳ�������ҵ���������������
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // ����ComboSystem
            ComboSystem comboSystem = player.GetComponent<ComboSystem>();
            if (comboSystem != null)
            {
                comboSystem.enabled = true;
            }

            // ����PlayerController
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // ����HumanInputProvider
            HumanInputProvider humanInput = player.GetComponent<HumanInputProvider>();
            if (humanInput != null)
            {
                humanInput.enabled = true;
            }
        }
    }

    // ��������
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