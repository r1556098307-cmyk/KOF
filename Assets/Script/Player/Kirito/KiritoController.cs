using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class PlayerData
{
    public float runMaxSpeed = 10f;
    public float runAccelAmount = 9.5f; // 地面加速度
    public float runDeccelAmount = 9.5f; // 地面减速度
    public float accelInAir = 1f; // 空中加速倍率
    public float deccelInAir = 1f; // 空中减速倍率
    public float jumpHangTimeThreshold = 1f; // 跳跃悬停判定阈值
    public float jumpHangAccelerationMult = 1.1f; // 跳跃顶点加速倍率
    public float jumpHangMaxSpeedMult = 1.3f; // 跳跃顶点最大速度倍率
    public float coyoteTime = 0.1f; // 土狼时间

    public bool doConserveMomentum = false; // 是否开启动量保持

    [Header("攻击位移参数")]
    public float attackForce = 15f; // 攻击时施加的瞬间力
    public float attackMaxSpeed = 8f; // 攻击时的最大速度限制
    public float attackSpeedDecay = 0.95f; // 攻击后速度衰减系数（每帧）
    public float attackSpeedDecayDuration = 0.5f; // 速度衰减持续时间

    [Header("冲刺攻击参数")]
    public float dashAttackForce = 20f;        // 冲刺攻击力度
    public float dashAttackMaxSpeed = 15f;     // 冲刺最大速度
    public float dashAttackDuration = 0.3f;    // 冲刺持续时间
    public float dashAttackDecay = 0.92f;      // 冲刺速度衰减
    public bool dashAttackIgnoreInput = true;  // 冲刺时是否忽略移动输入
}

public class KiritoController : MonoBehaviour
{
    public PlayerInputControl inputControl;
    private Rigidbody2D rb;
    public KiritoAnimator animator;

    public Vector2 inputDirection;
    public PlayerData playerData;

    public bool isFacingRight;
    public bool isDash;
    public bool isWalk;
    public bool isJump;
    public bool isGround;
    public bool isAttack;
    public bool isJumpFall;

    // 攻击速度控制
    private bool isAttackSpeedActive = false;
    private float attackSpeedTimer = 0f;

    // 冲刺攻击状态
    private bool isDashAttacking = false;
    private float dashAttackTimer = 0f;
    private Vector2 dashDirection;

    // 输入缓冲和组合攻击控制
    [Header("输入控制参数")]
    public float inputBufferTime = 0.15f; // 输入缓冲时间
    public float comboWindowTime = 0.2f;  // 组合技窗口时间

    private float lastAttackInputTime = -1f;  // J键输入时间
    private float lastCrouchInputTime = -1f;  // S键输入时间
    private bool isProcessingCombo = false;
    private bool isCrouching = false;

    public float lastOnGroundTime; // 实现土狼时间优化

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
        inputControl.GamePlay.Attack.started += Attack;  // J键 - 普通攻击
        inputControl.GamePlay.DownAttack.started += Crouch;  // S键 - 下蹲
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
        lastOnGroundTime -= Time.deltaTime;

        inputDirection = inputControl.GamePlay.Move.ReadValue<Vector2>();

