using System;
using UnityEngine;

// 攻击类型枚举
public enum AttackType
{
    Light,
    Heavy,
    knockdown
}

public class HitstunSystem : MonoBehaviour
{
    [Header("硬直配置")]
    public HitstunData hitstunData;

    [Header("状态显示")]
    [SerializeField] private bool isInHitstun = false;
    [SerializeField] private float hitstunTimer = 0f;
    [SerializeField] private bool isKnockedDown = false;

    // 组件引用
    private PlayerController playerController;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerAnimator animator;

    // 硬直效果
    private Vector3 originalPosition;
    private Color originalColor;

    // 事件
    public event Action OnHitstunStart;
    public event Action OnHitstunEnd;
    public event Action OnKnockdown;
    public event Action OnGetUp;

    // 硬直期间禁用的输入
    [SerializeField]
    private bool canMove = true;
    [SerializeField]
    private bool canAttack = true;
    [SerializeField]
    private bool canJump = true;
    [SerializeField]
    private bool canDash = true;
    [SerializeField]
    private bool canBlock = true;
    [SerializeField]
    private bool canCrouch = true;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<PlayerAnimator>();

        originalColor = spriteRenderer.color;
    }

    private void Update()
    {
        UpdateHitstun();
        //UpdateShakeEffect();
    }

    private void UpdateHitstun()
    {
        if (isInHitstun)
        {
            hitstunTimer -= Time.deltaTime;

            if (hitstunTimer <= 0f)
            {
                EndHitstun();
            }
        }
    }

    // 受到攻击的主要方法
    public void TakeHit(AttackConfig attackConfig, Vector2 attackDirection)
    {

        if (playerController != null && playerController.GetIsInvulnerable())
        {
            //Debug.Log("角色处于无敌状态，攻击无效");
            return;
        }

        // 击倒攻击的特殊处理
        if (attackConfig.attackType == AttackType.knockdown)
        {
            HandleKnockdown();
        }
        else
        {
            // 开始硬直
            StartHitstun(attackConfig.attackType);
        }

        // 应用击退
        ApplyKnockback(attackConfig, attackDirection);

        // 播放受击动画
        PlayHitAnimation(attackConfig.attackType);

        // 播放受击音效和特效
        PlayHitEffects();
    }

    private void StartHitstun(AttackType attackType)
    {
        isInHitstun = true;

        // 设置硬直时间
        hitstunTimer = GetHitstunDuration(attackType);
        Debug.Log("僵直时间："+hitstunTimer);


        // 禁用玩家输入
        DisablePlayerInput();

        // 改变颜色表示受击状态
        spriteRenderer.color = hitstunData.hitstunColor;

        // 触发事件
        OnHitstunStart?.Invoke();

        //Debug.Log($"开始硬直: {hitstunTimer}秒, 攻击类型: {attackType}");
    }

    private void EndHitstun()
    {
        bool wasKnockedDown = isKnockedDown;

        isInHitstun = false;
        hitstunTimer = 0f;
 

        // 恢复玩家输入
        EnablePlayerInput();

        // 恢复原始颜色
        spriteRenderer.color = originalColor;


        // 如果之前是击倒状态，触发恢复
        if (wasKnockedDown)
        {
            RecoverFromKnockdown();
        }

        // 触发事件
        OnHitstunEnd?.Invoke();

        //Debug.Log("硬直结束");
    }

    private float GetHitstunDuration(AttackType attackType)
    {
        float baseDuration = attackType switch
        {
            AttackType.Light => hitstunData.lightHitstun,
            AttackType.Heavy => hitstunData.heavyHitstun,
            AttackType.knockdown => hitstunData.knockdownHitstun,
            _ => hitstunData.lightHitstun
        };


        return baseDuration;
    }

    //private float GetCurrentHitstunDuration()
    //{
    //    return hitstunTimer + (Time.time - Time.fixedTime);
    //}

    private void ApplyKnockback(AttackConfig attackConfig, Vector2 attackDirection)
    {
        if (rb == null || attackConfig == null)
        {
            Debug.LogWarning("Rigidbody2D 或 AttackConfig 缺失，无法应用击退");
            return;
        }

        float knockbackForce = attackConfig.knockbackForce;


        // 停止当前速度
        rb.velocity = Vector2.zero;

        Vector2 knockback;

         if (attackConfig.isAddUpForce)
        {
            knockback = new Vector2(
             Mathf.Sign(attackDirection.x) * knockbackForce,  // 使用方向符号 * 完整水平力
             attackConfig.knockupForce                        // 使用完整垂直力
         );

        }
        else
        {
            // 只有水平击退
            knockback = new Vector2(attackDirection.x * knockbackForce, 0f);
   
        }
        rb.AddForce(knockback, ForceMode2D.Impulse);
    }

    private void PlayHitAnimation(AttackType attackType)
    {
        if (animator != null)
        {
        
            string animationName = attackType switch
            {
                AttackType.Light => "LightHit",
                AttackType.Heavy => "HeavyHit",
                AttackType.knockdown => "KnockdownHit",
                _ => "LightHit"
            };
            Debug.Log(animationName);
            animator.PlayAnimation(animationName);
            
        }
    }

    private void PlayHitEffects()
    {
        // TODO: 播放受击音效
        // AudioManager.Instance.PlaySFX("HitSound");

        // TODO: 播放受击特效
        // EffectManager.Instance.PlayEffect("HitEffect", transform.position);
    }

    private void DisablePlayerInput()
    {
        canMove = false;
        canAttack = false;
        canJump = false;
        canDash = false;
        canBlock = false;
        canCrouch = false;

        // 如果PlayerController有这些状态，强制停止
        if (playerController != null)
        {
            // 停止攻击状态
            playerController.isAttack = false;

            // 停止格挡状态
            playerController.isBlock = false;

            // 击倒时停止所有动作
            if (isKnockedDown)
            {
                playerController.isDash = false;
                playerController.isJump = false;
                playerController.isWalk = false;
                playerController.isCrouch = false;
            }
        }
    }

    private void EnablePlayerInput()
    {
        // 如果仍在击倒状态，延迟恢复某些输入
        if (isKnockedDown)
        {
            canMove = false; // 击倒状态下短暂不能移动
            canAttack = false;
            canJump = false;
            canDash = false;
            canBlock = false;
            canCrouch=false;
        }
        else
        {
            canMove = true;
            canAttack = true;
            canJump = true;
            canDash = true;
            canBlock = true;
            canCrouch = true;
        }
    }

    // 击倒特殊处理
    private void HandleKnockdown()
    {
        isKnockedDown = true;

        // 击倒时设置为无敌状态
        SetInvincible(true);

        // 击倒时的特殊硬直处理
        StartHitstun(AttackType.knockdown);

        // 击倒时强制进入倒地状态
        if (playerController != null)
        {
            // 停止所有动作
            playerController.isAttack = false;
            playerController.isBlock = false;
            playerController.isDash = false;
            playerController.isJump = false;
            playerController.isCrouch = false;
        }

        // 触发击倒事件
        OnKnockdown?.Invoke();

        //Debug.Log("玩家被击倒!");
    }

    // 从击倒状态恢复
    private void RecoverFromKnockdown()
    {
        isKnockedDown = false;

        // 播放起身动画
        if (animator != null)
        {
            //animator.PlayAnimation("GetUp");
        }

        // 延迟恢复完全控制（起身动画期间）
        Invoke(nameof(DelayedInputRecovery), 0.5f);

        // 起身后延迟取消无敌状态
        Invoke(nameof(EndInvincibilityAfterGetUp), 1.0f);

        // 触发起身事件
        OnGetUp?.Invoke();

        //Debug.Log("玩家从击倒状态恢复");
    }

    // 延迟输入恢复（用于起身动画）
    private void DelayedInputRecovery()
    {
        if (!isInHitstun) // 确保不在其他硬直中
        {
            EnablePlayerInput();
        }
    }

    // 起身后结束无敌状态
    private void EndInvincibilityAfterGetUp()
    {
        if (!isInHitstun && !isKnockedDown)
        {
            SetInvincible(false);
        }
    }

    // 设置无敌状态（使用PlayerController的方法）
    public void SetInvincible(bool invincible)
    {
        if (playerController == null) return;

        // 使用PlayerController的SetInvulnerable方法
        playerController.SetInvulnerable(invincible);

        //if (invincible)
        //{
        //    Debug.Log("进入无敌状态");
        //}
        //else
        //{
        //    Debug.Log("无敌状态结束");
        //}
    }

    // 公共方法供其他系统查询
    public bool IsInHitstun() => isInHitstun;
    public bool IsKnockedDown() => isKnockedDown;
    public bool IsMoveAllowed() => canMove;
    public bool IsAttackAllowed() => canAttack;
    public bool IsJumpAllowed() => canJump;
    public bool IsDashAllowed() => canDash;
    public bool IsBlockAllowed() => canBlock;
    public bool IsCrouchAllowed() => canCrouch;
    public float GetRemainingHitstunTime() => hitstunTimer;

    // 强制结束硬直（用于特殊情况）
    public void ForceEndHitstun()
    {
        if (isInHitstun)
        {
            EndHitstun();
        }
    }

    // 强制从击倒状态恢复
    public void ForceGetUp()
    {
        if (isKnockedDown)
        {
            RecoverFromKnockdown();
        }
    }

    // 修改硬直时间（用于特殊效果）
    public void ModifyHitstunDuration(float multiplier)
    {
        if (isInHitstun)
        {
            hitstunTimer *= multiplier;
        }
    }




}