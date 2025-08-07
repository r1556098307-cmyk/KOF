// 修复后的AudioManager.cs
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
    [Header("音频混合器")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("音频源")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("音频资源")]
    [SerializeField] private AudioClip[] bgmClips;
    [SerializeField] private AudioClip[] sfxClips;

    // 音频字典，方便通过名称查找
    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();

    // 音量设置 - 这些是实际应用的音量值
    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    // BGM淡入淡出
    private Coroutine bgmFadeCoroutine;

    [Header("默认BGM设置")]
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
        // 初始化音频字典
        InitializeAudioDictionaries();

        // 从SettingsManager加载音量
        LoadVolumeSettings();

        // 应用音量设置到AudioMixer
        ApplyVolumeSettings();
    }

    private void InitializeAudioDictionaries()
    {
        // 初始化BGM字典
        foreach (var clip in bgmClips)
        {
            if (clip != null && !bgmDict.ContainsKey(clip.name))
            {
                bgmDict.Add(clip.name, clip);
            }
        }

        // 初始化SFX字典
        foreach (var clip in sfxClips)
        {
            if (clip != null && !sfxDict.ContainsKey(clip.name))
            {
                sfxDict.Add(clip.name, clip);
            }
        }
    }

    #region 音量控制
    // 这个方法用于设置界面的实时预览，不保存设置
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

    // 这些方法用于实际应用音量设置
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

    // 加载音量设置
    private void LoadVolumeSettings()
    {

        string settingsJson = PlayerPrefs.GetString("GameSettings");
        var settingsData = JsonUtility.FromJson<GameSettingsData>(settingsJson);
        masterVolume = settingsData.masterVolume;
        bgmVolume = settingsData.bgmVolume;
        sfxVolume = settingsData.sfxVolume;

        Debug.Log($"主音量{masterVolume}，背景音量{bgmVolume}，特效音量{sfxVolume}");

    }

    // 应用当前保存的音量设置
    private void ApplyVolumeSettings()
    {
        ApplyMasterVolume();
        ApplyBGMVolume();
        ApplySFXVolume();
    }

    // 恢复到保存的音量设置（用于取消预览）
    public void RestoreVolumeSettings()
    {
        LoadVolumeSettings(); // 重新加载保存的设置
        ApplyVolumeSettings(); // 应用到AudioMixer
        Debug.Log("AudioManager: 恢复音量设置");
    }

    // 刷新音量设置（当SettingsManager更新后调用）
    public void RefreshVolumeSettings()
    {
        LoadVolumeSettings();
        ApplyVolumeSettings();
        Debug.Log("AudioManager: 刷新音量设置");
    }

    public float GetMasterVolume() => masterVolume;
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
    #endregion

    #region BGM控制
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

        // 停止当前的淡入淡出协程
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        // 如果当前没有播放音乐，直接播放
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
            // 淡出当前音乐，然后播放新音乐
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
        // 淡出当前BGM
        float startVolume = bgmSource.volume;
        float currentTime = 0f;

        while (currentTime < fadeTime / 2)
        {
            currentTime += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / (fadeTime / 2));
            yield return null;
        }

        // 切换音乐
        bgmSource.clip = newClip;
        bgmSource.loop = loop;
        bgmSource.Play();

        // 淡入新BGM
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

    #region SFX控制
    // 循环音效管理字典
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

    // 播放循环音效
    public void PlayLoopingSFX(string clipName, float volumeScale = 1f)
    {
        if (sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            PlayLoopingSFX(clipName, clip, volumeScale);
        }
        else
        {
            Debug.LogWarning($"循环SFX '{clipName}' not found!");
        }
    }

    public void PlayLoopingSFX(string sfxName, AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        // 如果已经在播放这个循环音效，直接返回
        if (loopingSFX.ContainsKey(sfxName) && loopingSFX[sfxName] != null && loopingSFX[sfxName].isPlaying)
        {
            return;
        }

        // 创建新的AudioSource用于循环播放
        GameObject loopAudio = new GameObject($"Loop_{sfxName}");
        loopAudio.transform.SetParent(transform); // 设置为AudioManager的子对象

        AudioSource loopSource = loopAudio.AddComponent<AudioSource>();
        loopSource.clip = clip;
        loopSource.loop = true; // 设置为循环
        loopSource.volume = sfxVolume * volumeScale;
        loopSource.outputAudioMixerGroup = sfxMixerGroup;
        loopSource.Play();

        // 存储到字典中管理
        if (loopingSFX.ContainsKey(sfxName))
        {
            // 如果之前有同名的循环音效，先停止并销毁
            StopLoopingSFX(sfxName);
        }
        loopingSFX[sfxName] = loopSource;
    }

    // 停止循环音效
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

    // 检查循环音效是否正在播放
    public bool IsLoopingSFXPlaying(string sfxName)
    {
        if (loopingSFX.ContainsKey(sfxName) && loopingSFX[sfxName] != null)
        {
            return loopingSFX[sfxName].isPlaying;
        }
        return false;
    }

    // 停止所有循环音效
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
        tempSource.spatialBlend = 1f; // 3D音效
        tempSource.outputAudioMixerGroup = sfxMixerGroup;
        tempSource.Play();

        // 播放完成后销毁
        Destroy(tempAudio, clip.length + 0.1f);

        return tempSource;
    }
    #endregion

    #region 场景音效管理
    public void OnSceneChanged(string sceneName)
    {
        // 根据场景切换BGM
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
                // 保持当前BGM或使用默认BGM
                break;
        }
    }

    // 暂停所有音效（用于游戏暂停时）
    public void PauseAllAudio()
    {
        PauseBGM();
    }

    // 恢复所有音效
    public void ResumeAllAudio()
    {
        ResumeBGM();
    }

    // 静音所有音效
    public void MuteAll(bool mute)
    {
        AudioListener.volume = mute ? 0f : 1f;
    }
    #endregion

    #region 工具方法
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

        // 清理所有循环音效
        StopAllLoopingSFX();
    }
}

