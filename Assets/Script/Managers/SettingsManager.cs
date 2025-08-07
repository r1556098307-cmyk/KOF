using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : Singleton<SettingsManager>
{
    [Header("音频设置")]
    [SerializeField] private AudioMixer audioMixer;

    private GameSettings currentSettings;
    private const string SETTINGS_KEY = "GameSettings"; // 与 SettingsMenuUI 使用相同的键名

    // 获取当前设置
    public GameSettings CurrentSettings => currentSettings;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        // 初始化当前设置
        currentSettings = ScriptableObject.CreateInstance<GameSettings>();
        LoadSettingsFromPlayerPrefs();
    }

    // 从 PlayerPrefs 加载设置
    private void LoadSettingsFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(SETTINGS_KEY))
        {
            try
            {
                string jsonData = PlayerPrefs.GetString(SETTINGS_KEY);
                JsonUtility.FromJsonOverwrite(jsonData, currentSettings);
                //Debug.Log("SettingsManager: 从 PlayerPrefs 加载设置成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SettingsManager: 加载设置失败: {e.Message}");
                currentSettings.ResetToDefaults();
            }
        }
        else
        {
            //Debug.Log("SettingsManager: 使用默认设置");
            currentSettings.ResetToDefaults();
        }

        // 应用设置到音频系统
        ApplySettings();
    }

    // 重新加载设置（可以在其他地方调用以刷新设置）
    public void ReloadSettings()
    {
        LoadSettingsFromPlayerPrefs();
    }

    // 应用所有设置
    public void ApplySettings()
    {
        ApplyVolumeSettings();
        ApplyDifficultySettings();
    }

    // 应用音量设置到 AudioMixer
    private void ApplyVolumeSettings()
    {
        if (audioMixer != null && currentSettings != null)
        {
            // 转换为分贝值（0-1 范围转换为 -80dB 到 0dB）
            float masterDB = Mathf.Log10(Mathf.Max(0.0001f, currentSettings.masterVolume)) * 20;
            float bgmDB = Mathf.Log10(Mathf.Max(0.0001f, currentSettings.bgmVolume)) * 20;
            float sfxDB = Mathf.Log10(Mathf.Max(0.0001f, currentSettings.sfxVolume)) * 20;

            audioMixer.SetFloat("MasterVolume", masterDB);
            audioMixer.SetFloat("BGMVolume", bgmDB);
            audioMixer.SetFloat("SFXVolume", sfxDB);

            //Debug.Log($"应用音量设置 - Master: {currentSettings.masterVolume}, BGM: {currentSettings.bgmVolume}, SFX: {currentSettings.sfxVolume}");
        }
    }

    // 应用难度设置
    private void ApplyDifficultySettings()
    {
        // 可以在这里发送事件通知其他系统难度已改变
        // 例如：GameEvents.OnDifficultyChanged?.Invoke(currentSettings.difficultyLevel);
        //Debug.Log($"应用难度设置: {currentSettings.difficultyLevel}");
    }

    // 实时音量调整方法（用于预览）
    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dB = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20;
            audioMixer.SetFloat("MasterVolume", dB);
        }
    }

    public void SetBGMVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dB = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20;
            audioMixer.SetFloat("BGMVolume", dB);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dB = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20;
            audioMixer.SetFloat("SFXVolume", dB);
        }
    }

    // 获取当前难度（其他系统可能需要）
    public DifficultyLevel GetCurrentDifficulty()
    {
        return currentSettings != null ? currentSettings.difficultyLevel : DifficultyLevel.Normal;
    }

    // 获取当前音量设置（其他系统可能需要）
    public float GetMasterVolume()
    {
        return currentSettings != null ? currentSettings.masterVolume : 0.8f;
    }

    public float GetBGMVolume()
    {
        return currentSettings != null ? currentSettings.bgmVolume : 0.7f;
    }

    public float GetSFXVolume()
    {
        return currentSettings != null ? currentSettings.sfxVolume : 0.8f;
    }
}