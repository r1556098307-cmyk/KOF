using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Data", menuName = "Player Config/Data")]
public class PlayerData_SO : ScriptableObject
{
    [Header("Stats Info")]
    public int maxHealth;
    public int currentHealth;
    public int maxEnergyNum; // 最大能量数
    public int currentEnergyNum; // 当前能量数
    public int maxEnergy; // 积蓄满一个能量槽需要的能量
    public int currentEnergy; // 当前能量，到达maxEnergy时，当前能量数++，不超过最大能量数
}
