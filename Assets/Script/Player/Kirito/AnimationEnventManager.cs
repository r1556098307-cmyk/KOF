using UnityEngine;

public class AnimationEventManager : MonoBehaviour
{

    private void Awake()
    {


    }


    // ͨ�÷��� - ͨ�����Ƽ����
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

    // ͨ�÷��� - ͨ�����ƹرչ���
    public void DeactivateAttackByName(string attackRangeName)
    {
        Transform attackRange = transform.Find($"{attackRangeName}/Range");
        if (attackRange != null)
        {
            UnifiedAttackTrigger trigger = attackRange.GetComponent<UnifiedAttackTrigger>();
            trigger?.DeactivateAttack();
        }
    }

    // �ر����й�������������򶯻��ж�ʱʹ�ã�
    public void DeactivateAllAttacks()
    {
        UnifiedAttackTrigger[] allTriggers = GetComponentsInChildren<UnifiedAttackTrigger>();
        foreach (var trigger in allTriggers)
        {
            trigger.DeactivateAttack();
        }
    }
}