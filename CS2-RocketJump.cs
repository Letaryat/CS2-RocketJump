using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace cs2_rocketjump;

public class cs2_rocketjump : BasePlugin
{
    public override string ModuleName => "CS2-Rocketjump";

    public override string ModuleVersion => "0.0.4";

    public override string ModuleAuthor => "Letaryat";
    public override string ModuleDescription => "Rocket jump yipee!";
    public override void Load(bool hotReload)
    {
        Console.WriteLine("CS2-Rocket Jump on!");
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        RegisterEventHandler<EventBulletImpact>(OnBulletImpact);
    }
    public override void Unload(bool hotReload)
    {
        Console.WriteLine("CS2-Rocket Jump off!");
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if (victim == null) return HookResult.Continue;
        if (attacker == null)
        {
            victim!.PlayerPawn!.Value!.Health += @event.DmgHealth;
            Utilities.SetStateChanged(victim!.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
        }
        return HookResult.Continue;
    }

    public Vector CalculateRJ(Vector playerPos, Vector bulletPos, float recoil)
    {
        var distanceVector = new Vector
        {
            X = playerPos.X - bulletPos.X,
            Y = playerPos.Y - bulletPos.Y,
            Z = playerPos.Z - bulletPos.Z
        };
        float distance = (float)Math.Sqrt(
            distanceVector.X * distanceVector.X +
            distanceVector.Y * distanceVector.Y +
            distanceVector.Z * distanceVector.Z
        );
        var normalizedDirection = new Vector
        {
            X = distanceVector.X / distance,
            Y = distanceVector.Y / distance,
            Z = distanceVector.Z / distance
        };
        return new Vector
        {
            X = normalizedDirection.X,
            Y = normalizedDirection.Y,
            Z = normalizedDirection.Z
        };
    }

    public Vector CalculateDistance(Vector playerPos, Vector bulletPos)
    {
        var distance = new Vector
        {
            X = playerPos.X - bulletPos.X,
            Y = playerPos.Y - bulletPos.Y,
            Z = playerPos.Z - bulletPos.Z
        };
        return distance;
    }

    public HookResult OnBulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsBot || player.IsHLTV || !player.PawnIsAlive) { return HookResult.Continue; }

        var playerPawn = player!.PlayerPawn.Value;
        var bullet = @event;

        var bulletPos = new Vector
        {
            X = bullet.X,
            Y = bullet.Y,
            Z = bullet.Z
        };

        var distance = CalculateDistance(playerPawn!.AbsOrigin!, bulletPos);

        if(distance.Z >= 300) return HookResult.Continue;

        /*  This part is yoinked: https://github.com/edgegamers/Jailbreak/blob/main/mod/Jailbreak.Warden/Paint/WardenPaintBehavior.cs#L131 */

        var eyeAngle = player!.PlayerPawn!.Value!.EyeAngles;
        var pitch = Math.PI / 180 * eyeAngle.X;
        var yaw = Math.PI / 180 * eyeAngle.Y;
        var eyeVector = new Vector((float)(Math.Cos(yaw) * Math.Cos(pitch)), (float)(Math.Sin(yaw) * Math.Cos(pitch)), (float)-Math.Sin(pitch));
        /* ---------------------- */
        if (eyeVector.Z < -0.85)
        {
            var playerWeapon = playerPawn!.WeaponServices!.ActiveWeapon.Value;
            CBasePlayerWeapon weapon = playerWeapon!;
            CCSWeaponBase _weapon = weapon.As<CCSWeaponBase>();
            /* dumb stuff that i was playing with */
            //_weapon.VData!.ZoomLevels = 0;
            //_weapon.VData.Penetration = 0;
            _weapon.VData!.InaccuracyJump.Values[0] = 0;
            _weapon.VData.InaccuracyJump.Values[1] = 0;
            //_weapon.VData.IsFullAuto = true;
            /* */

            var recoil = _weapon.VData!.RecoilMagnitude.Values[0];
            Vector knockback = CalculateRJ(playerPawn.AbsOrigin!, bulletPos, recoil);

            /* exkludera particles */

            var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system")!;
            particle.EffectName = "particles/explosions_fx/explosion_basic.vpcf";
            particle.DispatchSpawn();
            particle.AcceptInput("Start");

            particle.Teleport(bulletPos);

            /* ---------------------- */
            
            player.PlayerPawn.Value.AbsVelocity.X += knockback.X * (recoil * 10);
            player.PlayerPawn.Value.AbsVelocity.Y += knockback.Y * (recoil * 10);
            player.PlayerPawn.Value.AbsVelocity.Z += knockback.Z * (recoil * 10);
        }

        return HookResult.Continue;
    }
}
