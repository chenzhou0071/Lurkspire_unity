using UnityEngine;
using UnityEngine.InputSystem;

// PlayerInput — 输入采集（只读）：WASD 移动/空格跳/C 滑铲（点按）/W 按住
// 只做"读键盘 → 填属性"，一切逻辑在 PlayerController
// DefaultExecutionOrder(-100)：强制本帧最先执行（PlayerController 才能读到本帧点按）
// Awake 自动挂载 PlayerController（场景零操作——找不到就补）
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(-100)]
public class PlayerInput : MonoBehaviour
{
    public Vector2 Move { get; private set; }        // WASD 方向
    public bool JumpPressed { get; private set; }    // 空格（点按）
    public bool SlidePressed { get; private set; }   // C（点按）
    public bool WPressed { get; private set; }       // W（按住）

    private void Awake()
    {
        if (GetComponent<PlayerController>() == null)
            gameObject.AddComponent<PlayerController>(); // 自动挂协调器（防漏挂）
    }

    private void Update()
    {
        Move = Vector2.zero;
        JumpPressed = false;
        SlidePressed = false;
        WPressed = false;
        var kb = Keyboard.current;
        if (kb == null) return;
        Vector2 move = Vector2.zero;
        if (kb.wKey.isPressed) { move.y += 1; WPressed = true; }
        if (kb.sKey.isPressed) move.y -= 1;
        if (kb.aKey.isPressed) move.x -= 1;
        if (kb.dKey.isPressed) move.x += 1;
        Move = move;
        if (kb.spaceKey.wasPressedThisFrame) JumpPressed = true;
        if (kb.cKey.wasPressedThisFrame) SlidePressed = true;
    }
}
