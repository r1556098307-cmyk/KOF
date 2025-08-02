using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Player/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [Header("碰撞体设置")]
    public ColliderSettings standingCollider = new ColliderSettings
    {
        offset = new Vector2(0, -0.84f),
        size = new Vector2(1.2f, 3.66f)
    };

    public ColliderSettings crouchingCollider = new ColliderSettings
    {
        offset = new Vector2(0, -1.5f),
        size = new Vector2(1.2f, 2.35f)
    };

    [Header("层级设置")]
    public LayerSettings layers = new LayerSettings();

    [Header("检测设置")]
    public DetectionSettings detection = new DetectionSettings();

    [Header("技能设置")]
    public SkillSettings skills = new SkillSettings();

    [Header("物理材质")]
    public PhysicsMaterial2D normalMaterial;
    public PhysicsMaterial2D wallMaterial;
}

[System.Serializable]
public class ColliderSettings
{
    public Vector2 offset;
    public Vector2 size;
}

[System.Serializable]
public class LayerSettings
{
    [Tooltip("无敌层级名称")]
    public string invulnerableLayerName = "Invulnerable";

    [Tooltip("玩家穿透层级名称")]
    public string playerPassThroughLayerName = "PlayerPassThrough";
}

[System.Serializable]
public class DetectionSettings
{
    [Header("地面检测")]
    public Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);

    [Header("墙面检测")]
    public Vector2 wallCheckSize = new Vector2(1.5f, 0.8f);
}

[System.Serializable]
public class SkillSettings
{
    [Header("特殊技能冲刺速度")]
    public List<SpecialMoveConfig> specialMoveConfigs = new List<SpecialMoveConfig>();
}