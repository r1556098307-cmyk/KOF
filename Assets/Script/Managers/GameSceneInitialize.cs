using UnityEngine;
using System.Collections;

public class GameSceneInitializer : Singleton<GameSceneInitializer>
{
    [Header("��ɫԤ����")]
    [SerializeField] private GameObject kiritoPrefab;
    [SerializeField] private GameObject misakaPrefab;

    [Header("����λ��")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;

    [Header("AI�����ļ�")]
    [SerializeField] private AIConfig easyAIConfig;
    [SerializeField] private AIConfig normalAIConfig;
    [SerializeField] private AIConfig hardAIConfig;

    // ��ɫʵ��
    private GameObject playerCharacter;
    private GameObject enemyCharacter;

    // ����Ƿ��ѳ�ʼ���ı�־
    private bool isInitialized = false;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
        {
            ValidateReferences();
        }
    }

    void Start()
    {
        // ֻ�е�ǰʵ����ִ�г�ʼ��
        if (Instance == this && !isInitialized)
        {
            InitializeGame();
            isInitialized = true;
        }
    }

    void ValidateReferences()
    {
        // ��֤��Ҫ�������Ƿ�����
        if (kiritoPrefab == null || misakaPrefab == null)
        {
            Debug.LogError("��ɫԤ����δ���ã�");
        }

        if (playerSpawnPoint == null || enemySpawnPoint == null)
        {
            Debug.LogError("���ɵ�δ���ã�");
        }

        if (easyAIConfig == null || normalAIConfig == null || hardAIConfig == null)
        {
            Debug.LogError("AI�����ļ�δ���ã�");
        }
    }

    void InitializeGame()
    {
        // ��PlayerPrefs��ȡѡ��
        int playerCharacterIndex = PlayerPrefs.GetInt("PlayerCharacter", 0);
        int computerCharacterIndex = PlayerPrefs.GetInt("ComputerCharacter", 0);

        // �������ж�ȡ�Ѷȣ�����еĻ���
        int difficultyIndex = 1; // Ĭ����ͨ�Ѷ�
        if (PlayerPrefs.HasKey("GameSettings"))
        {
            string settingsJson = PlayerPrefs.GetString("GameSettings");
            try
            {
                var settings = JsonUtility.FromJson<GameSettingsData>(settingsJson);
                difficultyIndex = (int)settings.difficultyLevel;
            }
            catch
            {
                Debug.LogWarning("�޷���ȡ��Ϸ���ã�ʹ��Ĭ���Ѷ�");
            }
        }

        Debug.Log($"��Ϸ��ʼ�� - ��ҽ�ɫ: {(CharacterType)playerCharacterIndex}, " +
                  $"���Խ�ɫ: {(CharacterType)computerCharacterIndex}, " +
                  $"�Ѷ�: {(DifficultyLevel)difficultyIndex}");

        // ���ɽ�ɫ
        SpawnPlayer(playerCharacterIndex);
        SpawnEnemy(computerCharacterIndex, difficultyIndex);
    }

    void SpawnPlayer(int characterIndex)
    {
        GameObject prefabToSpawn = GetCharacterPrefab(characterIndex);

        if (prefabToSpawn != null && playerSpawnPoint != null)
        {
            playerCharacter = Instantiate(prefabToSpawn, playerSpawnPoint.position, Quaternion.identity);
            playerCharacter.name = "Player";
            playerCharacter.tag = "Player";

            // ȷ����ҽ�ɫ��������ȷ
            SetupPlayerCharacter(playerCharacter);

            Debug.Log($"������ҽ�ɫ: {(CharacterType)characterIndex}");
        }
        else
        {
            Debug.LogError("�޷�������ҽ�ɫ�����Ԥ��������ɵ��Ƿ�����");
        }
    }

    void SpawnEnemy(int characterIndex, int difficultyIndex)
    {
        GameObject prefabToSpawn = GetCharacterPrefab(characterIndex);

        if (prefabToSpawn != null && enemySpawnPoint != null)
        {
            enemyCharacter = Instantiate(prefabToSpawn, enemySpawnPoint.position, Quaternion.identity);
            enemyCharacter.name = "Enemy";
            enemyCharacter.tag = "Enemy";

            // ����ΪAI����
            SetupAICharacter(enemyCharacter, difficultyIndex);

            Debug.Log($"���ɵ��˽�ɫ: {(CharacterType)characterIndex}, �Ѷ�: {(DifficultyLevel)difficultyIndex}");
        }
        else
        {
            Debug.LogError("�޷����ɵ��˽�ɫ�����Ԥ��������ɵ��Ƿ�����");
        }
    }

