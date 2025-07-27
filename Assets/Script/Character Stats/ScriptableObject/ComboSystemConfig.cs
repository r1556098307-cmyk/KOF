using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Combo System Config", menuName = "Player Config/Combo System Config")]
public class ComboSystemConfig : ScriptableObject
{
    [Header("ϵͳ����")]
    public float inputBufferTime = 1.0f;
    public int maxInputRecords = 10;
    public bool showDebugInfo = true;

    [Header("���м����б�")]
    public List<ComboSkillData> comboSkills = new List<ComboSkillData>();

    [Header("�߼�����")]
    public bool allowCancelCombo = true;
    public float comboCancelWindow = 0.2f;
    public bool prioritizeNewerInputs = true;
}
