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

    // ������֤�����Ƿ���Ч
    private bool referencesValid = false;

    private void Awake()
    {
        // ��һ����֤����
        ValidateReferences();
    }

    private void Start()
    {
        // ȷ��������Ч
        if (!referencesValid)
        {
            FindAndAssignReferences();
        }

        InitializeUI();
        RegisterToGameManager();
    }

    private void ValidateReferences()
    {
        // �������Inspector��ֵ�������Ƿ���Ч
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

        // ����ResultUIPanel������δ����ģ�
        Transform resultTransform = canvas.transform.Find("ResultUIPanel");
        if (resultTransform != null)
        {
            resultUIPanel = resultTransform.gameObject;

            // ���������
            Transform victoryTransform = resultTransform.Find("VictoryPanel");
            Transform defeatTransform = resultTransform.Find("DefeatPanel");

            if (victoryTransform != null)
                victoryPanel = victoryTransform.gameObject;
            if (defeatTransform != null)
                defeatPanel = defeatTransform.gameObject;

            // ���Ұ�ť
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

            // ������֤
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
        // ��ʼ���ؽ������
        if (resultUIPanel != null)
        {
            resultUIPanel.SetActive(false);
        }

        // �󶨰�ť�¼���������ɵļ�������
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
        // ע�ᵽGameManager
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
        Time.timeScale = 1f;  // �ָ�ʱ��
        SceneController.Instance.TransitionToMenuScene();
    }

    private void OnRestartGame()
    {
        // �ָ�ʱ�䲢���¿�ʼ
        Time.timeScale = 1f;

        // ����ǰ�Ĺ۲���ע��
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveObserver(this);
        }

        // ���¼��س���
        StartCoroutine(RestartGameCoroutine());
    }

    private IEnumerator RestartGameCoroutine()
    {
        // TODO:��ѡ����Ӽ��ػ���
        //Debug.Log("Restarting game...");

        // ���¼��ص�ǰ����
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
        // ����ʱ�ٴμ������
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

        // ��ʾ�������
        resultUIPanel.SetActive(true);

        // �������������ʾ��Ӧ���
        bool isPlayer1Victory = (deadPlayerId == PlayerID.Player2);

        if (victoryPanel != null)
            victoryPanel.SetActive(isPlayer1Victory);
        if (defeatPanel != null)
            defeatPanel.SetActive(!isPlayer1Victory);

        // ��ͣ��Ϸ
        Time.timeScale = 0f;

        //Debug.Log($"Game Over! Player {(isPlayer1Victory ? "1" : "2")} wins!");
    }

    private void OnDestroy()
    {
        // ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveObserver(this);
        }

        // ����ť������
        if (victoryBackButton != null)
            victoryBackButton.onClick.RemoveAllListeners();
        if (defeatBackButton != null)
            defeatBackButton.onClick.RemoveAllListeners();
        if (defeatRestartButton != null)
            defeatRestartButton.onClick.RemoveAllListeners();

        // ȷ��ʱ��ָ�����
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    // ��������ʱ���³�ʼ��
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
        // ����������ɺ�������֤����
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