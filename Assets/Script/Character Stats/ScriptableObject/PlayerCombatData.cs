using UnityEngine;

[CreateAssetMenu(fileName = "New Combat Data", menuName = "Player Config/Combat Data")]
public class PlayerCombatData : ScriptableObject
{
    [Header("ÆÕÍ¨¹¥»÷")]
    public float attackForce = 15f;
    public float attackMaxSpeed = 8f;
    public float attackSpeedDecay = 0.95f;
    public float attackSpeedDecayDuration = 0.5f;

    [Header("³å´Ì¹¥»÷")]
    public float dashAttackForce = 20f;
    public float dashAttackMaxSpeed = 15f;
    public float dashAttackDuration = 0.3f;
    public float dashAttackDecay = 0.92f;
    public bool dashAttackIgnoreInput = true;

}
