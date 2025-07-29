using UnityEngine;

[CreateAssetMenu(fileName = "New Movement Data", menuName = "Player Config/Movement Data")]
public class PlayerMovementData : ScriptableObject
{
    [Header("========== 基础移动参数 ==========")]
    [Space(5)]

    [Header("【地面移动速度】")]
    [Tooltip("角色在地面上的最大移动速度")]
    [Range(5f, 20f)]
    public float runMaxSpeed = 10f;

    [Header("【地面加速度】")]
    [Tooltip("加速时的加速度大小，值越大角色达到最大速度越快")]
    [Range(5f, 20f)]
    public float runAccelAmount = 9.5f;

    [Header("【地面减速度】")]
    [Tooltip("减速时的减速度大小，值越大角色停下来越快")]
    [Range(5f, 20f)]
    public float runDeccelAmount = 9.5f;


    [Header("【下蹲减速倍率】")]
    [Tooltip("下蹲时的减速度大小，值越小角色移动速度越慢")]
    [Range(0f, 1f)]
    public float crouchSpeedMultiplier = 0.5f;

    [Space(20)]
    [Header("========== 空中控制参数 ==========")]
    [Space(5)]

    [Header("【空中加速度倍数】")]
    [Tooltip("空中加速度相对于地面的倍数。1=相同，<1=空中更难改变方向")]
    [Range(0.1f, 1f)]
    public float accelInAir = 1f;

    [Header("【空中减速度倍数】")]
    [Tooltip("空中减速度相对于地面的倍数。较小值让角色在空中保持惯性")]
    [Range(0.1f, 1f)]
    public float deccelInAir = 1f;

    [Header("【动量保持】")]
    [Tooltip("是否保持动量。开启后玩家在空中松开方向键时会保持当前速度")]
    public bool doConserveMomentum = false;

    [Space(20)]
    [Header("========== 跳跃机制参数 ==========")]
    [Space(5)]

    [Header("【跳跃力度】")]
    [Tooltip("跳跃的初始向上冲力，直接决定跳跃高度")]
    [Range(15f, 40f)]
    public float jumpForce = 26f;

    [Header("【土狼时间】")]
    [Tooltip("角色离开平台后仍可跳跃的宽容时间（秒），提升操作手感")]
    [Range(0f, 0.5f)]
    public float coyoteTime = 0.2f;

    [Header("【跳跃输入缓冲】")]
    [Tooltip("按下跳跃键后的有效时间（秒），防止玩家过早按键")]
    [Range(0f, 0.5f)]
    public float jumpInputBufferTime = 0.2f;

    [Space(10)]
    [Header("--- 跳跃顶点控制 ---")]

    [Header("【顶点判定阈值】")]
    [Tooltip("当垂直速度小于此值时，认为角色处于跳跃顶点")]
    [Range(0.5f, 2f)]
    public float jumpHangTimeThreshold = 1f;

    [Header("【顶点加速度倍数】")]
    [Tooltip("跳跃顶点时的水平加速度倍数，提供更好的空中控制")]
    [Range(1f, 2f)]
    public float jumpHangAccelerationMult = 1.1f;

    [Header("【顶点最大速度倍数】")]
    [Tooltip("跳跃顶点时的最大水平速度倍数，允许顶点时移动更快")]
    [Range(1f, 2f)]
    public float jumpHangMaxSpeedMult = 1.3f;

    [Header("【顶点重力倍数】")]
    [Tooltip("跳跃顶点的重力倍数，<1产生悬停感，格斗游戏通常接近1")]
    [Range(0.5f, 1.5f)]
    public float jumpHangGravityMult = 1f;

    [Space(20)]
    [Header("========== 重力系统参数 ==========")]
    [Space(5)]

    [Header("【基础重力缩放】")]
    [Tooltip("整体重力倍数，格斗游戏通常设置较大值(2-4)让动作更快")]
    [Range(1f, 5f)]
    public float gravityScale = 3f;

    [Header("【下落重力倍数】")]
    [Tooltip("普通下落时的额外重力倍数，让下落比上升更快")]
    [Range(1f, 3f)]
    public float fallGravityMult = 2f;

    [Header("【最大下落速度】")]
    [Tooltip("普通下落的速度上限，防止无限加速")]
    [Range(10f, 30f)]
    public float maxFallSpeed = 18f;

    [Space(10)]
    [Header("--- 快速下落 ---")]

    [Header("【快落重力倍数】")]
    [Tooltip("按住下键时的额外重力倍数，实现主动快速下落")]
    [Range(2f, 5f)]
    public float fastFallGravityMult = 3f;

    [Header("【快落最大速度】")]
    [Tooltip("快速下落的速度上限")]
    [Range(20f, 50f)]
    public float maxFastFallSpeed = 35f;


    [Space(20)]
    [Header("========== 冲刺参数 ==========")]
    [Space(5)]

    [Header("【冲刺持续时间】")]
    [Tooltip("单次冲刺的持续时间")]
    [Range(0.1f, 0.5f)]
    public float dashDuration = 0.2f;

    [Header("【冲刺冷却时间】")]
    [Tooltip("冲刺后的冷却时间")]
    [Range(0.5f, 2f)]
    public float dashCooldown = 0.5f;

    [Header("【冲刺速度】")]
    [Tooltip("冲刺时给一个固定的速度")]
    public float dashSpeed = 30f;
}