        // 处理Sprite的翻转
        if (inputDirection.x != 0)
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
                if (lastOnGroundTime < -0.1f)
                {
                    // Animator的params设置在地面上
                }
                isGround = true;
                lastOnGroundTime = playerData.coyoteTime;
            }
        }

        // 更新攻击速度状态
        UpdateAttackSpeed();

        // 更新冲刺攻击状态
        UpdateDashAttack();

        // 处理输入缓冲和组合攻击
        ProcessCombatInput();
    }

    private void FixedUpdate()
    {
        Move(1);
    }

    public void Move(float lerpAmount)
    {
        // 如果正在冲刺攻击且忽略输入，则不处理常规移动
        if (isDashAttacking && playerData.dashAttackIgnoreInput)
        {
            ApplyDashMovement();
            return;
        }

        // 使用平滑插值来加速
        float targetSpeed = inputDirection.x * playerData.runMaxSpeed;
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

        float accelRate;

        // 动态加速度计算，在空中和地面使用不同的加速度
        if (lastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? playerData.runAccelAmount : playerData.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? playerData.runAccelAmount * playerData.accelInAir : playerData.runDeccelAmount * playerData.deccelInAir;

        // 跳跃顶点加速
        if ((isJump || isJumpFall) && Mathf.Abs(rb.velocity.y) < playerData.jumpHangTimeThreshold)
        {
            accelRate *= playerData.jumpHangAccelerationMult;
            targetSpeed *= playerData.jumpHangMaxSpeedMult;
        }

        // 动量保持
        if (playerData.doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) &&
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

    private void UpdateDashAttack()
    {
        if (isDashAttacking)
        {
            dashAttackTimer -= Time.deltaTime;
            if (dashAttackTimer <= 0f)
            {
                isDashAttacking = false;
                isProcessingCombo = false; // 冲刺结束后重置组合状态
                // 冲刺结束后可以添加一些额外逻辑，比如恢复正常状态
            }
        }
    }

    private void ProcessCombatInput()
    {
        // 如果正在处理组合攻击，不处理新输入
        if (isProcessingCombo) return;

        float currentTime = Time.time;

        // 检查是否有组合攻击输入 (S+J 在时间窗口内)
        bool hasRecentAttack = (currentTime - lastAttackInputTime) <= comboWindowTime;
        bool hasRecentCrouch = (currentTime - lastCrouchInputTime) <= comboWindowTime;

        // 优先级1: S+J组合攻击 (冲刺攻击)
        if (hasRecentAttack && hasRecentCrouch)
        {
            ExecuteComboAttack();
            return;
        }

        // 优先级2: 单独的普通攻击 (J)
        if (hasRecentAttack && !hasRecentCrouch)
        {
            ExecuteNormalAttack();
            return;
        }

        // 优先级3: 单独的下蹲 (S)
        if (hasRecentCrouch && !hasRecentAttack)
        {
            ExecuteCrouch();
            return;
        }
    }

    private void ExecuteComboAttack()
    {
        // 如果已经在冲刺攻击中，则不能再次触发
        if (isDashAttacking) return;

        Debug.Log("执行冲刺攻击: S+J");

        isProcessingCombo = true;
        animator.DownAttack(); // 冲刺攻击动画
        DashAttack();

        // 清除输入记录
        lastAttackInputTime = -1f;
        lastCrouchInputTime = -1f;
    }

    private void ExecuteNormalAttack()
    {
        // 如果正在冲刺攻击，则不能进行普通攻击
        if (isDashAttacking) return;

        Debug.Log("执行普通攻击: J");

        isProcessingCombo = true;
        animator.Attack();
        isAttack = true;
        ApplyAttackForce();

        // 清除输入记录
        lastAttackInputTime = -1f;

        // 普通攻击结束得快一些
        StartCoroutine(ResetComboStateAfterDelay(0.1f));
    }

    private void ExecuteCrouch()
    {
        // 如果正在攻击，则不能下蹲
        if (isDashAttacking || isAttack) return;

        Debug.Log("执行下蹲: S");

        // 下蹲逻辑
        isCrouching = true;
        // 这里可以添加下蹲动画
        // animator.Crouch();

        // 清除输入记录
        lastCrouchInputTime = -1f;

        // 下蹲状态可以持续到松开按键或其他条件
        StartCoroutine(HandleCrouchState());
    }

    private System.Collections.IEnumerator HandleCrouchState()
    {
        // 等待一小段时间后允许其他输入
        yield return new WaitForSeconds(0.05f);

        // 检查是否还在按住下蹲键
        Vector2 currentInput = inputControl.GamePlay.Move.ReadValue<Vector2>();
        if (currentInput.y >= -0.5f) // 如果没有向下输入
        {
            isCrouching = false;
            // animator.StopCrouch(); // 停止下蹲动画
        }
    }

    private System.Collections.IEnumerator ResetComboStateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isProcessingCombo = false;
    }

    private void ApplyDashMovement()
    {
        // 在冲刺期间维持冲刺速度并应用衰减
        Vector2 currentVelocity = rb.velocity;

        // 限制冲刺速度不超过最大值
        float currentHorizontalSpeed = currentVelocity.x;
        if (Mathf.Abs(currentHorizontalSpeed) > playerData.dashAttackMaxSpeed)
        {
            currentHorizontalSpeed = Mathf.Sign(currentHorizontalSpeed) * playerData.dashAttackMaxSpeed;
        }

        // 应用冲刺速度衰减
        currentHorizontalSpeed *= playerData.dashAttackDecay;

        // 更新速度
        rb.velocity = new Vector2(currentHorizontalSpeed, currentVelocity.y);
    }

    private void ApplyAttackSpeedLimit()
    {
        if (isAttackSpeedActive && !isDashAttacking) // 冲刺时不应用普通攻击的速度限制
        {
            // 限制水平速度不超过攻击最大速度
            float currentSpeed = rb.velocity.x;
            float maxSpeed = playerData.attackMaxSpeed;

            if (Mathf.Abs(currentSpeed) > maxSpeed)
            {
                // 将速度限制在最大攻击速度内，但保持方向
                float clampedSpeed = Mathf.Sign(currentSpeed) * maxSpeed;
                rb.velocity = new Vector2(clampedSpeed, rb.velocity.y);
            }

            // 应用速度衰减
            float decayFactor = Mathf.Lerp(1f, playerData.attackSpeedDecay, Time.fixedDeltaTime / 0.02f);
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
        // 记录攻击输入时间 (J键)
        lastAttackInputTime = Time.time;
    }

    private void Crouch(InputAction.CallbackContext obj)
    {
        // 记录下蹲输入时间 (S键)
        lastCrouchInputTime = Time.time;
    }

    private void DashAttack()
    {
        // 确定冲刺方向
        Vector2 targetDirection = GetDashDirection();

        // 先停止当前水平速度（可选，让冲刺更有冲击感）
        rb.velocity = new Vector2(0, rb.velocity.y);

        // 施加冲刺力
        Vector2 dashForce = targetDirection * playerData.dashAttackForce;
        rb.AddForce(dashForce, ForceMode2D.Impulse);

        // 启动冲刺状态
        isDashAttacking = true;
        dashAttackTimer = playerData.dashAttackDuration;
        dashDirection = targetDirection;

        // 停止普通攻击的速度效果，避免冲突
        StopAttackSpeed();

        //TODO: 视觉效果
    }

    private Vector2 GetDashDirection()
    {
        Vector2 forwardDirection = isFacingRight ? Vector2.right : Vector2.left;
        return forwardDirection;
    }

    private void ApplyAttackForce()
    {
        // 计算攻击方向
        Vector2 attackDirection = isFacingRight ? Vector2.right : Vector2.left;

        // 施加瞬间攻击力
        rb.AddForce(attackDirection * playerData.attackForce, ForceMode2D.Impulse);

        // 启动攻击速度控制
        isAttackSpeedActive = true;
        attackSpeedTimer = playerData.attackSpeedDecayDuration;
    }

    // 可以通过动画事件调用，在攻击动画的特定帧触发
    public void TriggerAttackForceFromAnimation()
    {
        ApplyAttackForce();
    }

    // 立即停止攻击速度效果
    public void StopAttackSpeed()
    {
        isAttackSpeedActive = false;
        attackSpeedTimer = 0f;
    }

    // 立即停止冲刺攻击
    public void StopDashAttack()
    {
        isDashAttacking = false;
        dashAttackTimer = 0f;
    }

    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
    }
    #endregion
}
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.InputSystem;

