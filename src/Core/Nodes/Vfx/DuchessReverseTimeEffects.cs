using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

public static class DuchessReverseTimeEffects
{
    public static async Task PlayPrelude()
    {
        if(TestMode.IsOn || NCombatRoom.Instance == null) return;
        // Viewport-space overlay covers the whole battlefield at every aspect ratio.
        // CanvasLayer and Node2D do not capture input or change creature transforms.
        var layer=new CanvasLayer { Name="DuchessReverseTime", Layer=10 };
        NCombatRoom.Instance.AddChildSafely(layer);
        layer.AddChild(new DuchessReverseTimeVfx { CleanupLayer=true, SoundEnabled=true });
        // Grand-Finale-length anticipation, then the existing snapshot restoration.
        await Cmd.Wait(DuchessReverseTimeVfx.RewindCue,false);
    }
}
