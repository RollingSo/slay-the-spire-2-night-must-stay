using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Changes the background/frame beneath the original Layer2 water and native label.</summary>
public partial class WylderPursuitEnergyVisual : Control
{
    public Player Player { get; set; }
    private TextureRect _baseLayer;
    private Texture2D _normalTexture;
    private Texture2D _pursuitTexture;
    private TextureRect _waterLayer;
    private CanvasItem[] _normalDetails;
    private bool[] _normalVisibility;
    private bool? _active;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Control layers = GetParent().GetNode<Control>("Layers");
        _baseLayer = layers.GetNode<TextureRect>("Layer1");
        _normalTexture = _baseLayer.Texture;
        _pursuitTexture = PreloadManager.Cache.GetTexture2D(
            "res://wylder_assets/energy_counter/pursuit_orb.png");
        _waterLayer = new TextureRect {
            Name = "PursuitWater", MouseFilter = MouseFilterEnum.Ignore,
            Texture = PreloadManager.Cache.GetTexture2D("res://wylder_assets/energy_counter/pursuit_water.png"),
            Material = layers.GetNode<TextureRect>("RotationLayers/Layer2").Material,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Visible = false
        };
        layers.AddChild(_waterLayer);
        layers.MoveChild(_waterLayer, _baseLayer.GetIndex() + 1);
        _waterLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _normalDetails = new CanvasItem[] {
            // Original water crosses Layer1 and Layer2 alpha masks. The complete
            // extracted water is drawn above the new background in pursuit.
            layers.GetNode<CanvasItem>("RotationLayers"),
            layers.GetNode<CanvasItem>("Layer4"),
            layers.GetNode<CanvasItem>("Layer5") };
        _normalVisibility = new bool[_normalDetails.Length];
        UpdateState();
    }

    public override void _Process(double delta) => UpdateState();

    private void UpdateState()
    {
        bool active = WylderPursuitPower.IsActive(Player?.Creature);
        if (_active == active) return;
        _waterLayer.Visible = active;
        if (active)
        {
            for (int i = 0; i < _normalDetails.Length; i++)
            {
                _normalVisibility[i] = _normalDetails[i].Visible;
                _normalDetails[i].Visible = false;
            }
            _baseLayer.Texture = _pursuitTexture;
        }
        else if (_active == true)
        {
            _baseLayer.Texture = _normalTexture;
            for (int i = 0; i < _normalDetails.Length; i++)
                _normalDetails[i].Visible = _normalVisibility[i];
        }
        _active = active;
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_waterLayer)) _waterLayer.QueueFree();
        if (_active != true || !GodotObject.IsInstanceValid(_baseLayer)) return;
        _baseLayer.Texture = _normalTexture;
        for (int i = 0; i < _normalDetails.Length; i++)
            if (GodotObject.IsInstanceValid(_normalDetails[i]))
                _normalDetails[i].Visible = _normalVisibility[i];
    }
}
