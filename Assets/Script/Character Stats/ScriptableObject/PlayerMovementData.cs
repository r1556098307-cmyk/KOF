using UnityEngine;


[CreateAssetMenu(fileName = "New Movement Data", menuName = "Player Config/Movement Data")]
public class PlayerMovementData : ScriptableObject
{
    [Header("基础移动")]
    public float runMaxSpeed = 10f;
    public float runAccelAmount = 9.5f;
    public float runDeccelAmount = 9.5f;

    [Header("空中移动")]
    public float accelInAir = 1f;
    public float deccelInAir = 1f;

    [Header("跳跃相关")]
    public float jumpHangTimeThreshold = 1f;
    public float jumpHangAccelerationMult = 1.1f;
    public float jumpHangMaxSpeedMult = 1.3f;
    public float coyoteTime = 0.1f;
    public float jumpHangGravityMult = 1f;      // 跳跃顶点重力倍数
    public float jumpForce = 26f;           // 跳跃力度
    public float fallGravityMult =2f;    // 下降时的重力倍数
    public float maxFallSpeed = 18f;        // 最大下降速度


    [Header("高级设置")]
    public bool doConserveMomentum = false;
}