    GameObject GetCharacterPrefab(int characterIndex)
    {
        switch ((CharacterType)characterIndex)
        {
            case CharacterType.Kirito:
                return kiritoPrefab;
            case CharacterType.Misaka:
                return misakaPrefab;
            default:
                Debug.LogWarning($"δ֪�Ľ�ɫ����: {characterIndex}��ʹ��Ĭ�Ͻ�ɫ");
                return kiritoPrefab;
        }
    }

    void SetupPlayerCharacter(GameObject character)
    {
        // ����PlayerIDΪPlayer1
        PlayerController playerController = character.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.PlayerId = PlayerID.Player1;
            Debug.Log("������ҽ�ɫΪ Player1");
        }
        else
        {
            Debug.LogError("��ҽ�ɫȱ�� PlayerController �����");
        }

        // ȷ��AIStateMachine���������
        AIStateMachine aiStateMachine = character.GetComponent<AIStateMachine>();
        if (aiStateMachine != null)
        {
            aiStateMachine.enabled = false;
        }

        // ȷ��AIInputProvider������
        AIInputProvider aiInput = character.GetComponent<AIInputProvider>();
        if (aiInput != null)
        {
            aiInput.enabled = false;
        }

        // ȷ��HumanInputProvider������
        HumanInputProvider humanInput = character.GetComponent<HumanInputProvider>();
        if (humanInput == null)
        {
            humanInput = character.AddComponent<HumanInputProvider>();
        }
        humanInput.enabled = true;

        // ����ComboSystemΪ�������
        ComboSystem comboSystem = character.GetComponent<ComboSystem>();
        if (comboSystem != null)
        {
            comboSystem.SetAIControlled(false);
        }

    }

    void SetupAICharacter(GameObject character, int difficultyIndex)
    {
        // ����PlayerIDΪPlayer2
        PlayerController playerController = character.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.PlayerId = PlayerID.Player2;
            Debug.Log("����AI��ɫΪ Player2");
        }
        else
        {
            Debug.LogError("AI��ɫȱ�� PlayerController �����");
        }

        // ����AIStateMachine���
        AIStateMachine aiStateMachine = character.GetComponent<AIStateMachine>();
        if (aiStateMachine == null)
        {
            aiStateMachine = character.AddComponent<AIStateMachine>();
        }
        aiStateMachine.enabled = true;

        // ����AI����
        switch ((DifficultyLevel)difficultyIndex)
        {
            case DifficultyLevel.Easy:
                aiStateMachine.aiConfig = easyAIConfig;
                Debug.Log("AI�Ѷ�����Ϊ����");
                break;
            case DifficultyLevel.Normal:
                aiStateMachine.aiConfig = normalAIConfig;
                Debug.Log("AI�Ѷ�����Ϊ����ͨ");
                break;
            case DifficultyLevel.Hard:
                aiStateMachine.aiConfig = hardAIConfig;
                Debug.Log("AI�Ѷ�����Ϊ������");
                break;
            default:
                aiStateMachine.aiConfig = normalAIConfig;
                Debug.LogWarning("δ֪�Ѷȣ�ʹ��Ĭ����ͨ�Ѷ�");
                break;
        }

        // ȷ��AIInputProvider������
        AIInputProvider aiInput = character.GetComponent<AIInputProvider>();
        if (aiInput == null)
        {
            aiInput = character.AddComponent<AIInputProvider>();
        }
        aiInput.enabled = true;

        // ����HumanInputProvider
        HumanInputProvider humanInput = character.GetComponent<HumanInputProvider>();
        if (humanInput != null)
        {
            humanInput.enabled = false;
        }

        // ����ComboSystemΪAI����
        ComboSystem comboSystem = character.GetComponent<ComboSystem>();
        if (comboSystem != null)
        {
            comboSystem.SetAIControlled(true);
        }

    }

    // ��ȡ��ɫʵ���Ĺ�������
    public GameObject GetPlayerCharacter() => playerCharacter;
    public GameObject GetEnemyCharacter() => enemyCharacter;

    // �������¿�ʼ��Ϸ
    public void RestartGame()
    {
        // �������н�ɫ
        if (playerCharacter != null) Destroy(playerCharacter);
        if (enemyCharacter != null) Destroy(enemyCharacter);

        // ���ó�ʼ����־
        isInitialized = false;

        // ���³�ʼ��
        InitializeGame();
        isInitialized = true;
    }

    // ������
    protected override void OnDestory()
    {
        // �����ɫ����
        playerCharacter = null;
        enemyCharacter = null;

        base.OnDestory();
    }

    // ����ж��ʱ������
    void OnDestroy()
    {
        OnDestory();
    }
}

// �����ࣺ���ڽ�������JSON
[System.Serializable]
public class GameSettingsData
{
    public float masterVolume;
    public float bgmVolume;
    public float sfxVolume;
    public DifficultyLevel difficultyLevel;
}


public enum DifficultyLevel
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}