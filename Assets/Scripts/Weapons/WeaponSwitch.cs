using UnityEngine;
using UnityEngine.InputSystem;

// WeaponSwitch — 武器切换：1=双枪 2=武士刀（切换组件启用）
// 挂载：玩家物体；GunView/SwordView 同物体自动找
public class WeaponSwitch : MonoBehaviour
{
    private GunView _gunView;
    private SwordView _swordView;
    private int _current = -1; // 初始 -1：确保开局 SetWeapon(1) 强制执行显示逻辑

    private void Awake()
    {
        _gunView = GetComponent<GunView>();
        _swordView = GetComponent<SwordView>();
        SetWeapon(1); // 开局双枪
    }

    private void Update()
    {
        // 后台充能/冷却：切刀后 GunView 禁用，充能仍每帧累计；切枪后 SwordView 禁用，冲刺冷却仍恢复
        if (_gunView != null) _gunView.TickCharge(Time.deltaTime);
        if (_swordView != null) _swordView.TickCooldowns(Time.deltaTime);
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        // 数字键切换
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) SetWeapon(1);
            else if (kb.digit2Key.wasPressedThisFrame) SetWeapon(2);
        }
        // 滚轮切换：向上=枪 向下=刀
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f) SetWeapon(1);
            else if (scroll < 0f) SetWeapon(2);
        }
    }

    private void SetWeapon(int w)
    {
        if (w == _current) return;
        _current = w;
        if (_gunView != null)
        {
            _gunView.enabled = w == 1;
            _gunView.SetMuzzlesVisible(w == 1); // 枪口显隐（相机子物体不受组件 enabled 影响）
        }
        if (_swordView != null)
        {
            _swordView.enabled = w == 2;
            _swordView.SetBladeVisible(w == 2); // 刀显隐
        }
    }
}
