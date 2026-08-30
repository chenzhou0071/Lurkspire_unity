using UnityEngine;
using UnityEngine.InputSystem;

// PlayerCamera — 第一人称相机：鼠标 yaw（旋转玩家）+ pitch（相机俯仰）+ 跑墙倾斜（身体+相机 roll）
// 挂载：Main Camera（作为玩家子物体，位置 = 头部）
// 说明：CharacterController 是轴对齐胶囊无法旋转——倾斜是视觉层（身体+相机），碰撞保持直立
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float sensitivity = 0.15f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private float fov = 80f; // 视场角（广角：默认 60 → 80）
    [SerializeField] private float nearClip = 0.1f; // 近裁剪面（小：贴墙不穿帮）
    [SerializeField] private Transform playerBody; // 玩家物体（yaw + 倾斜作用对象；不拖自动取父物体）
    private float _pitch;
    private float _yaw;
    private float _targetTilt; // 跑墙倾斜目标（PlayerInput 设置）

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked; // 锁鼠标（FPS 标准）
        var cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fov; // 广角视场
            cam.nearClipPlane = nearClip; // 近裁剪面（防贴墙看到墙后）
        }
        if (playerBody == null && transform.parent != null)
            playerBody = transform.parent;
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            var delta = mouse.delta.ReadValue();
            _yaw += delta.x * sensitivity; // 水平转向（累加）
            _pitch = Mathf.Clamp(_pitch - delta.y * sensitivity, -maxPitch, maxPitch); // 俯仰
        }
        // 跑墙倾斜平滑（从当前倾斜 Lerp 到目标）
        float tilt = Mathf.LerpAngle(GetCurrentTilt(), _targetTilt, 10f * Time.deltaTime);

        // 身体：yaw + 倾斜（相机作为子物体自动继承倾斜）
        if (playerBody != null)
            playerBody.localRotation = Quaternion.Euler(0f, _yaw, tilt);
        // 相机：只做 pitch（roll 由父物体继承）
        transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private float GetCurrentTilt()
    {
        if (playerBody == null) return 0f;
        return playerBody.localEulerAngles.z;
    }

    // 跑墙倾斜设置（PlayerInput 调用：±12°）
    public void SetTilt(float degrees) => _targetTilt = degrees;
}
