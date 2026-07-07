using GFrameworkGodotTemplate.scripts.card;
using Godot;

namespace GFrameworkGodotTemplate.scripts.tests;

public partial class CardPhysicsTest : Node3D
{
    private const float MaxDragPixels = 320.0f;
    private const float ImpulseScale = 0.018f;
    private const float MaxImpulse = 7.5f;
    private const float SpinScale = 0.05f;

    [Export] public Card? PlayerCard { get; set; }
    [Export] public Card? TargetCard { get; set; }

    private readonly Transform3D _playerStart = new(Basis.Identity, new Vector3(-2.5f, 0.12f, 0.0f));
    private readonly Transform3D _targetStart = new(Basis.Identity, new Vector3(2.5f, 0.12f, 0.0f));

    private Camera3D? _camera;
    private Vector2 _dragStart;
    private bool _isDragging;

    public override void _Ready()
    {
        PlayerCard ??= GetNodeOrNull<Card>("PlayerCard");
        TargetCard ??= GetNodeOrNull<Card>("TargetCard");
        _camera = GetNodeOrNull<Camera3D>("Camera3D");

        ApplyPlaceholderTextures();
        ResetCards();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.R })
        {
            ResetCards();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton)
        {
            if (mouseButton.Pressed)
            {
                _dragStart = mouseButton.Position;
                _isDragging = true;
            }
            else if (_isDragging)
            {
                LaunchFromDrag(mouseButton.Position);
                _isDragging = false;
            }

            GetViewport().SetInputAsHandled();
        }
    }

    private void ApplyPlaceholderTextures()
    {
        var front = GD.Load<Texture2D>("res://assets/art/textures/card/正面.png");
        var back = GD.Load<Texture2D>("res://assets/art/textures/card/背面.png");

        PlayerCard?.SetCardTextures(front, back);
        TargetCard?.SetCardTextures(front, back);
    }

    private void LaunchFromDrag(Vector2 dragEnd)
    {
        if (PlayerCard == null || _camera == null) return;

        var drag = _dragStart - dragEnd;
        var clampedDrag = drag.LimitLength(MaxDragPixels);
        if (clampedDrag.LengthSquared() < 16.0f) return;

        var cameraRight = _camera.GlobalTransform.Basis.X;
        var cameraForward = -_camera.GlobalTransform.Basis.Z;
        cameraRight.Y = 0.0f;
        cameraForward.Y = 0.0f;
        cameraRight = cameraRight.Normalized();
        cameraForward = cameraForward.Normalized();

        var direction = (cameraRight * clampedDrag.X + cameraForward * clampedDrag.Y).Normalized();
        var strength = Mathf.Min(clampedDrag.Length() * ImpulseScale, MaxImpulse);
        var impulse = direction * strength;
        var torque = Vector3.Up * clampedDrag.X * SpinScale;

        PlayerCard.Launch(impulse, torque);
    }

    private void ResetCards()
    {
        PlayerCard?.ResetMotion(_playerStart);
        TargetCard?.ResetMotion(_targetStart);
    }
}
