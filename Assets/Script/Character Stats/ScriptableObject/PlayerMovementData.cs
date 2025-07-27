using UnityEngine;


[CreateAssetMenu(fileName = "New Movement Data", menuName = "Player Config/Movement Data")]
public class PlayerMovementData : ScriptableObject
{
    [Header("�����ƶ�")]
    public float runMaxSpeed = 10f;
    public float runAccelAmount = 9.5f;
    public float runDeccelAmount = 9.5f;

    [Header("�����ƶ�")]
    public float accelInAir = 1f;
    public float deccelInAir = 1f;

    [Header("��Ծ���")]
    public float jumpHangTimeThreshold = 1f;
    public float jumpHangAccelerationMult = 1.1f;
    public float jumpHangMaxSpeedMult = 1.3f;
    public float coyoteTime = 0.1f;
    public float jumpHangGravityMult = 1f;      // ��Ծ������������
    public float jumpForce = 26f;           // ��Ծ����
    public float fallGravityMult =2f;    // �½�ʱ����������
    public float maxFallSpeed = 18f;        // ����½��ٶ�


    [Header("�߼�����")]
    public bool doConserveMomentum = false;
}