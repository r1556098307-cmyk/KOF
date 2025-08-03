using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

public enum AIState
{
    Idle, Approach, Attack, Defend, Retreat, Pursuit
}

public class AIStateMachine : MonoBehaviour
{
    [Header("配置")]
    public AIConfig aiConfig;

    [Header("状态显示")]
    [SerializeField] private AIState currentState = AIState.Idle;
    [SerializeField] private float stateTimer = 0f;

    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;

    // 核心组件引用
    private PlayerController playerController;
    private PlayerStats playerStats;
    private HitstunSystem hitstunSystem;
    private AIInputProvider aiInput;
    private ComboSystem comboSystem;

    // 目标引用
    private Transform target;
    private PlayerController targetController;

    // 状态字典
    private Dictionary<AIState, System.Action> stateActions;
    private Dictionary<AIState, System.Action> stateEnterActions;
    private Dictionary<AIState, System.Action> stateExitActions;

    // 当前行为记录
    [SerializeField] private AIAttackType currentAttackType = AIAttackType.Normal;

    // 连招相关
    private List<ComboSystem.ComboData> availableCombos = new List<ComboSystem.ComboData>();
    private ComboSystem.ComboData selectedCombo = null;
    private bool isComboInProgress = false;

    // 缓存状态
    private bool isGrounded;
    private bool targetIsAttacking;
    private bool targetIsDefending;
    private bool targetIsHigher;

    private bool isPerformingAttack = false;
    private float lastAttackTime = 0f;

    private void Awake()
    {
        InitializeComponents();
        InitializeStateMachine();
        FindTarget();
    }

    private void Start()
    {
        ValidateComponents();
        LoadAvailableCombos();
    }

    // ========== 初始化方法 ==========
    private void InitializeComponents()
    {
        playerController = GetComponent<PlayerController>();
        playerStats = GetComponent<PlayerStats>();
        hitstunSystem = GetComponent<HitstunSystem>();
        aiInput = GetComponent<AIInputProvider>();
        comboSystem = GetComponent<ComboSystem>();
    }

    private void ValidateComponents()
    {
        if (!playerController || !playerStats || !hitstunSystem || !aiInput || !comboSystem || !aiConfig)
        {
            Debug.LogError("AI缺少所需的组件!");
            enabled = false;
        }
    }

    private void LoadAvailableCombos()
    {
        availableCombos.Clear();

        if (comboSystem?.combos?.Count <= 0) return;

        foreach (var combo in comboSystem.combos)
        {
            if (combo?.keySequence?.Count > 0 && !string.IsNullOrEmpty(combo.skillName))
            {
                availableCombos.Add(combo);
            }
        }
    }

    private void InitializeStateMachine()
    {
        stateActions = new Dictionary<AIState, System.Action>
        {
            { AIState.Idle, UpdateIdle },
            { AIState.Approach, UpdateApproach },
            { AIState.Attack, UpdateAttack },
            { AIState.Defend, UpdateDefend },
            { AIState.Retreat, UpdateRetreat },
            { AIState.Pursuit, UpdatePursuit }
        };

        stateEnterActions = new Dictionary<AIState, System.Action>
        {
            { AIState.Idle, EnterIdle },
            { AIState.Approach, EnterApproach },
            { AIState.Attack, EnterAttack },
            { AIState.Defend, EnterDefend },
            { AIState.Retreat, EnterRetreat },
            { AIState.Pursuit, EnterPursuit }
        };

        stateExitActions = new Dictionary<AIState, System.Action>
        {
            { AIState.Idle, ExitIdle },
            { AIState.Approach, ExitApproach },
            { AIState.Attack, ExitAttack },
            { AIState.Defend, ExitDefend },
            { AIState.Retreat, ExitRetreat },
            { AIState.Pursuit, ExitPursuit }
        };
    }

