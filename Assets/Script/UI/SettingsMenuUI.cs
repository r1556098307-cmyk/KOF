using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class SettingsMenuUI : MonoBehaviour
{
    [Header("面板引用")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject audioTab;
    [SerializeField] private GameObject gameplayTab;

    [Header("标签按钮")]
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button gameplayTabButton;

    [Header("音频设置")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Text masterVolumeText;
    [SerializeField] private Text bgmVolumeText;
    [SerializeField] private Text sfxVolumeText;

    [Header("游戏设置")]
    [SerializeField] private Dropdown difficultyDropdown;

    [Header("按钮")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    [Header("主菜单引用")]
    [SerializeField] private MainMenu mainMenuController;


    private GameSettings tempSettings;
    private GameSettings originalSettings; // 保存打开设置时的原始值
    private const string SETTINGS_KEY = "GameSettings"; // PlayerPrefs的键名

    private void Start()
    {
        // 自动查找MainMenu组件
        if (mainMenuController == null)
            mainMenuController = FindObjectOfType<MainMenu>();

        // 绑定标签页按钮事件
        if (audioTabButton != null) audioTabButton.onClick.AddListener(ShowAudioTab);
        if (gameplayTabButton != null) gameplayTabButton.onClick.AddListener(ShowGameplayTab);

        // 绑定功能按钮事件
        if (applyButton != null) applyButton.onClick.AddListener(ApplySettings);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelSettings);
        if (resetButton != null) resetButton.onClick.AddListener(ResetSettings);
        if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);

        // 绑定滑动条事件
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (difficultyDropdown != null) difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);

        // 初始化难度下拉菜单选项
        InitializeDifficultyDropdown();
    }

    private void InitializeDifficultyDropdown()
    {
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            List<string> options = new List<string> { "简单", "普通", "困难" };
            difficultyDropdown.AddOptions(options);
        }
    }

    private void InitializeUI()
    {
        // 创建临时设置副本
        tempSettings = ScriptableObject.CreateInstance<GameSettings>();
        originalSettings = ScriptableObject.CreateInstance<GameSettings>();

        // 优先从PlayerPrefs加载设置
        if (LoadSettingsFromPlayerPrefs())
        {
            Debug.Log("从PlayerPrefs加载设置成功");
        }
        // 如果PlayerPrefs没有数据，从SettingsManager加载
        else if (SettingsManager.Instance != null && SettingsManager.Instance.CurrentSettings != null)
        {
            Debug.Log("从SettingsManager加载设置");
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(SettingsManager.Instance.CurrentSettings), tempSettings);
        }
        // 如果都没有，使用默认值
        else
        {
            Debug.LogWarning("使用默认设置值");
            InitializeWithDefaultValues();
            return;
        }

        // 保存原始设置的副本，用于取消操作
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tempSettings), originalSettings);

        // 更新UI显示
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
                Debug.LogError($"从PlayerPrefs加载设置失败: {e.Message}");
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
            Debug.Log("设置已保存到PlayerPrefs");
        }
    }

    private void UpdateUIFromSettings()
    {
        // 初始化音频设置
        if (masterVolumeSlider != null) masterVolumeSlider.value = tempSettings.masterVolume;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = tempSettings.bgmVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = tempSettings.sfxVolume;
        UpdateVolumeTexts();

        // 初始化难度设置
        if (difficultyDropdown != null) difficultyDropdown.value = (int)tempSettings.difficultyLevel;
    }

    private void InitializeWithDefaultValues()
    {
        // 创建默认的tempSettings
        if (tempSettings == null)
            tempSettings = ScriptableObject.CreateInstance<GameSettings>();

        // 重置为默认值
        tempSettings.ResetToDefaults();

        // 保存原始设置的副本
        if (originalSettings == null)
            originalSettings = ScriptableObject.CreateInstance<GameSettings>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tempSettings), originalSettings);

        // 更新UI
        UpdateUIFromSettings();
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (tempSettings != null) tempSettings.masterVolume = value;
        if (masterVolumeText != null) masterVolumeText.text = $"{(int)(value * 100)}%";

        // 实时预览 - 使用预览方法，不保存到实际设置
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolumePreview(value);
        else if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetMasterVolume(value);
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (tempSettings != null) tempSettings.bgmVolume = value;
        if (bgmVolumeText != null) bgmVolumeText.text = $"{(int)(value * 100)}%";

        // 实时预览 - 使用预览方法，不保存到实际设置
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBGMVolumePreview(value);
        else if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (tempSettings != null) tempSettings.sfxVolume = value;
        if (sfxVolumeText != null) sfxVolumeText.text = $"{(int)(value * 100)}%";

        // 实时预览 - 使用预览方法，不保存到实际设置
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
        // 保存到PlayerPrefs
        SaveSettingsToPlayerPrefs();

        // 更新SettingsManager
        if (SettingsManager.Instance != null && tempSettings != null)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tempSettings), SettingsManager.Instance.CurrentSettings);
            SettingsManager.Instance.ApplySettings();
        }

        // 应用到AudioManager（实际保存音量设置）
        if (AudioManager.Instance != null && tempSettings != null)
        {
            AudioManager.Instance.SetMasterVolume(tempSettings.masterVolume);
            AudioManager.Instance.SetBGMVolume(tempSettings.bgmVolume);
            AudioManager.Instance.SetSFXVolume(tempSettings.sfxVolume);
        }

        // 更新originalSettings为当前保存的值
        if (originalSettings != null && tempSettings != null)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tempSettings), originalSettings);
            Debug.Log("设置已应用并保存");
        }
    }

    private void CancelSettings()
    {
        // 恢复到打开设置界面时的状态
        if (originalSettings != null)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(originalSettings), tempSettings);
            UpdateUIFromSettings();

            // 恢复AudioManager到保存的状态
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.RestoreVolumeSettings();
            }
            // 恢复SettingsManager的音频预览
            else if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.SetMasterVolume(originalSettings.masterVolume);
                SettingsManager.Instance.SetBGMVolume(originalSettings.bgmVolume);
                SettingsManager.Instance.SetSFXVolume(originalSettings.sfxVolume);
            }

            Debug.Log("设置已取消，恢复到原始状态");
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

        // 通过MainMenu组件返回主菜单
        if (mainMenuController != null)
        {
            if(originalSettings!=null&&AudioManager.Instance!=null)
            {
                // 恢复AudioManager到保存的状态
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.RestoreVolumeSettings();
                }
                // 恢复SettingsManager的音频预览
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

    // 标签页切换
    public void ShowAudioTab()
    {
        if (audioTab != null) audioTab.SetActive(true);
        if (gameplayTab != null) gameplayTab.SetActive(false);

        // 更新标签按钮视觉状态
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
        // 可以在这里更新标签按钮的视觉效果，比如改变颜色
        Color activeColor = new Color(1f, 1f, 1f, 1f);
        Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        if (audioTabButton != null)
            audioTabButton.image.color = (activeTab == 0) ? activeColor : inactiveColor;
        if (gameplayTabButton != null)
            gameplayTabButton.image.color = (activeTab == 1) ? activeColor : inactiveColor;
    }

    // 公共方法：清除PlayerPrefs中的设置数据
    public void ClearSavedSettings()
    {
        if (PlayerPrefs.HasKey(SETTINGS_KEY))
        {
            PlayerPrefs.DeleteKey(SETTINGS_KEY);
            PlayerPrefs.Save();
            Debug.Log("已清除保存的设置");
        }
    }

    // 公共方法：检查是否有保存的设置
    public bool HasSavedSettings()
    {
        return PlayerPrefs.HasKey(SETTINGS_KEY);
    }

    private void OnDestroy()
    {
        // 清理标签按钮事件
        if (audioTabButton != null) audioTabButton.onClick.RemoveListener(ShowAudioTab);
        if (gameplayTabButton != null) gameplayTabButton.onClick.RemoveListener(ShowGameplayTab);

        // 清理功能按钮事件
        if (applyButton != null) applyButton.onClick.RemoveListener(ApplySettings);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(CancelSettings);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetSettings);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseSettings);

        // 清理滑动条事件
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

        if (difficultyDropdown != null) difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged);
    }
}