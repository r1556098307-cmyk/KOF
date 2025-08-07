using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ComboListUI : MonoBehaviour
{
    [Header("UI����")]
    [SerializeField] private GameObject comboListPanel;
    [SerializeField] private Transform comboItemContainer;
    [SerializeField] private GameObject comboItemPrefab;

    [Header("����")]
    [SerializeField] private bool pauseGameWhenOpen = true;

    private PlayerInputControl inputControl;
    private ComboSystem playerComboSystem;
    private List<GameObject> comboItemInstances = new List<GameObject>();
    private bool isShowing = false;
    private PauseMenuUI pauseMenuUI; // ������ͣ�˵�

    void Awake()
    {
        // ��ʼ��Input System
        inputControl = new PlayerInputControl();

        // ��ToggleComboList�¼�
        inputControl.GamePlay.ToggleComboList.performed += OnToggleComboList;

        // ����PauseMenuUI
        pauseMenuUI = GetComponent<PauseMenuUI>();
        if (pauseMenuUI == null)
            pauseMenuUI = FindObjectOfType<PauseMenuUI>();
    }

    void Start()
    {
        // ��ʼʱ�������
        if (comboListPanel != null)
            comboListPanel.SetActive(false);

        // ������ҵ�ComboSystem
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
        // ����¼�
        if (inputControl != null)
        {
            inputControl.GamePlay.ToggleComboList.performed -= OnToggleComboList;
            inputControl.Dispose();
        }
    }

    void OnToggleComboList(InputAction.CallbackContext context)
    {
        // �����ͣ�˵��򿪣�����Ӧ
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
                Debug.LogWarning("�ҵ���Ҷ���û��ComboSystem���");
            }
        }
        else
        {
            Debug.LogWarning("δ�ҵ���Ҷ�����ȷ����Ҷ�����Player��ǩ");
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

        // �����ͣ�˵��򿪣�������򿪳��б�
        if (pauseMenuUI != null && pauseMenuUI.IsPaused())
        {
            return;
        }

        isShowing = true;
        comboListPanel.SetActive(true);

        // ��ͣ��Ϸ
        if (pauseGameWhenOpen)
            Time.timeScale = 0f;

        // �����������
        DisablePlayerInput();

        // ˢ�������б�
        RefreshComboList();
    }

    public void HideComboList()
    {
        if (comboListPanel == null) return;

        isShowing = false;
        comboListPanel.SetActive(false);

        // �ָ���Ϸ
        if (pauseGameWhenOpen)
            Time.timeScale = 1f;

        // �ָ��������
        EnablePlayerInput();
    }

    void DisablePlayerInput()
    {
        // ����GamePlay����
        if (inputControl != null)
        {
            inputControl.GamePlay.Disable();
            // ������ToggleComboList��������
            inputControl.GamePlay.ToggleComboList.Enable();
        }

        // ������ҵ�ComboSystem��PlayerController
        if (playerComboSystem != null)
        {
            GameObject player = playerComboSystem.gameObject;

            // ��ʱ����ComboSystem
            playerComboSystem.enabled = false;
            playerComboSystem.ClearInputHistory();

            // ��ʱ����PlayerController
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // ��ʱ���������ṩ��
            HumanInputProvider humanInput = player.GetComponent<HumanInputProvider>();
            if (humanInput != null)
            {
                humanInput.enabled = false;
            }
        }
    }

    void EnablePlayerInput()
    {
        // �ָ�GamePlay����
        if (inputControl != null)
        {
            inputControl.GamePlay.Enable();
        }

        // �ָ���ҵ�ComboSystem��PlayerController
        if (playerComboSystem != null)
        {
            GameObject player = playerComboSystem.gameObject;

            // �ָ�ComboSystem
            playerComboSystem.enabled = true;

            // �ָ�PlayerController
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // �ָ������ṩ��
            HumanInputProvider humanInput = player.GetComponent<HumanInputProvider>();
            if (humanInput != null)
            {
                humanInput.enabled = true;
            }
        }
    }

    void RefreshComboList()
    {
        // ����ɵ��б���
        foreach (var item in comboItemInstances)
        {
            Destroy(item);
        }
        comboItemInstances.Clear();

        // ���û���ҵ�ComboSystem���������²���
        if (playerComboSystem == null)
        {
            FindPlayerComboSystem();
            if (playerComboSystem == null)
            {
                Debug.LogError("�޷��ҵ���ҵ�ComboSystem���");
                return;
            }
        }

        // �����µ��б���
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

        // ���Ҳ����ü�����
        Transform skillNameTrans = item.transform.Find("SkillName");
        if (skillNameTrans != null)
        {
            Text skillNameText = skillNameTrans.GetComponent<Text>();
            if (skillNameText != null)
                skillNameText.text = combo.skillName;
        }

        // ���Ҳ�������������
        Transform keySequenceTrans = item.transform.Find("KeySequence");
        if (keySequenceTrans != null)
        {
            Text sequenceText = keySequenceTrans.GetComponent<Text>();
            if (sequenceText != null)
            {
                sequenceText.text = GetKeySequenceText(combo.keySequence);
            }
        }

        // ���Ҳ�������������
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
            case ComboSystem.GameInputKey.MoveUp: return "��";
            case ComboSystem.GameInputKey.MoveDown: return "��";
            case ComboSystem.GameInputKey.MoveLeft: return "��";
            case ComboSystem.GameInputKey.MoveRight: return "��";
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
            return new Color(1f, 0.3f, 0.3f); // ��ɫ
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