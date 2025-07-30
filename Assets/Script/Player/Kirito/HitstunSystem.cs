using System;
using UnityEngine;

// ��������ö��
public enum AttackType
{
    Light,
    Heavy,
    knockdown
}

public class HitstunSystem : MonoBehaviour
{
    [Header("Ӳֱ����")]
    public HitstunData hitstunData;

    [Header("״̬��ʾ")]
    [SerializeField] private bool isInHitstun = false;
    [SerializeField] private float hitstunTimer = 0f;
    [SerializeField] private bool isKnockedDown = false;

    // �������
    private PlayerController playerController;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerAnimator animator;

    // ӲֱЧ��
    private Vector3 originalPosition;
    private Color originalColor;

    // �¼�
    public event Action OnHitstunStart;
    public event Action OnHitstunEnd;
    public event Action OnKnockdown;
    public event Action OnGetUp;

    // Ӳֱ�ڼ���õ�����
    [SerializeField]
    private bool canMove = true;
    [SerializeField]
    private bool canAttack = true;
    [SerializeField]
    private bool canJump = true;
    [SerializeField]
    private bool canDash = true;
    [SerializeField]
    private bool canBlock = true;
    [SerializeField]
    private bool canCrouch = true;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<PlayerAnimator>();

        originalColor = spriteRenderer.color;
    }

    private void Update()
    {
        UpdateHitstun();
        //UpdateShakeEffect();
    }

    private void UpdateHitstun()
    {
        if (isInHitstun)
        {
            hitstunTimer -= Time.deltaTime;

            if (hitstunTimer <= 0f)
            {
                EndHitstun();
            }
        }
    }

    // �ܵ���������Ҫ����
    public void TakeHit(AttackConfig attackConfig, Vector2 attackDirection)
    {

        if (playerController != null && playerController.GetIsInvulnerable())
        {
            //Debug.Log("��ɫ�����޵�״̬��������Ч");
            return;
        }

        // �������������⴦��
        if (attackConfig.attackType == AttackType.knockdown)
        {
            HandleKnockdown();
        }
        else
        {
            // ��ʼӲֱ
            StartHitstun(attackConfig.attackType);
        }

        // Ӧ�û���
        ApplyKnockback(attackConfig, attackDirection);

        // �����ܻ�����
        PlayHitAnimation(attackConfig.attackType);

        // �����ܻ���Ч����Ч
        PlayHitEffects();
    }

    private void StartHitstun(AttackType attackType)
    {
        isInHitstun = true;

        // ����Ӳֱʱ��
        hitstunTimer = GetHitstunDuration(attackType);
        Debug.Log("��ֱʱ�䣺"+hitstunTimer);


        // �����������
        DisablePlayerInput();

        // �ı���ɫ��ʾ�ܻ�״̬
        spriteRenderer.color = hitstunData.hitstunColor;

        // �����¼�
        OnHitstunStart?.Invoke();

        //Debug.Log($"��ʼӲֱ: {hitstunTimer}��, ��������: {attackType}");
    }

    private void EndHitstun()
    {
        bool wasKnockedDown = isKnockedDown;

        isInHitstun = false;
        hitstunTimer = 0f;
 

        // �ָ��������
        EnablePlayerInput();

        // �ָ�ԭʼ��ɫ
        spriteRenderer.color = originalColor;


        // ���֮ǰ�ǻ���״̬�������ָ�
        if (wasKnockedDown)
        {
            RecoverFromKnockdown();
        }

        // �����¼�
        OnHitstunEnd?.Invoke();

        //Debug.Log("Ӳֱ����");
    }

    private float GetHitstunDuration(AttackType attackType)
    {
        float baseDuration = attackType switch
        {
            AttackType.Light => hitstunData.lightHitstun,
            AttackType.Heavy => hitstunData.heavyHitstun,
            AttackType.knockdown => hitstunData.knockdownHitstun,
            _ => hitstunData.lightHitstun
        };


        return baseDuration;
    }

    //private float GetCurrentHitstunDuration()
    //{
    //    return hitstunTimer + (Time.time - Time.fixedTime);
    //}

    private void ApplyKnockback(AttackConfig attackConfig, Vector2 attackDirection)
    {
        if (rb == null || attackConfig == null)
        {
            Debug.LogWarning("Rigidbody2D �� AttackConfig ȱʧ���޷�Ӧ�û���");
            return;
        }

        float knockbackForce = attackConfig.knockbackForce;


        // ֹͣ��ǰ�ٶ�
        rb.velocity = Vector2.zero;

        Vector2 knockback;

         if (attackConfig.isAddUpForce)
        {
            knockback = new Vector2(
             Mathf.Sign(attackDirection.x) * knockbackForce,  // ʹ�÷������ * ����ˮƽ��
             attackConfig.knockupForce                        // ʹ��������ֱ��
         );

        }
        else
        {
            // ֻ��ˮƽ����
            knockback = new Vector2(attackDirection.x * knockbackForce, 0f);
   
        }
        rb.AddForce(knockback, ForceMode2D.Impulse);
    }

    private void PlayHitAnimation(AttackType attackType)
    {
        if (animator != null)
        {
        
            string animationName = attackType switch
            {
                AttackType.Light => "LightHit",
                AttackType.Heavy => "HeavyHit",
                AttackType.knockdown => "KnockdownHit",
                _ => "LightHit"
            };
            Debug.Log(animationName);
            animator.PlayAnimation(animationName);
            
        }
    }

    private void PlayHitEffects()
    {
        // TODO: �����ܻ���Ч
        // AudioManager.Instance.PlaySFX("HitSound");

        // TODO: �����ܻ���Ч
        // EffectManager.Instance.PlayEffect("HitEffect", transform.position);
    }

    private void DisablePlayerInput()
    {
        canMove = false;
        canAttack = false;
        canJump = false;
        canDash = false;
        canBlock = false;
        canCrouch = false;

        // ���PlayerController����Щ״̬��ǿ��ֹͣ
        if (playerController != null)
        {
            // ֹͣ����״̬
            playerController.isAttack = false;

            // ֹͣ��״̬
            playerController.isBlock = false;

            // ����ʱֹͣ���ж���
            if (isKnockedDown)
            {
                playerController.isDash = false;
                playerController.isJump = false;
                playerController.isWalk = false;
                playerController.isCrouch = false;
            }
        }
    }

    private void EnablePlayerInput()
    {
        // ������ڻ���״̬���ӳٻָ�ĳЩ����
        if (isKnockedDown)
        {
            canMove = false; // ����״̬�¶��ݲ����ƶ�
            canAttack = false;
            canJump = false;
            canDash = false;
            canBlock = false;
            canCrouch=false;
        }
        else
        {
            canMove = true;
            canAttack = true;
            canJump = true;
            canDash = true;
            canBlock = true;
            canCrouch = true;
        }
    }

    // �������⴦��
    private void HandleKnockdown()
    {
        isKnockedDown = true;

        // ����ʱ����Ϊ�޵�״̬
        SetInvincible(true);

        // ����ʱ������Ӳֱ����
        StartHitstun(AttackType.knockdown);

        // ����ʱǿ�ƽ��뵹��״̬
        if (playerController != null)
        {
            // ֹͣ���ж���
            playerController.isAttack = false;
            playerController.isBlock = false;
            playerController.isDash = false;
            playerController.isJump = false;
            playerController.isCrouch = false;
        }

        // ���������¼�
        OnKnockdown?.Invoke();

        //Debug.Log("��ұ�����!");
    }

    // �ӻ���״̬�ָ�
    private void RecoverFromKnockdown()
    {
        isKnockedDown = false;

        // ����������
        if (animator != null)
        {
            //animator.PlayAnimation("GetUp");
        }

        // �ӳٻָ���ȫ���ƣ��������ڼ䣩
        Invoke(nameof(DelayedInputRecovery), 0.5f);

        // ������ӳ�ȡ���޵�״̬
        Invoke(nameof(EndInvincibilityAfterGetUp), 1.0f);

        // ���������¼�
        OnGetUp?.Invoke();

        //Debug.Log("��Ҵӻ���״̬�ָ�");
    }

    // �ӳ�����ָ���������������
    private void DelayedInputRecovery()
    {
        if (!isInHitstun) // ȷ����������Ӳֱ��
        {
            EnablePlayerInput();
        }
    }

    // ���������޵�״̬
    private void EndInvincibilityAfterGetUp()
    {
        if (!isInHitstun && !isKnockedDown)
        {
            SetInvincible(false);
        }
    }

    // �����޵�״̬��ʹ��PlayerController�ķ�����
    public void SetInvincible(bool invincible)
    {
        if (playerController == null) return;

        // ʹ��PlayerController��SetInvulnerable����
        playerController.SetInvulnerable(invincible);

        //if (invincible)
        //{
        //    Debug.Log("�����޵�״̬");
        //}
        //else
        //{
        //    Debug.Log("�޵�״̬����");
        //}
    }

    // ��������������ϵͳ��ѯ
    public bool IsInHitstun() => isInHitstun;
    public bool IsKnockedDown() => isKnockedDown;
    public bool IsMoveAllowed() => canMove;
    public bool IsAttackAllowed() => canAttack;
    public bool IsJumpAllowed() => canJump;
    public bool IsDashAllowed() => canDash;
    public bool IsBlockAllowed() => canBlock;
    public bool IsCrouchAllowed() => canCrouch;
    public float GetRemainingHitstunTime() => hitstunTimer;

    // ǿ�ƽ���Ӳֱ���������������
    public void ForceEndHitstun()
    {
        if (isInHitstun)
        {
            EndHitstun();
        }
    }

    // ǿ�ƴӻ���״̬�ָ�
    public void ForceGetUp()
    {
        if (isKnockedDown)
        {
            RecoverFromKnockdown();
        }
    }

    // �޸�Ӳֱʱ�䣨��������Ч����
    public void ModifyHitstunDuration(float multiplier)
    {
        if (isInHitstun)
        {
            hitstunTimer *= multiplier;
        }
    }




}