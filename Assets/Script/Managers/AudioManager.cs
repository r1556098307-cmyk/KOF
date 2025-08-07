// �޸����AudioManager.cs
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
    [Header("��Ƶ�����")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("��ƵԴ")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("��Ƶ��Դ")]
    [SerializeField] private AudioClip[] bgmClips;
    [SerializeField] private AudioClip[] sfxClips;

    // ��Ƶ�ֵ䣬����ͨ�����Ʋ���
    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();

    // �������� - ��Щ��ʵ��Ӧ�õ�����ֵ
    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    // BGM���뵭��
    private Coroutine bgmFadeCoroutine;

    [Header("Ĭ��BGM����")]
    [SerializeField] private string menuSceneBGM = "menu";
    [SerializeField] private string selectSceneBGM = "select";
    [SerializeField] private string gameSceneBGM = "game";

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
        InitializeAudioManager();
    }

    private void InitializeAudioManager()
    {
        // ��ʼ����Ƶ�ֵ�
        InitializeAudioDictionaries();

        // ��SettingsManager��������
        LoadVolumeSettings();

        // Ӧ���������õ�AudioMixer
        ApplyVolumeSettings();
    }

    private void InitializeAudioDictionaries()
    {
        // ��ʼ��BGM�ֵ�
        foreach (var clip in bgmClips)
        {
            if (clip != null && !bgmDict.ContainsKey(clip.name))
            {
                bgmDict.Add(clip.name, clip);
            }
        }

        // ��ʼ��SFX�ֵ�
        foreach (var clip in sfxClips)
        {
            if (clip != null && !sfxDict.ContainsKey(clip.name))
            {
                sfxDict.Add(clip.name, clip);
            }
        }
    }

    #region ��������
    // ��������������ý����ʵʱԤ��������������
    public void SetMasterVolumePreview(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        if (masterMixerGroup != null)
        {
            float dbValue = clampedVolume > 0 ? Mathf.Log10(clampedVolume) * 20 : -80f;
            masterMixerGroup.audioMixer.SetFloat("MasterVolume", dbValue);
        }
    }

    public void SetBGMVolumePreview(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        if (bgmMixerGroup != null)
        {
            float dbValue = clampedVolume > 0 ? Mathf.Log10(clampedVolume) * 20 : -80f;
            bgmMixerGroup.audioMixer.SetFloat("BGMVolume", dbValue);
        }
        else if (bgmSource != null)
        {
            bgmSource.volume = clampedVolume;
        }
    }

    public void SetSFXVolumePreview(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        if (sfxMixerGroup != null)
        {
            float dbValue = clampedVolume > 0 ? Mathf.Log10(clampedVolume) * 20 : -80f;
            sfxMixerGroup.audioMixer.SetFloat("SFXVolume", dbValue);
        }
        else if (sfxSource != null)
        {
            sfxSource.volume = clampedVolume;
        }
    }

    // ��Щ��������ʵ��Ӧ����������
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyMasterVolume();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyBGMVolume();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplySFXVolume();
    }

    private void ApplyMasterVolume()
    {
        if (masterMixerGroup != null)
        {
            float dbValue = masterVolume > 0 ? Mathf.Log10(masterVolume) * 20 : -80f;
            masterMixerGroup.audioMixer.SetFloat("MasterVolume", dbValue);
        }
    }

    private void ApplyBGMVolume()
    {
        if (bgmMixerGroup != null)
        {
            float dbValue = bgmVolume > 0 ? Mathf.Log10(bgmVolume) * 20 : -80f;
            bgmMixerGroup.audioMixer.SetFloat("BGMVolume", dbValue);
        }
        else if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    private void ApplySFXVolume()
    {
        if (sfxMixerGroup != null)
        {
            float dbValue = sfxVolume > 0 ? Mathf.Log10(sfxVolume) * 20 : -80f;
            sfxMixerGroup.audioMixer.SetFloat("SFXVolume", dbValue);
        }
        else if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    // ������������
    private void LoadVolumeSettings()
    {

        string settingsJson = PlayerPrefs.GetString("GameSettings");
        var settingsData = JsonUtility.FromJson<GameSettingsData>(settingsJson);
        masterVolume = settingsData.masterVolume;
        bgmVolume = settingsData.bgmVolume;
        sfxVolume = settingsData.sfxVolume;

        Debug.Log($"������{masterVolume}����������{bgmVolume}����Ч����{sfxVolume}");

    }

    // Ӧ�õ�ǰ�������������
    private void ApplyVolumeSettings()
    {
        ApplyMasterVolume();
        ApplyBGMVolume();
        ApplySFXVolume();
    }

    // �ָ���������������ã�����ȡ��Ԥ����
    public void RestoreVolumeSettings()
    {
        LoadVolumeSettings(); // ���¼��ر��������
        ApplyVolumeSettings(); // Ӧ�õ�AudioMixer
        Debug.Log("AudioManager: �ָ���������");
    }

    // ˢ���������ã���SettingsManager���º���ã�
    public void RefreshVolumeSettings()
    {
        LoadVolumeSettings();
        ApplyVolumeSettings();
        Debug.Log("AudioManager: ˢ����������");
    }

    public float GetMasterVolume() => masterVolume;
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
    #endregion

    #region BGM����
    public void PlayBGM(string clipName, bool loop = true, float fadeTime = 1f)
    {
        if (bgmDict.TryGetValue(clipName, out AudioClip clip))
        {
            PlayBGM(clip, loop, fadeTime);
        }
        else
        {
            Debug.LogWarning($"BGM '{clipName}' not found!");
        }
    }

    public void PlayBGM(AudioClip clip, bool loop = true, float fadeTime = 1f)
    {
        if (clip == null || bgmSource == null) return;

        // ֹͣ��ǰ�ĵ��뵭��Э��
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        // �����ǰû�в������֣�ֱ�Ӳ���
        if (!bgmSource.isPlaying)
        {
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();

            if (fadeTime > 0)
            {
                bgmFadeCoroutine = StartCoroutine(FadeInBGM(fadeTime));
            }
        }
        else
        {
            // ������ǰ���֣�Ȼ�󲥷�������
            bgmFadeCoroutine = StartCoroutine(CrossFadeBGM(clip, loop, fadeTime));
        }
    }

    public void StopBGM(float fadeTime = 1f)
    {
        if (bgmSource == null) return;

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        if (fadeTime > 0)
        {
            bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeTime));
        }
        else
        {
            bgmSource.Stop();
        }
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying && bgmSource.clip != null)
        {
            bgmSource.UnPause();
        }
    }

    private IEnumerator FadeInBGM(float fadeTime)
    {
        bgmSource.volume = 0f;
        float targetVolume = bgmVolume;
        float currentTime = 0f;

        while (currentTime < fadeTime)
        {
            currentTime += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, currentTime / fadeTime);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutBGM(float fadeTime)
    {
        float startVolume = bgmSource.volume;
        float currentTime = 0f;

        while (currentTime < fadeTime)
        {
            currentTime += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeTime);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
        bgmFadeCoroutine = null;
    }

    private IEnumerator CrossFadeBGM(AudioClip newClip, bool loop, float fadeTime)
    {
        // ������ǰBGM
        float startVolume = bgmSource.volume;
        float currentTime = 0f;

        while (currentTime < fadeTime / 2)
        {
            currentTime += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / (fadeTime / 2));
            yield return null;
        }

        // �л�����
        bgmSource.clip = newClip;
        bgmSource.loop = loop;
        bgmSource.Play();

        // ������BGM
        currentTime = 0f;
        while (currentTime < fadeTime / 2)
        {
            currentTime += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, currentTime / (fadeTime / 2));
            yield return null;
        }

        bgmSource.volume = bgmVolume;
        bgmFadeCoroutine = null;
    }
    #endregion

    #region SFX����
    // ѭ����Ч�����ֵ�
    private Dictionary<string, AudioSource> loopingSFX = new Dictionary<string, AudioSource>();

    public void PlaySFXByName(string clipName)
    {
        PlaySFX(clipName, 1f);
    }

    public void PlaySFX(string clipName, float volumeScale = 1f)
    {
        if (sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            PlaySFX(clip, volumeScale);
        }
        else
        {
            Debug.LogWarning($"SFX '{clipName}' not found!");
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    // ����ѭ����Ч
    public void PlayLoopingSFX(string clipName, float volumeScale = 1f)
    {
        if (sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            PlayLoopingSFX(clipName, clip, volumeScale);
        }
        else
        {
            Debug.LogWarning($"ѭ��SFX '{clipName}' not found!");
        }
    }

    public void PlayLoopingSFX(string sfxName, AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        // ����Ѿ��ڲ������ѭ����Ч��ֱ�ӷ���
        if (loopingSFX.ContainsKey(sfxName) && loopingSFX[sfxName] != null && loopingSFX[sfxName].isPlaying)
        {
            return;
        }

        // �����µ�AudioSource����ѭ������
        GameObject loopAudio = new GameObject($"Loop_{sfxName}");
        loopAudio.transform.SetParent(transform); // ����ΪAudioManager���Ӷ���

        AudioSource loopSource = loopAudio.AddComponent<AudioSource>();
        loopSource.clip = clip;
        loopSource.loop = true; // ����Ϊѭ��
        loopSource.volume = sfxVolume * volumeScale;
        loopSource.outputAudioMixerGroup = sfxMixerGroup;
        loopSource.Play();

        // �洢���ֵ��й���
        if (loopingSFX.ContainsKey(sfxName))
        {
            // ���֮ǰ��ͬ����ѭ����Ч����ֹͣ������
            StopLoopingSFX(sfxName);
        }
        loopingSFX[sfxName] = loopSource;
    }

    // ֹͣѭ����Ч
    public void StopLoopingSFX(string sfxName)
    {
        if (loopingSFX.ContainsKey(sfxName) && loopingSFX[sfxName] != null)
        {
            AudioSource source = loopingSFX[sfxName];
            source.Stop();
            Destroy(source.gameObject);
            loopingSFX.Remove(sfxName);
        }
    }

    // ���ѭ����Ч�Ƿ����ڲ���
    public bool IsLoopingSFXPlaying(string sfxName)
    {
        if (loopingSFX.ContainsKey(sfxName) && loopingSFX[sfxName] != null)
        {
            return loopingSFX[sfxName].isPlaying;
        }
        return false;
    }

    // ֹͣ����ѭ����Ч
    public void StopAllLoopingSFX()
    {
        foreach (var kvp in loopingSFX)
        {
            if (kvp.Value != null)
            {
                kvp.Value.Stop();
                Destroy(kvp.Value.gameObject);
            }
        }
        loopingSFX.Clear();
    }

    public AudioSource PlaySFXAtPosition(string clipName, Vector3 position, float volumeScale = 1f)
    {
        if (sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            return PlaySFXAtPosition(clip, position, volumeScale);
        }
        else
        {
            Debug.LogWarning($"SFX '{clipName}' not found!");
            return null;
        }
    }

    public AudioSource PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return null;

        GameObject tempAudio = new GameObject("TempAudio");
        tempAudio.transform.position = position;

        AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = sfxVolume * volumeScale;
        tempSource.spatialBlend = 1f; // 3D��Ч
        tempSource.outputAudioMixerGroup = sfxMixerGroup;
        tempSource.Play();

        // ������ɺ�����
        Destroy(tempAudio, clip.length + 0.1f);

        return tempSource;
    }
    #endregion

    #region ������Ч����
    public void OnSceneChanged(string sceneName)
    {
        // ���ݳ����л�BGM
        switch (sceneName)
        {
            case "MenuScene":
                PlayBGM(menuSceneBGM, true, 2f);
                break;
            case "SelectScene":
                PlayBGM(selectSceneBGM, true, 1f);
                break;
            case "GameScene":
                PlayBGM(gameSceneBGM, true, 1.5f);
                break;
            default:
                // ���ֵ�ǰBGM��ʹ��Ĭ��BGM
                break;
        }
    }

    // ��ͣ������Ч��������Ϸ��ͣʱ��
    public void PauseAllAudio()
    {
        PauseBGM();
    }

    // �ָ�������Ч
    public void ResumeAllAudio()
    {
        ResumeBGM();
    }

    // ����������Ч
    public void MuteAll(bool mute)
    {
        AudioListener.volume = mute ? 0f : 1f;
    }
    #endregion

    #region ���߷���
    public bool IsBGMPlaying()
    {
        return bgmSource != null && bgmSource.isPlaying;
    }

    public string GetCurrentBGMName()
    {
        if (bgmSource != null && bgmSource.clip != null)
        {
            return bgmSource.clip.name;
        }
        return null;
    }

    public void AddBGM(string name, AudioClip clip)
    {
        if (!bgmDict.ContainsKey(name))
        {
            bgmDict.Add(name, clip);
        }
    }

    public void AddSFX(string name, AudioClip clip)
    {
        if (!sfxDict.ContainsKey(name))
        {
            sfxDict.Add(name, clip);
        }
    }
    #endregion

    private void OnDestroy()
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        // ��������ѭ����Ч
        StopAllLoopingSFX();
    }
}

