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
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) SetWeapon(1);
        else if (kb.digit2Key.wasPressedThisFrame) SetWeapon(2);
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
