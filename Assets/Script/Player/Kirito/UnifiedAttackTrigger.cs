using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnifiedAttackTrigger : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private AttackConfig attackConfig;

    [Header("�������")]
    [SerializeField] private LayerMask targetLayers; // �ɹ����Ĳ㼶


    private Collider2D attackCollider;
    private HashSet<GameObject> hitTargets; // ��ֹ�ظ�����
    private bool isActive = false;
    [SerializeField]
    private Transform attackerTransform; // �����ߵ�Transform

    private PlayerController playerController;

    private void Awake()
    {
        attackCollider = GetComponent<Collider2D>();
        hitTargets = new HashSet<GameObject>();
        playerController = GetComponentInParent<PlayerController>();

        // ��ȡ������Transform
        attackerTransform = transform.parent.parent != null ? transform.parent.parent : transform;

        // ��ʼ״̬������ײ��
        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    // ������� - �ɶ����¼�����
    public void ActivateAttack()
    {
        isActive = true;
       hitTargets.Clear(); // ����������б������µĹ���

        if (attackCollider != null)
            attackCollider.enabled = true;

        if (attackConfig.attackName == "SpecialMove1")
            playerController.StartSpecialMove1Dash();

        //Debug.Log($"�����򼤻�: {attackConfig.attackName}");
    }

    // �رչ����� - �ɶ����¼�����
    public void DeactivateAttack()
    {
        isActive = false;

        if (attackCollider != null)
            attackCollider.enabled = false;

        //Debug.Log($"������ر�: {attackConfig.attackName}");
    }

    // ���ù������ã�����ʱ�޸ģ�
    public void SetAttackConfig(AttackConfig config)
    {
        attackConfig = config;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActive) return;

        // ����Ƿ��������Ŀ��
        if (CanHitTarget(collision.gameObject))
        {
            //Debug.Log("������");
            ProcessHit(collision.gameObject);
        }
    }

    private bool CanHitTarget(GameObject target)
    {
        // �����ײ�����Ƿ���Ŀ��㼶
        bool isInTargetLayer = ((1 << target.layer) & targetLayers) != 0;
        if (!isInTargetLayer) return false;

        // ���ܴ��Լ�
        if (target.transform.parent == attackerTransform || target.transform == attackerTransform)
            return false;

        // ��ֹ�ظ�����
        if (hitTargets.Contains(target)) return false;

        // ����Ƿ�����Ч��Ŀ�����
        HitstunSystem targetHitstun = target.GetComponent<HitstunSystem>();
        PlayerController targetPlayer = target.GetComponent<PlayerController>();

        return targetHitstun != null || targetPlayer != null;
    }

    private void ProcessHit(GameObject target)
    {
        // ��ӵ��������б�
        hitTargets.Add(target);

        // ��ȡĿ�����
        HitstunSystem targetHitstun = target.GetComponent<HitstunSystem>();
        PlayerController targetPlayer = target.GetComponent<PlayerController>();
        PlayerStats targetStats = target.GetComponent<PlayerStats>();

        // ���㹥������
        Vector2 attackDirection = CalculateAttackDirection(target);

        // ����
        if (IsBlocking(targetPlayer, attackDirection))
        {
            ProcessBlockedHit(target, attackDirection);
            return;
        }

        // �����˺��������ָ�
        if (targetStats != null)
        {
            PlayerStats attackerStats = playerController.GetComponent<PlayerStats>();
            if (attackerStats != null)
            {
                // �����˺�����
                bool targetDied = attackerStats.TakeDamage(targetStats, attackConfig.damage, attackConfig.energyRecovery);

                if (targetDied)
                {
                    //Debug.Log($"{target.name} �������ˣ�");
                    // ���������߼�
                    HandleTargetDeath(target);
                }
            }
        }

        // ʹ���µ�Ӳֱϵͳ
        if (targetHitstun != null)
        {
            // ȷ�����չ�������
            AttackType finalAttackType =attackConfig.attackType;


            // �޸�Ӳֱʱ��
            if (attackConfig.stunDurationMultiplier != 1f)
            {
                targetHitstun.ModifyHitstunDuration(attackConfig.stunDurationMultiplier);
            }

            bool isAddUpForce = attackConfig.isAddUpForce;
            targetHitstun.TakeHit(attackConfig, attackDirection);

        }

        // ��������Ч��
        PlayHitEffects(target.transform.position);

        //Debug.Log($"����Ŀ��: {target.name}, ����: {attackConfig.attackName}");
    }

    // �������������
    private void HandleTargetDeath(GameObject target)
    {
        //TODO:���ʤ���������˵������ʧ��ѡ�����¿�ʼ���߷������˵�


    }

    private Vector2 CalculateAttackDirection(GameObject target)
    {
        Vector2 direction;

        // ������Զ�����˷���ʹ����
        if (attackConfig.customKnockbackDirection != Vector2.zero)
        {
            direction = attackConfig.customKnockbackDirection.normalized;
        }
        else
        {
            // ����ӹ����ߵ�Ŀ��ķ���
            direction = (target.transform.position - attackerTransform.position).normalized;

            // �������Ϊ�㣨�ص�����ʹ�ù����߳���
            if (direction.magnitude < 0.1f)
            {
                PlayerController attackerPlayer = attackerTransform.GetComponent<PlayerController>();
                if (attackerPlayer != null)
                {
                    direction = attackerPlayer.isFacingRight ? Vector2.right : Vector2.left;
                }
                else
                {
                    direction = Vector2.right;
                }
            }
        }


        return new Vector2(direction.x, 0f); ;
    }

    private bool IsBlocking(PlayerController targetPlayer, Vector2 attackDirection)
    {
        if (targetPlayer == null || !targetPlayer.isBlock) return false;

        // ���񵲷����Ƿ���ȷ
        bool attackFromRight = attackDirection.x > 0;
        bool targetFacingRight = targetPlayer.isFacingRight;

        // ��ȷ�ĸ񵲷������򹥻���Դ
        return (attackFromRight && targetFacingRight) || (!attackFromRight && !targetFacingRight);
    }

    private void ProcessBlockedHit(GameObject target, Vector2 attackDirection)
    {
        Debug.Log($"��������: {target.name}");

        // ������΢����
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            Vector2 blockKnockback = attackDirection * attackConfig.blockKnockbackForce;
            targetRb.AddForce(blockKnockback, ForceMode2D.Impulse);
        }

        // ���Ÿ�Ч��
        //PlayBlockEffects(target.transform.position);
    }


    private void PlayHitEffects(Vector3 position)
    {
        // TODO: ����������Ч
        // AudioManager.Instance.PlaySFX(attackConfig.hitSound);

        // TODO: ����������Ч
        // EffectManager.Instance.PlayEffect(attackConfig.hitEffect, position);

    }

    private void PlayBlockEffects(Vector3 position)
    {
        // TODO: ���Ÿ���Ч����Ч
        // AudioManager.Instance.PlaySFX("BlockSound");
        // EffectManager.Instance.PlayEffect("BlockEffect", position);
    }



 
}
