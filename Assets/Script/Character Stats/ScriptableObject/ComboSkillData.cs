using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Combo Skill", menuName = "Player Config/Combo Skill")]
public class ComboSkillData : ScriptableObject
{
    [Header("������Ϣ")]
    public string skillName;
    public int priority = 1;

    [Header("��������")]
    public List<KeyCode> inputSequence = new List<KeyCode>();

    [Header("ʱ������")]
    public float maxInputInterval = 0.5f;
    public float skillCooldown = 0f;

    [Header("ƥ������")]
    public bool requireExactSequence = true;
    public bool allowExtraInputs = false;

    [Header("�Ӿ�Ч��")]
    public string animationTrigger;
    public GameObject effectPrefab;
    public AudioClip soundEffect;

    [Header("��������")]
    [Range(0.5f, 3.0f)]
    public float forceMultiplier = 1.0f;
    [Range(0.1f, 2.0f)]
    public float durationMultiplier = 1.0f;

    [System.NonSerialized]
    private float lastUsedTime = -999f;

    public bool IsOnCooldown() => Time.time - lastUsedTime < skillCooldown;
    public void UseSkill() => lastUsedTime = Time.time;

    // ������ȴʱ�䣨���ڵ��Ի����������
    public void ResetCooldown() => lastUsedTime = -999f;
}