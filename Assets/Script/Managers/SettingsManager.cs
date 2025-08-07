using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : Singleton<SettingsManager>
{
    [Header("��Ƶ����")]
    [SerializeField] private AudioMixer audioMixer;

    private GameSettings currentSettings;
    private const string SETTINGS_KEY = "GameSettings"; // �� SettingsMenuUI ʹ����ͬ�ļ���

    // ��ȡ��ǰ����
    public GameSettings CurrentSettings => currentSettings;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        // ��ʼ����ǰ����
        currentSettings = ScriptableObject.CreateInstance<GameSettings>();
        LoadSettingsFromPlayerPrefs();
    }

    // �� PlayerPrefs ��������
    private void LoadSettingsFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(SETTINGS_KEY))
        {
            try
            {
                string jsonData = PlayerPrefs.GetString(SETTINGS_KEY);
                JsonUtility.FromJsonOverwrite(jsonData, currentSettings);
                //Debug.Log("SettingsManager: �� PlayerPrefs �������óɹ�");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SettingsManager: ��������ʧ��: {e.Message}");
                currentSettings.ResetToDefaults();
            }
        }
        else
        {
            //Debug.Log("SettingsManager: ʹ��Ĭ������");
            currentSettings.ResetToDefaults();
        }

        // Ӧ�����õ���Ƶϵͳ
        ApplySettings();
    }

    // ���¼������ã������������ط�������ˢ�����ã�
    public void ReloadSettings()
    {
        LoadSettingsFromPlayerPrefs();
    }

    // Ӧ����������
    public void ApplySettings()
    {
        ApplyVolumeSettings();
        ApplyDifficultySettings();
    }

    // Ӧ���������õ� AudioMixer
    private void ApplyVolumeSettings()
    {
        if (audioMixer != null && currentSettings != null)
        {
            // ת��Ϊ�ֱ�ֵ��0-1 ��Χת��Ϊ -80dB �� 0dB��
            float masterDB = Mathf.Log10(Mathf.Max(0.0001f, currentSettings.masterVolume)) * 20;
            float bgmDB = Mathf.Log10(Mathf.Max(0.0001f, currentSettings.bgmVolume)) * 20;
            float sfxDB = Mathf.Log10(Mathf.Max(0.0001f, currentSettings.sfxVolume)) * 20;

            audioMixer.SetFloat("MasterVolume", masterDB);
            audioMixer.SetFloat("BGMVolume", bgmDB);
            audioMixer.SetFloat("SFXVolume", sfxDB);

            //Debug.Log($"Ӧ���������� - Master: {currentSettings.masterVolume}, BGM: {currentSettings.bgmVolume}, SFX: {currentSettings.sfxVolume}");
        }
    }

    // Ӧ���Ѷ�����
    private void ApplyDifficultySettings()
    {
        // ���������﷢���¼�֪ͨ����ϵͳ�Ѷ��Ѹı�
        // ���磺GameEvents.OnDifficultyChanged?.Invoke(currentSettings.difficultyLevel);
        //Debug.Log($"Ӧ���Ѷ�����: {currentSettings.difficultyLevel}");
    }

    // ʵʱ������������������Ԥ����
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

    // ��ȡ��ǰ�Ѷȣ�����ϵͳ������Ҫ��
    public DifficultyLevel GetCurrentDifficulty()
    {
        return currentSettings != null ? currentSettings.difficultyLevel : DifficultyLevel.Normal;
    }

    // ��ȡ��ǰ�������ã�����ϵͳ������Ҫ��
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