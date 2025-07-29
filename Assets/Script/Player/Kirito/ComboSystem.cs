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
        public UnityEngine.Events.UnityEvent onSkillTriggered;
    }
    public KiritoController kiritoController;


    [Header("连招配置")]
    public List<ComboData> combos;

    [Header("输入按键记录")]
    public List<GameInputKey> inputHistory = new List<GameInputKey>();
    private List<GameInputKey> tempSequence;

    [Header("清除按键记录设置")]
    public float inputClearDelay = 2f;
    private float inputClearTimer;

    [Header("移动检测")]
    [Tooltip("手柄读取输入强度")]
    public float movementThreshold = 0.5f;
    [Tooltip("控制方向键是否计入连招，true为不记录")]
    public bool excludeMovementFromCombos = false;

    private PlayerInputControl inputControl;

    // 输入状态追踪
    private Vector2 lastMovementInput = Vector2.zero;
    private bool wasMovingUp = false;
    private bool wasMovingDown = false;
    private bool wasMovingLeft = false;
    private bool wasMovingRight = false;

    private bool comboTriggered = false;

    private void Awake()
    {
        inputControl = new PlayerInputControl();
        kiritoController = GetComponent<KiritoController>();
        BindInputEvents();
    }

    private void BindInputEvents()
    {
        // 绑定移动
        inputControl.GamePlay.Move.performed += OnMovePerformed;
        inputControl.GamePlay.Move.canceled += OnMoveCanceled;

        // 绑定动作
        //inputControl.GamePlay.Attack.started += ctx => HandleKeyInput(GameInputKey.Attack);
        //inputControl.GamePlay.Block.started += ctx => HandleKeyInput(GameInputKey.Block);
        //inputControl.GamePlay.Dash.started += ctx => HandleKeyInput(GameInputKey.Dash);
        //inputControl.GamePlay.Jump.started += ctx => HandleKeyInput(GameInputKey.Jump);
        // 修改攻击绑定
        inputControl.GamePlay.Attack.started += ctx => HandleAttackInput();

        // 其他动作
        inputControl.GamePlay.Block.started += ctx => HandleBlockInput(true);
        inputControl.GamePlay.Block.canceled += ctx => HandleBlockInput(false);
        inputControl.GamePlay.Dash.started += ctx => HandleDashInput();
        inputControl.GamePlay.Jump.started += ctx => HandleJumpInput();
        inputControl.GamePlay.Crouch.started += ctx => HandleCrouchInput(true);
        inputControl.GamePlay.Crouch.canceled += ctx => HandleCrouchInput(false);

    }

    private void HandleAttackInput()
    {
        // 记录输入
        HandleKeyInput(GameInputKey.Attack);

        // 如果没有触发连招，执行普通攻击
        if (!comboTriggered && kiritoController != null)
        {
            kiritoController.PerformAttack();
        }
        comboTriggered = false;
    }

    private void HandleBlockInput(bool isPressed)
    {
        if (isPressed)
            HandleKeyInput(GameInputKey.Block);

        if (kiritoController != null)
            kiritoController.PerformBlock(isPressed);
        comboTriggered = false;
    }

    private void HandleCrouchInput(bool isPressed)
    {
        if (isPressed)
            HandleKeyInput(GameInputKey.MoveDown);

        if (kiritoController != null)
            kiritoController.PerformCrouch(isPressed);
        comboTriggered = false;
    }

    private void HandleDashInput()
    {
        HandleKeyInput(GameInputKey.Dash);
        if (kiritoController != null)
            kiritoController.PerformDash();
        comboTriggered = false;
    }

    private void HandleJumpInput()
    {
        HandleKeyInput(GameInputKey.Jump);
        if (kiritoController != null)
            kiritoController.PerformJump();
        comboTriggered = false;
    }


    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();

        // 检查方向变化转化为离散输入
        CheckMovementDirection(moveInput);

        lastMovementInput = moveInput;
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        lastMovementInput = Vector2.zero;
        wasMovingUp = false;
        wasMovingDown = false;
        wasMovingLeft = false;
        wasMovingRight = false;
    }

    private void CheckMovementDirection(Vector2 moveInput)
    {
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

        //Debug.Log($"Key pressed: {inputKey}, 当前队列: {string.Join(",", inputHistory)}");
    }

    public void CheckForCombo()
    {
        for (int i = 0; i < combos.Count; i++)
        {
            // 当输入队列的按键数量大于搓招的按键数
            if (combos[i].keySequence.Count <= inputHistory.Count)
            {
                // 获取最后对应位数的输入按键
                tempSequence = inputHistory.GetRange(
                    inputHistory.Count - combos[i].keySequence.Count,
                    combos[i].keySequence.Count
                );

                bool isValidSequence = true;

                // 比较按键是否满足技能释放
                for (int j = 0; j < combos[i].keySequence.Count; j++)
                {
                    isValidSequence = isValidSequence && tempSequence[j] == combos[i].keySequence[j];
                }

                if (isValidSequence)
                {
                    //Debug.Log($"Combo triggered: {combos[i].skillName}");
                    comboTriggered = true;
                    combos[i].onSkillTriggered.Invoke();
                    // 连招释放成功后清除列表
                    inputHistory.Clear();
                    return;
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
        // 处理连招定时器
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
        // 解除绑定
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

    #region 公开方法
    // 外部添加输入按键
    public void AddManualInput(GameInputKey inputKey)
    {
        HandleKeyInput(inputKey);
    }

    // 清除输入队列
    public void ClearInputHistory()
    {
        inputHistory.Clear();
        inputClearTimer = inputClearDelay;
    }

    // 获取当前输入队列
    public string GetCurrentInputSequence()
    {
        return string.Join(" -> ", inputHistory);
    }

    // 获取移动输入
    public Vector2 GetMovementInput()
    {
        return lastMovementInput;
    }

    // 检查动作按键是否按下
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

    // 检查是否移动，需要强度大于阈值
    public bool IsMoving()
    {
        return lastMovementInput.magnitude > movementThreshold;
    }

    // 获取移动方向
    public Vector2 GetMovementDirection()
    {
        return lastMovementInput.normalized;
    }
    #endregion
}