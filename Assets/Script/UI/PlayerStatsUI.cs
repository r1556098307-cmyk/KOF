using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("Player 1 UI References")]
    [SerializeField] private Text player1EnergyNumText;
    [SerializeField] private Image player1HealthSlider;
    [SerializeField] private Image player1EnergySlider;

    [Header("Player 2 UI References")]
    [SerializeField] private Text player2EnergyNumText;
    [SerializeField] private Image player2HealthSlider;
    [SerializeField] private Image player2EnergySlider;

    private void Start()
    {
        // 初始化UI显示
        StartCoroutine(WaitForPlayersAndInitialize());
    }
    private IEnumerator WaitForPlayersAndInitialize()
    {
        while (GameManager.Instance.player1Stats == null || GameManager.Instance.player2Stats == null)
        {
            yield return null;  // 等待一帧
        }

        SubscribeToPlayerEvents();
        InitializeUI();
    }

    private void SubscribeToPlayerEvents()
    {
        if (GameManager.Instance.player1Stats != null)
        {
            GameManager.Instance.player1Stats.UpdateHealthBarOnAttack += UpdatePlayer1Health;
            GameManager.Instance.player1Stats.UpdateEnergyBarOnAttack += UpdatePlayer1Energy;
        }

        if (GameManager.Instance.player2Stats != null)
        {
            GameManager.Instance.player2Stats.UpdateHealthBarOnAttack += UpdatePlayer2Health;
            GameManager.Instance.player2Stats.UpdateEnergyBarOnAttack += UpdatePlayer2Energy;
        }
    }

    private void InitializeUI()
    {
        // 初始化时设置UI
        if (GameManager.Instance.player1Stats != null)
        {
            UpdatePlayer1Health(GameManager.Instance.player1Stats.CurrentHealth,
                              GameManager.Instance.player1Stats.MaxHealth);
            UpdatePlayer1Energy(GameManager.Instance.player1Stats.CurrentEnergy,
                              GameManager.Instance.player1Stats.MaxEnergy,
                              GameManager.Instance.player1Stats.CurrentEnergyNum,
                              GameManager.Instance.player1Stats.MaxEnergyNum);
        }

        if (GameManager.Instance.player2Stats != null)
        {
            UpdatePlayer2Health(GameManager.Instance.player2Stats.CurrentHealth,
                              GameManager.Instance.player2Stats.MaxHealth);
            UpdatePlayer2Energy(GameManager.Instance.player2Stats.CurrentEnergy,
                              GameManager.Instance.player2Stats.MaxEnergy,
                              GameManager.Instance.player2Stats.CurrentEnergyNum,
                              GameManager.Instance.player2Stats.MaxEnergyNum);
        }
    }

    #region Player 1 UI Updates
    private void UpdatePlayer1Health(int currentHealth, int maxHealth)
    {
        if (player1HealthSlider != null && maxHealth > 0)
        {
            float sliderPercent = (float)currentHealth / maxHealth;
            player1HealthSlider.fillAmount = sliderPercent;
        }
    }

    private void UpdatePlayer1Energy(int currentEnergy, int maxEnergy, int currentEnergyNum, int maxEnergyNum)
    {
        if (player1EnergySlider != null && maxEnergy > 0)
        {
            float sliderPercent = (float)currentEnergy / maxEnergy;
            player1EnergySlider.fillAmount = sliderPercent;
            //Debug.Log($"当前能量：{currentEnergy}最大能量：{maxEnergy}");
        }

        if (player1EnergyNumText != null)
        {
            player1EnergyNumText.text = currentEnergyNum.ToString("0");
        }
    }
    #endregion

    #region Player 2 UI Updates
    private void UpdatePlayer2Health(int currentHealth, int maxHealth)
    {
        if (player2HealthSlider != null && maxHealth > 0)
        {
            float sliderPercent = (float)currentHealth / maxHealth;
            player2HealthSlider.fillAmount = sliderPercent;
        }
    }

    private void UpdatePlayer2Energy(int currentEnergy, int maxEnergy, int currentEnergyNum, int maxEnergyNum)
    {
        if (player2EnergySlider != null && maxEnergy > 0)
        {
            float sliderPercent = (float)currentEnergy / maxEnergy;
            player2EnergySlider.fillAmount = sliderPercent;
        }

        if (player2EnergyNumText != null)
        {
            player2EnergyNumText.text = currentEnergyNum.ToString("0");
        }
    }
    #endregion

    private void OnDestroy()
    {
        // 取消订阅事件防止内存泄漏
        UnsubscribeFromPlayerEvents();
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (GameManager.Instance?.player1Stats != null)
        {
            GameManager.Instance.player1Stats.UpdateHealthBarOnAttack -= UpdatePlayer1Health;
            GameManager.Instance.player1Stats.UpdateEnergyBarOnAttack -= UpdatePlayer1Energy;
        }

        if (GameManager.Instance?.player2Stats != null)
        {
            GameManager.Instance.player2Stats.UpdateHealthBarOnAttack -= UpdatePlayer2Health;
            GameManager.Instance.player2Stats.UpdateEnergyBarOnAttack -= UpdatePlayer2Energy;
        }
    }
}