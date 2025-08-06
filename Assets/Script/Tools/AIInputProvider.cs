using UnityEngine;

public class AIInputProvider : MonoBehaviour, IInputProvider
{
    private ComboSystem comboSystem;
    [SerializeField]
    private Vector2 currentMovementInput;
    private bool isBlockPressed;
    private bool isCrouchPressed;

    private void Awake()
    {
        comboSystem = GetComponent<ComboSystem>();
    }

    public Vector2 GetMovementInput() => currentMovementInput;
    public bool IsAttackPressed() => false; // AI不需要持续按键检测
    public bool IsBlockPressed() => isBlockPressed;
    public bool IsDashPressed() => false;
    public bool IsJumpPressed() => false;
    public bool IsCrouchPressed() => isCrouchPressed;

    // AI控制方法
    public void SetMovementInput(Vector2 input)
    {
        currentMovementInput = input;
        // 通知ComboSystem处理移动输入（用于连招检测）
        comboSystem?.HandleMovementInput(input);
    }

    public void SetBlockInput(bool pressed)
    {
        isBlockPressed = pressed;
        OnBlockPerformed(pressed);
    }

    public void SetCrouchInput(bool pressed)
    {
        isCrouchPressed = pressed;
        OnCrouchPerformed(pressed);
    }

    // IInputProvider接口实现
    public void OnAttackPerformed() => comboSystem?.HandleAttackInput();
    public void OnBlockPerformed(bool isPressed) => comboSystem?.HandleBlockInput(isPressed);
    public void OnDashPerformed() => comboSystem?.HandleDashInput();
    public void OnJumpPerformed() => comboSystem?.HandleJumpInput();
    public void OnCrouchPerformed(bool isPressed) => comboSystem?.HandleCrouchInput(isPressed);

    // AI专用方法
    public void PerformAttack()
    {
        //Debug.Log("AI Input: Performing Attack");
        OnAttackPerformed();
    }

    public void PerformDash()
    {
        //Debug.Log("AI Input: Performing Dash");
        OnDashPerformed();
    }

    public void PerformJump()
    {
        //Debug.Log("AI Input: Performing Jump");
        OnJumpPerformed();
    }

    public void PerformBlock(bool isPressed)
    {
        //Debug.Log($"AI Input: Block {(isPressed ? "Start" : "Stop")}");
        SetBlockInput(isPressed);
    }

    public void PerformCrouch(bool isPressed)
    {
        //Debug.Log($"AI Input: Crouch {(isPressed ? "Start" : "Stop")}");
        SetCrouchInput(isPressed);
    }

    public void PerformCombo(string comboName)
    {
        // 特定连招的执行逻辑
        //Debug.Log($"AI Input: Performing Combo - {comboName}");

        // 示例：下蹲攻击连招
        if (comboName.Contains("Crouch"))
        {
            SetCrouchInput(true);
            comboSystem?.AddManualInput(ComboSystem.GameInputKey.MoveDown);
            comboSystem?.AddManualInput(ComboSystem.GameInputKey.Attack);
        }
        else
        {
            comboSystem?.AddManualInput(ComboSystem.GameInputKey.MoveDown);
            comboSystem?.AddManualInput(ComboSystem.GameInputKey.Attack);
        }
    }

    // 重置所有输入状态
    public void ResetInputs()
    {
        currentMovementInput = Vector2.zero;
        isBlockPressed = false;
        isCrouchPressed = false;

        // 确保停止所有动作
        SetBlockInput(false);
        SetCrouchInput(false);
    }
}