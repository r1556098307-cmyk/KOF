using System;
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

    // �����ٶȿ���
    private bool isAttackSpeedActive = false;
    private float attackSpeedTimer = 0f;


    public float lastOnGroundTime; // ʵ������ʱ���Ż�
    //TODO:ʵ����Ծ�����ָ��Ż�
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
        #endregion
        inputDirection = inputControl.GamePlay.Move.ReadValue<Vector2>();

        // ����Sprite�ķ�ת
        if (inputDirection.x != 0)
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
                Debug.Log("����");
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

        if (!isDash)
        {
            if(CanJump()&&LastPressedJumpTime>0)
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

        //TODO:������������
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
        Move(1);
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
        // �ڳ���ڼ�ά�ֳ���ٶȲ�Ӧ��˥��
        Vector2 currentVelocity = rb.velocity;

        // ���Ƴ���ٶȲ��������ֵ
        float currentHorizontalSpeed = currentVelocity.x;
        if (Mathf.Abs(currentHorizontalSpeed) > combatData.dashAttackMaxSpeed)
        {
            currentHorizontalSpeed = Mathf.Sign(currentHorizontalSpeed) * combatData.dashAttackMaxSpeed;
        }

        // Ӧ�ó���ٶ�˥��
        currentHorizontalSpeed *= combatData.dashAttackDecay;

        // �����ٶ�
        rb.velocity = new Vector2(currentHorizontalSpeed, currentVelocity.y);
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

        animator.Attack();
        isAttack = true;

        // ʩ�ӹ������������ٶȿ���
        ApplyAttackForce();
    }

    private void Jump(InputAction.CallbackContext obj)
    {
        LastPressedJumpTime = movementData.jumpInputBufferTime;
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