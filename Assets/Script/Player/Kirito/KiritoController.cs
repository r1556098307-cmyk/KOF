using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MovementData
{
    [Header("Run Settings")]
    public float runMaxSpeed = 5f;
    public float runAccelAmount = 10f;
    public float runDeccelAmount = 10f;
    [Range(0f, 1f)] public float accelInAir = 0.65f;
    [Range(0f, 1f)] public float deccelInAir = 0.65f;
    public bool doConserveMomentum = false;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpCutGravityMult = 2f;
    public float jumpHangGravityMult = 0.5f;
    public float jumpHangTimeThreshold = 2f;
    public float jumpHangAccelerationMult = 1.1f;
    public float jumpHangMaxSpeedMult = 1.3f;

    [Header("Gravity & Falling")]
    public float gravityScale = 1f;
    public float fallGravityMult = 1.5f;
    public float maxFallSpeed = 20f;
    public float fastFallGravityMult = 2f;
    public float maxFastFallSpeed = 25f;

    [Header("Coyote Time & Jump Buffer")]
    public float coyoteTime = 0.1f;
    public float jumpInputBufferTime = 0.1f;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.3f;
    public float dashAttackTime = 0.15f;
    public float dashEndTime = 0.2f;
    public Vector2 dashEndSpeed = new Vector2(0.5f, 0.5f);
    public float dashSleepTime = 0.02f;
}

public class KiritoController : MonoBehaviour
{
    [Header("Movement Data")]
    public MovementData Data;

    // 如果Data为空，使用默认值
    private MovementData GetData()
    {
        if (Data == null)
        {
            Data = new MovementData();
        }
        return Data;
    }

    [Header("Combat Settings")]
    public int maxEnergy = 5;
    public float energyRegenRate = 1f;
    public float attackCooldown = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayerMask;

    // Components
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // Input
    private float horizontalInput;
    private float verticalInput;

    // State
    public bool IsFacingRight { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsDashing { get; private set; }

    private bool isGrounded;
    private bool isAttacking;
    private bool isBlocking;
    private int currentEnergy;
    private float lastAttackTime;

    // Jump mechanics
    public float LastOnGroundTime { get; private set; }
    public float LastPressedJumpTime { get; private set; }
    private bool isJumpCut;
    private bool isJumpFalling;

    // Dash mechanics
    private bool isDashAttacking;
    private Vector2 lastDashDir;

    // Special move input buffer
    private float[] inputBuffer = new float[4];
    private int bufferIndex = 0;
    private float lastInputTime;
    private const float INPUT_BUFFER_TIME = 1f;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentEnergy = maxEnergy;
        IsFacingRight = true;

        // 确保Data不为空
        GetData();

        SetGravityScale(GetData().gravityScale);
        StartCoroutine(EnergyRegeneration());

        // 初始化地面状态
        CheckGrounded();
        if (isGrounded)
        {
            LastOnGroundTime = GetData().coyoteTime;
        }

        Debug.Log("KiritoController initialized. Data: " + (Data != null ? "OK" : "NULL"));
    }

    void Update()
    {
        #region TIMERS
        LastOnGroundTime -= Time.deltaTime;
        LastPressedJumpTime -= Time.deltaTime;
        #endregion

        HandleInput();
        CheckGrounded();
        UpdateAnimatorParameters();
        CheckSpecialMoves();
        HandleJumpLogic();
        HandleGravity();
    }

    void FixedUpdate()
    {
        if (!isAttacking && !IsDashing)
        {
            HandleMovement();
        }
        else if (isDashAttacking)
        {
            HandleMovement(Data.dashEndSpeed.x);
        }
    }

    void HandleInput()
    {
        // Basic input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Face direction
        if (horizontalInput != 0)
            CheckDirectionToFace(horizontalInput > 0);

        // Movement inputs
        bool jumpInput = Input.GetKeyDown(KeyCode.Space);
        bool jumpUpInput = Input.GetKeyUp(KeyCode.Space);
        bool crouchInput = Input.GetKey(KeyCode.S);
        bool dashInput = Input.GetKeyDown(KeyCode.L);

        // Combat inputs
        bool lightAttackInput = Input.GetKeyDown(KeyCode.J);
        bool heavyAttackInput = Input.GetKeyDown(KeyCode.K);
        bool blockInput = Input.GetKey(KeyCode.LeftControl);

        // Handle jump input
        if (jumpInput)
        {
            Debug.Log("Jump input detected!");
            OnJumpInput();
        }

        if (jumpUpInput)
        {
            OnJumpUpInput();
        }

        // Handle walking animation
        bool walkInput = Mathf.Abs(horizontalInput) > 0.1f;
        animator.SetBool("isWalking", walkInput && isGrounded && !isAttacking);

        // Handle crouching
        animator.SetBool("isCrouching", crouchInput && isGrounded);

        // Handle dashing
        if (dashInput && !IsDashing && !isAttacking)
        {
            StartCoroutine(StartDash());
        }

        // Handle blocking
        isBlocking = blockInput && !isAttacking;
        animator.SetBool("isBlocking", isBlocking);

        // Handle attacks
        if (Time.time - lastAttackTime > attackCooldown)
        {
            HandleAttacks(lightAttackInput, heavyAttackInput, crouchInput);
        }

        // Update input buffer for special moves
        UpdateInputBuffer();
    }

