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
    public float retreatRange = 1.5f;
    public float pursuitRange = 10f;
    public float jumpRange = 4f;
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
    public float reactionTime = 0.2f;
    public float blockReactionTime = 0.3f;
    public float crouchReactionTime = 0.25f;
    public float jumpReactionTime = 0.4f;
    public float dashReactionTime = 0.3f;
    public float attackReactionTime = 0.2f;


    [Header("状态持续时间")]
    public float attackDuration = 3f;
    public float defendDuration = 1.5f;
    public float retreatDuration = 1f;
    public float pursuitDuration = 2f;
    public float idleDuration = 0.5f;

    [Header("高度检测")]
    public float jumpThreshold = 2f;
    public float groundCheckDistance = 0.5f;
}