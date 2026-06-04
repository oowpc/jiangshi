using UnityEngine;
using TMPro;

/// <summary>
/// 第一人称控制器：移动 + 鼠标视角 + 简单交互
/// 挂在Player物体上，Player需要有CharacterController组件
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 4.5f;

    [Header("视角")]
    public float mouseSensitivity = 2f;
    public float maxLookUp = 80f;
    public float maxLookDown = -80f;

    [Header("交互")]
    public float interactDistance = 2f;
    public LayerMask interactLayer;
    public TextMeshProUGUI interactHint;   // 拖入一个UI文字用于显示"按E拾取"

    [Header("视角抖动")]
    public float bobFrequency = 8f;
    public float bobAmountY = 0.03f;
    public float bobAmountX = 0.015f;

    private CharacterController controller;
    private Camera cam;
    private float rotationX = 0f;
    private float bobTimer = 0f;
    private Vector3 camOriginalPos;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        camOriginalPos = cam.transform.localPosition;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleHeadBob();
        HandleInteract();

        // ESC解锁鼠标（方便调试）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 move = transform.right * x + transform.forward * z;
        move.y = -2f;
        controller.Move(move * speed * Time.deltaTime);
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, maxLookDown, maxLookUp);

        cam.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleInteract()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        bool looking = Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer);

        // 显示/隐藏交互提示
        if (interactHint != null)
        {
            if (looking && hit.collider.GetComponent<IInteractable>() != null)
            {
                interactHint.gameObject.SetActive(true);
                interactHint.text = "按 E 交互";
            }
            else
            {
                interactHint.gameObject.SetActive(false);
            }
        }

        // 按E交互
        if (Input.GetKeyDown(KeyCode.E) && looking)
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
                interactable.Interact();
        }
    }

    void HandleHeadBob()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobY = Mathf.Sin(bobTimer) * bobAmountY;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobAmountX;
            cam.transform.localPosition = camOriginalPos + new Vector3(bobX, bobY, 0);
        }
        else
        {
            bobTimer = 0f;
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, camOriginalPos, Time.deltaTime * 5f);
        }
    }
}

/// <summary>
/// 可交互物体接口，任何可以按E交互的物体实现这个接口
/// </summary>
public interface IInteractable
{
    void Interact();
}
