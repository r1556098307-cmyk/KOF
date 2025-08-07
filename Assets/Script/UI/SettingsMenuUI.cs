using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class SettingsMenuUI : MonoBehaviour
{
    [Header("�������")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject audioTab;
    [SerializeField] private GameObject gameplayTab;

    [Header("��ǩ��ť")]
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button gameplayTabButton;

    [Header("��Ƶ����")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Text masterVolumeText;
    [SerializeField] private Text bgmVolumeText;
    [SerializeField] private Text sfxVolumeText;

    [Header("��Ϸ����")]
    [SerializeField] private Dropdown difficultyDropdown;

    [Header("��ť")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    [Header("���˵�����")]
    [SerializeField] private MainMenu mainMenuController;


    private GameSettings tempSettings;
    private GameSettings originalSettings; // ���������ʱ��ԭʼֵ
    private const string SETTINGS_KEY = "GameSettings"; // PlayerPrefs�ļ���

    private void Start()
    {
        // �Զ�����MainMenu���
        if (mainMenuController == null)
            mainMenuController = FindObjectOfType<MainMenu>();

        // �󶨱�ǩҳ��ť�¼�
        if (audioTabButton != null) audioTabButton.onClick.AddListener(ShowAudioTab);
        if (gameplayTabButton != null) gameplayTabButton.onClick.AddListener(ShowGameplayTab);

        // �󶨹��ܰ�ť�¼�
        if (applyButton != null) applyButton.onClick.AddListener(ApplySettings);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelSettings);
        if (resetButton != null) resetButton.onClick.AddListener(ResetSettings);
        if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);

        // �󶨻������¼�
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (difficultyDropdown != null) difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);

        // ��ʼ���Ѷ������˵�ѡ��
        InitializeDifficultyDropdown();
    }

    private void InitializeDifficultyDropdown()
    {
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            List<string> options = new List<string> { "��", "��ͨ", "����" };
            difficultyDropdown.AddOptions(options);
        }
    }

    private void InitializeUI()
    {
        // ������ʱ���ø���
        tempSettings = ScriptableObject.CreateInstance<GameSettings>();
        originalSettings = ScriptableObject.CreateInstance<GameSettings>();

        // ���ȴ�PlayerPrefs��������
        if (LoadSettingsFromPlayerPrefs())
        {
            Debug.Log("��PlayerPrefs�������óɹ�");
        }
        // ���PlayerPrefsû�����ݣ���SettingsManager����
        else if (SettingsManager.Instance != null && SettingsManager.Instance.CurrentSettings != null)
        {
            Debug.Log("��SettingsManager��������");
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(SettingsManager.Instance.CurrentSettings), tempSettings);
        }
        // �����û�У�ʹ��Ĭ��ֵ
        else
        {
            Debug.LogWarning("ʹ��Ĭ������ֵ");
            InitializeWithDefaultValues();
            return;
        }

        // ����ԭʼ���õĸ���������ȡ������
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tempSettings), originalSettings);

        // ����UI��ʾ
        UpdateUIFromSettings();
    }

    private bool LoadSettingsFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(SETTINGS_KEY))
        {
            string jsonData = PlayerPrefs.GetString(SETTINGS_KEY);
            try
            {
                JsonUtility.FromJsonOverwrite(jsonData, tempSettings);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"��PlayerPrefs��������ʧ��: {e.Message}");
                return false;
            }
        }
        return false;
    }

    private void SaveSettingsToPlayerPrefs()
    {
        if (tempSettings != null)
        {
            string jsonData = JsonUtility.ToJson(tempSettings);
            PlayerPrefs.SetString(SETTINGS_KEY, jsonData);
            PlayerPrefs.Save();
            Debug.Log("�����ѱ��浽PlayerPrefs");
        }
    }

    private void UpdateUIFromSettings()
    {
        // ��ʼ����Ƶ����
        if (masterVolumeSlider != null) masterVolumeSlider.value = tempSettings.masterVolume;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = tempSettings.bgmVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = tempSettings.sfxVolume;
        UpdateVolumeTexts();

        // ��ʼ���Ѷ�����
        if (difficultyDropdown != null) difficultyDropdown.value = (int)tempSettings.difficultyLevel;
    }

    private void InitializeWithDefaultValues()
    {
        // ����Ĭ�ϵ�tempSettings
        if (tempSettings == null)
            tempSettings = ScriptableObject.CreateInstance<GameSettings>();

        // ����ΪĬ��ֵ
        tempSettings.ResetToDefaults();

        // ����ԭʼ���õĸ���
        if (originalSettings == null)
            originalSettings = ScriptableObject.CreateInstance<GameSettings>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tempSettings), originalSettings);

        // ����UI
        UpdateUIFromSettings();
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (tempSettings != null) tempSettings.masterVolume = value;
        if (masterVolumeText != null) masterVolumeText.text = $"{(int)(value * 100)}%";

        // ʵʱԤ�� - ʹ��Ԥ�������������浽ʵ������
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolumePreview(value);
        else if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetMasterVolume(value);
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (tempSettings != null) tempSettings.bgmVolume = value;
        if (bgmVolumeText != null) bgmVolumeText.text = $"{(int)(value * 100)}%";

        // ʵʱԤ�� - ʹ��Ԥ�������������浽ʵ������
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBGMVolumePreview(value);
        else if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (tempSettings != null) tempSettings.sfxVolume = value;
        if (sfxVolumeText != null) sfxVolumeText.text = $"{(int)(value * 100)}%";

        // ʵʱԤ�� - ʹ��Ԥ�������������浽ʵ������
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolumePreview(value);
        else if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetSFXVolume(value);
    }
    private void OnDifficultyChanged(int index)
    {
        if (tempSettings != null) tempSettings.difficultyLevel = (DifficultyLevel)index;
    }

    private void UpdateVolumeTexts()
    {
        if (tempSettings != null)
        {
            UpdateVolumeTextsWithValues(tempSettings.masterVolume, tempSettings.bgmVolume, tempSettings.sfxVolume);
        }
    }

    private void UpdateVolumeTextsWithValues(float master, float bgm, float sfx)
    {
        if (masterVolumeText != null) masterVolumeText.text = $"{(int)(master * 100)}%";
        if (bgmVolumeText != null) bgmVolumeText.text = $"{(int)(bgm * 100)}%";
        if (sfxVolumeText != null) sfxVolumeText.text = $"{(int)(sfx * 100)}%";
    }

    private void ApplySettings()
    {
        // ���浽PlayerPrefs
        SaveSettingsToPlayerPrefs();

        // ����SettingsManager
        if (SettingsManager.Instance != null && tempSettings != null)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tempSettings), SettingsManager.Instance.CurrentSettings);
            SettingsManager.Instance.ApplySettings();
        }

        // Ӧ�õ�AudioManager��ʵ�ʱ����������ã�
        if (AudioManager.Instance != null && tempSettings != null)
        {
            AudioManager.Instance.SetMasterVolume(tempSettings.masterVolume);
            AudioManager.Instance.SetBGMVolume(tempSettings.bgmVolume);
            AudioManager.Instance.SetSFXVolume(tempSettings.sfxVolume);
        }

        // ����originalSettingsΪ��ǰ�����ֵ
        if (originalSettings != null && tempSettings != null)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tempSettings), originalSettings);
            Debug.Log("������Ӧ�ò�����");
        }
    }

    private void CancelSettings()
    {
        // �ָ��������ý���ʱ��״̬
        if (originalSettings != null)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(originalSettings), tempSettings);
            UpdateUIFromSettings();

            // �ָ�AudioManager�������״̬
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.RestoreVolumeSettings();
            }
            // �ָ�SettingsManager����ƵԤ��
            else if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.SetMasterVolume(originalSettings.masterVolume);
                SettingsManager.Instance.SetBGMVolume(originalSettings.bgmVolume);
                SettingsManager.Instance.SetSFXVolume(originalSettings.sfxVolume);
            }

            Debug.Log("������ȡ�����ָ���ԭʼ״̬");
        }
    }

    private void ResetSettings()
    {
        if (tempSettings != null)
        {
            tempSettings.ResetToDefaults();
            UpdateUIFromSettings();
        }
        else
        {
            InitializeWithDefaultValues();
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        InitializeUI();
        ShowAudioTab();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // ͨ��MainMenu����������˵�
        if (mainMenuController != null)
        {
            if(originalSettings!=null&&AudioManager.Instance!=null)
            {
                // �ָ�AudioManager�������״̬
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.RestoreVolumeSettings();
                }
                // �ָ�SettingsManager����ƵԤ��
                else if (SettingsManager.Instance != null)
                {
                    SettingsManager.Instance.SetMasterVolume(originalSettings.masterVolume);
                    SettingsManager.Instance.SetBGMVolume(originalSettings.bgmVolume);
                    SettingsManager.Instance.SetSFXVolume(originalSettings.sfxVolume);
                }
            }
            mainMenuController.ReturnToMainMenu();
        }
    }

    // ��ǩҳ�л�
    public void ShowAudioTab()
    {
        if (audioTab != null) audioTab.SetActive(true);
        if (gameplayTab != null) gameplayTab.SetActive(false);

        // ���±�ǩ��ť�Ӿ�״̬
        UpdateTabButtonVisuals(0);
    }

    public void ShowGameplayTab()
    {
        if (audioTab != null) audioTab.SetActive(false);
        if (gameplayTab != null) gameplayTab.SetActive(true);

        UpdateTabButtonVisuals(1);
    }

    private void UpdateTabButtonVisuals(int activeTab)
    {
        // ������������±�ǩ��ť���Ӿ�Ч��������ı���ɫ
        Color activeColor = new Color(1f, 1f, 1f, 1f);
        Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        if (audioTabButton != null)
            audioTabButton.image.color = (activeTab == 0) ? activeColor : inactiveColor;
        if (gameplayTabButton != null)
            gameplayTabButton.image.color = (activeTab == 1) ? activeColor : inactiveColor;
    }

    // �������������PlayerPrefs�е���������
    public void ClearSavedSettings()
    {
        if (PlayerPrefs.HasKey(SETTINGS_KEY))
        {
            PlayerPrefs.DeleteKey(SETTINGS_KEY);
            PlayerPrefs.Save();
            Debug.Log("��������������");
        }
    }

    // ��������������Ƿ��б��������
    public bool HasSavedSettings()
    {
        return PlayerPrefs.HasKey(SETTINGS_KEY);
    }

    private void OnDestroy()
    {
        // �����ǩ��ť�¼�
        if (audioTabButton != null) audioTabButton.onClick.RemoveListener(ShowAudioTab);
        if (gameplayTabButton != null) gameplayTabButton.onClick.RemoveListener(ShowGameplayTab);

        // �����ܰ�ť�¼�
        if (applyButton != null) applyButton.onClick.RemoveListener(ApplySettings);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(CancelSettings);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetSettings);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseSettings);

        // ���������¼�
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

        if (difficultyDropdown != null) difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged);
    }
}