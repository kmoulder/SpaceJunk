using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// DebrisEntity - A piece of floating space debris that can be collected.
/// Drifts across the screen and can be clicked to collect resources.
/// </summary>
public partial class DebrisEntity : Area2D
{
    /// <summary>
    /// Type of debris (iron_asteroid, copper_asteroid, etc.)
    /// </summary>
    public string DebrisType { get; private set; } = "";

    /// <summary>
    /// Drift velocity in pixels per second
    /// </summary>
    public Vector2 DriftVelocity { get; private set; } = Vector2.Zero;

    /// <summary>
    /// Contents when collected (array of {item_id, count})
    /// </summary>
    private Array _contents = new();

    /// <summary>
    /// Sprite reference
    /// </summary>
    private Sprite2D _sprite;

    public override void _Ready()
    {
        // Set up collision
        CollisionLayer = 2; // Debris layer
        CollisionMask = 0;
        InputPickable = true;
    }

    /// <summary>
    /// Initialize the debris with type and contents
    /// </summary>
    public void Initialize(string type, Array itemContents, int variationSeed = 0)
    {
        DebrisType = type;
        _contents = itemContents;

        // Create collision shape if not exists
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
        {
            var collision = new CollisionShape2D();
            var shape = new CircleShape2D { Radius = Constants.DebrisClickRadius };
            collision.Shape = shape;
            AddChild(collision);
        }

        // Create sprite if not exists
        if (_sprite == null)
        {
            _sprite = new Sprite2D();
            AddChild(_sprite);
        }

        _sprite.Texture = SpriteGenerator.Instance?.GenerateDebris(DebrisType, variationSeed);

        // Set z-index
        ZIndex = Constants.ZDebris;
    }

    /// <summary>
    /// Set the drift velocity
    /// </summary>
    public void SetDriftVelocity(Vector2 velocity)
    {
        DriftVelocity = velocity;
    }

    /// <summary>
    /// Update movement - called by DebrisManager
    /// </summary>
    public void UpdateMovement(float delta)
    {
        GlobalPosition += DriftVelocity * delta;
    }

    /// <summary>
    /// Get the debris contents
    /// </summary>
    public Array GetContents()
    {
        return _contents;
    }

    /// <summary>
    /// Get the debris type
    /// </summary>
    public string GetDebrisType()
    {
        return DebrisType;
    }
}
