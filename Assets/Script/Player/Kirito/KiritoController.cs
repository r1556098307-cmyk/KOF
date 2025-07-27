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
    public bool isGround;
    public bool isAttack;
    public bool isJumpFall;

    // 攻击速度控制
    private bool isAttackSpeedActive = false;
    private float attackSpeedTimer = 0f;

    // 冲刺攻击状态
    //[SerializeField]
    //private bool isDashAttacking = false;
    [SerializeField]
    private float dashAttackTimer = 0f;

    public float lastOnGroundTime; // 实现土狼时间优化
    //TODO:实现跳跃缓冲手感优化

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
                Debug.Log("地面");
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

        // 跳跃状态检测
        if (isJump && rb.velocity.y < 0)
        {
            // 当跳跃到顶点开始下降时，切换到下降状态
            isJump = false;
            isJumpFall = true;
        }

        // 如果回到地面，重置跳跃状态
        if (lastOnGroundTime > 0 && !isJump)
        {
            isJumpFall = false;
        }

        // 更新攻击速度状态
        UpdateAttackSpeed();

        //TODO:调整重力缩放
    }

    private void FixedUpdate()
    {
        Move(1);
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
        // 在冲刺期间维持冲刺速度并应用衰减
        Vector2 currentVelocity = rb.velocity;

        // 限制冲刺速度不超过最大值
        float currentHorizontalSpeed = currentVelocity.x;
        if (Mathf.Abs(currentHorizontalSpeed) > combatData.dashAttackMaxSpeed)
        {
            currentHorizontalSpeed = Mathf.Sign(currentHorizontalSpeed) * combatData.dashAttackMaxSpeed;
        }

        // 应用冲刺速度衰减
        currentHorizontalSpeed *= combatData.dashAttackDecay;

        // 更新速度
        rb.velocity = new Vector2(currentHorizontalSpeed, currentVelocity.y);
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

        animator.Attack();
        isAttack = true;

        // 施加攻击力和启动速度控制
        ApplyAttackForce();
    }

    private void Jump(InputAction.CallbackContext obj)
    {
        Debug.Log("Jump");
        // 检查是否可以跳跃（在地面上或土狼时间内）
        if (CanJump())
        {
            // 重置跳跃相关计时器
            lastOnGroundTime = 0;

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