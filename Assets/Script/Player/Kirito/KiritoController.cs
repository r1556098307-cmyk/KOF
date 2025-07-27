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
    public float runAccelAmount = 9.5f; // ������ٶ�
    public float runDeccelAmount = 9.5f; // ������ٶ�
    public float accelInAir = 1f; // ���м��ٱ���
    public float deccelInAir = 1f; // ���м��ٱ���
    public float jumpHangTimeThreshold = 1f; // ��Ծ��ͣ�ж���ֵ
    public float jumpHangAccelerationMult = 1.1f; // ��Ծ������ٱ���
    public float jumpHangMaxSpeedMult = 1.3f; // ��Ծ��������ٶȱ���
    public float coyoteTime = 0.1f; // ����ʱ��

    public bool doConserveMomentum = false; // �Ƿ�����������

    [Header("����λ�Ʋ���")]
    public float attackForce = 15f; // ����ʱʩ�ӵ�˲����
    public float attackMaxSpeed = 8f; // ����ʱ������ٶ�����
    public float attackSpeedDecay = 0.95f; // �������ٶ�˥��ϵ����ÿ֡��
    public float attackSpeedDecayDuration = 0.5f; // �ٶ�˥������ʱ��

    [Header("��̹�������")]
    public float dashAttackForce = 20f;        // ��̹�������
    public float dashAttackMaxSpeed = 15f;     // �������ٶ�
    public float dashAttackDuration = 0.3f;    // ��̳���ʱ��
    public float dashAttackDecay = 0.92f;      // ����ٶ�˥��
    public bool dashAttackIgnoreInput = true;  // ���ʱ�Ƿ�����ƶ�����
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

    // �����ٶȿ���
    private bool isAttackSpeedActive = false;
    private float attackSpeedTimer = 0f;

    // ��̹���״̬
    private bool isDashAttacking = false;
    private float dashAttackTimer = 0f;
    private Vector2 dashDirection;

    // ���뻺�����Ϲ�������
    [Header("������Ʋ���")]
    public float inputBufferTime = 0.15f; // ���뻺��ʱ��
    public float comboWindowTime = 0.2f;  // ��ϼ�����ʱ��

    private float lastAttackInputTime = -1f;  // J������ʱ��
    private float lastCrouchInputTime = -1f;  // S������ʱ��
    private bool isProcessingCombo = false;
    private bool isCrouching = false;

    public float lastOnGroundTime; // ʵ������ʱ���Ż�

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
        inputControl.GamePlay.Attack.started += Attack;  // J�� - ��ͨ����
        inputControl.GamePlay.DownAttack.started += Crouch;  // S�� - �¶�
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
                if (lastOnGroundTime < -0.1f)
                {
                    // Animator��params�����ڵ�����
                }
                isGround = true;
                lastOnGroundTime = playerData.coyoteTime;
            }
        }

        // ���¹����ٶ�״̬
        UpdateAttackSpeed();

        // ���³�̹���״̬
        UpdateDashAttack();

        // �������뻺�����Ϲ���
        ProcessCombatInput();
    }

    private void FixedUpdate()
    {
        Move(1);
    }

    public void Move(float lerpAmount)
    {
        // ������ڳ�̹����Һ������룬�򲻴������ƶ�
        if (isDashAttacking && playerData.dashAttackIgnoreInput)
        {
            ApplyDashMovement();
            return;
        }

        // ʹ��ƽ����ֵ������
        float targetSpeed = inputDirection.x * playerData.runMaxSpeed;
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

        float accelRate;

        // ��̬���ٶȼ��㣬�ڿ��к͵���ʹ�ò�ͬ�ļ��ٶ�
        if (lastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? playerData.runAccelAmount : playerData.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? playerData.runAccelAmount * playerData.accelInAir : playerData.runDeccelAmount * playerData.deccelInAir;

        // ��Ծ�������
        if ((isJump || isJumpFall) && Mathf.Abs(rb.velocity.y) < playerData.jumpHangTimeThreshold)
        {
            accelRate *= playerData.jumpHangAccelerationMult;
            targetSpeed *= playerData.jumpHangMaxSpeedMult;
        }

        // ��������
        if (playerData.doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) &&
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

    private void UpdateDashAttack()
    {
        if (isDashAttacking)
        {
            dashAttackTimer -= Time.deltaTime;
            if (dashAttackTimer <= 0f)
            {
                isDashAttacking = false;
                isProcessingCombo = false; // ��̽������������״̬
                // ��̽�����������һЩ�����߼�������ָ�����״̬
            }
        }
    }

    private void ProcessCombatInput()
    {
        // ������ڴ�����Ϲ�����������������
        if (isProcessingCombo) return;

        float currentTime = Time.time;

        // ����Ƿ�����Ϲ������� (S+J ��ʱ�䴰����)
        bool hasRecentAttack = (currentTime - lastAttackInputTime) <= comboWindowTime;
        bool hasRecentCrouch = (currentTime - lastCrouchInputTime) <= comboWindowTime;

        // ���ȼ�1: S+J��Ϲ��� (��̹���)
        if (hasRecentAttack && hasRecentCrouch)
        {
            ExecuteComboAttack();
            return;
        }

        // ���ȼ�2: ��������ͨ���� (J)
        if (hasRecentAttack && !hasRecentCrouch)
        {
            ExecuteNormalAttack();
            return;
        }

        // ���ȼ�3: �������¶� (S)
        if (hasRecentCrouch && !hasRecentAttack)
        {
            ExecuteCrouch();
            return;
        }
    }

    private void ExecuteComboAttack()
    {
        // ����Ѿ��ڳ�̹����У������ٴδ���
        if (isDashAttacking) return;

        Debug.Log("ִ�г�̹���: S+J");

        isProcessingCombo = true;
        animator.DownAttack(); // ��̹�������
        DashAttack();

        // ��������¼
        lastAttackInputTime = -1f;
        lastCrouchInputTime = -1f;
    }

    private void ExecuteNormalAttack()
    {
        // ������ڳ�̹��������ܽ�����ͨ����
        if (isDashAttacking) return;

        Debug.Log("ִ����ͨ����: J");

        isProcessingCombo = true;
        animator.Attack();
        isAttack = true;
        ApplyAttackForce();

        // ��������¼
        lastAttackInputTime = -1f;

        // ��ͨ���������ÿ�һЩ
        StartCoroutine(ResetComboStateAfterDelay(0.1f));
    }

    private void ExecuteCrouch()
    {
        // ������ڹ����������¶�
        if (isDashAttacking || isAttack) return;

        Debug.Log("ִ���¶�: S");

        // �¶��߼�
        isCrouching = true;
        // �����������¶׶���
        // animator.Crouch();

        // ��������¼
        lastCrouchInputTime = -1f;

        // �¶�״̬���Գ������ɿ���������������
        StartCoroutine(HandleCrouchState());
    }

    private System.Collections.IEnumerator HandleCrouchState()
    {
        // �ȴ�һС��ʱ���������������
        yield return new WaitForSeconds(0.05f);

        // ����Ƿ��ڰ�ס�¶׼�
        Vector2 currentInput = inputControl.GamePlay.Move.ReadValue<Vector2>();
        if (currentInput.y >= -0.5f) // ���û����������
        {
            isCrouching = false;
            // animator.StopCrouch(); // ֹͣ�¶׶���
        }
    }

    private System.Collections.IEnumerator ResetComboStateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isProcessingCombo = false;
    }

    private void ApplyDashMovement()
    {
        // �ڳ���ڼ�ά�ֳ���ٶȲ�Ӧ��˥��
        Vector2 currentVelocity = rb.velocity;

        // ���Ƴ���ٶȲ��������ֵ
        float currentHorizontalSpeed = currentVelocity.x;
        if (Mathf.Abs(currentHorizontalSpeed) > playerData.dashAttackMaxSpeed)
        {
            currentHorizontalSpeed = Mathf.Sign(currentHorizontalSpeed) * playerData.dashAttackMaxSpeed;
        }

        // Ӧ�ó���ٶ�˥��
        currentHorizontalSpeed *= playerData.dashAttackDecay;

        // �����ٶ�
        rb.velocity = new Vector2(currentHorizontalSpeed, currentVelocity.y);
    }

    private void ApplyAttackSpeedLimit()
    {
        if (isAttackSpeedActive && !isDashAttacking) // ���ʱ��Ӧ����ͨ�������ٶ�����
        {
            // ����ˮƽ�ٶȲ�������������ٶ�
            float currentSpeed = rb.velocity.x;
            float maxSpeed = playerData.attackMaxSpeed;

            if (Mathf.Abs(currentSpeed) > maxSpeed)
            {
                // ���ٶ���������󹥻��ٶ��ڣ������ַ���
                float clampedSpeed = Mathf.Sign(currentSpeed) * maxSpeed;
                rb.velocity = new Vector2(clampedSpeed, rb.velocity.y);
            }

            // Ӧ���ٶ�˥��
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
        // ��¼��������ʱ�� (J��)
        lastAttackInputTime = Time.time;
    }

    private void Crouch(InputAction.CallbackContext obj)
    {
        // ��¼�¶�����ʱ�� (S��)
        lastCrouchInputTime = Time.time;
    }

    private void DashAttack()
    {
        // ȷ����̷���
        Vector2 targetDirection = GetDashDirection();

        // ��ֹͣ��ǰˮƽ�ٶȣ���ѡ���ó�̸��г���У�
        rb.velocity = new Vector2(0, rb.velocity.y);

        // ʩ�ӳ����
        Vector2 dashForce = targetDirection * playerData.dashAttackForce;
        rb.AddForce(dashForce, ForceMode2D.Impulse);

        // �������״̬
        isDashAttacking = true;
        dashAttackTimer = playerData.dashAttackDuration;
        dashDirection = targetDirection;

        // ֹͣ��ͨ�������ٶ�Ч���������ͻ
        StopAttackSpeed();

        //TODO: �Ӿ�Ч��
    }

    private Vector2 GetDashDirection()
    {
        Vector2 forwardDirection = isFacingRight ? Vector2.right : Vector2.left;
        return forwardDirection;
    }

    private void ApplyAttackForce()
    {
        // ���㹥������
        Vector2 attackDirection = isFacingRight ? Vector2.right : Vector2.left;

        // ʩ��˲�乥����
        rb.AddForce(attackDirection * playerData.attackForce, ForceMode2D.Impulse);

        // ���������ٶȿ���
        isAttackSpeedActive = true;
        attackSpeedTimer = playerData.attackSpeedDecayDuration;
    }

    // ����ͨ�������¼����ã��ڹ����������ض�֡����
    public void TriggerAttackForceFromAnimation()
    {
        ApplyAttackForce();
    }

    // ����ֹͣ�����ٶ�Ч��
    public void StopAttackSpeed()
    {
        isAttackSpeedActive = false;
        attackSpeedTimer = 0f;
    }

    // ����ֹͣ��̹���
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
//    public float runAccelAmount = 9.5f; // ������ٶ�
//    public float runDeccelAmount = 9.5f; // ������ٶ�
//    public float accelInAir = 1f; // ���м��ٱ���
//    public float deccelInAir = 1f; // ���м��ٱ���
//    public float jumpHangTimeThreshold = 1f; // ��Ծ��ͣ�ж���ֵ
//    public float jumpHangAccelerationMult = 1.1f; // ��Ծ������ٱ���
//    public float jumpHangMaxSpeedMult = 1.3f; // ��Ծ��������ٶȱ���
//    public float coyoteTime = 0.1f; // ����ʱ��

//    public bool doConserveMomentum = false; // �Ƿ�����������

//    [Header("����λ�Ʋ���")]
//    public float attackForce = 15f; // ����ʱʩ�ӵ�˲����
//    public float attackMaxSpeed = 8f; // ����ʱ������ٶ�����
//    public float attackSpeedDecay = 0.95f; // �������ٶ�˥��ϵ����ÿ֡��
//    public float attackSpeedDecayDuration = 0.5f; // �ٶ�˥������ʱ��

//    [Header("��̹�������")]
//    public float dashAttackForce = 20f;        // ��̹�������
//    public float dashAttackMaxSpeed = 15f;     // �������ٶ�
//    public float dashAttackDuration = 0.3f;    // ��̳���ʱ��
//    public float dashAttackDecay = 0.92f;      // ����ٶ�˥��
//    public bool dashAttackIgnoreInput = true;  // ���ʱ�Ƿ�����ƶ�����
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

//    // �����ٶȿ���
//    private bool isAttackSpeedActive = false;
//    private float attackSpeedTimer = 0f;

//    // ��̹���״̬
//    private bool isDashAttacking = false;
//    private float dashAttackTimer = 0f;
//    private Vector2 dashDirection;

//    public float lastOnGroundTime; // ʵ������ʱ���Ż�

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

//        // ����Sprite�ķ�ת
//        if (inputDirection.x != 0)
//        {
//            isWalk = true;
//            CheckDirectionToFace(inputDirection.x > 0);
//        }
//        else
//        {
//            isWalk = false;
//        }

//        // ������
//        if (!isDash && !isJump)
//        {
//            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
//            {
//                if (lastOnGroundTime < -0.1f)
//                {
//                    // Animator��params�����ڵ�����
//                }
//                isGround = true;
//                lastOnGroundTime = playerData.coyoteTime;
//            }
//        }

//        // ���¹����ٶ�״̬
//        UpdateAttackSpeed();

//        // ���³�̹���״̬
//        UpdateDashAttack();
//    }

//    private void FixedUpdate()
//    {
//        Move(1);
//    }

//    public void Move(float lerpAmount)
//    {
//        // ������ڳ�̹����Һ������룬�򲻴������ƶ�
//        if (isDashAttacking && playerData.dashAttackIgnoreInput)
//        {
//            ApplyDashMovement();
//            return;
//        }

//        // ʹ��ƽ����ֵ������
//        float targetSpeed = inputDirection.x * playerData.runMaxSpeed;
//        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

//        float accelRate;

//        // ��̬���ٶȼ��㣬�ڿ��к͵���ʹ�ò�ͬ�ļ��ٶ�
//        if (lastOnGroundTime > 0)
//            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? playerData.runAccelAmount : playerData.runDeccelAmount;
//        else
//            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? playerData.runAccelAmount * playerData.accelInAir : playerData.runDeccelAmount * playerData.deccelInAir;

//        // ��Ծ�������
//        if ((isJump || isJumpFall) && Mathf.Abs(rb.velocity.y) < playerData.jumpHangTimeThreshold)
//        {
//            accelRate *= playerData.jumpHangAccelerationMult;
//            targetSpeed *= playerData.jumpHangMaxSpeedMult;
//        }

//        // ��������
//        if (playerData.doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) &&
//            Mathf.Sign(rb.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && lastOnGroundTime < 0)
//        {
//            accelRate = 0;
//        }

//        // ��rb�ṩ�����ٶ���Ŀ��Զ����ٿ죬���������
//        float speedDif = targetSpeed - rb.velocity.x;
//        float movement = speedDif * accelRate;
//        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

//        // Ӧ�ù����ٶ�����
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
//                // ��̽�����������һЩ�����߼�������ָ�����״̬
//            }
//        }
//    }

//    private void ApplyDashMovement()
//    {
//        // �ڳ���ڼ�ά�ֳ���ٶȲ�Ӧ��˥��
//        Vector2 currentVelocity = rb.velocity;

//        // ���Ƴ���ٶȲ��������ֵ
//        float currentHorizontalSpeed = currentVelocity.x;
//        if (Mathf.Abs(currentHorizontalSpeed) > playerData.dashAttackMaxSpeed)
//        {
//            currentHorizontalSpeed = Mathf.Sign(currentHorizontalSpeed) * playerData.dashAttackMaxSpeed;
//        }

//        // Ӧ�ó���ٶ�˥��
//        currentHorizontalSpeed *= playerData.dashAttackDecay;

//        // �����ٶ�
//        rb.velocity = new Vector2(currentHorizontalSpeed, currentVelocity.y);
//    }

//    private void ApplyAttackSpeedLimit()
//    {
//        if (isAttackSpeedActive && !isDashAttacking) // ���ʱ��Ӧ����ͨ�������ٶ�����
//        {
//            // ����ˮƽ�ٶȲ�������������ٶ�
//            float currentSpeed = rb.velocity.x;
//            float maxSpeed = playerData.attackMaxSpeed;

//            if (Mathf.Abs(currentSpeed) > maxSpeed)
//            {
//                // ���ٶ���������󹥻��ٶ��ڣ������ַ���
//                float clampedSpeed = Mathf.Sign(currentSpeed) * maxSpeed;
//                rb.velocity = new Vector2(clampedSpeed, rb.velocity.y);
//            }

//            // Ӧ���ٶ�˥��
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
//        // ������ڳ�̹��������ܽ�����ͨ����
//        if (isDashAttacking) return;

//        animator.Attack();
//        isAttack = true;

//        // ʩ�ӹ������������ٶȿ���
//        ApplyAttackForce();
//    }

//    private void DownAttack(InputAction.CallbackContext obj)
//    {
//        // ����Ѿ��ڳ�̹����У������ٴδ���
//        if (isDashAttacking) return;

//        animator.DownAttack();
//        DashAttack();
//    }

//    private void DashAttack()
//    {
//        // ȷ����̷���
//        Vector2 targetDirection = GetDashDirection();

//        // ��ֹͣ��ǰˮƽ�ٶȣ���ѡ���ó�̸��г���У�
//        rb.velocity = new Vector2(0, rb.velocity.y);

//        // ʩ�ӳ����
//        Vector2 dashForce = targetDirection * playerData.dashAttackForce;
//        rb.AddForce(dashForce, ForceMode2D.Impulse);

//        // �������״̬
//        isDashAttacking = true;
//        dashAttackTimer = playerData.dashAttackDuration;
//        dashDirection = targetDirection;

//        // ֹͣ��ͨ�������ٶ�Ч���������ͻ
//        StopAttackSpeed();

//        //TODO: �Ӿ�Ч��
//    }

//    private Vector2 GetDashDirection()
//    {
//        Vector2 forwardDirection = isFacingRight ? Vector2.right : Vector2.left;
//        return forwardDirection;
//    }

//    private void ApplyAttackForce()
//    {
//        // ���㹥������
//        Vector2 attackDirection = isFacingRight ? Vector2.right : Vector2.left;

//        // ʩ��˲�乥����
//        rb.AddForce(attackDirection * playerData.attackForce, ForceMode2D.Impulse);

//        // ���������ٶȿ���
//        isAttackSpeedActive = true;
//        attackSpeedTimer = playerData.attackSpeedDecayDuration;
//    }

//    // ����ͨ�������¼����ã��ڹ����������ض�֡����
//    public void TriggerAttackForceFromAnimation()
//    {
//        ApplyAttackForce();
//    }

//    // ����ֹͣ�����ٶ�Ч��
//    public void StopAttackSpeed()
//    {
//        isAttackSpeedActive = false;
//        attackSpeedTimer = 0f;
//    }

//    // ����ֹͣ��̹���
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