using System;
using UnityEngine;

// 硬直数据配置
[CreateAssetMenu(fileName = "HitstunData", menuName = "Player Config/Hitstun Data")]
public class HitstunData : ScriptableObject
{
    [Header("硬直时间设置")]
    public float lightHitstun = 0.3f;      // 轻攻击硬直时间
    public float heavyHitstun = 0.5f;      // 重攻击硬直时间
    public float knockdownHitstun = 2f;     // 击倒攻击硬直时间  


    [Header("硬直效果")]
    public Color hitstunColor = Color.red; // 硬直时的颜色
}
