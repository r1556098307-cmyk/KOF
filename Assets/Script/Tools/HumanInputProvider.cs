using UnityEngine;
using UnityEngine.InputSystem;

public class HumanInputProvider : MonoBehaviour, IInputProvider
{
    private PlayerInputControl inputControl;
    private Vector2 lastMovementInput;
    private ComboSystem comboSystem;

    private void Awake()
    {
        inputControl = new PlayerInputControl();
        comboSystem = GetComponent<ComboSystem>();
        BindInputEvents();
    }

    private void BindInputEvents()
    {
        // 绑定移动输入
        inputControl.GamePlay.Move.performed += OnMovePerformed;
        inputControl.GamePlay.Move.canceled += OnMoveCanceled;

        // 绑定动作输入
        inputControl.GamePlay.Attack.started += ctx => OnAttackPerformed();
        inputControl.GamePlay.Block.started += ctx => OnBlockPerformed(true);
        inputControl.GamePlay.Block.canceled += ctx => OnBlockPerformed(false);
        inputControl.GamePlay.Dash.started += ctx => OnDashPerformed();
        inputControl.GamePlay.Jump.started += ctx => OnJumpPerformed();
        inputControl.GamePlay.Crouch.started += ctx => OnCrouchPerformed(true);
        inputControl.GamePlay.Crouch.canceled += ctx => OnCrouchPerformed(false);
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        lastMovementInput = moveInput;

        // 通知ComboSystem处理移动输入（用于连招检测）
        comboSystem?.HandleMovementInput(moveInput);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        lastMovementInput = Vector2.zero;
        comboSystem?.HandleMovementInput(Vector2.zero);
    }

    // IInputProvider接口实现
    public Vector2 GetMovementInput() => lastMovementInput;
    public bool IsAttackPressed() => inputControl.GamePlay.Attack.IsPressed();
    public bool IsBlockPressed() => inputControl.GamePlay.Block.IsPressed();
    public bool IsDashPressed() => inputControl.GamePlay.Dash.IsPressed();
    public bool IsJumpPressed() => inputControl.GamePlay.Jump.IsPressed();
    public bool IsCrouchPressed() => inputControl.GamePlay.Crouch.IsPressed();

    // 输入事件处理方法
    public void OnAttackPerformed() => comboSystem?.HandleAttackInput();
    public void OnBlockPerformed(bool isPressed) => comboSystem?.HandleBlockInput(isPressed);
    public void OnDashPerformed() => comboSystem?.HandleDashInput();
    public void OnJumpPerformed() => comboSystem?.HandleJumpInput();
    public void OnCrouchPerformed(bool isPressed) => comboSystem?.HandleCrouchInput(isPressed);

    private void OnEnable() => inputControl?.Enable();
    private void OnDisable() => inputControl?.Disable();
}