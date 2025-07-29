using System;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.AllocatorManager;

public class PlayerController : MonoBehaviour
{
    public PlayerMovementData movementData;
    public PlayerCombatData combatData;

    private Rigidbody2D rb;
    public PlayerAnimator animator;
    private ComboSystem comboSystem;

    public Vector2 inputDirection;

    public bool isFacingRight;
    public bool isDash;
    public bool isWalk;
    public bool isJump;
    public bool isCrouch;
    public bool isBlock;
    public bool isGround;
    public bool isAttack;
    public bool isJumpFall;

    // �����ٶȿ���
    private bool isAttackSpeedActive = false;
    private float attackSpeedTimer = 0f;

    //��̿���
    private float dashTimer = 0f;
    private bool canDash = true;
    private float dashCooldownTimer = 0f;

    // ��Ծ�Ż�
    public float lastOnGroundTime; // ����ʱ���Ż�
    public float LastPressedJumpTime; // ��Ծ�����Ż�

    public LayerMask wallLayer;                 // ǽ��㣨ͨ����groundLayer��ͬ��

    private bool isWallSliding = false;         // �Ƿ���ǽ�滬��
    private bool isTouchingWall = false;        // �Ƿ�Ӵ�ǽ��

    [SerializeField]
    private Transform groundCheckPoint;
    [SerializeField]
    private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField]
    private LayerMask groundLayer;

    // ǽ�����
    [SerializeField]
    private Transform wallCheckPoint;           // ǽ�����
    [SerializeField]
    private Vector2 wallCheckSize = new Vector2(1f, 0.8f); // ǽ���������С

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<PlayerAnimator>();
        comboSystem = GetComponent<ComboSystem>();

        // ���û������wallLayer��ʹ��groundLayer
        if (wallLayer == 0)
        {
            wallLayer = groundLayer;
        }
    }

    private void Start()
    {
        isFacingRight = true;
    }


    private void Update()
    {
        #region ��ʱ������
        lastOnGroundTime -= Time.deltaTime;
        LastPressedJumpTime -= Time.deltaTime;

        // ��̼�ʱ��
        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                EndDash();
            }
        }

        // �����ȴ
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0)
            {
                canDash = true;
            }
        }
        #endregion

        if (comboSystem != null)
        {
            inputDirection = comboSystem.GetMovementInput();
        }

        // ����Sprite�ķ�ת�����ʱ����ת��
        if (!isDash && inputDirection.x != 0)
        {
            if (!isBlock)
                isWalk = true;
            CheckDirectionToFace(inputDirection.x > 0);
        }
        else
        {
            isWalk = false;
        }

        // ǽ���⣨�ڵ�����֮ǰ��
        CheckWallSliding();

        // ������
        if (!isDash && !isJump)
        {
            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
            {
                // ���֮ǰ���ڵ����ϣ�������½��
                if (lastOnGroundTime < -0.1f)
                {
                    // ���������������½��Ч����Ч
                    //animator.JustLanded();
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

        // ��Ծ��⣨���ʱ������Ծ��
        if (!isDash)
        {
            if (CanJump() && LastPressedJumpTime > 0)
            {
                // ������Ծ��ؼ�ʱ��
                lastOnGroundTime = 0;
                LastPressedJumpTime = 0;

                // ������Ծ״̬
                isJump = true;
                isGround = false;
                isWallSliding = false; // ��Ծʱֹͣǽ�滬��

                // ������Ծ���ȣ���������½��򲹳����µ��ٶ�
                float jumpForce = movementData.jumpForce;
                if (rb.velocity.y < 0)
                    jumpForce -= rb.velocity.y;

                // ʩ�����ϵ���Ծ��
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
        }

        // ����ص����棬������Ծ״̬
        if (lastOnGroundTime > 0 && !isJump)
        {
            isJumpFall = false;
            isWallSliding = false; // ��½ʱֹͣǽ�滬��
        }

        // ���¹����ٶ�״̬
        UpdateAttackSpeed();

        // ������������
        UpdateGravity();
    }

    private void CheckWallSliding()
    {
        // ֻ���ڿ����Ҳ��ڳ��״̬ʱ�ż��ǽ�滬��
        if (isGround || isDash)
        {
            isWallSliding = false;
            isTouchingWall = false;
            return;
        }

        // ����Ƿ�Ӵ�ǽ��
        isTouchingWall = Physics2D.OverlapBox(wallCheckPoint.position, wallCheckSize, 0, wallLayer);
        // Debug.Log(isTouchingWall);
        if (isTouchingWall)
        {

            // �ж��Ƿ�Ӧ�ÿ�ʼǽ�滬��
            // �������Ӵ�ǽ�� + �ڿ��� + ��������
            if (rb.velocity.y <= 0)
            {
                isWallSliding = true;
            }
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void UpdateGravity()
    {
        if (!isDash)  // �ǳ��״̬
        {
            if (isWallSliding)
            {
                // ǽ�滬��ʱʹ����������
                SetGravityScale(movementData.gravityScale * movementData.wallSlideGravity);

                // ����ǽ�滬���ٶ�
                if (rb.velocity.y < -movementData.wallSlideSpeed)
                {
                    rb.velocity = new Vector2(rb.velocity.x, -movementData.wallSlideSpeed);
                }
            }
            // �������䣨��ס�¼���
            else if (rb.velocity.y < 0 && inputDirection.y < 0)
            {
                SetGravityScale(movementData.gravityScale * movementData.fastFallGravityMult);
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -movementData.maxFastFallSpeed));
            }
            // ��Ծ����ʱ����ͣ�У���ѡ��
            else if ((isJump || isJumpFall) && Mathf.Abs(rb.velocity.y) < movementData.jumpHangTimeThreshold)
            {
                SetGravityScale(movementData.gravityScale * movementData.jumpHangGravityMult);
            }
            // ����ʱ��������
            else if (rb.velocity.y < 0)
            {
                SetGravityScale(movementData.gravityScale * movementData.fallGravityMult);
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -movementData.maxFallSpeed));
            }
            // Ĭ������������ʱ��
            else
            {
                SetGravityScale(movementData.gravityScale);
            }
        }
        else
        {
            // ���ʱ������
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
            // ���ʱʹ��������ƶ��߼�
            ApplyDashMovement();
        }
        else
        {
            // ��ͨ�ƶ�
            Move(1);
        }
    }

    public void Move(float lerpAmount)
    {
        // ���ڼ䲻���ƶ�
        if (isBlock)
        {
            // ֹͣˮƽ�ƶ�
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }


        // ʹ��ƽ����ֵ������
        float speedMultiplier = isCrouch ? movementData.crouchSpeedMultiplier : 1f;

        float targetSpeed = inputDirection.x * movementData.runMaxSpeed * speedMultiplier;
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

        float accelRate;

        // ��̬���ٶȼ��㣬�ڿ��к͵���ʹ�ò�ͬ�ļ��ٶ�
        if (lastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? movementData.runAccelAmount : movementData.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? movementData.runAccelAmount * movementData.accelInAir : movementData.runDeccelAmount * movementData.deccelInAir;

        // ��Ծ�������
        if ((isJump || isJumpFall) && Mathf.Abs(rb.velocity.y) < movementData.jumpHangTimeThreshold)
        {
            accelRate *= movementData.jumpHangAccelerationMult;
            targetSpeed *= movementData.jumpHangMaxSpeedMult;
        }

        // ��rb�ṩ�����ٶ���Ŀ��Զ����ٿ죬���������
        float speedDif = targetSpeed - rb.velocity.x;
        float movement = speedDif * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // Ӧ�ù����ٶ�����
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
        // ���ʱ���ú㶨�ٶ�
        float dashDirection = isFacingRight ? 1f : -1f;
        rb.velocity = new Vector2(dashDirection * movementData.dashSpeed, 0f);
    }

    private void ApplyAttackSpeedLimit()
    {
        if (isAttackSpeedActive)
        {
            // ����ˮƽ�ٶȲ�������������ٶ�
            float currentSpeed = rb.velocity.x;
            float maxSpeed = combatData.attackMaxSpeed;

            if (Mathf.Abs(currentSpeed) > maxSpeed)
            {
                // ���ٶ���������󹥻��ٶ��ڣ������ַ���
                float clampedSpeed = Mathf.Sign(currentSpeed) * maxSpeed;
                rb.velocity = new Vector2(clampedSpeed, rb.velocity.y);
            }

            // Ӧ���ٶ�˥��
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

    public void PerformAttack()
    {
        // ���ʱ��ǽ�滬��ʱ���ܹ���
        if (isDash || isWallSliding) return;

        animator.Attack();
        isAttack = true;

        // ʩ�ӹ������������ٶȿ���
        if (!isCrouch)
            ApplyAttackForce();
    }

    public void PerformJump()
    {
        LastPressedJumpTime = movementData.jumpInputBufferTime;
    }

    public void PerformDash()
    {
        if (canDash && !isDash)
        {
            StartDash();
        }
    }

    public void PerformCrouch(bool isPressed)
    {
        if (isPressed)
        {
            if (CanCrouch())
            {
                isCrouch = true;
            }
        }
        else
        {
            isCrouch = false;
        }
    }

    public void PerformBlock(bool isPressed)
    {
        if (isPressed)
        {
            if (CanBlock())
            {
                isBlock = true;
                rb.velocity = new Vector2(0, rb.velocity.y);
                isAttack = false;
                isAttackSpeedActive = false;
            }
        }
        else
        {
            isBlock = false;
        }
    }

    private bool CanCrouch()
    {
        return isGround && !isDash && !isWallSliding;
    }

    private bool CanBlock()
    {
        return isGround && !isDash && !isWallSliding;
    }

    private void StartDash()
    {
        isDash = true;
        canDash = false;
        dashTimer = movementData.dashDuration;

        // ȡ������״̬
        isAttack = false;
        isAttackSpeedActive = false;
        isWallSliding = false; // ���ʱֹͣǽ�滬��

        //TODO:���ʱ�޵У�����ȡ����skill�㡢player�����ײ��
    }

    private void EndDash()
    {
        isDash = false;
        dashCooldownTimer = movementData.dashCooldown;
    }

    private bool CanJump()
    {
        // ������Ծ��������
        // 1. �ڵ����ϻ�������ʱ����
        // 2. ��ǰû������Ծ״̬
        return lastOnGroundTime > 0 && !isJump;
    }

    private void ApplyAttackForce()
    {
        // ���㹥������
        Vector2 attackDirection = isFacingRight ? Vector2.right : Vector2.left;

        // ʩ��˲�乥����
        rb.AddForce(attackDirection * combatData.attackForce, ForceMode2D.Impulse);

        // ���������ٶȿ���
        isAttackSpeedActive = true;
        attackSpeedTimer = combatData.attackSpeedDecayDuration;
    }

    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        // ��������ӻ�
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }

        // ǽ������ӻ�
        if (wallCheckPoint != null)
        {
            Gizmos.color = isWallSliding ? Color.red : Color.blue;
            Gizmos.DrawWireCube(wallCheckPoint.position, wallCheckSize);
        }
    }
    #endregion
}