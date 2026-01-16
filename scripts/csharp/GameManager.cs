using Godot;

// SpaceFactory

/// <summary>
/// GameManager - Core game state controller (Autoload singleton).
/// Manages game state, tick system, and global game operations.
/// </summary>
public partial class GameManager : Node
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static GameManager Instance { get; private set; }

    // Signals
    [Signal]
    public delegate void GameTickEventHandler(int tick);

    [Signal]
    public delegate void GameStateChangedEventHandler(Enums.GameState newState, Enums.GameState oldState);

    [Signal]
    public delegate void GameSpeedChangedEventHandler(float newSpeed);

    [Signal]
    public delegate void GameStartedEventHandler();

    [Signal]
    public delegate void GamePausedEventHandler();

    [Signal]
    public delegate void GameResumedEventHandler();

    /// <summary>
    /// Current game state
    /// </summary>
    public Enums.GameState CurrentState { get; private set; } = Enums.GameState.MainMenu;

    /// <summary>
    /// Current game speed multiplier
    /// </summary>
    public float GameSpeed { get; private set; } = 1.0f;

    /// <summary>
    /// Current tick count since game start
    /// </summary>
    public int CurrentTick { get; private set; } = 0;

    /// <summary>
    /// Accumulator for tick timing
    /// </summary>
    private float _tickAccumulator = 0.0f;

    /// <summary>
    /// Time per tick in seconds
    /// </summary>
    private const float TickTime = 1.0f / Constants.TickRate;

    public override void _EnterTree()
    {
        GD.Print("[GameManager] _EnterTree called");
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("[GameManager] _Ready called - Instance set up complete");
    }

    public override void _Process(double delta)
    {
        if (CurrentState != Enums.GameState.Playing)
            return;

        // Accumulate time and emit ticks
        _tickAccumulator += (float)delta * GameSpeed;

        while (_tickAccumulator >= TickTime)
        {
            _tickAccumulator -= TickTime;
            CurrentTick++;
            EmitSignal(SignalName.GameTick, CurrentTick);
        }
    }

    /// <summary>
    /// Start a new game
    /// </summary>
    public void StartNewGame()
    {
        CurrentTick = 0;
        _tickAccumulator = 0.0f;
        GameSpeed = 1.0f;

        SetGameState(Enums.GameState.Playing);
        EmitSignal(SignalName.GameStarted);
    }

    /// <summary>
    /// Set the current game state
    /// </summary>
    public void SetGameState(Enums.GameState newState)
    {
        if (newState == CurrentState)
            return;

        var oldState = CurrentState;
        CurrentState = newState;

        EmitSignal(SignalName.GameStateChanged, (int)newState, (int)oldState);

        // Handle pause/resume signals
        if (newState == Enums.GameState.Paused && oldState == Enums.GameState.Playing)
            EmitSignal(SignalName.GamePaused);
        else if (newState == Enums.GameState.Playing && oldState == Enums.GameState.Paused)
            EmitSignal(SignalName.GameResumed);
    }

    /// <summary>
    /// Set game speed multiplier
    /// </summary>
    public void SetGameSpeed(float speed)
    {
        GameSpeed = Mathf.Clamp(speed, 0.0f, 10.0f);
        EmitSignal(SignalName.GameSpeedChanged, GameSpeed);
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState == Enums.GameState.Playing)
            SetGameState(Enums.GameState.Paused);
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentState == Enums.GameState.Paused)
            SetGameState(Enums.GameState.Playing);
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (CurrentState == Enums.GameState.Playing)
            PauseGame();
        else if (CurrentState == Enums.GameState.Paused)
            ResumeGame();
    }

    /// <summary>
    /// Check if game is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return CurrentState == Enums.GameState.Playing;
    }

    /// <summary>
    /// Check if game is paused
    /// </summary>
    public bool IsPaused()
    {
        return CurrentState == Enums.GameState.Paused;
    }

    /// <summary>
    /// Get elapsed game time in seconds
    /// </summary>
    public float GetElapsedTime()
    {
        return CurrentTick / Constants.TickRate;
    }
}