//[System.Serializable]
//public class PlayerData
//{
//    public float runMaxSpeed = 10f;
//    public float runAccelAmount = 9.5f; // 地面加速度
//    public float runDeccelAmount = 9.5f; // 地面减速度
//    public float accelInAir = 1f; // 空中加速倍率
//    public float deccelInAir = 1f; // 空中减速倍率
//    public float jumpHangTimeThreshold = 1f; // 跳跃悬停判定阈值
//    public float jumpHangAccelerationMult = 1.1f; // 跳跃顶点加速倍率
//    public float jumpHangMaxSpeedMult = 1.3f; // 跳跃顶点最大速度倍率
//    public float coyoteTime = 0.1f; // 土狼时间

//    public bool doConserveMomentum = false; // 是否开启动量保持

//    [Header("攻击位移参数")]
//    public float attackForce = 15f; // 攻击时施加的瞬间力
//    public float attackMaxSpeed = 8f; // 攻击时的最大速度限制
//    public float attackSpeedDecay = 0.95f; // 攻击后速度衰减系数（每帧）
//    public float attackSpeedDecayDuration = 0.5f; // 速度衰减持续时间

//    [Header("冲刺攻击参数")]
//    public float dashAttackForce = 20f;        // 冲刺攻击力度
//    public float dashAttackMaxSpeed = 15f;     // 冲刺最大速度
//    public float dashAttackDuration = 0.3f;    // 冲刺持续时间
//    public float dashAttackDecay = 0.92f;      // 冲刺速度衰减
//    public bool dashAttackIgnoreInput = true;  // 冲刺时是否忽略移动输入
//}

//public class KiritoController : MonoBehaviour
//{
//    public PlayerInputControl inputControl;
//    private Rigidbody2D rb;
//    public KiritoAnimator animator;

//    public Vector2 inputDirection;
//    public PlayerData playerData;

