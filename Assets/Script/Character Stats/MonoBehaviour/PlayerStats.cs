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
            // ��ʼ����ǰֵΪ���ֵ
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
                // ����Ƿ���Ҫ����������
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
                // ��ӱ߽��飬��ֹ������Ϊ����
                int clampedValue = Mathf.Max(0, value);
                playerData.currentEnergyNum = clampedValue;

                // ����������ø�ֵ����¼����
                if (value < 0)
                {
                    Debug.LogWarning($"Attempted to set CurrentEnergyNum to negative value {value}, clamped to 0");
                }

                UpdateEnergyBarOnAttack?.Invoke(CurrentEnergy, MaxEnergy, CurrentEnergyNum, MaxEnergyNum);
            }
        }
    }

    // �򵥵�get/set
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
        // ����Ѿ��ﵽ���������������������ֵΪ0
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

    //    // ����Ѫ��UI
    //    targetStats.UpdateHealthBarOnAttack?.Invoke(targetStats.CurrentHealth,targetStats.MaxHealth);
    //    // ��������UI
    //    UpdateEnergyBarOnAttack?.Invoke(CurrentEnergy,MaxEnergy,CurrentEnergyNum,MaxEnergyNum);
    //    // ����Ƿ�����
    //    return targetStats.CurrentHealth == 0;
    //}

    public bool TakeDamage(PlayerStats targetStats, int damage, int energyRecovery)
    {
        // ʹ���������Զ������¼�
        targetStats.CurrentHealth = Mathf.Max(targetStats.CurrentHealth - damage, 0);
        // �޸������ָ��߼��������������������
        int newEnergy = CurrentEnergy + energyRecovery;

        // ����Ѿ��ﵽ���������������������ֵΪ0
        if (CurrentEnergyNum >= MaxEnergyNum)
        {
            CurrentEnergy = 0;
        }
        else
        {
            CurrentEnergy = newEnergy; // ��CheckEnergyNumberIncrease�������
        }

        return targetStats.CurrentHealth == 0;
    }

    public bool HasSufficientEnergy(int energyCost)
    {
        return CurrentEnergyNum >= energyCost;
    }

    // �޸�����ӱ߽�����������ķ���
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
            // ����ѡ������Ϊ0���߲�ִ������
            CurrentEnergyNum = 0;
        }
        else
        {
            CurrentEnergyNum -= energyCost;
        }

        Debug.Log($"Energy consumed: {energyCost}, Remaining: {CurrentEnergyNum}");
    }

    // ���������������ķ�����������������������
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

    // ��������ȫ���������ķ����������Ƿ�ɹ���
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

    // ��������������ķ���
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

    // ���������������ķ���
    public void ResetEnergy()
    {
        CurrentEnergyNum = 0;
        CurrentEnergy = 0;
        Debug.Log("Energy reset to 0");
    }

    // ���������Է���
    public void LogEnergyStatus()
    {
        Debug.Log($"Energy Status - Current: {CurrentEnergyNum}/{MaxEnergyNum}, Energy Bar: {CurrentEnergy}/{MaxEnergy}");
    }
    #endregion
}