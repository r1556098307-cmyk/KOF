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
    public bool IsAttackPressed() => false; // AI����Ҫ�����������
    public bool IsBlockPressed() => isBlockPressed;
    public bool IsDashPressed() => false;
    public bool IsJumpPressed() => false;
    public bool IsCrouchPressed() => isCrouchPressed;

    // AI���Ʒ���
    public void SetMovementInput(Vector2 input)
    {
        currentMovementInput = input;
        // ֪ͨComboSystem�����ƶ����루�������м�⣩
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

    // IInputProvider�ӿ�ʵ��
    public void OnAttackPerformed() => comboSystem?.HandleAttackInput();
    public void OnBlockPerformed(bool isPressed) => comboSystem?.HandleBlockInput(isPressed);
    public void OnDashPerformed() => comboSystem?.HandleDashInput();
    public void OnJumpPerformed() => comboSystem?.HandleJumpInput();
    public void OnCrouchPerformed(bool isPressed) => comboSystem?.HandleCrouchInput(isPressed);

    // AIר�÷���
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
        // �ض����е�ִ���߼�
        //Debug.Log($"AI Input: Performing Combo - {comboName}");

        // ʾ�����¶׹�������
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

    // ������������״̬
    public void ResetInputs()
    {
        currentMovementInput = Vector2.zero;
        isBlockPressed = false;
        isCrouchPressed = false;

        // ȷ��ֹͣ���ж���
        SetBlockInput(false);
        SetCrouchInput(false);
    }
}