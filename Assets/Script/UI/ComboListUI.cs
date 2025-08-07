using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ComboListUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject comboListPanel;
    [SerializeField] private Transform comboItemContainer;
    [SerializeField] private GameObject comboItemPrefab;

    [Header("设置")]
    [SerializeField] private bool pauseGameWhenOpen = true;

    private PlayerInputControl inputControl;
    private ComboSystem playerComboSystem;
    private List<GameObject> comboItemInstances = new List<GameObject>();
    private bool isShowing = false;
    private PauseMenuUI pauseMenuUI; // 引用暂停菜单

    void Awake()
    {
        // 初始化Input System
        inputControl = new PlayerInputControl();

        // 绑定ToggleComboList事件
        inputControl.GamePlay.ToggleComboList.performed += OnToggleComboList;

        // 查找PauseMenuUI
        pauseMenuUI = GetComponent<PauseMenuUI>();
        if (pauseMenuUI == null)
            pauseMenuUI = FindObjectOfType<PauseMenuUI>();
    }

    void Start()
    {
        // 初始时隐藏面板
        if (comboListPanel != null)
            comboListPanel.SetActive(false);

        // 查找玩家的ComboSystem
        FindPlayerComboSystem();
    }

    void OnEnable()
    {
        inputControl?.Enable();
    }

    void OnDisable()
    {
        inputControl?.Disable();
    }

    void OnDestroy()
    {
        // 解绑事件
        if (inputControl != null)
        {
            inputControl.GamePlay.ToggleComboList.performed -= OnToggleComboList;
            inputControl.Dispose();
        }
    }

    void OnToggleComboList(InputAction.CallbackContext context)
    {
        // 如果暂停菜单打开，不响应
        if (pauseMenuUI != null && pauseMenuUI.IsPaused())
        {
            return;
        }

        ToggleComboList();
    }

    void FindPlayerComboSystem()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = GameObject.Find("Player");

        if (player != null)
        {
            playerComboSystem = player.GetComponent<ComboSystem>();
            if (playerComboSystem == null)
            {
                Debug.LogWarning("找到玩家对象但没有ComboSystem组件");
            }
        }
        else
        {
            Debug.LogWarning("未找到玩家对象，请确保玩家对象有Player标签");
        }
    }

    public void ToggleComboList()
    {
        if (isShowing)
            HideComboList();
        else
            ShowComboList();
    }

    public void ShowComboList()
    {
        if (comboListPanel == null) return;

        // 如果暂停菜单打开，不允许打开出招表
        if (pauseMenuUI != null && pauseMenuUI.IsPaused())
        {
            return;
        }

        isShowing = true;
        comboListPanel.SetActive(true);

        // 暂停游戏
        if (pauseGameWhenOpen)
            Time.timeScale = 0f;

        // 禁用玩家输入
        DisablePlayerInput();

        // 刷新连招列表
        RefreshComboList();
    }

    public void HideComboList()
    {
        if (comboListPanel == null) return;

        isShowing = false;
        comboListPanel.SetActive(false);

        // 恢复游戏
        if (pauseGameWhenOpen)
            Time.timeScale = 1f;

        // 恢复玩家输入
        EnablePlayerInput();
    }

    void DisablePlayerInput()
    {
        // 禁用GamePlay输入
        if (inputControl != null)
        {
            inputControl.GamePlay.Disable();
            // 但保持ToggleComboList动作可用
            inputControl.GamePlay.ToggleComboList.Enable();
        }

        // 禁用玩家的ComboSystem和PlayerController
        if (playerComboSystem != null)
        {
            GameObject player = playerComboSystem.gameObject;

            // 暂时禁用ComboSystem
            playerComboSystem.enabled = false;
            playerComboSystem.ClearInputHistory();

            // 暂时禁用PlayerController
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // 暂时禁用输入提供者
            HumanInputProvider humanInput = player.GetComponent<HumanInputProvider>();
            if (humanInput != null)
            {
                humanInput.enabled = false;
            }
        }
    }

    void EnablePlayerInput()
    {
        // 恢复GamePlay输入
        if (inputControl != null)
        {
            inputControl.GamePlay.Enable();
        }

        // 恢复玩家的ComboSystem和PlayerController
        if (playerComboSystem != null)
        {
            GameObject player = playerComboSystem.gameObject;

            // 恢复ComboSystem
            playerComboSystem.enabled = true;

            // 恢复PlayerController
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // 恢复输入提供者
            HumanInputProvider humanInput = player.GetComponent<HumanInputProvider>();
            if (humanInput != null)
            {
                humanInput.enabled = true;
            }
        }
    }

    void RefreshComboList()
    {
        // 清除旧的列表项
        foreach (var item in comboItemInstances)
        {
            Destroy(item);
        }
        comboItemInstances.Clear();

        // 如果没有找到ComboSystem，尝试重新查找
        if (playerComboSystem == null)
        {
            FindPlayerComboSystem();
            if (playerComboSystem == null)
            {
                Debug.LogError("无法找到玩家的ComboSystem组件");
                return;
            }
        }

        // 创建新的列表项
        foreach (var combo in playerComboSystem.combos)
        {
            CreateComboItem(combo);
        }
    }

    void CreateComboItem(ComboSystem.ComboData combo)
    {
        if (comboItemPrefab == null || comboItemContainer == null) return;

        GameObject item = Instantiate(comboItemPrefab, comboItemContainer);
        comboItemInstances.Add(item);

        // 查找并设置技能名
        Transform skillNameTrans = item.transform.Find("SkillName");
        if (skillNameTrans != null)
        {
            Text skillNameText = skillNameTrans.GetComponent<Text>();
            if (skillNameText != null)
                skillNameText.text = combo.skillName;
        }

        // 查找并设置连招序列
        Transform keySequenceTrans = item.transform.Find("KeySequence");
        if (keySequenceTrans != null)
        {
            Text sequenceText = keySequenceTrans.GetComponent<Text>();
            if (sequenceText != null)
            {
                sequenceText.text = GetKeySequenceText(combo.keySequence);
            }
        }

        // 查找并设置能量消耗
        Transform energyCostTrans = item.transform.Find("EnergyCost");
        if (energyCostTrans != null)
        {
            Text energyText = energyCostTrans.GetComponent<Text>();
            if (energyText != null)
            {
                energyText.text = combo.energyCost.ToString();
                energyText.color = GetEnergyCostColor(combo.energyCost);
            }
        }
    }

    string GetKeySequenceText(List<ComboSystem.GameInputKey> keySequence)
    {
        string sequence = "";
        for (int i = 0; i < keySequence.Count; i++)
        {
            sequence += GetKeyText(keySequence[i]);
            if (i < keySequence.Count - 1)
                sequence += " + ";
        }
        return sequence;
    }

    string GetKeyText(ComboSystem.GameInputKey key)
    {
        switch (key)
        {
            case ComboSystem.GameInputKey.MoveUp: return "↑";
            case ComboSystem.GameInputKey.MoveDown: return "↓";
            case ComboSystem.GameInputKey.MoveLeft: return "←";
            case ComboSystem.GameInputKey.MoveRight: return "→";
            case ComboSystem.GameInputKey.Attack: return "J";
            case ComboSystem.GameInputKey.Block: return "K";
            case ComboSystem.GameInputKey.Dash: return "L";
            case ComboSystem.GameInputKey.Jump: return "Space";
            default: return "?";
        }
    }

    Color GetEnergyCostColor(int cost)
    {
        if (cost == 0)
            return Color.green;
        else if (cost == 1)
            return Color.yellow;
        else
            return new Color(1f, 0.3f, 0.3f); // 红色
    }

    public void SetPlayerComboSystem(ComboSystem comboSystem)
    {
        playerComboSystem = comboSystem;
    }

    public bool IsComboListOpen()
    {
        return isShowing;
    }
}