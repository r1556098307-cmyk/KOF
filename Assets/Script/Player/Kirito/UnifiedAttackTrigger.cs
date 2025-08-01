using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UnifiedAttackTrigger : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private AttackConfig attackConfig;

    [Header("�������")]
    [SerializeField] private LayerMask targetLayers; // �ɹ����Ĳ㼶

    [Header("���߼������")]
    [SerializeField] private bool useRaycast = false; // �Ƿ�ʹ�����߼��
    [SerializeField] private float rayDistance = 10f; // ���߾���
    [SerializeField] private float rayWidth = 1f; // ���߿�ȣ�����BoxCast��
    [SerializeField] private Vector2 rayOffset = Vector2.zero; // ������ʼƫ��


    private Collider2D attackCollider;
    private bool isActive = false;
    [SerializeField]
    private Transform attackerTransform; // �����ߵ�Transform
    private PlayerController playerController;

    private void Awake()
    {
        attackCollider = GetComponent<Collider2D>();
        //hitTargets = new HashSet<GameObject>();
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

        if (useRaycast && attackConfig.attackName == "SuperMove1")
        {
            PerformRaycastAttack();
        }
        else
        {
            if (attackCollider != null)
                attackCollider.enabled = true;
        }

        if (attackConfig.attackName == "SpecialMove1"|| attackConfig.attackName == "SuperMove2")
            playerController.StartSpecialDash(attackConfig.attackName);

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


   


    private void PerformRaycastAttack()
    {
        // ��ȡ�����߳���
        bool facingRight = playerController.isFacingRight;
        Vector2 rayDirection = facingRight ? Vector2.right : Vector2.left;

        // ����������ʼλ��
        Vector2 rayStart = (Vector2)attackerTransform.position + rayOffset;

        // ʹ��BoxCast���
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            rayStart,                    // ��ʼλ��
            new Vector2(rayWidth, 0.5f), // �����С
            0f,                          // ��ת�Ƕ�
            rayDirection,                // ����
            rayDistance,                 // ����
            targetLayers                 // Ŀ��㼶
        );

        // �����������е�Ŀ��
        foreach (RaycastHit2D hit in hits)
        {
            if (CanHitTarget(hit.collider.gameObject))
            {
                ProcessHit(hit.collider.gameObject);
            }
        }

        // ���Ƶ�������
        DrawDebugRay(rayStart, rayDirection);
    }

    private void DrawDebugRay(Vector2 start, Vector2 direction)
    {
        // ��Scene��ͼ�л��Ƶ�������
        Debug.DrawRay(start, direction * rayDistance, Color.red, 0.5f);

        // ���Ƽ���ı߽�
        Vector2 end = start + direction * rayDistance;
        Vector2 topLeft = start + Vector2.up * (rayWidth / 2);
        Vector2 bottomLeft = start + Vector2.down * (rayWidth / 2);
        Vector2 topRight = end + Vector2.up * (rayWidth / 2);
        Vector2 bottomRight = end + Vector2.down * (rayWidth / 2);

        Debug.DrawLine(topLeft, topRight, Color.yellow, 0.5f);
        Debug.DrawLine(bottomLeft, bottomRight, Color.yellow, 0.5f);
        Debug.DrawLine(topLeft, bottomLeft, Color.yellow, 0.5f);
        Debug.DrawLine(topRight, bottomRight, Color.yellow, 0.5f);
    }

    // ���ù������ã�����ʱ�޸ģ�
    public void SetAttackConfig(AttackConfig config)
    {
        attackConfig = config;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActive) return;

        // ��ͨ�����߼�
        if (CanHitTarget(collision.gameObject))
        {
            ProcessHit(collision.gameObject);
        }
    }



    private void ProcessFullScreenHit(GameObject target)
    {
        if (target == null) return;

        // ��ȡĿ�����
        HitstunSystem targetHitstun = target.GetComponent<HitstunSystem>();
        PlayerController targetPlayer = target.GetComponent<PlayerController>();
        PlayerStats targetStats = target.GetComponent<PlayerStats>();

        // ���㹥������
        Vector2 attackDirection = CalculateAttackDirection(target);

        if (IsBlocking(targetPlayer, attackDirection))
        {
            ProcessBlockedHit(target, attackDirection);
            return;
        }

        // �����˺�
        if (targetStats != null)
        {
            PlayerStats attackerStats = playerController.GetComponent<PlayerStats>();
            if (attackerStats != null)
            {
                // ÿ��Tick���˺�
                int tickDamage = Mathf.Max(1, attackConfig.damage / 5);
                int tickEnergyRecovery = attackConfig.energyRecovery / 5;

                bool targetDied = attackerStats.TakeDamage(targetStats, tickDamage, tickEnergyRecovery);

                if (targetDied)
                {
                    HandleTargetDeath(target);
                    return;
                }
            }
        }

        // Ӧ��Ӳֱ�ͻ���
        if (targetHitstun != null)
        {
            targetHitstun.TakeHit(attackConfig, attackDirection);
        }

        Debug.Log($"ȫ������Tick: {target.name}");
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
        //if (hitTargets.Contains(target)) return false;

        // ����Ƿ�����Ч��Ŀ�����
        HitstunSystem targetHitstun = target.GetComponent<HitstunSystem>();
        PlayerController targetPlayer = target.GetComponent<PlayerController>();

        return targetHitstun != null || targetPlayer != null;
    }

    private void ProcessHit(GameObject target)
    {
        // ��ӵ��������б�
        //hitTargets.Add(target);

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
