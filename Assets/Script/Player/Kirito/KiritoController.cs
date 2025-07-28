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

    // �����ٶȿ���
    private bool isAttackSpeedActive = false;
    private float attackSpeedTimer = 0f;

    //��̿���
    private float dashTimer = 0f;
    private bool canDash = true;
    private float dashCooldownTimer = 0f;

    public float lastOnGroundTime; // ʵ������ʱ���Ż�
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

        //TODO:����¶�
        inputControl.GamePlay.Crouch.started += Crouch;
        inputControl.GamePlay.Crouch.canceled += CrouchCancel;
        //TODO:��Ӹ�
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
        inputDirection = inputControl.GamePlay.Move.ReadValue<Vector2>();

        // ����Sprite�ķ�ת�����ʱ����ת��
        if (!isDash && inputDirection.x != 0)
        {
            isWalk = true;
            CheckDirectionToFace(inputDirection.x > 0);
        }
        else
        {
            isWalk = false;
        }

        // ������
        if (!isDash && !isJump)
        {
            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
            {
                // ���֮ǰ���ڵ����ϣ�������½��
                if (lastOnGroundTime < -0.1f)
                {
                    // ���������������½��Ч����Ч
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

                // ������Ծ���ȣ���������½��򲹳����µ��ٶ�
                float jumpForce = movementData.jumpForce;
                if (rb.velocity.y < 0)
                    jumpForce -= rb.velocity.y;

                // ʩ�����ϵ���Ծ��
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

                // ֪ͨ����ϵͳ��ʼ��Ծ
                animator.StartJump();
            }
        }

        // ����ص����棬������Ծ״̬
        if (lastOnGroundTime > 0 && !isJump)
        {
            isJumpFall = false;
        }

        // ���¹����ٶ�״̬
        UpdateAttackSpeed();

        // ������������
        UpdateGravity();
    }

    private void UpdateGravity()
    {
        if (!isDash)  // �ǳ��״̬
        {
            // �������䣨��ס�¼���
            if (rb.velocity.y < 0 && inputDirection.y < 0)
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
        // ʹ��ƽ����ֵ������
        float targetSpeed = inputDirection.x * movementData.runMaxSpeed;
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

        // ��������
        if (movementData.doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) &&
            Mathf.Sign(rb.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && lastOnGroundTime < 0)
        {
            accelRate = 0;
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

    private void Attack(InputAction.CallbackContext obj)
    {
        // ���ʱ���ܹ���
        if (isDash) return;

        animator.Attack();
        isAttack = true;

        // ʩ�ӹ������������ٶȿ���
        if(!isCrouch)
            ApplyAttackForce();
    }

    private void Jump(InputAction.CallbackContext obj)
    {
        LastPressedJumpTime = movementData.jumpInputBufferTime;
    }

    // ������̷���
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
            // TODO:�л�Ϊ����ʱ����ײ��
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

        // ȡ������״̬
        isAttack = false;
        isAttackSpeedActive = false;


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
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
    }
    #endregion
}