using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UnifiedAttackTrigger : MonoBehaviour
{
    [Header("攻击配置")]
    [SerializeField] private AttackConfig attackConfig;

    [Header("检测设置")]
    [SerializeField] private LayerMask targetLayers; // 可攻击的层级

    [Header("射线检测设置")]
    [SerializeField] private bool useRaycast = false; // 是否使用射线检测
    [SerializeField] private float rayDistance = 10f; // 射线距离
    [SerializeField] private float rayWidth = 1f; // 射线宽度（用于BoxCast）
    [SerializeField] private Vector2 rayOffset = Vector2.zero; // 射线起始偏移


    private Collider2D attackCollider;
    private bool isActive = false;
    [SerializeField]
    private Transform attackerTransform; // 攻击者的Transform
    private PlayerController playerController;

    private void Awake()
    {
        attackCollider = GetComponent<Collider2D>();
        //hitTargets = new HashSet<GameObject>();
        playerController = GetComponentInParent<PlayerController>();

        // 获取攻击者Transform
        attackerTransform = transform.parent.parent != null ? transform.parent.parent : transform;

        // 初始状态禁用碰撞体
        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    // 激活攻击框 - 由动画事件调用
    public void ActivateAttack()
    {
        isActive = true;

        if (useRaycast && attackConfig.attackName == "SuperMove1")
        {
            PerformRaycastAttack();
        }
        else
        {
            if (attackCollider != null)
                attackCollider.enabled = true;
        }

        if (attackConfig.attackName == "SpecialMove1"|| attackConfig.attackName == "SuperMove2")
            playerController.StartSpecialDash(attackConfig.attackName);

        //Debug.Log($"攻击框激活: {attackConfig.attackName}");
    }

    // 关闭攻击框 - 由动画事件调用
    public void DeactivateAttack()
    {
        isActive = false;

        if (attackCollider != null)
            attackCollider.enabled = false;

        //Debug.Log($"攻击框关闭: {attackConfig.attackName}");
    }


   


    private void PerformRaycastAttack()
    {
        // 获取攻击者朝向
        bool facingRight = playerController.isFacingRight;
        Vector2 rayDirection = facingRight ? Vector2.right : Vector2.left;

        // 计算射线起始位置
        Vector2 rayStart = (Vector2)attackerTransform.position + rayOffset;

        // 使用BoxCast检测
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            rayStart,                    // 起始位置
            new Vector2(rayWidth, 0.5f), // 检测框大小
            0f,                          // 旋转角度
            rayDirection,                // 方向
            rayDistance,                 // 距离
            targetLayers                 // 目标层级
        );

        // 处理所有命中的目标
        foreach (RaycastHit2D hit in hits)
        {
            if (CanHitTarget(hit.collider.gameObject))
            {
                ProcessHit(hit.collider.gameObject);
            }
        }

        // 绘制调试射线
        DrawDebugRay(rayStart, rayDirection);
    }

    private void DrawDebugRay(Vector2 start, Vector2 direction)
    {
        // 在Scene视图中绘制调试射线
        Debug.DrawRay(start, direction * rayDistance, Color.red, 0.5f);

        // 绘制检测框的边界
        Vector2 end = start + direction * rayDistance;
        Vector2 topLeft = start + Vector2.up * (rayWidth / 2);
        Vector2 bottomLeft = start + Vector2.down * (rayWidth / 2);
        Vector2 topRight = end + Vector2.up * (rayWidth / 2);
        Vector2 bottomRight = end + Vector2.down * (rayWidth / 2);

        Debug.DrawLine(topLeft, topRight, Color.yellow, 0.5f);
        Debug.DrawLine(bottomLeft, bottomRight, Color.yellow, 0.5f);
        Debug.DrawLine(topLeft, bottomLeft, Color.yellow, 0.5f);
        Debug.DrawLine(topRight, bottomRight, Color.yellow, 0.5f);
    }

    // 设置攻击配置（运行时修改）
    public void SetAttackConfig(AttackConfig config)
    {
        attackConfig = config;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActive) return;

        // 普通攻击逻辑
        if (CanHitTarget(collision.gameObject))
        {
            ProcessHit(collision.gameObject);
        }
    }



    private void ProcessFullScreenHit(GameObject target)
    {
        if (target == null) return;

        // 获取目标组件
        HitstunSystem targetHitstun = target.GetComponent<HitstunSystem>();
        PlayerController targetPlayer = target.GetComponent<PlayerController>();
        PlayerStats targetStats = target.GetComponent<PlayerStats>();

        // 计算攻击方向
        Vector2 attackDirection = CalculateAttackDirection(target);

        if (IsBlocking(targetPlayer, attackDirection))
        {
            ProcessBlockedHit(target, attackDirection);
            return;
        }

        // 处理伤害
        if (targetStats != null)
        {
            PlayerStats attackerStats = playerController.GetComponent<PlayerStats>();
            if (attackerStats != null)
            {
                // 每次Tick的伤害
                int tickDamage = Mathf.Max(1, attackConfig.damage / 5);
                int tickEnergyRecovery = attackConfig.energyRecovery / 5;

                bool targetDied = attackerStats.TakeDamage(targetStats, tickDamage, tickEnergyRecovery);

                if (targetDied)
                {
                    HandleTargetDeath(target);
                    return;
                }
            }
        }

        // 应用硬直和击退
        if (targetHitstun != null)
        {
            targetHitstun.TakeHit(attackConfig, attackDirection);
        }

        Debug.Log($"全屏攻击Tick: {target.name}");
    }


    private bool CanHitTarget(GameObject target)
    {
        // 检查碰撞对象是否在目标层级
        bool isInTargetLayer = ((1 << target.layer) & targetLayers) != 0;
        if (!isInTargetLayer) return false;

        // 不能打到自己
        if (target.transform.parent == attackerTransform || target.transform == attackerTransform)
            return false;

        // 防止重复命中
        //if (hitTargets.Contains(target)) return false;

        // 检查是否有有效的目标组件
        HitstunSystem targetHitstun = target.GetComponent<HitstunSystem>();
        PlayerController targetPlayer = target.GetComponent<PlayerController>();

        return targetHitstun != null || targetPlayer != null;
    }

    private void ProcessHit(GameObject target)
    {
        // 添加到已命中列表
        //hitTargets.Add(target);

        // 获取目标组件
        HitstunSystem targetHitstun = target.GetComponent<HitstunSystem>();
        PlayerController targetPlayer = target.GetComponent<PlayerController>();
        PlayerStats targetStats = target.GetComponent<PlayerStats>();

        // 计算攻击方向
        Vector2 attackDirection = CalculateAttackDirection(target);

        // 检查格挡
        if (IsBlocking(targetPlayer, attackDirection))
        {
            ProcessBlockedHit(target, attackDirection);
            return;
        }

        // 处理伤害和能量恢复
        if (targetStats != null)
        {
            PlayerStats attackerStats = playerController.GetComponent<PlayerStats>();
            if (attackerStats != null)
            {
                // 调用伤害计算
                bool targetDied = attackerStats.TakeDamage(targetStats, attackConfig.damage, attackConfig.energyRecovery);

                if (targetDied)
                {
                    //Debug.Log($"{target.name} 被击败了！");
                    // 处理死亡逻辑
                    HandleTargetDeath(target);
                }
            }
        }

        // 使用新的硬直系统
        if (targetHitstun != null)
        {
            // 确定最终攻击类型
            AttackType finalAttackType =attackConfig.attackType;


            // 修改硬直时间
            if (attackConfig.stunDurationMultiplier != 1f)
            {
                targetHitstun.ModifyHitstunDuration(attackConfig.stunDurationMultiplier);
            }

            bool isAddUpForce = attackConfig.isAddUpForce;
            targetHitstun.TakeHit(attackConfig, attackDirection);

        }

        // 播放命中效果
        PlayHitEffects(target.transform.position);

        //Debug.Log($"命中目标: {target.name}, 攻击: {attackConfig.attackName}");
    }

    // 添加死亡处理方法
    private void HandleTargetDeath(GameObject target)
    {
        //TODO:玩家胜利返回主菜单，玩家失败选择重新开始或者返回主菜单


    }

    private Vector2 CalculateAttackDirection(GameObject target)
    {
        Vector2 direction;

        // 如果有自定义击退方向，使用它
        if (attackConfig.customKnockbackDirection != Vector2.zero)
        {
            direction = attackConfig.customKnockbackDirection.normalized;
        }
        else
        {
            // 计算从攻击者到目标的方向
            direction = (target.transform.position - attackerTransform.position).normalized;

            // 如果方向为零（重叠），使用攻击者朝向
            if (direction.magnitude < 0.1f)
            {
                PlayerController attackerPlayer = attackerTransform.GetComponent<PlayerController>();
                if (attackerPlayer != null)
                {
                    direction = attackerPlayer.isFacingRight ? Vector2.right : Vector2.left;
                }
                else
                {
                    direction = Vector2.right;
                }
            }
        }


        return new Vector2(direction.x, 0f); ;
    }

    private bool IsBlocking(PlayerController targetPlayer, Vector2 attackDirection)
    {
        if (targetPlayer == null || !targetPlayer.isBlock) return false;

        // 检查格挡方向是否正确
        bool attackFromRight = attackDirection.x > 0;
        bool targetFacingRight = targetPlayer.isFacingRight;

        // 正确的格挡方向：面向攻击来源
        return (attackFromRight && targetFacingRight) || (!attackFromRight && !targetFacingRight);
    }

    private void ProcessBlockedHit(GameObject target, Vector2 attackDirection)
    {
        Debug.Log($"攻击被格挡: {target.name}");

        // 格挡者轻微后退
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            Vector2 blockKnockback = attackDirection * attackConfig.blockKnockbackForce;
            targetRb.AddForce(blockKnockback, ForceMode2D.Impulse);
        }

        // 播放格挡效果
        //PlayBlockEffects(target.transform.position);
    }


    private void PlayHitEffects(Vector3 position)
    {
        // TODO: 播放命中音效
        // AudioManager.Instance.PlaySFX(attackConfig.hitSound);

        // TODO: 播放命中特效
        // EffectManager.Instance.PlayEffect(attackConfig.hitEffect, position);

    }

    private void PlayBlockEffects(Vector3 position)
    {
        // TODO: 播放格挡音效和特效
        // AudioManager.Instance.PlaySFX("BlockSound");
        // EffectManager.Instance.PlayEffect("BlockEffect", position);
    }



 
}
