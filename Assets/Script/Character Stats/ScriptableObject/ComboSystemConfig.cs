using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Combo System Config", menuName = "Player Config/Combo System Config")]
public class ComboSystemConfig : ScriptableObject
{
    [Header("系统设置")]
    public float inputBufferTime = 1.0f;
    public int maxInputRecords = 10;
    public bool showDebugInfo = true;

    [Header("连招技能列表")]
    public List<ComboSkillData> comboSkills = new List<ComboSkillData>();

    [Header("高级设置")]
    public bool allowCancelCombo = true;
    public float comboCancelWindow = 0.2f;
    public bool prioritizeNewerInputs = true;
}