//    public bool isFacingRight;
//    public bool isDash;
//    public bool isWalk;
//    public bool isJump;
//    public bool isGround;
//    public bool isAttack;
//    public bool isJumpFall;

//    // 攻击速度控制
//    private bool isAttackSpeedActive = false;
//    private float attackSpeedTimer = 0f;

//    // 冲刺攻击状态
//    private bool isDashAttacking = false;
//    private float dashAttackTimer = 0f;
//    private Vector2 dashDirection;

//    public float lastOnGroundTime; // 实现土狼时间优化

//    [SerializeField]
//    private Transform groundCheckPoint;
//    [SerializeField]
//    private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
//    [SerializeField]
//    private LayerMask groundLayer;

//    private void Awake()
//    {
//        inputControl = new PlayerInputControl();
//        rb = GetComponent<Rigidbody2D>();
//        animator = GetComponent<KiritoAnimator>();
//        inputControl.GamePlay.Attack.started += Attack;
//        inputControl.GamePlay.DownAttack.started += DownAttack;
//    }

//    private void Start()
//    {
//        isFacingRight = true;
//    }

//    private void OnEnable()
//    {
//        inputControl.Enable();
//    }

//    private void OnDisable()
//    {
//        inputControl.Disable();
//    }

//    private void Update()
//    {
//        lastOnGroundTime -= Time.deltaTime;

//        inputDirection = inputControl.GamePlay.Move.ReadValue<Vector2>();

//        // 处理Sprite的翻转
//        if (inputDirection.x != 0)
//        {
//            isWalk = true;
//            CheckDirectionToFace(inputDirection.x > 0);
//        }
//        else
//        {
//            isWalk = false;
//        }

//        // 地面检测
//        if (!isDash && !isJump)
//        {
//            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
//            {
//                if (lastOnGroundTime < -0.1f)
//                {
//                    // Animator的params设置在地面上
//                }
//                isGround = true;
//                lastOnGroundTime = playerData.coyoteTime;
//            }
//        }

//        // 更新攻击速度状态
//        UpdateAttackSpeed();

//        // 更新冲刺攻击状态
//        UpdateDashAttack();
//    }

//    private void FixedUpdate()
//    {
//        Move(1);
//    }

//    public void Move(float lerpAmount)
//    {
//        // 如果正在冲刺攻击且忽略输入，则不处理常规移动
//        if (isDashAttacking && playerData.dashAttackIgnoreInput)
//        {
//            ApplyDashMovement();
//            return;
//        }

//        // 使用平滑插值来加速
//        float targetSpeed = inputDirection.x * playerData.runMaxSpeed;
//        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

//        float accelRate;

//        // 动态加速度计算，在空中和地面使用不同的加速度
//        if (lastOnGroundTime > 0)
//            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? playerData.runAccelAmount : playerData.runDeccelAmount;
//        else
//            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? playerData.runAccelAmount * playerData.accelInAir : playerData.runDeccelAmount * playerData.deccelInAir;

//        // 跳跃顶点加速
//        if ((isJump || isJumpFall) && Mathf.Abs(rb.velocity.y) < playerData.jumpHangTimeThreshold)
//        {
//            accelRate *= playerData.jumpHangAccelerationMult;
//            targetSpeed *= playerData.jumpHangMaxSpeedMult;
//        }

//        // 动量保持
//        if (playerData.doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) &&
//            Mathf.Sign(rb.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && lastOnGroundTime < 0)
//        {
//            accelRate = 0;
//        }

//        // 对rb提供力，速度离目标远则加速快，近则加速慢
//        float speedDif = targetSpeed - rb.velocity.x;
//        float movement = speedDif * accelRate;
//        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

//        // 应用攻击速度限制
//        ApplyAttackSpeedLimit();
//    }

//    private void UpdateAttackSpeed()
//    {
//        if (isAttackSpeedActive)
//        {
//            attackSpeedTimer -= Time.deltaTime;
//            if (attackSpeedTimer <= 0f)
//            {
//                isAttackSpeedActive = false;
//            }
//        }
//    }

//    private void UpdateDashAttack()
//    {
//        if (isDashAttacking)
//        {
//            dashAttackTimer -= Time.deltaTime;
//            if (dashAttackTimer <= 0f)
//            {
//                isDashAttacking = false;
//                // 冲刺结束后可以添加一些额外逻辑，比如恢复正常状态
//            }
//        }
//    }

//    private void ApplyDashMovement()
//    {
//        // 在冲刺期间维持冲刺速度并应用衰减
//        Vector2 currentVelocity = rb.velocity;

