using UnityEngine;
using UnityEngine.InputSystem;

// PlayerInput — 输入采集 + 角色驱动（新 Input System：Keyboard.current）
// 挂载：玩家物体（需 CharacterController）
[RequireComponent(typeof(CharacterController))]
public class PlayerInput : MonoBehaviour
{
    [SerializeField] private float gravity = -20f;
    private CharacterController _cc;
    private PlayerMotor _motor;
    private Vector3 _velocity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _motor = new PlayerMotor();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        Vector2 move = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed) move.y += 1;
            if (kb.sKey.isPressed) move.y -= 1;
            if (kb.aKey.isPressed) move.x -= 1;
            if (kb.dKey.isPressed) move.x += 1;
        }

        // 滑铲：C 键【点按】启动（0.8 秒自动结束，不用按住；Shift 留给武士刀冲刺斩）
        if (kb != null && kb.cKey.wasPressedThisFrame)
            _motor.SetSlide(true);
        _motor.TickSlide(Time.deltaTime);
        // 碰撞体压扁（CharacterController 高度不受 scale 影响，需独立设置）
        _cc.height = Mathf.Lerp(_cc.height, _motor.IsSliding ? 0.6f : 2f, 12f * Time.deltaTime);
        _cc.center = new Vector3(0, _cc.height / 2f, 0);
        // 视觉压扁（缩放根物体——原型阶段不做底部补偿，换正式建模后按模型调）
        float targetScaleY = _motor.IsSliding ? 0.7f : 1f;
        float sy = Mathf.Lerp(transform.localScale.y, targetScaleY, 16f * Time.deltaTime);
        transform.localScale = new Vector3(1f, sy, 1f);

        float speed = PlayerMotor.ComputeSpeed(false, _motor.IsSliding);
        Vector3 dir = (transform.right * move.x + transform.forward * move.y).normalized;
        _velocity.x = dir.x * speed;
        _velocity.z = dir.z * speed;
        _velocity.y += gravity * Time.deltaTime;

        // 跳（空格，落地才可跳）
        if (kb != null && kb.spaceKey.wasPressedThisFrame && _cc.isGrounded)
            _velocity.y = Mathf.Sqrt(GameConfig.JumpHeight * -2f * gravity);

        _cc.Move(_velocity * Time.deltaTime);
    }
}
