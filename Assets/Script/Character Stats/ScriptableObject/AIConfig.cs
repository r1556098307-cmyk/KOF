using UnityEngine;

public enum AIAttackType
{
    Normal,     // ��ͨ����
    Crouch,     // ���¹���
    Combo,      // ����
}

[CreateAssetMenu(fileName = "AIConfig", menuName = "AI/AI Configuration")]
public class AIConfig : ScriptableObject
{
    [Header("������ֵ")]
    public float attackRange = 2.5f;
    //public float retreatRange = 1.5f;
    public float pursuitRange = 10f;
    public float dashRange = 8.8f;

    [System.Serializable]
    public class AttackTypeWeight
    {
        public AIAttackType attackType = AIAttackType.Normal;
        [Range(0f, 1f)]
        public float weight = 0.5f;
    }
    public AttackTypeWeight[] attackTypeWeights;

    [Header("��Ӧʱ��")]
    //public float reactionTime = 0.2f;
    public float attackReactionTime = 0.2f;

    [Header("״̬����ʱ��")]
    public float attackDuration = 3f;
    public float defendDuration = 1.5f;
    public float retreatDuration = 1f;
    public float pursuitDuration = 2f;
    public float idleDuration = 0.5f;
    public float approachDuration = 1f;

    [Header("�߶ȼ��")]
    public float jumpThreshold = 2f;

    [Header("�������")]
    [Tooltip("��ͨ�����İ������")]
    public float attackButtonInterval = 0.3f;
    [Tooltip("��������İ������")]
    public float comboInputInterval = 0.2f;
    [Tooltip("���������ĳ���ʱ��")]
    public float blockHoldTime = 0.5f;
    [Tooltip("���¶����ӳ�")]
    public float crouchDelay = 0.1f;
    [Tooltip("�����ͷ��ӳ�")]
    public float keyReleaseDelay = 0.1f;

    [Header("ս������")]
    [Tooltip("����ʱ���µĸ���")]
    [Range(0f, 1f)]
    public float crouchDefendChance = 0.3f;
    [Tooltip("Ѫ����ֵ�����ڴ�ֵʱ��������")]
    [Range(0f, 1f)]
    public float healthThresholdForSaving = 0.6f;
}