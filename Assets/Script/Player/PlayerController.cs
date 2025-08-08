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
    [Header("配置文件")]
    public PlayerConfig config;

    [Header("数据配置")]
    public PlayerMovementData movementData;
    public PlayerCombatData combatData;

    private Rigidbody2D rb;
    public PlayerAnimator animator;
    [SerializeField]
    private ComboSystem comboSystem;
    private CapsuleCollider2D capsuleCollider; // 添加胶囊碰撞体引用
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

    //// 碰撞体配置
    //[Header("碰撞体设置")]
    //[SerializeField] private Vector2 standingColliderOffset = new Vector2(0, -0.84f);
    //[SerializeField] private Vector2 standingColliderSize = new Vector2(1.2f, 3.66f);
    //[SerializeField] private Vector2 crouchingColliderOffset = new Vector2(0, -1.5f);
    //[SerializeField] private Vector2 crouchingColliderSize = new Vector2(1.2f, 2.35f);

    //[Header("特殊技能配置")]
    //[SerializeField] private List<SpecialMoveConfig> specialMoveConfigs = new List<SpecialMoveConfig>();

    // 特殊技能配置字典，用于快速查找
    private Dictionary<string, SpecialMoveConfig> specialMoveDict;
    [Header("技能状态")]
    [SerializeField] private bool isInvulnerable = false;  // 无敌状态
    private bool isSpecialDashing = false;
    private string currentSpecialMove = "";

    // 原始层级存储
    private int originalLayer;
    private int invulnerableLayer;
    private int playerPassThroughLayer;

    // 攻击速度控制
    private bool isAttackSpeedActive = false;
    private float attackSpeedTimer = 0f;

    //冲刺控制
    private float dashTimer = 0f;
    private bool canDash = true;
    private float dashCooldownTimer = 0f;

    // 跳跃优化
    public float lastOnGroundTime; // 土狼时间优化
    public float LastPressedJumpTime; // 跳跃缓冲优化

    public LayerMask wallLayer;                 // 墙面层
    [SerializeField]
    private bool isWallSliding = false;         // 是否在墙面滑落
    [SerializeField]
    private bool isTouchingWall = false;        // 是否接触墙面

    [SerializeField]
    private Transform groundCheckPoint;
    [SerializeField]
    private LayerMask groundLayer;
    [SerializeField]
    private Transform wallCheckPoint;           // 墙面检测点

    // 记录玩家是否想要蹲下
    private bool wantsToCrouch = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<PlayerAnimator>();
        comboSystem = GetComponent<ComboSystem>();
        capsuleCollider = GetComponent<CapsuleCollider2D>(); // 获取胶囊碰撞体组件
        hitstunSystem = GetComponent<HitstunSystem>();
        playerStats = GetComponent<PlayerStats>();


        ValidateConfig();

        // 如果没有设置wallLayer，使用groundLayer
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

        // 确保开始时使用站立碰撞体
        UpdateColliderSize(false);

     

        invulnerableLayer = LayerMask.NameToLayer(config.layers.invulnerableLayerName);
        if (invulnerableLayer == -1)
        {
            Debug.LogError($"Layer '{config.layers.invulnerableLayerName}' 未找到! 请在Physics2D设置中创建该层级，并设置与Player层不碰撞..");
        }

        playerPassThroughLayer = LayerMask.NameToLayer(config.layers.invulnerableLayerName);
        if (playerPassThroughLayer == -1)
        {
            Debug.LogError($"Layer '{config.layers.invulnerableLayerName}' 未找到! 请在Physics2D设置中创建该层级，并设置与Player层不碰撞.");
        }

        originalLayer = gameObject.layer;
        GameManager.Instance.RigisterPlayer(playerStats, PlayerId);

        // 根据playerID决定角色面朝方向 
         if(PlayerId == PlayerID.Player2)
        {
            Turn();
        }
    }

    private void ValidateConfig()
    {
        if (config == null)
        {
            Debug.LogError("PlayerConfig 未设置！请在Inspector中指定配置文件。", this);
            return;
        }

        if (config.normalMaterial == null)
            Debug.LogWarning("PlayerConfig: normalMaterial 未设置", this);

        if (config.wallMaterial == null)
            Debug.LogWarning("PlayerConfig: wallMaterial 未设置", this);
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

        // 自动站起检测
        if (isCrouch && !wantsToCrouch && CanStandUp())
        {
            isCrouch = false;
            UpdateColliderSize(false);
        }

        if (comboSystem != null)
        {
            inputDirection = comboSystem.GetMovementInput();
        }

        // 处理Sprite的翻转（冲刺时不能转向）
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

        // 墙面检测（在地面检测之前）
        CheckWallSliding();

        // 地面检测
        if (!isDash && !isJump)
        {
            if (Physics2D.OverlapBox(groundCheckPoint.position, config.detection.groundCheckSize, 0, groundLayer))
            {
                // 着陆
                if (lastOnGroundTime < -0.1f)
                {
                    // 停止下落音效（如果正在播放）
                    if (AudioManager.Instance != null && AudioManager.Instance.IsLoopingSFXPlaying("fall"))
                    {
                        AudioManager.Instance.StopLoopingSFX("fall");
                    }

                    // 播放着陆音效
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
            // 开始播放循环下落音效
            AudioManager.Instance?.PlayLoopingSFX("fall");
        }

        // 如果不再下落，停止下落音效
        if (isJumpFall && (isGround || rb.velocity.y >= 0))
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsLoopingSFXPlaying("fall"))
            {
                AudioManager.Instance.StopLoopingSFX("fall");
            }
        }

        // 跳跃检测（冲刺时不能跳跃）
        if (!isDash)
        {
            bool canJump = hitstunSystem == null || hitstunSystem.IsJumpAllowed();

            if (canJump&&CanJump() && LastPressedJumpTime > 0)
            {
                // 重置跳跃相关计时器
                lastOnGroundTime = 0;
                LastPressedJumpTime = 0;

                // 设置跳跃状态
                isJump = true;
                isGround = false;
                isWallSliding = false; // 跳跃时停止墙面滑落

                if (AudioManager.Instance != null && AudioManager.Instance.IsLoopingSFXPlaying("fall"))
                {
                    AudioManager.Instance.StopLoopingSFX("fall");
                }

                // 计算跳跃力度，如果正在下降则补偿向下的速度
                float jumpForce = movementData.jumpForce;
                if (rb.velocity.y < 0)
                    jumpForce -= rb.velocity.y;

                AudioManager.Instance?.PlaySFX("jump");

                // 施加向上的跳跃力
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
        }

        // 如果回到地面，重置跳跃状态
        if (lastOnGroundTime > 0 && !isJump)
        {
            // 停止下落音效
            if (AudioManager.Instance != null && AudioManager.Instance.IsLoopingSFXPlaying("fall"))
            {
                AudioManager.Instance.StopLoopingSFX("fall");
            }


            isJumpFall = false;
            isWallSliding = false; // 着陆时停止墙面滑落
        }

        // 更新攻击速度状态
        UpdateAttackSpeed();

        // 调整重力缩放
        UpdateGravity();
    }

    private void CheckWallSliding()
    {
        // 只有在空中且不在冲刺状态时才检测墙面滑落
        if (isGround || isDash)
        {
            isWallSliding = false;
            isTouchingWall = false;
            return;
        }

        // 检测是否接触墙面
        isTouchingWall = Physics2D.OverlapBox(wallCheckPoint.position, config.detection.wallCheckSize, 0, wallLayer);
        // Debug.Log(isTouchingWall);
        if (isTouchingWall)
        {
            capsuleCollider.sharedMaterial = config.wallMaterial;

            // 判断是否应该开始墙面滑落
            // 条件：接触墙面 + 在空中 + 正在下落
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
        if (!isDash)  // 非冲刺状态
        {
            if (isWallSliding&&!hitstunSystem.IsInHitstun())
            {
                // 墙面滑落时使用特殊重力
                SetGravityScale(movementData.gravityScale * movementData.wallSlideGravity);

                // 限制墙面滑落速度
                if (rb.velocity.y < -movementData.wallSlideSpeed)
                {
                    rb.velocity = new Vector2(rb.velocity.x, -movementData.wallSlideSpeed);
                }
            }
            // 快速下落（按住下键）
            else if (rb.velocity.y < 0 && inputDirection.y < 0)
            {
                SetGravityScale(movementData.gravityScale * movementData.fastFallGravityMult);
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -movementData.maxFastFallSpeed));
            }
            // 跳跃顶点时的悬停感
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
        if (isSpecialDashing)
        {
            // 技能1冲刺移动
            ApplySpecialDashMovement();
        }
        else if (isDash)
        {
            // 普通冲刺
            ApplyDashMovement();
        }
        else
        {
            // 普通移动
            Move(1);
        }
    }

    // 开始技能1冲刺
    public void StartSpecialDash(bool flag,string skillName)
    {
        isSpecialDashing = true;
        isInvulnerable = true;
        currentSpecialMove = skillName;  // 记录当前技能名称

        if (flag)
            SetInvulnerable(true);
        else
            SetPlayerPassThrough(true);

        // 取消其他状态
        isAttack = false;
        isAttackSpeedActive = false;
        isWallSliding = false;
        isDash = false;
        canDash = false; 

        // TODO：添加特效
        // PlaySpecialMove1Effect();
    }

    // 结束技能1冲刺
    private void EndSpecialDash()
    {
        isSpecialDashing = false;
        isInvulnerable = false;
        currentSpecialMove = "";  // 清空技能记录
        // 恢复原始层级
        SetPlayerPassThrough(false);
        SetInvulnerable(false);  // 确保无敌状态也被重置
        // 恢复普通冲刺能力
        canDash = true;
    }

    // 冲刺移动
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
            // 找不到配置信息
            dashSpeed = movementData.dashSpeed;
        }
        rb.velocity = new Vector2(dashDirection * dashSpeed, 0f);

        // 冲刺期间无重力
        SetGravityScale(0);
    }

    // 设置无敌状态
    public void SetInvulnerable(bool invulnerable)
    {
        if (invulnerable)
        {
            gameObject.layer = invulnerableLayer;
            // Debug：改变角色颜色表示无敌状态
             GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
        }
        else
        {
            gameObject.layer = originalLayer;
             GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    // 设置穿透玩家状态
    public void SetPlayerPassThrough(bool passThrough)
    {
        if (passThrough)
        {
            gameObject.layer = playerPassThroughLayer;
            // Debug：改变角色颜色表示穿透状态
            GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.8f, 1f, 0.8f); // 淡蓝色表示穿透状态
        }
        else
        {
            gameObject.layer = originalLayer;
            GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    public void Move(float lerpAmount)
    {
        // 检查是否被僵直或格挡
        if (hitstunSystem != null && !hitstunSystem.IsMoveAllowed())
        {
            //rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // 格挡期间不能移动
        if (isBlock)
        {
            return;
        }


        // 使用平滑插值来加速
        float speedMultiplier = isCrouch ? movementData.crouchSpeedMultiplier : 1f;

        float targetSpeed = inputDirection.x * movementData.runMaxSpeed * speedMultiplier;

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

    public void PerformAttack()
    {
        if (hitstunSystem != null && !hitstunSystem.IsAttackAllowed())
            return;


        // 冲刺时和墙面滑落时不能攻击
        if (isDash || isWallSliding) return;

        animator.PlaySkill("Attack");
        isAttack = true;

        // 根据角色类型播放不同的攻击音效
        PlayCharacterSpecificAttackAudio();



        // 施加攻击力和启动速度控制
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
        // 检查僵直状态（只在按下时检查）
        if (isPressed && hitstunSystem != null && !hitstunSystem.IsMoveAllowed())
            return;


        wantsToCrouch = isPressed;

        if (isPressed)
        {
            if (CanCrouch())
            {
                isCrouch = true;
                UpdateColliderSize(true); // 切换到蹲下碰撞体
            }
        }
        else
        {
            // 尝试站起来
            if (CanStandUp())
            {
                isCrouch = false;
                UpdateColliderSize(false); // 切换到站立碰撞体
            }
            // 如果不能站起来，isCrouch保持true，等待自动站起
        }
    }

    private void UpdateColliderSize(bool isCrouching)
    {
        if (capsuleCollider != null)
        {
            if (isCrouching)
            {
                // 切换到蹲下碰撞体
                capsuleCollider.offset = config.crouchingCollider.offset;
                capsuleCollider.size = config.crouchingCollider.size;
            }
            else
            {
                // 切换到站立碰撞体
                capsuleCollider.offset = config.standingCollider.offset;
                capsuleCollider.size = config.standingCollider.size;
            }
        }
    }

    public bool CanStandUp()
    {
        if (config == null) return true;

        // 检查站起来时是否会碰到天花板
        float standingHeight = config.standingCollider.size.y;
        float crouchingHeight = config.crouchingCollider.size.y;
        float heightDiff = standingHeight - crouchingHeight;

        // 从当前位置向上检测
        Vector2 checkOrigin = (Vector2)transform.position + config.crouchingCollider.offset + Vector2.up * (crouchingHeight * 0.5f);
        Vector2 checkSize = new Vector2(config.standingCollider.size.x * 0.9f, heightDiff);

        // 检测是否有碰撞
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkOrigin + Vector2.up * (heightDiff * 0.5f), checkSize, 0f, groundLayer);

        // 排除自身碰撞体
        foreach (var hit in hits)
        {
            if (hit != capsuleCollider)
            {
                return false; // 有障碍物，不能站起来
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

        // 普通冲刺设置为无敌
        isInvulnerable = true;
        SetInvulnerable(true);

        // 取消其他状态
        isAttack = false;
        isAttackSpeedActive = false;
        isWallSliding = false; // 冲刺时停止墙面滑落

        // TODO: 添加冲刺音效
        AudioManager.Instance?.PlaySFX("dash");

        // TODO: 添加冲刺特效
        // PlayDashEffect();
    }

    private void EndDash()
    {
        isDash = false;
        dashCooldownTimer = movementData.dashCooldown;

        // 结束无敌状态
        isInvulnerable = false;
        SetInvulnerable(false);
    }

    public bool CanJump()
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

    public bool GetIsInvulnerable()
    {
        return isInvulnerable;
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            // 停止该角色相关的循环音效
            AudioManager.Instance.StopLoopingSFX("fall");
            // 如果有其他角色专属的循环音效，也在这里停止
        }
    }


    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        // 地面检测可视化
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Vector2 groundSize = config != null ? config.detection.groundCheckSize : new Vector2(0.49f, 0.03f);
            Gizmos.DrawWireCube(groundCheckPoint.position, groundSize);
        }

        // 墙面检测可视化
        if (wallCheckPoint != null)
        {
            Gizmos.color = isWallSliding ? Color.red : Color.blue;
            Vector2 wallSize = config != null ? config.detection.wallCheckSize : new Vector2(1.27f, 0.8f);
            Gizmos.DrawWireCube(wallCheckPoint.position, wallSize);
        }

        // 站起检测可视化（仅在蹲下时显示）
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