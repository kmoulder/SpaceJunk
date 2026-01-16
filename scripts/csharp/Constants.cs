using Godot;

// SpaceFactory

/// <summary>
/// Global game constants for easy balancing and configuration.
/// </summary>
public static class Constants
{
    // Grid and tiles
    public const int TileSize = 32;
    public const int StartingStationSize = 9; // 9x9 starting grid

    // Player inventory
    public const int PlayerInventorySlots = 40;
    public const int HotbarSlots = 10;
    public const int DefaultStackSize = 50;

    // Debris spawning
    public const float DebrisBaseSpawnRate = 3.0f;
    public const float DebrisSpawnVariance = 1.0f;
    public const float DebrisMinSpeed = 20.0f;
    public const float DebrisMaxSpeed = 50.0f;
    public const float DebrisSpawnMargin = 100.0f;
    public const float DespawnMargin = 200.0f;
    public const float DebrisClickRadius = 24.0f;

    // Buildings
    public const float FurnaceStoneSpeed = 1.0f;
    public const float FurnaceElectricSpeed = 2.0f;
    public const float AssemblerMk1Speed = 0.5f;
    public const float AssemblerMk2Speed = 0.75f;
    public const float AssemblerMk3Speed = 1.25f;

    // Transport
    public const float BeltSpeedTier1 = 1.0f;
    public const float BeltSpeedTier2 = 2.0f;
    public const float BeltSpeedTier3 = 3.0f;
    public const float InserterSwingTime = 0.8f;
    public const float FastInserterSwingTime = 0.4f;

    // Power
    public const float SolarPanelOutput = 60.0f;
    public const float AccumulatorCapacity = 5000.0f;

    // Camera
    public const float CameraPanSpeed = 400.0f;
    public const float CameraZoomSpeed = 0.1f;
    public const float CameraZoomMin = 0.5f;
    public const float CameraZoomMax = 2.0f;
    public const float CameraZoomStep = 0.1f;

    // UI Colors
    public static readonly Color UiBackground = new(0.1f, 0.1f, 0.15f, 0.95f);
    public static readonly Color UiBorder = new(0.3f, 0.35f, 0.4f, 1.0f);
    public static readonly Color UiHighlight = new(0.4f, 0.6f, 0.8f, 1.0f);
    public static readonly Color UiText = new(0.9f, 0.9f, 0.9f, 1.0f);
    public static readonly Color UiTextDim = new(0.6f, 0.6f, 0.6f, 1.0f);

    // Z-ordering
    public const int ZBackground = -100;
    public const int ZDebris = -10;
    public const int ZFoundation = 0;
    public const int ZBelts = 1;
    public const int ZBuildings = 2;
    public const int ZItems = 3;
    public const int ZInserters = 4;
    public const int ZGhost = 10;

    // Ghost alpha
    public const float BuildingGhostAlpha = 0.6f;

    // Game tick rate
    public const float TickRate = 60.0f;
}