//        // 限制冲刺速度不超过最大值
//        float currentHorizontalSpeed = currentVelocity.x;
//        if (Mathf.Abs(currentHorizontalSpeed) > playerData.dashAttackMaxSpeed)
//        {
//            currentHorizontalSpeed = Mathf.Sign(currentHorizontalSpeed) * playerData.dashAttackMaxSpeed;
//        }

//        // 应用冲刺速度衰减
//        currentHorizontalSpeed *= playerData.dashAttackDecay;

//        // 更新速度
//        rb.velocity = new Vector2(currentHorizontalSpeed, currentVelocity.y);
//    }

//    private void ApplyAttackSpeedLimit()
//    {
//        if (isAttackSpeedActive && !isDashAttacking) // 冲刺时不应用普通攻击的速度限制
//        {
//            // 限制水平速度不超过攻击最大速度
//            float currentSpeed = rb.velocity.x;
//            float maxSpeed = playerData.attackMaxSpeed;

//            if (Mathf.Abs(currentSpeed) > maxSpeed)
//            {
//                // 将速度限制在最大攻击速度内，但保持方向
//                float clampedSpeed = Mathf.Sign(currentSpeed) * maxSpeed;
//                rb.velocity = new Vector2(clampedSpeed, rb.velocity.y);
//            }

//            // 应用速度衰减
//            float decayFactor = Mathf.Lerp(1f, playerData.attackSpeedDecay, Time.fixedDeltaTime / 0.02f);
//            rb.velocity = new Vector2(rb.velocity.x * decayFactor, rb.velocity.y);
//        }
//    }

//    public void CheckDirectionToFace(bool isMovingRight)
//    {
//        if (isMovingRight != isFacingRight)
//            Turn();
//    }

//    private void Turn()
//    {
//        Vector3 scale = transform.localScale;
//        scale.x *= -1;
//        transform.localScale = scale;
//        isFacingRight = !isFacingRight;
//    }

//    private void Attack(InputAction.CallbackContext obj)
//    {
//        // 如果正在冲刺攻击，则不能进行普通攻击
//        if (isDashAttacking) return;

//        animator.Attack();
//        isAttack = true;

//        // 施加攻击力和启动速度控制
//        ApplyAttackForce();
//    }

//    private void DownAttack(InputAction.CallbackContext obj)
//    {
//        // 如果已经在冲刺攻击中，则不能再次触发
//        if (isDashAttacking) return;

//        animator.DownAttack();
//        DashAttack();
//    }

//    private void DashAttack()
//    {
//        // 确定冲刺方向
//        Vector2 targetDirection = GetDashDirection();

//        // 先停止当前水平速度（可选，让冲刺更有冲击感）
//        rb.velocity = new Vector2(0, rb.velocity.y);

//        // 施加冲刺力
//        Vector2 dashForce = targetDirection * playerData.dashAttackForce;
//        rb.AddForce(dashForce, ForceMode2D.Impulse);

//        // 启动冲刺状态
//        isDashAttacking = true;
//        dashAttackTimer = playerData.dashAttackDuration;
//        dashDirection = targetDirection;

//        // 停止普通攻击的速度效果，避免冲突
//        StopAttackSpeed();

//        //TODO: 视觉效果
//    }

//    private Vector2 GetDashDirection()
//    {
//        Vector2 forwardDirection = isFacingRight ? Vector2.right : Vector2.left;
//        return forwardDirection;
//    }

//    private void ApplyAttackForce()
//    {
//        // 计算攻击方向
//        Vector2 attackDirection = isFacingRight ? Vector2.right : Vector2.left;

//        // 施加瞬间攻击力
//        rb.AddForce(attackDirection * playerData.attackForce, ForceMode2D.Impulse);

//        // 启动攻击速度控制
//        isAttackSpeedActive = true;
//        attackSpeedTimer = playerData.attackSpeedDecayDuration;
//    }

//    // 可以通过动画事件调用，在攻击动画的特定帧触发
//    public void TriggerAttackForceFromAnimation()
//    {
//        ApplyAttackForce();
//    }

//    // 立即停止攻击速度效果
//    public void StopAttackSpeed()
//    {
//        isAttackSpeedActive = false;
//        attackSpeedTimer = 0f;
//    }

//    // 立即停止冲刺攻击
//    public void StopDashAttack()
//    {
//        isDashAttacking = false;
//        dashAttackTimer = 0f;
//    }

//    #region EDITOR METHODS
//    private void OnDrawGizmosSelected()
//    {
//        Gizmos.color = Color.green;
//        Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
//    }
//    #endregion
//}