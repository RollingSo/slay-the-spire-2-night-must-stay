namespace NightMustStay.Core.Nodes.Vfx;

// Offline visual harness never plays game audio.
public static class DuchessAudio
{
 public readonly record struct Cue(float At,string File,float Volume);
 public static Cue[] AttackCues(DuchessAttackVfx.Kind kind)=>System.Array.Empty<Cue>();
 public static void Play(string file,float volume,bool combat=true) { }
}
