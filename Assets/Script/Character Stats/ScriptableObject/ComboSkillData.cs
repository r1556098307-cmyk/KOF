using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Combo Skill", menuName = "Player Config/Combo Skill")]
public class ComboSkillData : ScriptableObject
{
    [Header("基本信息")]
    public string skillName;
    public int priority = 1;

    [Header("输入序列")]
    public List<KeyCode> inputSequence = new List<KeyCode>();

    [Header("时间设置")]
    public float maxInputInterval = 0.5f;
    public float skillCooldown = 0f;

    [Header("匹配设置")]
    public bool requireExactSequence = true;
    public bool allowExtraInputs = false;

    [Header("视觉效果")]
    public string animationTrigger;
    public GameObject effectPrefab;
    public AudioClip soundEffect;

    [Header("力度设置")]
    [Range(0.5f, 3.0f)]
    public float forceMultiplier = 1.0f;
    [Range(0.1f, 2.0f)]
    public float durationMultiplier = 1.0f;

    [System.NonSerialized]
    private float lastUsedTime = -999f;

    public bool IsOnCooldown() => Time.time - lastUsedTime < skillCooldown;
    public void UseSkill() => lastUsedTime = Time.time;

    // 重置冷却时间（用于调试或特殊情况）
    public void ResetCooldown() => lastUsedTime = -999f;
}