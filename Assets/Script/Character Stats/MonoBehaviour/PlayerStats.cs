using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ComboSystem;

public class PlayerStats : MonoBehaviour
{
    public PlayerData_SO templateData;

    public PlayerData_SO playerData;

    public event Action<int, int> UpdateHealthBarOnAttack;

    public event Action<int, int,int,int> UpdateEnergyBarOnAttack;

    private void Awake()
    {
        if (templateData != null)
        {
            playerData = Instantiate(templateData);
            // 初始化当前值为最大值
            InitializeStats();
        }
            
    }

    private void InitializeStats()
    {
        if (playerData != null)
        {
            playerData.currentHealth = playerData.maxHealth;
            playerData.currentEnergy = 0;
            playerData.currentEnergyNum = 0;
        }
    }


    #region Properties with Event Triggers
    public int CurrentHealth
    {
        get => playerData?.currentHealth ?? 0;
        set
        {
            if (playerData != null && playerData.currentHealth != value)
            {
                playerData.currentHealth = value;
                UpdateHealthBarOnAttack?.Invoke(CurrentHealth, MaxHealth);
            }
        }
    }

    public int CurrentEnergy
    {
        get => playerData?.currentEnergy ?? 0;
        set
        {
            if (playerData != null && playerData.currentEnergy != value)
            {
                playerData.currentEnergy = value;
                // 检查是否需要增加能量数
                CheckEnergyNumberIncrease();
                UpdateEnergyBarOnAttack?.Invoke(CurrentEnergy, MaxEnergy, CurrentEnergyNum, MaxEnergyNum);
            }
        }
    }

    public int CurrentEnergyNum
    {
        get => playerData?.currentEnergyNum ?? 0;
        set
        {
            if (playerData != null && playerData.currentEnergyNum != value)
            {
                playerData.currentEnergyNum = value;
                UpdateEnergyBarOnAttack?.Invoke(CurrentEnergy, MaxEnergy, CurrentEnergyNum, MaxEnergyNum);
            }
        }
    }

    // 简单的get/set
    public int MaxHealth
    {
        get => playerData?.maxHealth ?? 0;
        set { if (playerData != null) playerData.maxHealth = value; }
    }

    public int MaxEnergy
    {
        get => playerData?.maxEnergy ?? 0;
        set { if (playerData != null) playerData.maxEnergy = value; }
    }

    public int MaxEnergyNum
    {
        get => playerData?.maxEnergyNum ?? 0;
        set { if (playerData != null) playerData.maxEnergyNum = value; }
    }
    #endregion

    private void CheckEnergyNumberIncrease()
    {
        while (CurrentEnergy >= MaxEnergy && CurrentEnergyNum < MaxEnergyNum)
        {
            CurrentEnergyNum++;
            playerData.currentEnergy = CurrentEnergy - MaxEnergy;
        }
        // 如果已经达到最大能量槽数，限制能量值为0
        if (CurrentEnergyNum >= MaxEnergyNum)
        {
            playerData.currentEnergy = 0;
        }
    }

    #region Player Combat
    //public bool TakeDamage(PlayerStats targetStats,int damage,int energyRecovery)
    //{
    //    targetStats.CurrentHealth = Mathf.Max(targetStats.CurrentHealth - damage, 0);

    //    CurrentEnergy = Mathf.Min(CurrentEnergy + energyRecovery, MaxEnergy);

    //    // 更新血量UI
    //    targetStats.UpdateHealthBarOnAttack?.Invoke(targetStats.CurrentHealth,targetStats.MaxHealth);
    //    // 更新能量UI
    //    UpdateEnergyBarOnAttack?.Invoke(CurrentEnergy,MaxEnergy,CurrentEnergyNum,MaxEnergyNum);
    //    // 检查是否死亡
    //    return targetStats.CurrentHealth == 0;
    //}

    public bool TakeDamage(PlayerStats targetStats, int damage, int energyRecovery)
    {
        // 使用属性来自动触发事件
        targetStats.CurrentHealth = Mathf.Max(targetStats.CurrentHealth - damage, 0);
        // 修改能量恢复逻辑，考虑最大能量槽限制
        int newEnergy = CurrentEnergy + energyRecovery;

        // 如果已经达到最大能量槽数，限制能量值为0
        if (CurrentEnergyNum >= MaxEnergyNum)
        {
            CurrentEnergy = 0;
        }
        else
        {
            CurrentEnergy = newEnergy; // 让CheckEnergyNumberIncrease处理溢出
        }

        return targetStats.CurrentHealth == 0;
    }

    public bool HasSufficientEnergy(int energyCost)
    {

        return CurrentEnergyNum >= energyCost;
    }

    public void ConsumeEnergy(int energyCost)
    {
        CurrentEnergyNum -= energyCost;
    }
    #endregion

}
