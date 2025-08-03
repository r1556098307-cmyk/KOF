using UnityEngine;

public enum AIAttackType
{
    Normal,     // 普通攻击
    Crouch,     // 蹲下攻击
    Combo,      // 连招
}

[CreateAssetMenu(fileName = "AIConfig", menuName = "AI/AI Configuration")]
public class AIConfig : ScriptableObject
{
    [Header("距离阈值")]
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

    [Header("反应时间")]
    //public float reactionTime = 0.2f;
    public float attackReactionTime = 0.2f;

    [Header("状态持续时间")]
    public float attackDuration = 3f;
    public float defendDuration = 1.5f;
    public float retreatDuration = 1f;
    public float pursuitDuration = 2f;
    public float idleDuration = 0.5f;
    public float approachDuration = 1f;

    [Header("高度检测")]
    public float jumpThreshold = 2f;

    [Header("按键间隔")]
    [Tooltip("普通攻击的按键间隔")]
    public float attackButtonInterval = 0.3f;
    [Tooltip("连招输入的按键间隔")]
    public float comboInputInterval = 0.2f;
    [Tooltip("防御按键的持续时间")]
    public float blockHoldTime = 0.5f;
    [Tooltip("蹲下动作延迟")]
    public float crouchDelay = 0.1f;
    [Tooltip("按键释放延迟")]
    public float keyReleaseDelay = 0.1f;

    [Header("战斗策略")]
    [Tooltip("防御时蹲下的概率")]
    [Range(0f, 1f)]
    public float crouchDefendChance = 0.3f;
    [Tooltip("血量阈值，高于此值时保存能量")]
    [Range(0f, 1f)]
    public float healthThresholdForSaving = 0.6f;
}