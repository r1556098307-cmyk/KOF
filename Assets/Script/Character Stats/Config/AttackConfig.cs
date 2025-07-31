using UnityEngine;

// ������������
[System.Serializable]
public class AttackConfig
{
    [Header("������������")]
    public AttackType attackType = AttackType.Light;
    public int damage = 10;
    public int energyRecovery = 10;
    public string attackName = "Attack"; // ���ڱ�ʶ��ͬ����

    [Header("��������")]
    public float knockbackForce = 5f; // ��Ҫ��������ˮƽ����
    public float knockupForce = 0f; // ���ϵ���
    public bool  isAddUpForce = true; // �Ƿ�������ϵĻ���
    public Vector2 customKnockbackDirection = Vector2.zero; // �Զ�����˷���

    [Header("�񵲻���")]
    public float blockKnockbackForce = 2f; // ��ʱ�����˵���

    [Header("��Ч��Ч")]
    public string hitSound = "HitSound";
    public string hitEffect = "HitEffect";

    [Header("����Ч��")]
    public float stunDurationMultiplier = 1f; // Ӳֱʱ�䱶��
}