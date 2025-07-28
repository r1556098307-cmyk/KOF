using System;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.InputSystem;

public class KiritoController : MonoBehaviour
{
    public PlayerMovementData movementData;
    public PlayerCombatData combatData;

    public PlayerInputControl inputControl;
    private Rigidbody2D rb;
    public KiritoAnimator animator;

    public Vector2 inputDirection;

    public bool isFacingRight;
    public bool isDash;
    public bool isWalk;
    public bool isJump;
    public bool isCrouch;
    public bool isGround;
    public bool isAttack;
    public bool isJumpFall;

    // 攻击速度控制
    private bool isAttackSpeedActive = false;
    private float attackSpeedTimer = 0f;

    //冲刺控制
    private float dashTimer = 0f;
    private bool canDash = true;
    private float dashCooldownTimer = 0f;

    public float lastOnGroundTime; // 实现土狼时间优化
    public float LastPressedJumpTime;

    [SerializeField]
    private Transform groundCheckPoint;
    [SerializeField]
    private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField]
    private LayerMask groundLayer;

    private void Awake()
    {
        inputControl = new PlayerInputControl();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<KiritoAnimator>();
        inputControl.GamePlay.Attack.started += Attack;
        inputControl.GamePlay.Jump.started += Jump;
        inputControl.GamePlay.Dash.started += Dash;

        //TODO:添加下蹲
        inputControl.GamePlay.Crouch.started += Crouch;
        inputControl.GamePlay.Crouch.canceled += CrouchCancel;
        //TODO:添加格挡
    }



    private void Start()
    {
        isFacingRight = true;
    }

    private void OnEnable()
    {
        inputControl.Enable();
    }

    private void OnDisable()
    {
        inputControl.Disable();
    }

    private void Update()
    {
        #region 计时器更新
        lastOnGroundTime -= Time.deltaTime;
        LastPressedJumpTime -= Time.deltaTime;

        // 冲刺计时器
        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                EndDash();
            }
        }

        // 冲刺冷却
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0)
            {
                canDash = true;
            }
        }
        #endregion
        inputDirection = inputControl.GamePlay.Move.ReadValue<Vector2>();

        // 处理Sprite的翻转（冲刺时不能转向）
        if (!isDash && inputDirection.x != 0)
        {
            isWalk = true;
            CheckDirectionToFace(inputDirection.x > 0);
        }
        else
        {
            isWalk = false;
        }

        // 地面检测
        if (!isDash && !isJump)
        {
            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
            {
                // 如果之前不在地面上，现在着陆了
                if (lastOnGroundTime < -0.1f)
                {
                    // 可以在这里添加着陆音效或特效
                    animator.JustLanded();
                }

                isGround = true;
                lastOnGroundTime = movementData.coyoteTime;
            }
            else
            {
                isGround = false;
            }
        }

        if (isJump && rb.velocity.y < 0)
        {
            isJump = false;
            isJumpFall = true;
        }

        // 跳跃检测（冲刺时不能跳跃）
        if (!isDash)
        {
            if (CanJump() && LastPressedJumpTime > 0)
            {
                // 重置跳跃相关计时器
                lastOnGroundTime = 0;
                LastPressedJumpTime = 0;

                // 设置跳跃状态
                isJump = true;
                isGround = false;

                // 计算跳跃力度，如果正在下降则补偿向下的速度
                float jumpForce = movementData.jumpForce;
                if (rb.velocity.y < 0)
                    jumpForce -= rb.velocity.y;

                // 施加向上的跳跃力
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

                // 通知动画系统开始跳跃
                animator.StartJump();
            }
        }

        // 如果回到地面，重置跳跃状态
        if (lastOnGroundTime > 0 && !isJump)
        {
            isJumpFall = false;
        }

        // 更新攻击速度状态
        UpdateAttackSpeed();

        // 调整重力缩放
        UpdateGravity();
    }

    private void UpdateGravity()
    {
        if (!isDash)  // 非冲刺状态
        {
            // 快速下落（按住下键）
            if (rb.velocity.y < 0 && inputDirection.y < 0)
            {
                SetGravityScale(movementData.gravityScale * movementData.fastFallGravityMult);
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -movementData.maxFastFallSpeed));
            }
            // 跳跃顶点时的悬停感（可选）
            else if ((isJump || isJumpFall) && Mathf.Abs(rb.velocity.y) < movementData.jumpHangTimeThreshold)
            {
                SetGravityScale(movementData.gravityScale * movementData.jumpHangGravityMult);
            }
            // 下落时增加重力
            else if (rb.velocity.y < 0)
            {
                SetGravityScale(movementData.gravityScale * movementData.fallGravityMult);
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -movementData.maxFallSpeed));
            }
            // 默认重力（上升时）
            else
            {
                SetGravityScale(movementData.gravityScale);
            }
        }
        else
        {
            // 冲刺时无重力
            SetGravityScale(0);
        }
    }

    private void SetGravityScale(float scale)
    {
        rb.gravityScale = scale;
    }

    private void FixedUpdate()
    {
        if (isDash)
        {
            // 冲刺时使用特殊的移动逻辑
            ApplyDashMovement();
        }
        else
        {
            // 普通移动
            Move(1);
        }
    }

    public void Move(float lerpAmount)
    {
        // 使用平滑插值来加速
        float targetSpeed = inputDirection.x * movementData.runMaxSpeed;
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

        float accelRate;

        // 动态加速度计算，在空中和地面使用不同的加速度
        if (lastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? movementData.runAccelAmount : movementData.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? movementData.runAccelAmount * movementData.accelInAir : movementData.runDeccelAmount * movementData.deccelInAir;

        // 跳跃顶点加速
        if ((isJump || isJumpFall) && Mathf.Abs(rb.velocity.y) < movementData.jumpHangTimeThreshold)
        {
            accelRate *= movementData.jumpHangAccelerationMult;
            targetSpeed *= movementData.jumpHangMaxSpeedMult;
        }

        // 动量保持
        if (movementData.doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) &&
            Mathf.Sign(rb.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && lastOnGroundTime < 0)
        {
            accelRate = 0;
        }

        // 对rb提供力，速度离目标远则加速快，近则加速慢
        float speedDif = targetSpeed - rb.velocity.x;
        float movement = speedDif * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // 应用攻击速度限制
        ApplyAttackSpeedLimit();
    }

    private void UpdateAttackSpeed()
    {
        if (isAttackSpeedActive)
        {
            attackSpeedTimer -= Time.deltaTime;
            if (attackSpeedTimer <= 0f)
            {
                isAttackSpeedActive = false;
            }
        }
    }

    private void ApplyDashMovement()
    {
        // 冲刺时设置恒定速度
        float dashDirection = isFacingRight ? 1f : -1f;
        rb.velocity = new Vector2(dashDirection * movementData.dashSpeed, 0f);
    }

    private void ApplyAttackSpeedLimit()
    {
        if (isAttackSpeedActive)
        {
            // 限制水平速度不超过攻击最大速度
            float currentSpeed = rb.velocity.x;
            float maxSpeed = combatData.attackMaxSpeed;

            if (Mathf.Abs(currentSpeed) > maxSpeed)
            {
                // 将速度限制在最大攻击速度内，但保持方向
                float clampedSpeed = Mathf.Sign(currentSpeed) * maxSpeed;
                rb.velocity = new Vector2(clampedSpeed, rb.velocity.y);
            }

            // 应用速度衰减
            float decayFactor = Mathf.Lerp(1f, combatData.attackSpeedDecay, Time.fixedDeltaTime / 0.02f);
            rb.velocity = new Vector2(rb.velocity.x * decayFactor, rb.velocity.y);
        }
    }

    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != isFacingRight)
            Turn();
    }

    private void Turn()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        isFacingRight = !isFacingRight;
    }

    private void Attack(InputAction.CallbackContext obj)
    {
        // 冲刺时不能攻击
        if (isDash) return;

        animator.Attack();
        isAttack = true;

        // 施加攻击力和启动速度控制
        if(!isCrouch)
            ApplyAttackForce();
    }

    private void Jump(InputAction.CallbackContext obj)
    {
        LastPressedJumpTime = movementData.jumpInputBufferTime;
    }

    // 新增冲刺方法
    private void Dash(InputAction.CallbackContext obj)
    {
        if (canDash && !isDash)
        {
            StartDash();
        }
    }

    private void Crouch(InputAction.CallbackContext obj)
    {
        if (CanCrouch())
        {
            isCrouch = true;
            // TODO:切换为蹲下时的碰撞体
        }

    }
    private void CrouchCancel(InputAction.CallbackContext obj)
    {
        isCrouch = false;
    }

    private bool CanCrouch()
    {
        return isGround&&!isDash;
    }

    private void StartDash()
    {
        isDash = true;
        canDash = false;
        dashTimer = movementData.dashDuration;

        // 取消其他状态
        isAttack = false;
        isAttackSpeedActive = false;


        //TODO:冲刺时无敌，可以取消与skill层、player层的碰撞？
    }

    private void EndDash()
    {
        isDash = false;
        dashCooldownTimer = movementData.dashCooldown;

    }

    private bool CanJump()
    {
        // 可以跳跃的条件：
        // 1. 在地面上或在土狼时间内
        // 2. 当前没有在跳跃状态
        return lastOnGroundTime > 0 && !isJump;
    }

    private void ApplyAttackForce()
    {
        // 计算攻击方向
        Vector2 attackDirection = isFacingRight ? Vector2.right : Vector2.left;

        // 施加瞬间攻击力
        rb.AddForce(attackDirection * combatData.attackForce, ForceMode2D.Impulse);

        // 启动攻击速度控制
        isAttackSpeedActive = true;
        attackSpeedTimer = combatData.attackSpeedDecayDuration;
    }

    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
    }
    #endregion
}