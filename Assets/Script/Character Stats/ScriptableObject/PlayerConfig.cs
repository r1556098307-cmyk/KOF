using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Player/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [Header("��ײ������")]
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

    [Header("�㼶����")]
    public LayerSettings layers = new LayerSettings();

    [Header("�������")]
    public DetectionSettings detection = new DetectionSettings();

    [Header("��������")]
    public SkillSettings skills = new SkillSettings();

    [Header("�������")]
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
    [Tooltip("�޵в㼶����")]
    public string invulnerableLayerName = "Invulnerable";

    [Tooltip("��Ҵ�͸�㼶����")]
    public string playerPassThroughLayerName = "PlayerPassThrough";
}

[System.Serializable]
public class DetectionSettings
{
    [Header("������")]
    public Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);

    [Header("ǽ����")]
    public Vector2 wallCheckSize = new Vector2(1.5f, 0.8f);
}

[System.Serializable]
public class SkillSettings
{
    [Header("���⼼�ܳ���ٶ�")]
    public List<SpecialMoveConfig> specialMoveConfigs = new List<SpecialMoveConfig>();
}