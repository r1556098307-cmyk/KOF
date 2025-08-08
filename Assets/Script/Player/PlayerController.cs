using System;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.AllocatorManager;
using Random = UnityEngine.Random;

public enum PlayerID
{
    Player1,
    Player2
}
[System.Serializable]
public class SpecialMoveConfig
{
    public string moveName;
    public float dashSpeed;
}

public class PlayerController : MonoBehaviour
{
    [Header("�����ļ�")]
    public PlayerConfig config;

    [Header("��������")]
    public PlayerMovementData movementData;
    public PlayerCombatData combatData;

    private Rigidbody2D rb;
    public PlayerAnimator animator;
    [SerializeField]
    private ComboSystem comboSystem;
    private CapsuleCollider2D capsuleCollider; // ��ӽ�����ײ������
    private HitstunSystem hitstunSystem;
    private PlayerStats playerStats;

    [SerializeField]
    public Vector2 inputDirection;

    public PlayerID PlayerId;

    public CharacterType characterType;

    public bool isFacingRight;
    public bool isDash;
    public bool isWalk;
    public bool isJump;
    public bool isCrouch;
    public bool isBlock;
    public bool isGround;
    public bool isAttack;
    public bool isJumpFall;

    //// ��ײ������
    //[Header("��ײ������")]
    //[SerializeField] private Vector2 standingColliderOffset = new Vector2(0, -0.84f);
    //[SerializeField] private Vector2 standingColliderSize = new Vector2(1.2f, 3.66f);
    //[SerializeField] private Vector2 crouchingColliderOffset = new Vector2(0, -1.5f);
    //[SerializeField] private Vector2 crouchingColliderSize = new Vector2(1.2f, 2.35f);

    //[Header("���⼼������")]
    //[SerializeField] private List<SpecialMoveConfig> specialMoveConfigs = new List<SpecialMoveConfig>();

    // ���⼼�������ֵ䣬���ڿ��ٲ���
    private Dictionary<string, SpecialMoveConfig> specialMoveDict;
    [Header("����״̬")]
    [SerializeField] private bool isInvulnerable = false;  // �޵�״̬
    private bool isSpecialDashing = false;
    private string currentSpecialMove = "";

    // ԭʼ�㼶�洢
    private int originalLayer;
    private int invulnerableLayer;
    private int playerPassThroughLayer;

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

    public LayerMask wallLayer;                 // ǽ���
    [SerializeField]
    private bool isWallSliding = false;         // �Ƿ���ǽ�滬��
    [SerializeField]
    private bool isTouchingWall = false;        // �Ƿ�Ӵ�ǽ��

    [SerializeField]
    private Transform groundCheckPoint;
    [SerializeField]
    private LayerMask groundLayer;
    [SerializeField]
    private Transform wallCheckPoint;           // ǽ�����

    // ��¼����Ƿ���Ҫ����
    private bool wantsToCrouch = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<PlayerAnimator>();
        comboSystem = GetComponent<ComboSystem>();
        capsuleCollider = GetComponent<CapsuleCollider2D>(); // ��ȡ������ײ�����
        hitstunSystem = GetComponent<HitstunSystem>();
        playerStats = GetComponent<PlayerStats>();


        ValidateConfig();

        // ���û������wallLayer��ʹ��groundLayer
        if (wallLayer == 0)
        {
            wallLayer = groundLayer;
        }

