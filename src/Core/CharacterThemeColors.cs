using Godot;

namespace NightMustStay.Core;

/// <summary>Shared deck and multiplayer drawing colors; match frame_mid in each card material.</summary>
public static class CharacterThemeColors
{
    public static Color Guardian => new("6F4A2F");
    public static Color Ironeye => new("68734A");
    public static Color Revenant => new("5C7689");
    // Deep navy sampled from Wylder's approved split coat; shared by frame/map/deck.
    public static Color Wylder => new("252E4A");
    public static Color Duchess => new("48B9ED");
}
