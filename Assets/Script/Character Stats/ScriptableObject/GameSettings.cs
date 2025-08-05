using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;



[CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Settings")]
public class GameSettings : ScriptableObject
{
    [Header("��Ƶ����")]
    [Range(0f, 1f)]
    public float masterVolume = 0.5f;
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.5f;

    [Header("��Ϸ�Ѷ�")]
    public DifficultyLevel difficultyLevel = DifficultyLevel.Normal;

    // ����ΪĬ������
    public void ResetToDefaults()
    {
        masterVolume = 0.5f;
        bgmVolume = 0.5f;
        sfxVolume = 0.5f;
        difficultyLevel = DifficultyLevel.Normal;
    }
}

