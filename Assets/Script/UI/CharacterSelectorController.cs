using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
// ��ɫö��
public enum CharacterType
{
    Kirito,
    Misaka
}

public class CharacterSelectorController : MonoBehaviour
{
    [Header("��ɫ����")]
    private string[] characterNames = { "Kirito", "Misaka" };

    [Header("���UI")]
    public GameObject playerImage1;
    public GameObject playerImage2;
    public Text playerNameText;
    public Button playerLeftButton;
    public Button playerRightButton;

    [Header("����UI")]
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
        // ��ʼ��
        inputControl = new PlayerInputControl();
    }

    void OnEnable()
    {
        // ��������
        inputControl.Enable();

        // ��Enter���¼�
        inputControl.UI.Enter.performed += OnSubmitPerformed;
    }


    void OnDisable()
    {
        // ����¼�����������
        inputControl.UI.Submit.performed -= OnSubmitPerformed;
        inputControl.Disable();
    }

    void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        StartGame();
    }

    void StartGame()
    {
        // ����ѡ��Ľ�ɫ��Ϣ
        SaveCharacterSelection();

        // �л�����Ϸ����
        if (SceneController.Instance != null)
        {
            SceneController.Instance.TransitionToGameScene();
        }
        else
        {
            Debug.LogWarning("����������ʵ��δ�ҵ�!");
        }
    }

    // �����ɫѡ��
    void SaveCharacterSelection()
    {
        // ʹ��PlayerPrefs����ѡ��
        PlayerPrefs.SetInt("PlayerCharacter", playerIndex);
        PlayerPrefs.SetInt("ComputerCharacter", computerIndex);
        PlayerPrefs.Save();

        // ���û�������Ѷȣ�ʹ��Ĭ����ͨ�Ѷ�
        if (!PlayerPrefs.HasKey("GameSettings"))
        {
            // ����Ĭ������
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

        Debug.Log($"���ѡ��: {GetPlayerCharacter()}, ����ѡ��: {GetComputerCharacter()}");
    }

    void Start()
    {
        // �󶨰�ť
        playerLeftButton.onClick.AddListener(() => ChangePlayerCharacter(-1));
        playerRightButton.onClick.AddListener(() => ChangePlayerCharacter(1));

        computerLeftButton.onClick.AddListener(() => ChangeComputerCharacter(-1));
        computerRightButton.onClick.AddListener(() => ChangeComputerCharacter(1));

        backButton.onClick.AddListener(() =>SceneController.Instance.TransitionToMenuScene());

        // ��ʼ��ʾ
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
        // ����ͼƬ��ʾ
        playerImage1.SetActive(playerIndex == 0);
        playerImage2.SetActive(playerIndex == 1);

        // ��������
        playerNameText.text = characterNames[playerIndex];
    }

    void UpdateComputerDisplay()
    {
        // ����ͼƬ��ʾ
        computerImage1.SetActive(computerIndex == 0);
        computerImage2.SetActive(computerIndex == 1);

        // ��������
        computerNameText.text = characterNames[computerIndex];
    }

    // ��ȡѡ����
    public CharacterType GetPlayerCharacter()
    {
        return (CharacterType)playerIndex;
    }

    public CharacterType GetComputerCharacter()
    {
        return (CharacterType)computerIndex;
    }
}