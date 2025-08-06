using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
// 角色枚举
public enum CharacterType
{
    Kirito,
    Misaka
}

public class CharacterSelectorController : MonoBehaviour
{
    [Header("角色名称")]
    private string[] characterNames = { "Kirito", "Misaka" };

    [Header("玩家UI")]
    public GameObject playerImage1;
    public GameObject playerImage2;
    public Text playerNameText;
    public Button playerLeftButton;
    public Button playerRightButton;

    [Header("电脑UI")]
    public GameObject computerImage1;
    public GameObject computerImage2;
    public Text computerNameText;
    public Button computerLeftButton;
    public Button computerRightButton;

    private int playerIndex = 0;
    private int computerIndex = 0;

    private PlayerInputControl inputControl;

    public Button backButton;

    void Awake()
    {
        // 初始化
        inputControl = new PlayerInputControl();
    }

    void OnEnable()
    {
        // 启用输入
        inputControl.Enable();

        // 绑定Enter键事件
        inputControl.UI.Enter.performed += OnSubmitPerformed;
    }


    void OnDisable()
    {
        // 解绑事件并禁用输入
        inputControl.UI.Submit.performed -= OnSubmitPerformed;
        inputControl.Disable();
    }

    void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        StartGame();
    }

    void StartGame()
    {
        // 保存选择的角色信息
        SaveCharacterSelection();

        // 切换到游戏场景
        if (SceneController.Instance != null)
        {
            SceneController.Instance.TransitionToGameScene();
        }
        else
        {
            Debug.LogWarning("场景控制器实例未找到!");
        }
    }

    // 保存角色选择
    void SaveCharacterSelection()
    {
        // 使用PlayerPrefs保存选择
        PlayerPrefs.SetInt("PlayerCharacter", playerIndex);
        PlayerPrefs.SetInt("ComputerCharacter", computerIndex);
        PlayerPrefs.Save();

        // 如果没有设置难度，使用默认普通难度
        if (!PlayerPrefs.HasKey("GameSettings"))
        {
            // 创建默认设置
            var defaultSettings = new
            {
                masterVolume = 1f,
                bgmVolume = 1f,
                sfxVolume = 1f,
                difficultyLevel = 1 // Normal difficulty
            };
            PlayerPrefs.SetString("GameSettings", JsonUtility.ToJson(defaultSettings));
        }

        PlayerPrefs.Save();

        Debug.Log($"玩家选择: {GetPlayerCharacter()}, 电脑选择: {GetComputerCharacter()}");
    }

    void Start()
    {
        // 绑定按钮
        playerLeftButton.onClick.AddListener(() => ChangePlayerCharacter(-1));
        playerRightButton.onClick.AddListener(() => ChangePlayerCharacter(1));

        computerLeftButton.onClick.AddListener(() => ChangeComputerCharacter(-1));
        computerRightButton.onClick.AddListener(() => ChangeComputerCharacter(1));

        backButton.onClick.AddListener(() =>SceneController.Instance.TransitionToMenuScene());

        // 初始显示
        UpdatePlayerDisplay();
        UpdateComputerDisplay();
    }

    void ChangePlayerCharacter(int direction)
    {
        playerIndex += direction;
        if (playerIndex < 0) playerIndex = characterNames.Length - 1;
        if (playerIndex >= characterNames.Length) playerIndex = 0;

        UpdatePlayerDisplay();
    }

    void ChangeComputerCharacter(int direction)
    {
        computerIndex += direction;
        if (computerIndex < 0) computerIndex = characterNames.Length - 1;
        if (computerIndex >= characterNames.Length) computerIndex = 0;

        UpdateComputerDisplay();
    }

    void UpdatePlayerDisplay()
    {
        // 更新图片显示
        playerImage1.SetActive(playerIndex == 0);
        playerImage2.SetActive(playerIndex == 1);

        // 更新名称
        playerNameText.text = characterNames[playerIndex];
    }

    void UpdateComputerDisplay()
    {
        // 更新图片显示
        computerImage1.SetActive(computerIndex == 0);
        computerImage2.SetActive(computerIndex == 1);

        // 更新名称
        computerNameText.text = characterNames[computerIndex];
    }

    // 获取选择结果
    public CharacterType GetPlayerCharacter()
    {
        return (CharacterType)playerIndex;
    }

    public CharacterType GetComputerCharacter()
    {
        return (CharacterType)computerIndex;
    }
}