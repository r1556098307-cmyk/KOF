using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameResultUI : MonoBehaviour, IEndGameObserver
{
    [Header("Result Panels - Set in Inspector")]
    [SerializeField] private GameObject resultUIPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    [Header("Buttons - Set in Inspector")]
    [SerializeField] private Button victoryBackButton;
    [SerializeField] private Button defeatBackButton;
    [SerializeField] private Button defeatRestartButton;

    // 用于验证引用是否有效
    private bool referencesValid = false;

    private void Awake()
    {
        // 第一次验证引用
        ValidateReferences();
    }

    private void Start()
    {
        // 确保引用有效
        if (!referencesValid)
        {
            FindAndAssignReferences();
        }

        InitializeUI();
        RegisterToGameManager();
    }

    private void ValidateReferences()
    {
        // 检查所有Inspector赋值的引用是否有效
        referencesValid = resultUIPanel != null &&
                         victoryPanel != null &&
                         defeatPanel != null &&
                         victoryBackButton != null &&
                         defeatBackButton != null &&
                         defeatRestartButton != null;

    
    }

    private void FindAndAssignReferences()
    {

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene!");
            return;
        }

        // 查找ResultUIPanel（包括未激活的）
        Transform resultTransform = canvas.transform.Find("ResultUIPanel");
        if (resultTransform != null)
        {
            resultUIPanel = resultTransform.gameObject;

            // 查找子面板
            Transform victoryTransform = resultTransform.Find("VictoryPanel");
            Transform defeatTransform = resultTransform.Find("DefeatPanel");

            if (victoryTransform != null)
                victoryPanel = victoryTransform.gameObject;
            if (defeatTransform != null)
                defeatPanel = defeatTransform.gameObject;

            // 查找按钮
            if (victoryPanel != null)
            {
                Transform btnTransform = victoryPanel.transform.Find("BackButton");
                if (btnTransform != null)
                    victoryBackButton = btnTransform.GetComponent<Button>();
            }

            if (defeatPanel != null)
            {
                Transform backBtnTransform = defeatPanel.transform.Find("BackButton");
                Transform restartBtnTransform = defeatPanel.transform.Find("RestartButton");

                if (backBtnTransform != null)
                    defeatBackButton = backBtnTransform.GetComponent<Button>();
                if (restartBtnTransform != null)
                    defeatRestartButton = restartBtnTransform.GetComponent<Button>();
            }

            // 重新验证
            ValidateReferences();

            if (referencesValid)
            {
                Debug.Log("GameResultUI: All references successfully found!");
            }
            else
            {
                Debug.LogError("GameResultUI: Still missing some references after search!");
                LogMissingReferences();
            }
        }
        else
        {
            Debug.LogError("ResultUIPanel not found under Canvas!");
        }
    }

    private void LogMissingReferences()
    {
        if (resultUIPanel == null) Debug.LogError("- resultUIPanel is null");
        if (victoryPanel == null) Debug.LogError("- victoryPanel is null");
        if (defeatPanel == null) Debug.LogError("- defeatPanel is null");
        if (victoryBackButton == null) Debug.LogError("- victoryBackButton is null");
        if (defeatBackButton == null) Debug.LogError("- defeatBackButton is null");
        if (defeatRestartButton == null) Debug.LogError("- defeatRestartButton is null");
    }

    private void InitializeUI()
    {
        // 初始隐藏结算界面
        if (resultUIPanel != null)
        {
            resultUIPanel.SetActive(false);
        }

        // 绑定按钮事件（先清除旧的监听器）
        if (victoryBackButton != null)
        {
            victoryBackButton.onClick.RemoveAllListeners();
            victoryBackButton.onClick.AddListener(OnBackToMenu);
        }

        if (defeatBackButton != null)
        {
            defeatBackButton.onClick.RemoveAllListeners();
            defeatBackButton.onClick.AddListener(OnBackToMenu);
        }

        if (defeatRestartButton != null)
        {
            defeatRestartButton.onClick.RemoveAllListeners();
            defeatRestartButton.onClick.AddListener(OnRestartGame);
        }
    }

    private void RegisterToGameManager()
    {
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddObserver(this);
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }
    }

    private void OnBackToMenu()
    {
        Time.timeScale = 1f;  // 恢复时间
        SceneController.Instance.TransitionToMenuScene();
    }

    private void OnRestartGame()
    {
        // 恢复时间并重新开始
        Time.timeScale = 1f;

        // 清理当前的观察者注册
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveObserver(this);
        }

        // 重新加载场景
        StartCoroutine(RestartGameCoroutine());
    }

    private IEnumerator RestartGameCoroutine()
    {
        // TODO:可选：添加加载画面
        //Debug.Log("Restarting game...");

        // 重新加载当前场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        //Debug.Log("Game restarted!");
    }

    public void EndNotify(PlayerID deadPlayerId)
    {
        ShowGameResult(deadPlayerId);
    }

    private void ShowGameResult(PlayerID deadPlayerId)
    {
        // 运行时再次检查引用
        if (!referencesValid || resultUIPanel == null || resultUIPanel.Equals(null))
        {
            Debug.LogWarning("References invalid at ShowGameResult, attempting to reacquire...");
            FindAndAssignReferences();

            if (!referencesValid)
            {
                Debug.LogError("Cannot show game result - UI references are missing!");
                return;
            }
        }

        // 显示结算界面
        resultUIPanel.SetActive(true);

        // 根据死亡玩家显示对应面板
        bool isPlayer1Victory = (deadPlayerId == PlayerID.Player2);

        if (victoryPanel != null)
            victoryPanel.SetActive(isPlayer1Victory);
        if (defeatPanel != null)
            defeatPanel.SetActive(!isPlayer1Victory);

        // 暂停游戏
        Time.timeScale = 0f;

        //Debug.Log($"Game Over! Player {(isPlayer1Victory ? "1" : "2")} wins!");
    }

    private void OnDestroy()
    {
        // 清理
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveObserver(this);
        }

        // 清理按钮监听器
        if (victoryBackButton != null)
            victoryBackButton.onClick.RemoveAllListeners();
        if (defeatBackButton != null)
            defeatBackButton.onClick.RemoveAllListeners();
        if (defeatRestartButton != null)
            defeatRestartButton.onClick.RemoveAllListeners();

        // 确保时间恢复正常
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    // 场景加载时重新初始化
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载完成后重新验证引用
        if (scene.name == SceneManager.GetActiveScene().name)
        {
            ValidateReferences();
            if (!referencesValid)
            {
                FindAndAssignReferences();
            }
        }
    }
}