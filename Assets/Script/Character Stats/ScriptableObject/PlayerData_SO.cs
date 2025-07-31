using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Data", menuName = "Player Config/Data")]
public class PlayerData_SO : ScriptableObject
{
    [Header("Stats Info")]
    public int maxHealth;
    public int currentHealth;
    public int maxEnergyNum; // ���������
    public int currentEnergyNum; // ��ǰ������
    public int maxEnergy; // ������һ����������Ҫ������
    public int currentEnergy; // ��ǰ����������maxEnergyʱ����ǰ������++�����������������
}
