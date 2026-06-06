using UnityEngine;
using TMPro;

/// <summary>
/// 密码锁：最后一轮按E弹出输入界面，输入正确密码取得血清原液
/// 挂在EndDoor上
/// </summary>
public class PasswordLock : MonoBehaviour, IInteractable
{
    [Header("设置")]
    public string correctPassword = "1958";
    public int requiredLoop = 5;            // 第6轮（index 5）才能交互

    [Header("UI引用")]
    public GameObject passwordPanel;        // 密码输入面板
    public TMP_InputField inputField;       // 输入框
    public TextMeshProUGUI feedbackText;    // 反馈文字

    [Header("任务完成")]
    public GameObject doorToOpen;           // 密码正确后打开的门
    public GameObject endPanel;             // 任务完成画面（白色渐变+文字）
    public Chaser chaser;                   // 拖入追逐者

    private bool isOpen = false;
    private bool solved = false;

    void Start()
    {
        if (chaser == null)
            chaser = FindObjectOfType<Chaser>(true);

        passwordPanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
    }

    void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            ClosePanel();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            CheckPassword();
    }

    public void Interact()
    {
        if (solved) return;
        if (LoopManager.Instance.currentLoop < requiredLoop) return;
        OpenPanel();
    }

    void OpenPanel()
    {
        isOpen = true;
        passwordPanel.SetActive(true);
        inputField.text = "";
        feedbackText.text = "";
        inputField.ActivateInputField();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ClosePanel()
    {
        isOpen = false;
        passwordPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void CheckPassword()
    {
        if (inputField.text == correctPassword)
        {
            solved = true;
            feedbackText.text = "门已打开";

            if (doorToOpen != null)
                doorToOpen.SetActive(false);

            foreach (var activeChaser in FindObjectsOfType<Chaser>(true))
                activeChaser.StopChase();

            ClosePanel();
        }
        else
        {
            feedbackText.text = "......";
            feedbackText.color = Color.red;
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }
}
