using UnityEngine;

public class AnimationEventManager : MonoBehaviour
{

    private void Awake()
    {


    }


    // 通用方法 - 通过名称激活攻击
    public void ActivateAttackByName(string attackRangeName)
    {
        Transform attackRange = transform.Find($"{attackRangeName}/Range");
        if (attackRange != null)
        {
            UnifiedAttackTrigger trigger = attackRange.GetComponent<UnifiedAttackTrigger>();
            trigger?.ActivateAttack();
        }
        else
        {
            Debug.LogWarning($"Attack range '{attackRangeName}' not found!");
        }
    }

    // 通用方法 - 通过名称关闭攻击
    public void DeactivateAttackByName(string attackRangeName)
    {
        Transform attackRange = transform.Find($"{attackRangeName}/Range");
        if (attackRange != null)
        {
            UnifiedAttackTrigger trigger = attackRange.GetComponent<UnifiedAttackTrigger>();
            trigger?.DeactivateAttack();
        }
    }

    // 关闭所有攻击（紧急情况或动画中断时使用）
    public void DeactivateAllAttacks()
    {
        UnifiedAttackTrigger[] allTriggers = GetComponentsInChildren<UnifiedAttackTrigger>();
        foreach (var trigger in allTriggers)
        {
            trigger.DeactivateAttack();
        }
    }
}