    private void FindTarget()
    {
        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            if (player != playerController)
            {
                target = player.transform;
                targetController = player;
                break;
            }
        }
    }

    // ========== 主循环 ==========
    private void Update()
    {
        if (!target || !enabled) return;

        // 始终面朝玩家方向
        //FaceTarget();

        UpdateCachedStates();

        stateTimer += Time.deltaTime;
        stateActions[currentState]?.Invoke();
    }

    private void UpdateCachedStates()
    {
        isGrounded = playerController.isGround;
        targetIsAttacking = targetController.isAttack;
        targetIsDefending = targetController.isBlock;
        targetIsHigher = target.position.y - transform.position.y > aiConfig.jumpThreshold;
    }

    private void FaceTarget()
    {
        float direction = Mathf.Sign(target.position.x - transform.position.x);
        if (direction != 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction,
                                              transform.localScale.y,
                                              transform.localScale.z);
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;

        // 执行退出动作
        if (stateExitActions.ContainsKey(currentState))
        {
            stateExitActions[currentState]?.Invoke();
        }

        AIState previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        // 执行进入动作
        if (stateEnterActions.ContainsKey(newState))
        {
            stateEnterActions[newState]?.Invoke();
        }

        Debug.Log($"AI 状态切换: {previousState} -> {newState}");
    }

    // ========== 状态更新方法 ==========
    private void UpdateIdle()
    {
        aiInput.SetMovementInput(Vector2.zero);
        float distance = GetDistanceToTarget();

        if (stateTimer > aiConfig.idleDuration)
        {
            if (targetIsAttacking && distance < aiConfig.attackRange * 1.5f)
            {
                ChangeState(AIState.Defend);
            }
            else if (distance > aiConfig.pursuitRange)
            {
                ChangeState(AIState.Pursuit);
            }
            else if (distance <= aiConfig.attackRange)
            {
                ChangeState(AIState.Attack);
            }
            else if (distance <= aiConfig.dashRange && playerController.CanDash())
            {
                PerformDash();
                ChangeState(AIState.Attack);
            }
            else
            {
                // 如果在攻击范围和追击范围之间，主动接近
                ChangeState(AIState.Approach);
            }
        }
    }

    private void UpdateApproach()
    {
        float distance = GetDistanceToTarget();
        Vector2 direction = GetDirectionToTarget();

        aiInput.SetMovementInput(direction);

        if (targetIsDefending) PerformDash();

        if (targetIsHigher && playerController.CanJump()) PerformJump();

        if (targetIsAttacking && distance < aiConfig.attackRange * 1.5f)
        {
            ChangeState(AIState.Defend);
            return;
        }

        if (distance <= aiConfig.attackRange)
        {
            ChangeState(AIState.Attack);
        }

        if (stateTimer > aiConfig.approachDuration)
        {
            ChangeState(AIState.Idle);
        }
    }

    private void UpdateAttack()
    {
        if (targetIsDefending) PerformDash();

        // 如果没有在执行攻击
        if (!isPerformingAttack)
        {
            // 计算距离上次攻击的时间间隔
            float timeSinceLastAttack = stateTimer - lastAttackTime;

            // 如果是首次攻击或者间隔攻击反应时间后，且还有时间
            if ((lastAttackTime == 0f || timeSinceLastAttack > aiConfig.attackReactionTime) &&
                stateTimer < aiConfig.attackDuration)
            {
                currentAttackType = GetWeightedRandomAttackType();
                StartCoroutine(PerformAttackSequence());
                isPerformingAttack = true;
                lastAttackTime = stateTimer;
            }
        }

        // 时间到了就切换状态
        if (stateTimer > aiConfig.attackDuration)
        {
            ChangeState(AIState.Idle);
        }
    }

    private AIAttackType GetWeightedRandomAttackType()
    {
        // 如果没有配置权重，使用默认逻辑
        if (aiConfig.attackTypeWeights == null || aiConfig.attackTypeWeights.Length == 0)
        {
            float random = Random.value;
            if (random < 0.5f) return AIAttackType.Normal;
            else if (random < 0.7f) return AIAttackType.Crouch;
            else if (availableCombos.Count > 0) return AIAttackType.Combo;
            else return AIAttackType.Normal;
        }

        // 计算总权重
        float totalWeight = 0f;
        foreach (var weight in aiConfig.attackTypeWeights)
        {
            // 如果是连招但没有可用连招，跳过
            if (weight.attackType == AIAttackType.Combo && availableCombos.Count == 0)
                continue;

            totalWeight += weight.weight;
        }

        // 随机选择
        float randomPoint = Random.value * totalWeight;
        float currentWeight = 0f;

        foreach (var weight in aiConfig.attackTypeWeights)
        {
            // 如果是连招但没有可用连招，跳过
            if (weight.attackType == AIAttackType.Combo && availableCombos.Count == 0)
                continue;

            currentWeight += weight.weight;
            if (randomPoint <= currentWeight)
            {
                return weight.attackType;
            }
        }

        // 默认返回普通攻击
        return AIAttackType.Normal;
    }

    private void UpdateDefend()
    {
        // 使用配置中的概率
        bool isCrouch = Random.value < aiConfig.crouchDefendChance;

        aiInput.SetBlockInput(true);
        if (isCrouch) aiInput.SetCrouchInput(true);

        float duration = aiConfig.defendDuration;

        if (stateTimer > duration || !targetIsAttacking)
        {
            float distance = GetDistanceToTarget();

            if (distance > aiConfig.attackRange)
            {
                ChangeState(AIState.Approach);
            }
            else if (distance <= aiConfig.attackRange)
            {
                ChangeState(AIState.Attack);
            }
            else
            {
                ChangeState(AIState.Idle);
            }
        }
    }

    private void UpdateRetreat()
    {
        aiInput.SetMovementInput(-GetDirectionToTarget());

        if (stateTimer > aiConfig.retreatDuration)
        {
            ChangeState(AIState.Idle);
        }
    }

    private void UpdatePursuit()
    {
        Vector2 direction = GetDirectionToTarget();
        float distance = GetDistanceToTarget();

        if (playerController.CanDash() && distance > aiConfig.dashRange) PerformDash();
        if (targetIsHigher && playerController.CanJump()) PerformJump();

        aiInput.SetMovementInput(direction);

        if (distance <= aiConfig.attackRange)
            ChangeState(AIState.Attack);
        else if (stateTimer > aiConfig.pursuitDuration)
            ChangeState(AIState.Approach);
    }

    // ========== 攻击序列 ==========
    private IEnumerator PerformAttackSequence()
    {
        switch (currentAttackType)
        {
            case AIAttackType.Normal:
                yield return StartCoroutine(PerformNormalAttack());
                break;
            case AIAttackType.Crouch:
                yield return StartCoroutine(PerformCrouchAttack());
                break;
            case AIAttackType.Combo:
                selectedCombo = SelectBestAvailableCombo();
                if (selectedCombo != null)
                {
                    yield return StartCoroutine(PerformCombo());
                }
                else
                {
                    Debug.LogWarning("AI: 没有可用连招，退回普通攻击");
                    currentAttackType = AIAttackType.Normal;
                    yield return StartCoroutine(PerformNormalAttack());
                }
                break;
            default:
                yield return StartCoroutine(PerformNormalAttack());
                break;
        }

        isPerformingAttack = false;
    }

    private ComboSystem.ComboData SelectBestAvailableCombo()
    {
        if (availableCombos == null || availableCombos.Count == 0)
        {
            Debug.LogWarning("AI: 没有可用连招");
            return null;
        }

        float currentEnergy = playerStats.CurrentEnergyNum;
        float maxEnergy = playerStats.MaxEnergyNum;
        float healthPercentage = playerStats.CurrentHealth / playerStats.MaxHealth;

        // 使用配置中的血量阈值
        if (healthPercentage > aiConfig.healthThresholdForSaving && currentEnergy < maxEnergy)
        {
            var zeroCostCombos = availableCombos
                .Where(combo => combo.energyCost == 0)
                .ToList();

            if (zeroCostCombos.Count > 0)
            {
                return zeroCostCombos[Random.Range(0, zeroCostCombos.Count)];
            }
        }

        // 血量低于阈值：有什么用什么，优先高消耗
        var affordableCombos = availableCombos
            .Where(combo => combo.energyCost <= currentEnergy)
            .OrderByDescending(combo => combo.energyCost)
            .ToList();

        if (affordableCombos.Count > 0)
        {
            return affordableCombos[0];
        }

        // 如果没有可用技能，返回null使用普通攻击
        return null;
    }

    private IEnumerator PerformNormalAttack()
    {
        for (int i = 0; i < 3; i++)
        {
            aiInput.PerformAttack();
            yield return new WaitForSeconds(aiConfig.attackButtonInterval);
        }
    }

    private IEnumerator PerformCrouchAttack()
    {
        // 使用配置中的蹲下延迟
        aiInput.SetCrouchInput(true);
        yield return new WaitForSeconds(aiConfig.crouchDelay);

        // 连续攻击
        for (int i = 0; i < 3; i++)
        {
            aiInput.PerformAttack();
            yield return new WaitForSeconds(aiConfig.attackButtonInterval);
        }

        // 松开蹲下
        aiInput.SetCrouchInput(false);
    }

    private IEnumerator PerformCombo()
    {
        if (selectedCombo == null || selectedCombo.keySequence == null)
        {
            yield break;
        }

        isComboInProgress = true;

        // AI只负责按键序列，不管能量
        foreach (var inputKey in selectedCombo.keySequence)
        {
            ExecuteComboInput(inputKey);
            yield return new WaitForSeconds(aiConfig.comboInputInterval);
        }

        selectedCombo = null;
        isComboInProgress = false;
    }

    private void ExecuteComboInput(ComboSystem.GameInputKey inputKey)
    {
        // 使用配置中的按键释放延迟
        switch (inputKey)
        {
            case ComboSystem.GameInputKey.Attack:
                aiInput.PerformAttack();
                break;
            case ComboSystem.GameInputKey.MoveDown:
                aiInput.SetCrouchInput(true);
                StartCoroutine(ReleaseKeyAfterDelay(() => aiInput.SetCrouchInput(false), aiConfig.keyReleaseDelay));
                break;
            case ComboSystem.GameInputKey.MoveUp:
                aiInput.SetMovementInput(Vector2.up);
                StartCoroutine(ReleaseKeyAfterDelay(() => aiInput.SetMovementInput(Vector2.zero), aiConfig.keyReleaseDelay));
                break;
            case ComboSystem.GameInputKey.Block:
                aiInput.SetBlockInput(true);
                StartCoroutine(ReleaseKeyAfterDelay(() => aiInput.SetBlockInput(false), aiConfig.blockHoldTime));
                break;
            case ComboSystem.GameInputKey.MoveLeft:
                aiInput.SetMovementInput(Vector2.left);
                StartCoroutine(ReleaseKeyAfterDelay(() => aiInput.SetMovementInput(Vector2.zero), aiConfig.keyReleaseDelay));
                break;
            case ComboSystem.GameInputKey.MoveRight:
                aiInput.SetMovementInput(Vector2.right);
                StartCoroutine(ReleaseKeyAfterDelay(() => aiInput.SetMovementInput(Vector2.zero), aiConfig.keyReleaseDelay));
                break;
            case ComboSystem.GameInputKey.Jump:
                aiInput.PerformJump();
                break;
            case ComboSystem.GameInputKey.Dash:
                aiInput.PerformDash();
                break;
        }
    }


    // ========== 状态进入方法 ==========
    private void EnterIdle() { }
    private void EnterApproach() { }
    private void EnterAttack()
    {
        lastAttackTime = 0f;
    }
    private void EnterDefend() { }
    private void EnterRetreat() { }
    private void EnterPursuit() { }

    // ========== 状态退出方法 ==========
    private void ExitIdle() { }
    private void ExitApproach() { }

    private void ExitAttack()
    {
        isPerformingAttack = false;
        currentAttackType = AIAttackType.Normal;
        selectedCombo = null;
        isComboInProgress = false;

        // 重置所有输入状态
        aiInput.SetMovementInput(Vector2.zero);
        aiInput.SetCrouchInput(false);
    }

    private void ExitDefend()
    {
        aiInput.SetBlockInput(false);
        aiInput.SetCrouchInput(false);
    }

    private void ExitRetreat() { }
    private void ExitPursuit() { }

    // ========== 动作执行方法 ==========
    private void PerformJump()
    {
        if (playerController.CanJump())
        {
            aiInput.PerformJump();
        }
    }

    private void PerformDash()
    {
        if (playerController.CanDash())
        {
            aiInput.PerformDash();
        }
    }

    // ========== 辅助方法 ==========
    private float GetDistanceToTarget() => Vector2.Distance(transform.position, target.position);
    private Vector2 GetDirectionToTarget() => (target.position - transform.position).normalized;
    // 延迟释放按键
    private IEnumerator ReleaseKeyAfterDelay(System.Action releaseAction, float delay)
    {
        yield return new WaitForSeconds(delay);
        releaseAction?.Invoke();
    }


    // ========== 调试 ==========
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // 绘制攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aiConfig.attackRange);

        // 绘制追击范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aiConfig.pursuitRange);

        // 绘制冲刺范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, aiConfig.dashRange);
    }
}