    void HandleMovement(float lerpAmount = 1f)
    {
        // Calculate target speed
        float targetSpeed = horizontalInput * Data.runMaxSpeed;
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

        // Calculate acceleration rate
        float accelRate;
        if (LastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;

        // Jump apex bonus
        if ((IsJumping || isJumpFalling) && Mathf.Abs(rb.velocity.y) < Data.jumpHangTimeThreshold)
        {
            accelRate *= Data.jumpHangAccelerationMult;
            targetSpeed *= Data.jumpHangMaxSpeedMult;
        }

        // Conserve momentum
        if (Data.doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) &&
            Mathf.Sign(rb.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
        {
            accelRate = 0;
        }

        // Apply force
        float speedDif = targetSpeed - rb.velocity.x;
        float movement = speedDif * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    void HandleJumpLogic()
    {
        // Check if falling after jump
        if (IsJumping && rb.velocity.y < 0)
        {
            IsJumping = false;
            isJumpFalling = true;
        }

        // Reset jump states when grounded
        if (LastOnGroundTime > 0 && !IsJumping)
        {
            isJumpCut = false;
            isJumpFalling = false;
        }

        // Perform jump - 添加调试信息
        if (!IsDashing && CanJump() && LastPressedJumpTime > 0)
        {
            Debug.Log($"Jumping! CanJump: {CanJump()}, LastPressedJumpTime: {LastPressedJumpTime}, LastOnGroundTime: {LastOnGroundTime}");
            IsJumping = true;
            isJumpCut = false;
            isJumpFalling = false;
            Jump();
        }
    }

    void HandleGravity()
    {
        if (!isDashAttacking)
        {
            if (isJumpCut)
            {
                // Higher gravity if jump button released
                SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -Data.maxFallSpeed));
            }
            else if ((IsJumping || isJumpFalling) && Mathf.Abs(rb.velocity.y) < Data.jumpHangTimeThreshold)
            {
                // Reduced gravity at jump apex
                SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
            }
            else if (rb.velocity.y < 0 && verticalInput < 0)
            {
                // Fast fall
                SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -Data.maxFastFallSpeed));
            }
            else if (rb.velocity.y < 0)
            {
                // Normal falling
                SetGravityScale(Data.gravityScale * Data.fallGravityMult);
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -Data.maxFallSpeed));
            }
            else
            {
                // Default gravity
                SetGravityScale(Data.gravityScale);
            }
        }
        else
        {
            // No gravity during dash attack
            SetGravityScale(0);
        }
    }

    #region INPUT CALLBACKS
    public void OnJumpInput()
    {
        LastPressedJumpTime = GetData().jumpInputBufferTime;
        Debug.Log($"OnJumpInput called. LastPressedJumpTime set to: {LastPressedJumpTime}");
    }

    public void OnJumpUpInput()
    {
        if (CanJumpCut())
            isJumpCut = true;
    }
    #endregion

    void Jump()
    {
        Debug.Log("Jump executed!");
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        // Apply jump force with compensation for falling velocity
        float force = GetData().jumpForce;
        if (rb.velocity.y < 0)
            force -= rb.velocity.y;

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        animator.SetBool("isJumping", true);

        Debug.Log($"Jump force applied: {force}, Current velocity: {rb.velocity}");
    }

    IEnumerator StartDash()
    {
        LastOnGroundTime = 0;

        // Determine dash direction
        if (horizontalInput != 0)
            lastDashDir = new Vector2(horizontalInput, 0).normalized;
        else
            lastDashDir = IsFacingRight ? Vector2.right : Vector2.left;

        // Freeze time briefly for game feel
        Sleep(Data.dashSleepTime);

        IsDashing = true;
        isDashAttacking = true;

        SetGravityScale(0);
        animator.SetBool("isDashing", true);

        float startTime = Time.time;

        // Dash attack phase
        while (Time.time - startTime <= Data.dashAttackTime)
        {
            rb.velocity = lastDashDir * Data.dashSpeed;
            yield return null;
        }

        startTime = Time.time;
        isDashAttacking = false;

        // Dash end phase
        SetGravityScale(Data.gravityScale);
        rb.velocity = Data.dashEndSpeed * lastDashDir;

        while (Time.time - startTime <= Data.dashEndTime)
        {
            yield return null;
        }

        // Dash complete
        IsDashing = false;
        animator.SetBool("isDashing", false);
    }

    void HandleAttacks(bool lightAttack, bool heavyAttack, bool crouching)
    {
        bool upInput = Input.GetKey(KeyCode.W);

        if (lightAttack)
        {
            if (upInput && !crouching)
                PerformAttack("UpAttack");
            else if (crouching)
                PerformAttack("CrouchLightAttack");
            else
                PerformAttack("LightAttack");
        }
        else if (heavyAttack)
        {
            if (upInput && !crouching)
                PerformAttack("HeadButt");
            else if (crouching)
                PerformAttack("CrouchHeavyAttack");
            else
                PerformAttack("HeavyAttack");
        }

        if (lightAttack && heavyAttack && currentEnergy >= 3)
        {
            PerformSuperMove();
        }
    }

    void PerformAttack(string attackTrigger)
    {
        animator.SetTrigger(attackTrigger);
        isAttacking = true;
        lastAttackTime = Time.time;
        StartCoroutine(ResetAttackState(0.5f));
    }

    void PerformSuperMove()
    {
        if (currentEnergy >= 3)
        {
            animator.SetTrigger("SuperMove");
            currentEnergy -= 3;
            isAttacking = true;
            lastAttackTime = Time.time;
            StartCoroutine(ResetAttackState(1.0f));
        }
    }

    IEnumerator ResetAttackState(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAttacking = false;
    }

    void UpdateInputBuffer()
    {
        if (Time.time - lastInputTime > INPUT_BUFFER_TIME)
        {
            bufferIndex = 0;
        }

        if (Input.GetKeyDown(KeyCode.S))
            AddToInputBuffer(1);
        else if (Input.GetKeyDown(KeyCode.D))
            AddToInputBuffer(2);
        else if (Input.GetKeyDown(KeyCode.K))
            AddToInputBuffer(3);
    }

    void AddToInputBuffer(float input)
    {
        if (bufferIndex < inputBuffer.Length)
        {
            inputBuffer[bufferIndex] = input;
            bufferIndex++;
            lastInputTime = Time.time;
        }
    }

    void CheckSpecialMoves()
    {
        if (bufferIndex >= 4)
        {
            if (inputBuffer[0] == 1 && inputBuffer[1] == 2 &&
                inputBuffer[2] == 2 && inputBuffer[3] == 3)
            {
                if (currentEnergy >= 1)
                {
                    PerformSpecialMove();
                }
            }
            bufferIndex = 0;
        }
    }

    void PerformSpecialMove()
    {
        animator.SetTrigger("SpecialMove");
        currentEnergy -= 1;
        isAttacking = true;
        lastAttackTime = Time.time;
        StartCoroutine(ResetAttackState(0.8f));
        bufferIndex = 0;
    }

    void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);

        if (isGrounded)
        {
            LastOnGroundTime = GetData().coyoteTime;
        }

        animator.SetBool("isGrounded", isGrounded);

        // Handle falling animation
        if (!isGrounded && rb.velocity.y < 0)
        {
            animator.SetBool("isFalling", true);
            // Only reset jumping if we're actually falling (not just jumped)
            if (!IsJumping || rb.velocity.y < -1f)
            {
                animator.SetBool("isJumping", false);
            }
        }
        else if (isGrounded && rb.velocity.y <= 0.1f)
        {
            animator.SetBool("isFalling", false);
            animator.SetBool("isJumping", false);
        }

        // Debug信息（可选）
        if (wasGrounded != isGrounded)
        {
            Debug.Log($"Ground state changed: {isGrounded}, LastOnGroundTime: {LastOnGroundTime}");
        }
    }

    void UpdateAnimatorParameters()
    {
        animator.SetFloat("HorizontalInput", horizontalInput);
        animator.SetFloat("VerticalInput", verticalInput);
        animator.SetInteger("Energy", currentEnergy);
    }

    #region CHECK METHODS
    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != IsFacingRight)
            Turn();
    }

    private void Turn()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        IsFacingRight = !IsFacingRight;
    }

    private bool CanJump()
    {
        bool canJump = LastOnGroundTime > 0 && !IsJumping;
        Debug.Log($"CanJump: {canJump}, LastOnGroundTime: {LastOnGroundTime}, IsJumping: {IsJumping}, isGrounded: {isGrounded}");
        return canJump;
    }

    private bool CanJumpCut()
    {
        return IsJumping && rb.velocity.y > 0;
    }
    #endregion

    #region UTILITY METHODS
    public void SetGravityScale(float scale)
    {
        rb.gravityScale = scale;
    }

    private void Sleep(float duration)
    {
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private IEnumerator PerformSleep(float duration)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }
    #endregion

    IEnumerator EnergyRegeneration()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f / energyRegenRate);
            if (currentEnergy < maxEnergy)
            {
                currentEnergy++;
            }
        }
    }

    // Public methods for taking damage (called by other scripts)
    public void TakeHit(int damage, bool isHeavyHit = false)
    {
        if (!isBlocking)
        {
            if (isHeavyHit)
                animator.SetTrigger("HitHeavy");
            else
                animator.SetTrigger("HitLight");
        }
    }

    public void GetKnockedDown()
    {
        animator.SetTrigger("Knockdown");
        StartCoroutine(AutoGetUp());
    }

    IEnumerator AutoGetUp()
    {
        yield return new WaitForSeconds(2f);
        animator.SetTrigger("GetUp");
    }

    public void TriggerVictory()
    {
        animator.SetTrigger("Victory");
        enabled = false;
    }

    public void OnAttackHit()
    {
        Debug.Log("Attack Hit!");
    }

    public void OnAttackComplete()
    {
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}