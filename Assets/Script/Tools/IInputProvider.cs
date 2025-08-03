using UnityEngine;

public interface IInputProvider
{
    Vector2 GetMovementInput();
    bool IsAttackPressed();
    bool IsBlockPressed();
    bool IsDashPressed();
    bool IsJumpPressed();
    bool IsCrouchPressed();
    void OnAttackPerformed();
    void OnBlockPerformed(bool isPressed);
    void OnDashPerformed();
    void OnJumpPerformed();
    void OnCrouchPerformed(bool isPressed);
}