        if (capsuleCollider != null && config != null)
        {
            capsuleCollider.sharedMaterial = config.normalMaterial;
        }
        InitializeSpecialMoveDict(); 

    }

    private void InitializeSpecialMoveDict()
    {
        specialMoveDict = new Dictionary<string, SpecialMoveConfig>();

        if (config != null && config.skills.specialMoveConfigs != null)
        {
            foreach (var moveConfig in config.skills.specialMoveConfigs)
            {
                if (!string.IsNullOrEmpty(moveConfig.moveName))
                {
                    specialMoveDict[moveConfig.moveName] = moveConfig;
                }
            }
        }
    }

    private void Start()
    {
        isFacingRight = true;

        // ȷ����ʼʱʹ��վ����ײ��
        UpdateColliderSize(false);

     

        invulnerableLayer = LayerMask.NameToLayer(config.layers.invulnerableLayerName);
        if (invulnerableLayer == -1)
        {
            Debug.LogError($"Layer '{config.layers.invulnerableLayerName}' δ�ҵ�! ����Physics2D�����д����ò㼶����������Player�㲻��ײ..");
        }

        playerPassThroughLayer = LayerMask.NameToLayer(config.layers.invulnerableLayerName);
        if (playerPassThroughLayer == -1)
        {
            Debug.LogError($"Layer '{config.layers.invulnerableLayerName}' δ�ҵ�! ����Physics2D�����д����ò㼶����������Player�㲻��ײ.");
        }

        originalLayer = gameObject.layer;
        GameManager.Instance.RigisterPlayer(playerStats, PlayerId);

        // ����playerID������ɫ�泯���� 
         if(PlayerId == PlayerID.Player2)
        {
            Turn();
        }
    }

    private void ValidateConfig()
    {
        if (config == null)
        {
            Debug.LogError("PlayerConfig δ���ã�����Inspector��ָ�������ļ���", this);
            return;
        }

        if (config.normalMaterial == null)
            Debug.LogWarning("PlayerConfig: normalMaterial δ����", this);

        if (config.wallMaterial == null)
            Debug.LogWarning("PlayerConfig: wallMaterial δ����", this);
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

        // �Զ�վ����
        if (isCrouch && !wantsToCrouch && CanStandUp())
        {
            isCrouch = false;
            UpdateColliderSize(false);
        }

        if (comboSystem != null)
        {
            inputDirection = comboSystem.GetMovementInput();
        }

        // ����Sprite�ķ�ת�����ʱ����ת��
        if (!isDash && inputDirection.x != 0)
        {
            bool canMove = hitstunSystem == null || hitstunSystem.IsMoveAllowed();

            if (!isBlock)
                isWalk = true;
            if (canMove)
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
            if (Physics2D.OverlapBox(groundCheckPoint.position, config.detection.groundCheckSize, 0, groundLayer))
            {
                // ��½
                if (lastOnGroundTime < -0.1f)
                {
                    // ֹͣ������Ч��������ڲ��ţ�
                    if (AudioManager.Instance != null && AudioManager.Instance.IsLoopingSFXPlaying("fall"))
                    {
                        AudioManager.Instance.StopLoopingSFX("fall");
                    }

                    // ������½��Ч
                    AudioManager.Instance?.PlaySFX("land");
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
            // ��ʼ����ѭ��������Ч
            AudioManager.Instance?.PlayLoopingSFX("fall");
        }

        // ����������䣬ֹͣ������Ч
        if (isJumpFall && (isGround || rb.velocity.y >= 0))
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsLoopingSFXPlaying("fall"))
            {
                AudioManager.Instance.StopLoopingSFX("fall");
            }
        }

        // ��Ծ��⣨���ʱ������Ծ��
        if (!isDash)
        {
            bool canJump = hitstunSystem == null || hitstunSystem.IsJumpAllowed();

            if (canJump&&CanJump() && LastPressedJumpTime > 0)
            {
                // ������Ծ��ؼ�ʱ��
                lastOnGroundTime = 0;
                LastPressedJumpTime = 0;

                // ������Ծ״̬
                isJump = true;
                isGround = false;
                isWallSliding = false; // ��Ծʱֹͣǽ�滬��

                if (AudioManager.Instance != null && AudioManager.Instance.IsLoopingSFXPlaying("fall"))
                {
                    AudioManager.Instance.StopLoopingSFX("fall");
                }

                // ������Ծ���ȣ���������½��򲹳����µ��ٶ�
                float jumpForce = movementData.jumpForce;
                if (rb.velocity.y < 0)
                    jumpForce -= rb.velocity.y;

                AudioManager.Instance?.PlaySFX("jump");

                // ʩ�����ϵ���Ծ��
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
        }

        // ����ص����棬������Ծ״̬
        if (lastOnGroundTime > 0 && !isJump)
        {
            // ֹͣ������Ч
            if (AudioManager.Instance != null && AudioManager.Instance.IsLoopingSFXPlaying("fall"))
            {
                AudioManager.Instance.StopLoopingSFX("fall");
            }


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
        isTouchingWall = Physics2D.OverlapBox(wallCheckPoint.position, config.detection.wallCheckSize, 0, wallLayer);
        // Debug.Log(isTouchingWall);
        if (isTouchingWall)
        {
            capsuleCollider.sharedMaterial = config.wallMaterial;

            // �ж��Ƿ�Ӧ�ÿ�ʼǽ�滬��
            // �������Ӵ�ǽ�� + �ڿ��� + ��������
            if (rb.velocity.y <= 0)
            {
                isWallSliding = true;
            }
        }
        else
        {
            capsuleCollider.sharedMaterial = config.normalMaterial;

            isWallSliding = false;
        }
    }

    private void UpdateGravity()
    {
        if (!isDash)  // �ǳ��״̬
        {
            if (isWallSliding&&!hitstunSystem.IsInHitstun())
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
            // ��Ծ����ʱ����ͣ��
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
        if (isSpecialDashing)
        {
            // ����1����ƶ�
            ApplySpecialDashMovement();
        }
        else if (isDash)
        {
            // ��ͨ���
            ApplyDashMovement();
        }
        else
        {
            // ��ͨ�ƶ�
            Move(1);
        }
    }

    // ��ʼ����1���
    public void StartSpecialDash(bool flag,string skillName)
    {
        isSpecialDashing = true;
        isInvulnerable = true;
        currentSpecialMove = skillName;  // ��¼��ǰ��������

        if (flag)
            SetInvulnerable(true);
        else
            SetPlayerPassThrough(true);

        // ȡ������״̬
        isAttack = false;
        isAttackSpeedActive = false;
        isWallSliding = false;
        isDash = false;
        canDash = false; 

        // TODO�������Ч
        // PlaySpecialMove1Effect();
    }

    // ��������1���
    private void EndSpecialDash()
    {
        isSpecialDashing = false;
        isInvulnerable = false;
        currentSpecialMove = "";  // ��ռ��ܼ�¼
        // �ָ�ԭʼ�㼶
        SetPlayerPassThrough(false);
        SetInvulnerable(false);  // ȷ���޵�״̬Ҳ������
        // �ָ���ͨ�������
        canDash = true;
    }

    // ����ƶ�
    private void ApplySpecialDashMovement()
    {
        float dashDirection = isFacingRight ? 1f : -1f;
        float dashSpeed = movementData.dashSpeed;

        if (specialMoveDict != null && specialMoveDict.TryGetValue(currentSpecialMove, out SpecialMoveConfig moveConfig))
        {
            dashSpeed = moveConfig.dashSpeed;
        }
        else
        {
            // �Ҳ���������Ϣ
            dashSpeed = movementData.dashSpeed;
        }
        rb.velocity = new Vector2(dashDirection * dashSpeed, 0f);

        // ����ڼ�������
        SetGravityScale(0);
    }

    // �����޵�״̬
    public void SetInvulnerable(bool invulnerable)
    {
        if (invulnerable)
        {
            gameObject.layer = invulnerableLayer;
            // Debug���ı��ɫ��ɫ��ʾ�޵�״̬
             GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
        }
        else
        {
            gameObject.layer = originalLayer;
             GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    // ���ô�͸���״̬
    public void SetPlayerPassThrough(bool passThrough)
    {
        if (passThrough)
        {
            gameObject.layer = playerPassThroughLayer;
            // Debug���ı��ɫ��ɫ��ʾ��͸״̬
            GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.8f, 1f, 0.8f); // ����ɫ��ʾ��͸״̬
        }
        else
        {
            gameObject.layer = originalLayer;
            GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    public void Move(float lerpAmount)
    {
        // ����Ƿ񱻽�ֱ���
        if (hitstunSystem != null && !hitstunSystem.IsMoveAllowed())
        {
            //rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // ���ڼ䲻���ƶ�
        if (isBlock)
        {
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
        if (hitstunSystem != null && !hitstunSystem.IsAttackAllowed())
            return;


        // ���ʱ��ǽ�滬��ʱ���ܹ���
        if (isDash || isWallSliding) return;

        animator.PlaySkill("Attack");
        isAttack = true;

        // ���ݽ�ɫ���Ͳ��Ų�ͬ�Ĺ�����Ч
        PlayCharacterSpecificAttackAudio();



        // ʩ�ӹ������������ٶȿ���
        if (!isCrouch)
            ApplyAttackForce();


    }

    private void PlayCharacterSpecificAttackAudio()
    {
        if (AudioManager.Instance == null) return;

        string soundName = GetRandomAttackSound();
        if (!string.IsNullOrEmpty(soundName))
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    private string GetRandomAttackSound()
    {
        string[] attackSounds = GetCharacterAttackSounds();
        if (attackSounds.Length == 0) return null;

        int randomIndex = Random.Range(0, attackSounds.Length);
        return attackSounds[randomIndex];
    }

    private string[] GetCharacterAttackSounds()
    {
        switch (characterType)
        {
            case CharacterType.Kirito:
                return new string[] { "sword_1", "sword_2", "sword_3" };

            case CharacterType.Misaka:
                return new string[] { "attack" };
            default:
                return new string[] { "attack" };
        }
    }

    public void PerformJump()
    {
        if (hitstunSystem != null && !hitstunSystem.IsJumpAllowed())
            return;

        LastPressedJumpTime = movementData.jumpInputBufferTime;
    }

    public void PerformDash()
    {
        if (hitstunSystem != null && !hitstunSystem.IsDashAllowed())
            return;


        if (canDash && !isDash)
        {
            StartDash();
        }
    }

    public void PerformCrouch(bool isPressed)
    {
        // ��齩ֱ״̬��ֻ�ڰ���ʱ��飩
        if (isPressed && hitstunSystem != null && !hitstunSystem.IsMoveAllowed())
            return;


        wantsToCrouch = isPressed;

        if (isPressed)
        {
            if (CanCrouch())
            {
                isCrouch = true;
                UpdateColliderSize(true); // �л���������ײ��
            }
        }
        else
        {
            // ����վ����
            if (CanStandUp())
            {
                isCrouch = false;
                UpdateColliderSize(false); // �л���վ����ײ��
            }
            // �������վ������isCrouch����true���ȴ��Զ�վ��
        }
    }

    private void UpdateColliderSize(bool isCrouching)
    {
        if (capsuleCollider != null)
        {
            if (isCrouching)
            {
                // �л���������ײ��
                capsuleCollider.offset = config.crouchingCollider.offset;
                capsuleCollider.size = config.crouchingCollider.size;
            }
            else
            {
                // �л���վ����ײ��
                capsuleCollider.offset = config.standingCollider.offset;
                capsuleCollider.size = config.standingCollider.size;
            }
        }
    }

    public bool CanStandUp()
    {
        if (config == null) return true;

        // ���վ����ʱ�Ƿ�������컨��
        float standingHeight = config.standingCollider.size.y;
        float crouchingHeight = config.crouchingCollider.size.y;
        float heightDiff = standingHeight - crouchingHeight;

        // �ӵ�ǰλ�����ϼ��
        Vector2 checkOrigin = (Vector2)transform.position + config.crouchingCollider.offset + Vector2.up * (crouchingHeight * 0.5f);
        Vector2 checkSize = new Vector2(config.standingCollider.size.x * 0.9f, heightDiff);

        // ����Ƿ�����ײ
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkOrigin + Vector2.up * (heightDiff * 0.5f), checkSize, 0f, groundLayer);

        // �ų�������ײ��
        foreach (var hit in hits)
        {
            if (hit != capsuleCollider)
            {
                return false; // ���ϰ������վ����
            }
        }

        return true;
    }

    public void PerformBlock(bool isPressed)
    {
        if (isPressed && hitstunSystem != null && !hitstunSystem.IsBlockAllowed())
            return;


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

    public bool CanCrouch()
    {
        return isGround && !isDash && !isWallSliding;
    }

    public bool CanBlock()
    {
        return isGround && !isDash && !isWallSliding;
    }

    public bool CanDash()=>canDash;

    private void StartDash()
    {
        isDash = true;
        canDash = false;
        dashTimer = movementData.dashDuration;

        // ��ͨ�������Ϊ�޵�
        isInvulnerable = true;
        SetInvulnerable(true);

        // ȡ������״̬
        isAttack = false;
        isAttackSpeedActive = false;
        isWallSliding = false; // ���ʱֹͣǽ�滬��

        // TODO: ��ӳ����Ч
        AudioManager.Instance?.PlaySFX("dash");

        // TODO: ��ӳ����Ч
        // PlayDashEffect();
    }

    private void EndDash()
    {
        isDash = false;
        dashCooldownTimer = movementData.dashCooldown;

        // �����޵�״̬
        isInvulnerable = false;
        SetInvulnerable(false);
    }

    public bool CanJump()
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

    public bool GetIsInvulnerable()
    {
        return isInvulnerable;
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            // ֹͣ�ý�ɫ��ص�ѭ����Ч
            AudioManager.Instance.StopLoopingSFX("fall");
            // �����������ɫר����ѭ����Ч��Ҳ������ֹͣ
        }
    }


    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        // ��������ӻ�
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Vector2 groundSize = config != null ? config.detection.groundCheckSize : new Vector2(0.49f, 0.03f);
            Gizmos.DrawWireCube(groundCheckPoint.position, groundSize);
        }

        // ǽ������ӻ�
        if (wallCheckPoint != null)
        {
            Gizmos.color = isWallSliding ? Color.red : Color.blue;
            Vector2 wallSize = config != null ? config.detection.wallCheckSize : new Vector2(1.27f, 0.8f);
            Gizmos.DrawWireCube(wallCheckPoint.position, wallSize);
        }

        // վ������ӻ������ڶ���ʱ��ʾ��
        if (isCrouch && Application.isPlaying)
        {
            float heightDiff = config.standingCollider.size.y - config.crouchingCollider.size.y;
            Vector2 checkOrigin = (Vector2)transform.position + config.crouchingCollider.offset + Vector2.up * (config.crouchingCollider.size.y * 0.5f);
            Vector2 checkSize = new Vector2(config.standingCollider.size.x * 0.9f, heightDiff);

            Gizmos.color = CanStandUp() ? Color.yellow : Color.red;
            Gizmos.DrawWireCube(checkOrigin + Vector2.up * (heightDiff * 0.5f), checkSize);
        }
    }
    #endregion
}