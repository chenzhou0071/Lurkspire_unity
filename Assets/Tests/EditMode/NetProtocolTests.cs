// NetProtocolTests — 协议对拍：golden 字面量来自 Go 仓库测试（跨语言一致性钉死）
// 通过 = C# 编出的字节与 Go 完全一致（联机不会错位）
using NUnit.Framework;
using System;

public class NetProtocolTests
{
    [Test]
    public void EncodeFrame_GoldenBytes()
    {
        // Go frame_test.go: MsgID=310 seq=7 body={01,02}
        var got = NetProtocol.EncodeFrame(310, 7, new byte[] { 0x01, 0x02 });
        var want = new byte[]
        {
            0x53, 0x44, 0x01, 0x36,
            0x00, 0x00, 0x00, 0x07,
            0x00, 0x00, 0x00, 0x02,
            0x01, 0x02,
        };
        CollectionAssert.AreEqual(want, got);
    }

    [Test]
    public void EncodeState_GoldenBytes()
    {
        // Go battle_test.go golden: 单玩家 uid=1 x=1 y=2 z=3 yaw=4 hp=100 weapon=0 alt=1 block=90 anim=0 score=7 deaths=3
        var s = new NetProtocol.PlayerState
        {
            UID = 1, X = 1, Y = 2, Z = 3, Yaw = 4,
            HP = 100, Weapon = 0, Alt = 1, Block = 90, Anim = 0,
            Score = 7, Deaths = 3,
        };
        var got = NetProtocol.EncodeState(new[] { s });
        var want = new byte[]
        {
            0x01,
            0x00, 0x00, 0x00, 0x01,
            0x3F, 0x80, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00,
            0x40, 0x40, 0x00, 0x00,
            0x40, 0x80, 0x00, 0x00,
            0x64, 0x00, 0x01,
            0x42, 0xB4, 0x00, 0x00,
            0x00,
            0x00, 0x07,
            0x00, 0x03,
            0x00, 0x00, 0x00, 0x00,
        };
        CollectionAssert.AreEqual(want, got);
    }

    [Test]
    public void Input_RoundTrip()
    {
        var r = new NetProtocol.InputReport
        {
            MoveX = 1, MoveY = -1, Yaw = 0.5f,
            Buttons = NetProtocol.BtnFire | NetProtocol.BtnSlide,
            AimX = 45, AimY = -30,
        };
        var back = NetProtocol.DecodeInput(NetProtocol.EncodeInput(r));
        Assert.AreEqual(r.MoveX, back.MoveX);
        Assert.AreEqual(r.MoveY, back.MoveY);
        Assert.AreEqual(r.Yaw, back.Yaw, 0.0001f);
        Assert.AreEqual(r.Buttons, back.Buttons);
        Assert.AreEqual(r.AimX, back.AimX, 0.0001f);
        Assert.AreEqual(r.AimY, back.AimY, 0.0001f);
    }

    [Test]
    public void Hit_RoundTrip()
    {
        var h = new NetProtocol.HitEvent { Shooter = 2, Target = 1, Damage = 50, Headshot = true };
        var back = NetProtocol.DecodeHit(NetProtocol.EncodeHit(h));
        Assert.AreEqual(h.Shooter, back.Shooter);
        Assert.AreEqual(h.Target, back.Target);
        Assert.AreEqual(h.Damage, back.Damage);
        Assert.AreEqual(h.Headshot, back.Headshot);
    }

    [Test]
    public void State_RoundTrip()
    {
        var states = new[]
        {
            new NetProtocol.PlayerState { UID = 1, X = 100.5f, Y = 2.25f, Z = 30, Yaw = 1.5f, HP = 100, Weapon = 0, Alt = 1, Block = 80, Anim = 3 },
            new NetProtocol.PlayerState { UID = 2, X = 50, Y = 0, Z = -10.5f, Yaw = -2, HP = 55, Weapon = 1, Alt = 0, Block = 100, Anim = 1 },
        };
        var back = NetProtocol.DecodeState(NetProtocol.EncodeState(states));
        Assert.AreEqual(2, back.Length);
        Assert.AreEqual(states[0].UID, back[0].UID);
        Assert.AreEqual(states[0].X, back[0].X, 0.001f);
        Assert.AreEqual(states[0].Block, back[0].Block, 0.001f);
        Assert.AreEqual(states[1].Z, back[1].Z, 0.001f);
        Assert.AreEqual(states[1].HP, back[1].HP);
    }

    [Test]
    public void Settle_RoundTrip()
    {
        var entries = new[]
        {
            new NetProtocol.SettleEntry { UID = 1, Score = 20 },
            new NetProtocol.SettleEntry { UID = 2, Score = 12 },
        };
        var body = NetProtocol.EncodeSettle(new[] { entries[0], entries[1] });
        var back = NetProtocol.DecodeSettle(body);
        Assert.AreEqual(2, back.Length);
        Assert.AreEqual(entries[0].UID, back[0].UID);
        Assert.AreEqual(entries[0].Score, back[0].Score);
    }

    [Test]
    public void JoinOK_Decode()
    {
        // 构造：房间名 "arena" + uid=7 + 2 玩家状态
        var body = new byte[] { 5 }; // "arena".Length
        var name = System.Text.Encoding.UTF8.GetBytes("arena");
        var all = new byte[1 + name.Length + 4];
        Array.Copy(body, all, 1);
        Array.Copy(name, 0, all, 1, name.Length);
        NetProtocol.PutU32(all, 1 + name.Length, 7);
        var states = NetProtocol.EncodeState(new[]
        {
            new NetProtocol.PlayerState { UID = 7, X = 1, Y = 0, Z = 2, HP = 100 },
            new NetProtocol.PlayerState { UID = 8, X = 3, Y = 0, Z = 4, HP = 100 },
        });
        var full = new byte[all.Length + states.Length];
        Array.Copy(all, full, all.Length);
        Array.Copy(states, 0, full, all.Length, states.Length);

        NetProtocol.DecodeJoinOK(full, out var roomName, out var selfUID, out var players);
        Assert.AreEqual("arena", roomName);
        Assert.AreEqual(7u, selfUID);
        Assert.AreEqual(2, players.Length);
        Assert.AreEqual(8u, players[1].UID);
    }
}
