using UnityEngine;
using System.Collections;

public class GameSceneInitializer : Singleton<GameSceneInitializer>
{
    [Header("角色预制体")]
    [SerializeField] private GameObject kiritoPrefab;
    [SerializeField] private GameObject misakaPrefab;

    [Header("生成位置")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;

    [Header("AI配置文件")]
    [SerializeField] private AIConfig easyAIConfig;
    [SerializeField] private AIConfig normalAIConfig;
    [SerializeField] private AIConfig hardAIConfig;

    // 角色实例
    private GameObject playerCharacter;
    private GameObject enemyCharacter;

    // 添加是否已初始化的标志
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
        // 只有当前实例才执行初始化
        if (Instance == this && !isInitialized)
        {
            InitializeGame();
            isInitialized = true;
        }
    }

    void ValidateReferences()
    {
        // 验证必要的引用是否设置
        if (kiritoPrefab == null || misakaPrefab == null)
        {
            Debug.LogError("角色预制体未设置！");
        }

        if (playerSpawnPoint == null || enemySpawnPoint == null)
        {
            Debug.LogError("生成点未设置！");
        }

        if (easyAIConfig == null || normalAIConfig == null || hardAIConfig == null)
        {
            Debug.LogError("AI配置文件未设置！");
        }
    }

    void InitializeGame()
    {
        // 从PlayerPrefs读取选择
        int playerCharacterIndex = PlayerPrefs.GetInt("PlayerCharacter", 0);
        int computerCharacterIndex = PlayerPrefs.GetInt("ComputerCharacter", 0);

        // 从设置中读取难度（如果有的话）
        int difficultyIndex = 1; // 默认普通难度
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
                Debug.LogWarning("无法读取游戏设置，使用默认难度");
            }
        }

        Debug.Log($"游戏初始化 - 玩家角色: {(CharacterType)playerCharacterIndex}, " +
                  $"电脑角色: {(CharacterType)computerCharacterIndex}, " +
                  $"难度: {(DifficultyLevel)difficultyIndex}");

        // 生成角色
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

            // 确保玩家角色的配置正确
            SetupPlayerCharacter(playerCharacter);

            Debug.Log($"生成玩家角色: {(CharacterType)characterIndex}");
        }
        else
        {
            Debug.LogError("无法生成玩家角色！检查预制体和生成点是否设置");
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

            // 设置为AI控制
            SetupAICharacter(enemyCharacter, difficultyIndex);

            Debug.Log($"生成敌人角色: {(CharacterType)characterIndex}, 难度: {(DifficultyLevel)difficultyIndex}");
        }
        else
        {
            Debug.LogError("无法生成敌人角色！检查预制体和生成点是否设置");
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
                Debug.LogWarning($"未知的角色索引: {characterIndex}，使用默认角色");
                return kiritoPrefab;
        }
    }

    void SetupPlayerCharacter(GameObject character)
    {
        // 设置PlayerID为Player1
        PlayerController playerController = character.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.PlayerId = PlayerID.Player1;
            Debug.Log("设置玩家角色为 Player1");
        }
        else
        {
            Debug.LogError("玩家角色缺少 PlayerController 组件！");
        }

        // 确保AIStateMachine组件被禁用
        AIStateMachine aiStateMachine = character.GetComponent<AIStateMachine>();
        if (aiStateMachine != null)
        {
            aiStateMachine.enabled = false;
        }

        // 确保AIInputProvider被禁用
        AIInputProvider aiInput = character.GetComponent<AIInputProvider>();
        if (aiInput != null)
        {
            aiInput.enabled = false;
        }

        // 确保HumanInputProvider被启用
        HumanInputProvider humanInput = character.GetComponent<HumanInputProvider>();
        if (humanInput == null)
        {
            humanInput = character.AddComponent<HumanInputProvider>();
        }
        humanInput.enabled = true;

        // 设置ComboSystem为人类控制
        ComboSystem comboSystem = character.GetComponent<ComboSystem>();
        if (comboSystem != null)
        {
            comboSystem.SetAIControlled(false);
        }

    }

    void SetupAICharacter(GameObject character, int difficultyIndex)
    {
        // 设置PlayerID为Player2
        PlayerController playerController = character.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.PlayerId = PlayerID.Player2;
            Debug.Log("设置AI角色为 Player2");
        }
        else
        {
            Debug.LogError("AI角色缺少 PlayerController 组件！");
        }

        // 启用AIStateMachine组件
        AIStateMachine aiStateMachine = character.GetComponent<AIStateMachine>();
        if (aiStateMachine == null)
        {
            aiStateMachine = character.AddComponent<AIStateMachine>();
        }
        aiStateMachine.enabled = true;

        // 设置AI配置
        switch ((DifficultyLevel)difficultyIndex)
        {
            case DifficultyLevel.Easy:
                aiStateMachine.aiConfig = easyAIConfig;
                Debug.Log("AI难度设置为：简单");
                break;
            case DifficultyLevel.Normal:
                aiStateMachine.aiConfig = normalAIConfig;
                Debug.Log("AI难度设置为：普通");
                break;
            case DifficultyLevel.Hard:
                aiStateMachine.aiConfig = hardAIConfig;
                Debug.Log("AI难度设置为：困难");
                break;
            default:
                aiStateMachine.aiConfig = normalAIConfig;
                Debug.LogWarning("未知难度，使用默认普通难度");
                break;
        }

        // 确保AIInputProvider被启用
        AIInputProvider aiInput = character.GetComponent<AIInputProvider>();
        if (aiInput == null)
        {
            aiInput = character.AddComponent<AIInputProvider>();
        }
        aiInput.enabled = true;

        // 禁用HumanInputProvider
        HumanInputProvider humanInput = character.GetComponent<HumanInputProvider>();
        if (humanInput != null)
        {
            humanInput.enabled = false;
        }

        // 设置ComboSystem为AI控制
        ComboSystem comboSystem = character.GetComponent<ComboSystem>();
        if (comboSystem != null)
        {
            comboSystem.SetAIControlled(true);
        }

    }

    // 获取角色实例的公共方法
    public GameObject GetPlayerCharacter() => playerCharacter;
    public GameObject GetEnemyCharacter() => enemyCharacter;

    // 用于重新开始游戏
    public void RestartGame()
    {
        // 销毁现有角色
        if (playerCharacter != null) Destroy(playerCharacter);
        if (enemyCharacter != null) Destroy(enemyCharacter);

        // 重置初始化标志
        isInitialized = false;

        // 重新初始化
        InitializeGame();
        isInitialized = true;
    }

    // 清理方法
    protected override void OnDestory()
    {
        // 清理角色引用
        playerCharacter = null;
        enemyCharacter = null;

        base.OnDestory();
    }

    // 场景卸载时的清理
    void OnDestroy()
    {
        OnDestory();
    }
}

// 辅助类：用于解析设置JSON
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