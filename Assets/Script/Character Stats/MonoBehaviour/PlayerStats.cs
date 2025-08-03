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

    public event Action<int, int, int, int> UpdateEnergyBarOnAttack;

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
                // 添加边界检查，防止能量变为负数
                int clampedValue = Mathf.Max(0, value);
                playerData.currentEnergyNum = clampedValue;

                // 如果尝试设置负值，记录警告
                if (value < 0)
                {
                    Debug.LogWarning($"Attempted to set CurrentEnergyNum to negative value {value}, clamped to 0");
                }

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

    // 修复：添加边界检查的能量消耗方法
    public void ConsumeEnergy(int energyCost)
    {
        if (energyCost < 0)
        {
            Debug.LogWarning($"Attempted to consume negative energy: {energyCost}");
            return;
        }

        if (CurrentEnergyNum < energyCost)
        {
            Debug.LogWarning($"Insufficient energy to consume! Current: {CurrentEnergyNum}, Required: {energyCost}");
            // 可以选择设置为0或者不执行消耗
            CurrentEnergyNum = 0;
        }
        else
        {
            CurrentEnergyNum -= energyCost;
        }

        Debug.Log($"Energy consumed: {energyCost}, Remaining: {CurrentEnergyNum}");
    }

    // 新增：设置能量的方法（用于修正负数能量）
    public void SetEnergy(int energyNum)
    {
        if (energyNum < 0)
        {
            Debug.LogWarning($"Attempted to set energy to negative value: {energyNum}, setting to 0 instead");
            energyNum = 0;
        }

        if (energyNum > MaxEnergyNum)
        {
            Debug.LogWarning($"Attempted to set energy to value higher than max: {energyNum}, clamping to {MaxEnergyNum}");
            energyNum = MaxEnergyNum;
        }

        CurrentEnergyNum = energyNum;
        Debug.Log($"Energy set to: {CurrentEnergyNum}");
    }

    // 新增：安全的能量消耗方法（返回是否成功）
    public bool TryConsumeEnergy(int energyCost)
    {
        if (HasSufficientEnergy(energyCost))
        {
            ConsumeEnergy(energyCost);
            return true;
        }
        else
        {
            Debug.LogWarning($"Cannot consume {energyCost} energy. Current energy: {CurrentEnergyNum}");
            return false;
        }
    }

    // 新增：添加能量的方法
    public void AddEnergy(int energyAmount)
    {
        if (energyAmount < 0)
        {
            Debug.LogWarning($"Attempted to add negative energy: {energyAmount}");
            return;
        }

        int newEnergy = Mathf.Min(CurrentEnergyNum + energyAmount, MaxEnergyNum);
        CurrentEnergyNum = newEnergy;
        Debug.Log($"Energy added: {energyAmount}, Current: {CurrentEnergyNum}");
    }

    // 新增：重置能量的方法
    public void ResetEnergy()
    {
        CurrentEnergyNum = 0;
        CurrentEnergy = 0;
        Debug.Log("Energy reset to 0");
    }

    // 新增：调试方法
    public void LogEnergyStatus()
    {
        Debug.Log($"Energy Status - Current: {CurrentEnergyNum}/{MaxEnergyNum}, Energy Bar: {CurrentEnergy}/{MaxEnergy}");
    }
    #endregion
}