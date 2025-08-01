using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ComboSystem : MonoBehaviour
{
    public enum GameInputKey
    {
        MoveUp, MoveDown, MoveLeft, MoveRight,
        Attack,    // J
        Block,     // K
        Dash,      // L
        Jump       // Space
    }

    [System.Serializable]
    public class ComboData
    {
        public string skillName;
        public List<GameInputKey> keySequence = new List<GameInputKey>();

        public int energyCost = 0;

        public UnityEngine.Events.UnityEvent onSkillTriggered;
    }
    private PlayerController playerController;
    private HitstunSystem hitstunSystem;
    private PlayerStats playerStats;

    [Header("��������")]
    public List<ComboData> combos;

    [Header("���밴����¼")]
    public List<GameInputKey> inputHistory = new List<GameInputKey>();
    private List<GameInputKey> tempSequence;

    [Header("���������¼����")]
    public float inputClearDelay = 2f;
    private float inputClearTimer;

    [Header("�ƶ����")]
    [Tooltip("�ֱ���ȡ����ǿ��")]
    public float movementThreshold = 0.5f;
    [Tooltip("���Ʒ�����Ƿ�������У�trueΪ����¼")]
    public bool excludeMovementFromCombos = false;

    private PlayerInputControl inputControl;

    // ����״̬׷��
    private Vector2 lastMovementInput = Vector2.zero;
    private bool wasMovingUp = false;
    private bool wasMovingLeft = false;
    private bool wasMovingRight = false;

    private bool comboTriggered = false;

    private void Awake()
    {
        inputControl = new PlayerInputControl();
        playerController = GetComponent<PlayerController>();
        hitstunSystem = GetComponent<HitstunSystem>();
        playerStats = GetComponent<PlayerStats>();
        BindInputEvents();
    }

    private void BindInputEvents()
    {
        // ���ƶ�
        inputControl.GamePlay.Move.performed += OnMovePerformed;
        inputControl.GamePlay.Move.canceled += OnMoveCanceled;

        // �󶨶���
        //inputControl.GamePlay.Attack.started += ctx => HandleKeyInput(GameInputKey.Attack);
        //inputControl.GamePlay.Block.started += ctx => HandleKeyInput(GameInputKey.Block);
        //inputControl.GamePlay.Dash.started += ctx => HandleKeyInput(GameInputKey.Dash);
        //inputControl.GamePlay.Jump.started += ctx => HandleKeyInput(GameInputKey.Jump);
        // �޸Ĺ�����
        inputControl.GamePlay.Attack.started += ctx => HandleAttackInput();

        // ��������
        inputControl.GamePlay.Block.started += ctx => HandleBlockInput(true);
        inputControl.GamePlay.Block.canceled += ctx => HandleBlockInput(false);
        inputControl.GamePlay.Dash.started += ctx => HandleDashInput();
        inputControl.GamePlay.Jump.started += ctx => HandleJumpInput();
        inputControl.GamePlay.Crouch.started += ctx => HandleCrouchInput(true);
        inputControl.GamePlay.Crouch.canceled += ctx => HandleCrouchInput(false);

    }

    private void HandleAttackInput()
    {
        if (hitstunSystem != null && !hitstunSystem.IsAttackAllowed())
            return;


        // ��¼����
        HandleKeyInput(GameInputKey.Attack);

        // ���û�д������У�ִ����ͨ����
        if (!comboTriggered && playerController != null)
        {
            playerController.PerformAttack();
        }
        comboTriggered = false;
    }

    private void HandleBlockInput(bool isPressed)
    {
        if (isPressed && hitstunSystem != null && !hitstunSystem.IsBlockAllowed())
            return;


        if (isPressed)
            HandleKeyInput(GameInputKey.Block);

        if (playerController != null)
            playerController.PerformBlock(isPressed);
        comboTriggered = false;
    }

    private void HandleCrouchInput(bool isPressed)
    {
        if (isPressed && hitstunSystem != null && !hitstunSystem.IsMoveAllowed())
            return;


        if (isPressed)
            HandleKeyInput(GameInputKey.MoveDown);

        if (playerController != null)
            playerController.PerformCrouch(isPressed);
        comboTriggered = false;
    }

    private void HandleDashInput()
    {
        // ��齩ֱ״̬
        if (hitstunSystem != null && !hitstunSystem.IsDashAllowed())
            return;


        HandleKeyInput(GameInputKey.Dash);
        if (playerController != null)
            playerController.PerformDash();
        comboTriggered = false;
    }

    private void HandleJumpInput()
    {
        // ��齩ֱ״̬
        if (hitstunSystem != null && !hitstunSystem.IsJumpAllowed())
            return;


        HandleKeyInput(GameInputKey.Jump);
        if (playerController != null)
            playerController.PerformJump();
        comboTriggered = false;
    }


    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();

        // ��鷽��仯ת��Ϊ��ɢ����
        CheckMovementDirection(moveInput);

        lastMovementInput = moveInput;
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        lastMovementInput = Vector2.zero;
        wasMovingUp = false;
        wasMovingLeft = false;
        wasMovingRight = false;
    }

    private void CheckMovementDirection(Vector2 moveInput)
    {
        // ��齩ֱ״̬���������ֱ�򲻴����ƶ�����
        if (hitstunSystem != null && !hitstunSystem.IsMoveAllowed())
        {
            return;
        }

        // Check Up
        if (moveInput.y > movementThreshold && !wasMovingUp)
        {
            wasMovingUp = true;
            if (!excludeMovementFromCombos)
                HandleKeyInput(GameInputKey.MoveUp);
        }
        else if (moveInput.y <= movementThreshold)
        {
            wasMovingUp = false;
        }

        // Check Down
        //if (moveInput.y < -movementThreshold && !wasMovingDown)
        //{
        //    wasMovingDown = true;
        //    if (!excludeMovementFromCombos)
        //        HandleKeyInput(GameInputKey.MoveDown);
        //}
        //else if (moveInput.y >= -movementThreshold)
        //{
        //    wasMovingDown = false;
        //}


        // Check Left
        if (moveInput.x < -movementThreshold && !wasMovingLeft)
        {
            wasMovingLeft = true;
            if (!excludeMovementFromCombos)
                HandleKeyInput(GameInputKey.MoveLeft);
        }
        else if (moveInput.x >= -movementThreshold)
        {
            wasMovingLeft = false;
        }

        // Check Right
        if (moveInput.x > movementThreshold && !wasMovingRight)
        {
            wasMovingRight = true;
            if (!excludeMovementFromCombos)
                HandleKeyInput(GameInputKey.MoveRight);
        }
        else if (moveInput.x <= movementThreshold)
        {
            wasMovingRight = false;
        }
    }

    private void HandleKeyInput(GameInputKey inputKey)
    {
        inputHistory.Add(inputKey);
        CheckForCombo();
        inputClearTimer = inputClearDelay;

        //Debug.Log($"Key pressed: {inputKey}, ��ǰ����: {string.Join(",", inputHistory)}");
    }

    public void CheckForCombo()
    {
        for (int i = 0; i < combos.Count; i++)
        {
            // ��������еİ����������ڴ��еİ�����
            if (combos[i].keySequence.Count <= inputHistory.Count)
            {
                // ��ȡ����Ӧλ�������밴��
                tempSequence = inputHistory.GetRange(
                    inputHistory.Count - combos[i].keySequence.Count,
                    combos[i].keySequence.Count
                );

                bool isValidSequence = true;

                // �Ƚϰ����Ƿ����㼼���ͷ�
                for (int j = 0; j < combos[i].keySequence.Count; j++)
                {
                    isValidSequence = isValidSequence && tempSequence[j] == combos[i].keySequence[j];
                }

                if (isValidSequence)
                {
                    ////Debug.Log($"Combo triggered: {combos[i].skillName}");
                    //comboTriggered = true;
                    //combos[i].onSkillTriggered.Invoke();
                    //// �����ͷųɹ�������б�
                    //inputHistory.Clear();
                    //return;

                    // ��������Ƿ��㹻
                if (playerStats.HasSufficientEnergy(combos[i].energyCost))
                    {
                        //Debug.Log($"���д���: {combos[i].skillName}");
                        comboTriggered = true;

                        // ��������
                        playerStats.ConsumeEnergy(combos[i].energyCost);

                        // ��������
                        combos[i].onSkillTriggered.Invoke();

                        // �����ͷųɹ�������б�
                        inputHistory.Clear();
                        return;
                    }
                    else
                    {
                        Debug.Log($"�������㣬�޷��ͷ� {combos[i].skillName}");
                        return;
                    }
                }
            }
        }
    }

    private void Start()
    {
        inputClearTimer = inputClearDelay;
    }

    private void Update()
    {
        // �������ж�ʱ��
        inputClearTimer -= Time.deltaTime;
        if (inputClearTimer <= 0)
        {
            inputClearTimer = inputClearDelay;
            inputHistory.Clear();
        }
    }

    private void OnEnable()
    {
        inputControl?.Enable();
    }

    private void OnDisable()
    {
        inputControl?.Disable();
    }

    private void OnDestroy()
    {
        // �����
        if (inputControl != null)
        {
            inputControl.GamePlay.Move.performed -= OnMovePerformed;
            inputControl.GamePlay.Move.canceled -= OnMoveCanceled;

            inputControl.GamePlay.Attack.started -= ctx => HandleKeyInput(GameInputKey.Attack);
            inputControl.GamePlay.Block.started -= ctx => HandleKeyInput(GameInputKey.Block);
            inputControl.GamePlay.Dash.started -= ctx => HandleKeyInput(GameInputKey.Dash);
            inputControl.GamePlay.Jump.started -= ctx => HandleKeyInput(GameInputKey.Jump);
        }
    }

    #region ��������
    // �ⲿ������밴��
    public void AddManualInput(GameInputKey inputKey)
    {
        HandleKeyInput(inputKey);
    }

    // ����������
    public void ClearInputHistory()
    {
        inputHistory.Clear();
        inputClearTimer = inputClearDelay;
    }

    // ��ȡ��ǰ�������
    public string GetCurrentInputSequence()
    {
        return string.Join(" -> ", inputHistory);
    }

    // ��ȡ�ƶ�����
    public Vector2 GetMovementInput()
    {
        // �������ֱ������������
        if (hitstunSystem != null && !hitstunSystem.IsMoveAllowed())
        {
            return Vector2.zero;
        }

        return lastMovementInput;
    }

    // ��鶯�������Ƿ���
    public bool IsActionKeyPressed(GameInputKey actionKey)
    {
        switch (actionKey)
        {
            case GameInputKey.Attack:
                return inputControl.GamePlay.Attack.IsPressed();
            case GameInputKey.Block:
                return inputControl.GamePlay.Block.IsPressed();
            case GameInputKey.Dash:
                return inputControl.GamePlay.Dash.IsPressed();
            case GameInputKey.Jump:
                return inputControl.GamePlay.Jump.IsPressed();
            default:
                return false;
        }
    }

    // ����Ƿ��ƶ�����Ҫǿ�ȴ�����ֵ
    public bool IsMoving()
    {
        if (hitstunSystem != null && !hitstunSystem.IsMoveAllowed())
        {
            return false;
        }

        return lastMovementInput.magnitude > movementThreshold;
    }

    // ��ȡ�ƶ�����
    public Vector2 GetMovementDirection()
    {
        return lastMovementInput.normalized;
    }
    #endregion
}