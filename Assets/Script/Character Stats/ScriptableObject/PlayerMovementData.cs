using UnityEngine;

[CreateAssetMenu(fileName = "New Movement Data", menuName = "Player Config/Movement Data")]
public class PlayerMovementData : ScriptableObject
{
    [Header("========== �����ƶ����� ==========")]
    [Space(5)]

    [Header("�������ƶ��ٶȡ�")]
    [Tooltip("��ɫ�ڵ����ϵ�����ƶ��ٶ�")]
    [Range(5f, 20f)]
    public float runMaxSpeed = 10f;

    [Header("��������ٶȡ�")]
    [Tooltip("����ʱ�ļ��ٶȴ�С��ֵԽ���ɫ�ﵽ����ٶ�Խ��")]
    [Range(5f, 20f)]
    public float runAccelAmount = 9.5f;

    [Header("��������ٶȡ�")]
    [Tooltip("����ʱ�ļ��ٶȴ�С��ֵԽ���ɫͣ����Խ��")]
    [Range(5f, 20f)]
    public float runDeccelAmount = 9.5f;


    [Header("���¶׼��ٱ��ʡ�")]
    [Tooltip("�¶�ʱ�ļ��ٶȴ�С��ֵԽС��ɫ�ƶ��ٶ�Խ��")]
    [Range(0f, 1f)]
    public float crouchSpeedMultiplier = 0.5f;

    [Space(20)]
    [Header("========== ���п��Ʋ��� ==========")]
    [Space(5)]

    [Header("�����м��ٶȱ�����")]
    [Tooltip("���м��ٶ�����ڵ���ı�����1=��ͬ��<1=���и��Ѹı䷽��")]
    [Range(0.1f, 1f)]
    public float accelInAir = 1f;

    [Header("�����м��ٶȱ�����")]
    [Tooltip("���м��ٶ�����ڵ���ı�������Сֵ�ý�ɫ�ڿ��б��ֹ���")]
    [Range(0.1f, 1f)]
    public float deccelInAir = 1f;

    [Header("���������֡�")]
    [Tooltip("�Ƿ񱣳ֶ���������������ڿ����ɿ������ʱ�ᱣ�ֵ�ǰ�ٶ�")]
    public bool doConserveMomentum = false;

    [Space(20)]
    [Header("========== ��Ծ���Ʋ��� ==========")]
    [Space(5)]

    [Header("����Ծ���ȡ�")]
    [Tooltip("��Ծ�ĳ�ʼ���ϳ�����ֱ�Ӿ�����Ծ�߶�")]
    [Range(15f, 40f)]
    public float jumpForce = 26f;

    [Header("������ʱ�䡿")]
    [Tooltip("��ɫ�뿪ƽ̨���Կ���Ծ�Ŀ���ʱ�䣨�룩�����������ָ�")]
    [Range(0f, 0.5f)]
    public float coyoteTime = 0.2f;

    [Header("����Ծ���뻺�塿")]
    [Tooltip("������Ծ�������Чʱ�䣨�룩����ֹ��ҹ��簴��")]
    [Range(0f, 0.5f)]
    public float jumpInputBufferTime = 0.2f;

    [Space(10)]
    [Header("--- ��Ծ������� ---")]

    [Header("�������ж���ֵ��")]
    [Tooltip("����ֱ�ٶ�С�ڴ�ֵʱ����Ϊ��ɫ������Ծ����")]
    [Range(0.5f, 2f)]
    public float jumpHangTimeThreshold = 1f;

    [Header("��������ٶȱ�����")]
    [Tooltip("��Ծ����ʱ��ˮƽ���ٶȱ������ṩ���õĿ��п���")]
    [Range(1f, 2f)]
    public float jumpHangAccelerationMult = 1.1f;

    [Header("����������ٶȱ�����")]
    [Tooltip("��Ծ����ʱ�����ˮƽ�ٶȱ�����������ʱ�ƶ�����")]
    [Range(1f, 2f)]
    public float jumpHangMaxSpeedMult = 1.3f;

    [Header("����������������")]
    [Tooltip("��Ծ���������������<1������ͣ�У�����Ϸͨ���ӽ�1")]
    [Range(0.5f, 1.5f)]
    public float jumpHangGravityMult = 1f;

    [Space(20)]
    [Header("========== ����ϵͳ���� ==========")]
    [Space(5)]

    [Header("�������������š�")]
    [Tooltip("������������������Ϸͨ�����ýϴ�ֵ(2-4)�ö�������")]
    [Range(1f, 5f)]
    public float gravityScale = 3f;

    [Header("����������������")]
    [Tooltip("��ͨ����ʱ�Ķ����������������������������")]
    [Range(1f, 3f)]
    public float fallGravityMult = 2f;

    [Header("����������ٶȡ�")]
    [Tooltip("��ͨ������ٶ����ޣ���ֹ���޼���")]
    [Range(10f, 30f)]
    public float maxFallSpeed = 18f;

    [Space(10)]
    [Header("--- �������� ---")]

    [Header("����������������")]
    [Tooltip("��ס�¼�ʱ�Ķ�������������ʵ��������������")]
    [Range(2f, 5f)]
    public float fastFallGravityMult = 3f;

    [Header("����������ٶȡ�")]
    [Tooltip("����������ٶ�����")]
    [Range(20f, 50f)]
    public float maxFastFallSpeed = 35f;


    [Space(20)]
    [Header("========== ��̲��� ==========")]
    [Space(5)]

    [Header("����̳���ʱ�䡿")]
    [Tooltip("���γ�̵ĳ���ʱ��")]
    [Range(0.1f, 0.5f)]
    public float dashDuration = 0.2f;

    [Header("�������ȴʱ�䡿")]
    [Tooltip("��̺����ȴʱ��")]
    [Range(0.5f, 2f)]
    public float dashCooldown = 0.5f;

    [Header("������ٶȡ�")]
    [Tooltip("���ʱ��һ���̶����ٶ�")]
    public float dashSpeed = 30f;
}