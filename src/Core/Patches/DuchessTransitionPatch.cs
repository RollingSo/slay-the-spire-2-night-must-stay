using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.TestSupport;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Patches;

// Retain native cancellation, input blocking and Instant-mode behavior.
[HarmonyPatch]
public static class DuchessTransitionPatch
{
    private const string ClockPath = "res://materials/transitions/duchess_transition_mat.tres";
    private const string DefaultPath = "res://materials/transitions/fade_transition_mat.tres";

    [HarmonyPatch(typeof(NTransition), nameof(NTransition.FadeOut))]
    [HarmonyPrefix]
    public static void Cover(NTransition __instance, float time, string transitionPath)
    {
        if (__instance.Material is ShaderMaterial current && IsClock(current))
        {
            current.SetShaderParameter("revealing", false);
            PlayClockSound(time, "map_split_tick.mp3", .2f);
        }
        else if (transitionPath == ClockPath)
            PlayClockSound(time, "map_split_tick.mp3", .2f);
    }

    [HarmonyPatch(typeof(NTransition), nameof(NTransition.FadeIn))]
    [HarmonyPrefix]
    public static void Reveal(NTransition __instance, ref string transitionPath, float time)
    {
        if (__instance.Material is not ShaderMaterial current || !IsClock(current))
            return;
        if (transitionPath != DefaultPath && transitionPath != ClockPath)
            return;
        transitionPath = ClockPath;
        current.SetShaderParameter("revealing", true);
        PlayClockSound(time, "glass_orb_passive.mp3", .25f);
    }

    private static void PlayClockSound(float time, string file, float volume)
    {
        if (!TestMode.IsOn && time > 0
            && SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant)
            DuchessAudio.Play(file, volume, combat: false);
    }

    private static bool IsClock(ShaderMaterial material) =>
        material.Shader?.ResourcePath == "res://shaders/duchess_clock_transition.gdshader";
}
