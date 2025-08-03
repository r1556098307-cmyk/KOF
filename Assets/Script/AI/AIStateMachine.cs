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
    [Header("����")]
    public AIConfig aiConfig;

    [Header("״̬��ʾ")]
    [SerializeField] private AIState currentState = AIState.Idle;
    [SerializeField] private float stateTimer = 0f;

    [Header("����")]
    [SerializeField] private bool showDebugInfo = true;

    // �����������
    private PlayerController playerController;
    private PlayerStats playerStats;
    private HitstunSystem hitstunSystem;
    private AIInputProvider aiInput;
    private ComboSystem comboSystem;

    // Ŀ������
    private Transform target;
    private PlayerController targetController;

    // ״̬�ֵ�
    private Dictionary<AIState, System.Action> stateActions;
    private Dictionary<AIState, System.Action> stateEnterActions;
    private Dictionary<AIState, System.Action> stateExitActions;

    // ��ǰ��Ϊ��¼
    [SerializeField] private AIAttackType currentAttackType = AIAttackType.Normal;

    // �������
    private List<ComboSystem.ComboData> availableCombos = new List<ComboSystem.ComboData>();
    private ComboSystem.ComboData selectedCombo = null;
    private bool isComboInProgress = false;

    // ����״̬
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

    // ========== ��ʼ������ ==========
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
            Debug.LogError("AIȱ����������!");
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

    // ========== ��ѭ�� ==========
    private void Update()
    {
        if (!target || !enabled) return;

        // ʼ���泯��ҷ���
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

        // ִ���˳�����
        if (stateExitActions.ContainsKey(currentState))
        {
            stateExitActions[currentState]?.Invoke();
        }

        AIState previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        // ִ�н��붯��
        if (stateEnterActions.ContainsKey(newState))
        {
            stateEnterActions[newState]?.Invoke();
        }

        Debug.Log($"AI ״̬�л�: {previousState} -> {newState}");
    }

    // ========== ״̬���·��� ==========
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
                // ����ڹ�����Χ��׷����Χ֮�䣬�����ӽ�
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

        // ���û����ִ�й���
        if (!isPerformingAttack)
        {
            // ��������ϴι�����ʱ����
            float timeSinceLastAttack = stateTimer - lastAttackTime;

            // ������״ι������߼��������Ӧʱ����һ���ʱ��
            if ((lastAttackTime == 0f || timeSinceLastAttack > aiConfig.attackReactionTime) &&
                stateTimer < aiConfig.attackDuration)
            {
                currentAttackType = GetWeightedRandomAttackType();
                StartCoroutine(PerformAttackSequence());
                isPerformingAttack = true;
                lastAttackTime = stateTimer;
            }
        }

        // ʱ�䵽�˾��л�״̬
        if (stateTimer > aiConfig.attackDuration)
        {
            ChangeState(AIState.Idle);
        }
    }

    private AIAttackType GetWeightedRandomAttackType()
    {
        // ���û������Ȩ�أ�ʹ��Ĭ���߼�
        if (aiConfig.attackTypeWeights == null || aiConfig.attackTypeWeights.Length == 0)
        {
            float random = Random.value;
            if (random < 0.5f) return AIAttackType.Normal;
            else if (random < 0.7f) return AIAttackType.Crouch;
            else if (availableCombos.Count > 0) return AIAttackType.Combo;
            else return AIAttackType.Normal;
        }

        // ������Ȩ��
        float totalWeight = 0f;
        foreach (var weight in aiConfig.attackTypeWeights)
        {
            // ��������е�û�п������У�����
            if (weight.attackType == AIAttackType.Combo && availableCombos.Count == 0)
                continue;

            totalWeight += weight.weight;
        }

        // ���ѡ��
        float randomPoint = Random.value * totalWeight;
        float currentWeight = 0f;

        foreach (var weight in aiConfig.attackTypeWeights)
        {
            // ��������е�û�п������У�����
            if (weight.attackType == AIAttackType.Combo && availableCombos.Count == 0)
                continue;

            currentWeight += weight.weight;
            if (randomPoint <= currentWeight)
            {
                return weight.attackType;
            }
        }

        // Ĭ�Ϸ�����ͨ����
        return AIAttackType.Normal;
    }

    private void UpdateDefend()
    {
        // ʹ�������еĸ���
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

    // ========== �������� ==========
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
                    Debug.LogWarning("AI: û�п������У��˻���ͨ����");
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
            Debug.LogWarning("AI: û�п�������");
            return null;
        }

        float currentEnergy = playerStats.CurrentEnergyNum;
        float maxEnergy = playerStats.MaxEnergyNum;
        float healthPercentage = playerStats.CurrentHealth / playerStats.MaxHealth;

        // ʹ�������е�Ѫ����ֵ
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

        // Ѫ��������ֵ����ʲô��ʲô�����ȸ�����
        var affordableCombos = availableCombos
            .Where(combo => combo.energyCost <= currentEnergy)
            .OrderByDescending(combo => combo.energyCost)
            .ToList();

        if (affordableCombos.Count > 0)
        {
            return affordableCombos[0];
        }

        // ���û�п��ü��ܣ�����nullʹ����ͨ����
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
        // ʹ�������еĶ����ӳ�
        aiInput.SetCrouchInput(true);
        yield return new WaitForSeconds(aiConfig.crouchDelay);

        // ��������
        for (int i = 0; i < 3; i++)
        {
            aiInput.PerformAttack();
            yield return new WaitForSeconds(aiConfig.attackButtonInterval);
        }

        // �ɿ�����
        aiInput.SetCrouchInput(false);
    }

    private IEnumerator PerformCombo()
    {
        if (selectedCombo == null || selectedCombo.keySequence == null)
        {
            yield break;
        }

        isComboInProgress = true;

        // AIֻ���𰴼����У���������
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
        // ʹ�������еİ����ͷ��ӳ�
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


    // ========== ״̬���뷽�� ==========
    private void EnterIdle() { }
    private void EnterApproach() { }
    private void EnterAttack()
    {
        lastAttackTime = 0f;
    }
    private void EnterDefend() { }
    private void EnterRetreat() { }
    private void EnterPursuit() { }

    // ========== ״̬�˳����� ==========
    private void ExitIdle() { }
    private void ExitApproach() { }

    private void ExitAttack()
    {
        isPerformingAttack = false;
        currentAttackType = AIAttackType.Normal;
        selectedCombo = null;
        isComboInProgress = false;

        // ������������״̬
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

    // ========== ����ִ�з��� ==========
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

    // ========== �������� ==========
    private float GetDistanceToTarget() => Vector2.Distance(transform.position, target.position);
    private Vector2 GetDirectionToTarget() => (target.position - transform.position).normalized;
    // �ӳ��ͷŰ���
    private IEnumerator ReleaseKeyAfterDelay(System.Action releaseAction, float delay)
    {
        yield return new WaitForSeconds(delay);
        releaseAction?.Invoke();
    }


    // ========== ���� ==========
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // ���ƹ�����Χ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aiConfig.attackRange);

        // ����׷����Χ
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aiConfig.pursuitRange);

        // ���Ƴ�̷�Χ
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, aiConfig.dashRange);
    }
}