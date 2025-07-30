using UnityEngine;

// 攻击配置数据
[System.Serializable]
public class AttackConfig
{
    [Header("基础攻击设置")]
    public AttackType attackType = AttackType.Light;
    public float damage = 10f;
    public string attackName = "Attack"; // 用于标识不同攻击

    [Header("击退设置")]
    public float knockbackForce = 5f; // 主要击退力（水平方向）
    public float knockupForce = 0f; // 向上的力
    public bool  isAddUpForce = true; // 是否会有向上的击退
    public Vector2 customKnockbackDirection = Vector2.zero; // 自定义击退方向

    [Header("格挡击退")]
    public float blockKnockbackForce = 2f; // 格挡时被击退的力

    [Header("音效特效")]
    public string hitSound = "HitSound";
    public string hitEffect = "HitEffect";

    [Header("特殊效果")]
    public float stunDurationMultiplier = 1f; // 硬直时间倍数
}