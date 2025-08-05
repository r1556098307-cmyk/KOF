using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;



[CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Settings")]
public class GameSettings : ScriptableObject
{
    [Header("音频设置")]
    [Range(0f, 1f)]
    public float masterVolume = 0.5f;
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.5f;

    [Header("游戏难度")]
    public DifficultyLevel difficultyLevel = DifficultyLevel.Normal;

    // 重置为默认设置
    public void ResetToDefaults()
    {
        masterVolume = 0.5f;
        bgmVolume = 0.5f;
        sfxVolume = 0.5f;
        difficultyLevel = DifficultyLevel.Normal;
    }
}

