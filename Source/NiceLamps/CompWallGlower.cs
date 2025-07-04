using System.Collections.Generic;
using RimWorld;
using Verse;

public class CompWallGlower : ThingComp
{
    private Thing glower;

    private bool glowOnInt;

    private IntVec3 glowPos;

    private CompProperties_WallGlower Props => (CompProperties_WallGlower)props;

    private bool ShouldBeLitNow
    {
        get
        {
            if (!parent.Spawned)
            {
                return false;
            }

            if (!FlickUtility.WantsToBeOn(parent))
            {
                return false;
            }

            var compPowerTrader = parent.TryGetComp<CompPowerTrader>();
            if (compPowerTrader is { PowerOn: false })
            {
                return false;
            }

            var compRefuelable = parent.TryGetComp<CompRefuelable>();
            if (compRefuelable is { HasFuel: false })
            {
                return false;
            }

            var compSendSignalOnCountdown = parent.TryGetComp<CompSendSignalOnCountdown>();
            if (compSendSignalOnCountdown is { ticksLeft: <= 0 })
            {
                return false;
            }

            var val = parent.TryGetComp<CompSendSignalOnMotion>();
            if (val is { Sent: true })
            {
                return false;
            }

            var compLoudspeaker = parent.TryGetComp<CompLoudspeaker>();
            if (compLoudspeaker is { Active: false })
            {
                return false;
            }

            var compHackable = parent.TryGetComp<CompHackable>();
            if (compHackable is { IsHacked: true } && !compHackable.Props.glowIfHacked)
            {
                return false;
            }

            var compRitualSignalSender = parent.TryGetComp<CompRitualSignalSender>();
            if (compRitualSignalSender is { ritualTarget: false })
            {
                return false;
            }

            return parent is not Building_Crate { HasAnyContents: false };
        }
    }

    private void updateLit()
    {
        var shouldBeLitNow = ShouldBeLitNow;
        if (glowOnInt == shouldBeLitNow)
        {
            return;
        }

        glowOnInt = shouldBeLitNow;
        if (!glowOnInt)
        {
            despawnGlower();
        }
        else
        {
            spawnGlower();
        }
    }

    private void spawnGlower()
    {
        glowPos = parent.Position + IntVec3.South.RotatedBy(parent.Rotation);
        glower = ThingMaker.MakeThing(ThingDef.Named(Props.glowerDefName));
        GenSpawn.Spawn(glower, glowPos, parent.Map, parent.Rotation);
    }

    private void despawnGlower()
    {
        if (glower is not { Spawned: true })
        {
            return;
        }

        glower.DeSpawn();
        glower = null;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }

        if (glower == null)
        {
            yield break;
        }

        foreach (var gizmo in glower.TryGetComp<CompGlower>().CompGetGizmosExtra())
        {
            yield return gizmo;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        updateLit();
    }

    public override void ReceiveCompSignal(string signal)
    {
        switch (signal)
        {
            default:
                if (signal != "CrateContentsChanged")
                {
                    break;
                }

                goto case "PowerTurnedOn";
            case "PowerTurnedOn":
            case "PowerTurnedOff":
            case "FlickedOn":
            case "FlickedOff":
            case "Refueled":
            case "RanOutOfFuel":
            case "ScheduledOn":
            case "ScheduledOff":
            case "MechClusterDefeated":
            case "Hackend":
            case "RitualTargetChanged":
                updateLit();
                break;
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        updateLit();
    }
}