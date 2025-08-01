using System;
using UnityEngine;

// Ӳֱ��������
[CreateAssetMenu(fileName = "HitstunData", menuName = "Player Config/Hitstun Data")]
public class HitstunData : ScriptableObject
{
    [Header("Ӳֱʱ������")]
    public float lightHitstun = 0.3f;      // �ṥ��Ӳֱʱ��
    public float heavyHitstun = 0.5f;      // �ع���Ӳֱʱ��
    public float knockdownHitstun = 2f;     // ��������Ӳֱʱ��  


    [Header("ӲֱЧ��")]
    public Color hitstunColor = Color.red; // Ӳֱʱ����ɫ
}
