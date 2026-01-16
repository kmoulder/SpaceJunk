using Godot;
using Godot.Collections;

// SpaceFactory

/// <summary>
/// PowerManager - Handles power production, consumption, and distribution (Autoload singleton).
/// </summary>
public partial class PowerManager : Node
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static PowerManager Instance { get; private set; }

    // Signals
    [Signal]
    public delegate void PowerChangedEventHandler(float production, float consumption);

    [Signal]
    public delegate void BrownoutStartedEventHandler();

    [Signal]
    public delegate void BrownoutEndedEventHandler();

    [Signal]
    public delegate void ProducerRegisteredEventHandler(Node2D building, float output);

    [Signal]
    public delegate void ConsumerRegisteredEventHandler(Node2D building, float input);

    /// <summary>
    /// Producer buildings -> power output (kW)
    /// </summary>
    private readonly Dictionary<Node2D, float> _producers = new();

    /// <summary>
    /// Consumer buildings -> power consumption (kW)
    /// </summary>
    private readonly Dictionary<Node2D, float> _consumers = new();

    /// <summary>
    /// Total power production (kW)
    /// </summary>
    public float TotalProduction { get; private set; } = 0.0f;

    /// <summary>
    /// Total power consumption (kW)
    /// </summary>
    public float TotalConsumption { get; private set; } = 0.0f;

    /// <summary>
    /// Power satisfaction ratio (0.0 to 1.0)
    /// </summary>
    public float Satisfaction { get; private set; } = 1.0f;

    /// <summary>
    /// Whether we're currently in brownout
    /// </summary>
    public bool IsBrownout { get; private set; } = false;

    /// <summary>
    /// Stored energy (for accumulators) in kJ
    /// </summary>
    public float StoredEnergy { get; private set; } = 0.0f;

    /// <summary>
    /// Maximum energy storage capacity in kJ
    /// </summary>
    public float StorageCapacity { get; private set; } = 0.0f;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameTick += OnGameTick;
        }
    }

    private void OnGameTick(int tick)
    {
        UpdatePowerNetwork();
    }

    private void UpdatePowerNetwork()
    {
        // Calculate totals
        float newProduction = 0.0f;
        float newConsumption = 0.0f;

        // Clean up invalid producers
        var invalidProducers = new Array<Node2D>();
        foreach (var kvp in _producers)
        {
            if (IsInstanceValid(kvp.Key))
                newProduction += kvp.Value;
            else
                invalidProducers.Add(kvp.Key);
        }
        foreach (var invalid in invalidProducers)
            _producers.Remove(invalid);

        // Clean up invalid consumers
        var invalidConsumers = new Array<Node2D>();
        foreach (var kvp in _consumers)
        {
            if (IsInstanceValid(kvp.Key))
                newConsumption += kvp.Value;
            else
                invalidConsumers.Add(kvp.Key);
        }
        foreach (var invalid in invalidConsumers)
            _consumers.Remove(invalid);

        // Check if values changed
        bool changed = newProduction != TotalProduction || newConsumption != TotalConsumption;
        TotalProduction = newProduction;
        TotalConsumption = newConsumption;

        // Calculate satisfaction
        float oldSatisfaction = Satisfaction;
        if (TotalConsumption <= 0)
        {
            Satisfaction = 1.0f;
        }
        else if (TotalProduction >= TotalConsumption)
        {
            Satisfaction = 1.0f;
            // Charge accumulators with excess
            float excess = TotalProduction - TotalConsumption;
            ChargeStorage(excess / 60.0f); // Per tick (60 ticks/sec)
        }
        else
        {
            // Try to draw from storage
            float deficit = TotalConsumption - TotalProduction;
            float fromStorage = DischargeStorage(deficit / 60.0f);
            float effectiveProduction = TotalProduction + fromStorage * 60.0f;

            Satisfaction = TotalConsumption > 0
                ? Mathf.Clamp(effectiveProduction / TotalConsumption, 0.0f, 1.0f)
                : 1.0f;
        }

        // Check brownout state
        bool wasBrownout = IsBrownout;
        IsBrownout = Satisfaction < 1.0f;

        if (IsBrownout && !wasBrownout)
            EmitSignal(SignalName.BrownoutStarted);
        else if (!IsBrownout && wasBrownout)
            EmitSignal(SignalName.BrownoutEnded);

        if (changed)
            EmitSignal(SignalName.PowerChanged, TotalProduction, TotalConsumption);
    }

    private void ChargeStorage(float amountKj)
    {
        if (StorageCapacity <= 0)
            return;
        StoredEnergy = Mathf.Min(StoredEnergy + amountKj, StorageCapacity);
    }

    private float DischargeStorage(float amountKj)
    {
        if (StoredEnergy <= 0)
            return 0.0f;
        float discharged = Mathf.Min(amountKj, StoredEnergy);
        StoredEnergy -= discharged;
        return discharged;
    }

    /// <summary>
    /// Register a power producer
    /// </summary>
    public void RegisterProducer(Node2D building, float outputKw)
    {
        if (building == null || outputKw <= 0)
            return;

        _producers[building] = outputKw;
        EmitSignal(SignalName.ProducerRegistered, building, outputKw);
        UpdatePowerNetwork();
    }

    /// <summary>
    /// Unregister a power producer
    /// </summary>
    public void UnregisterProducer(Node2D building)
    {
        if (_producers.ContainsKey(building))
        {
            _producers.Remove(building);
            UpdatePowerNetwork();
        }
    }

    /// <summary>
    /// Update a producer's output
    /// </summary>
    public void UpdateProducer(Node2D building, float outputKw)
    {
        if (building == null)
            return;

        if (outputKw <= 0)
            UnregisterProducer(building);
        else
        {
            _producers[building] = outputKw;
            UpdatePowerNetwork();
        }
    }

    /// <summary>
    /// Register a power consumer
    /// </summary>
    public void RegisterConsumer(Node2D building, float consumptionKw)
    {
        if (building == null || consumptionKw <= 0)
            return;

        _consumers[building] = consumptionKw;
        EmitSignal(SignalName.ConsumerRegistered, building, consumptionKw);
        UpdatePowerNetwork();
    }

    /// <summary>
    /// Unregister a power consumer
    /// </summary>
    public void UnregisterConsumer(Node2D building)
    {
        if (_consumers.ContainsKey(building))
        {
            _consumers.Remove(building);
            UpdatePowerNetwork();
        }
    }

    /// <summary>
    /// Update a consumer's consumption
    /// </summary>
    public void UpdateConsumer(Node2D building, float consumptionKw)
    {
        if (building == null)
            return;

        if (consumptionKw <= 0)
            UnregisterConsumer(building);
        else
        {
            _consumers[building] = consumptionKw;
            UpdatePowerNetwork();
        }
    }

    /// <summary>
    /// Add storage capacity (when accumulator is placed)
    /// </summary>
    public void AddStorageCapacity(float capacityKj)
    {
        StorageCapacity += capacityKj;
    }

    /// <summary>
    /// Remove storage capacity (when accumulator is removed)
    /// </summary>
    public void RemoveStorageCapacity(float capacityKj)
    {
        StorageCapacity = Mathf.Max(0.0f, StorageCapacity - capacityKj);
        StoredEnergy = Mathf.Min(StoredEnergy, StorageCapacity);
    }

    /// <summary>
    /// Get the effective power for a consumer (accounting for brownout)
    /// </summary>
    public float GetEffectivePower(float baseConsumption)
    {
        return baseConsumption * Satisfaction;
    }

    /// <summary>
    /// Check if there's enough power for a specific consumption
    /// </summary>
    public bool HasPowerFor(float consumptionKw)
    {
        return Satisfaction >= 1.0f || (TotalProduction - TotalConsumption) >= consumptionKw;
    }

    /// <summary>
    /// Get power statistics
    /// </summary>
    public Dictionary GetStats()
    {
        return new Dictionary
        {
            { "production", TotalProduction },
            { "consumption", TotalConsumption },
            { "satisfaction", Satisfaction },
            { "is_brownout", IsBrownout },
            { "stored_energy", StoredEnergy },
            { "storage_capacity", StorageCapacity },
            { "producer_count", _producers.Count },
            { "consumer_count", _consumers.Count }
        };
    }

    /// <summary>
    /// Clear all power network data
    /// </summary>
    public void Clear()
    {
        _producers.Clear();
        _consumers.Clear();
        TotalProduction = 0.0f;
        TotalConsumption = 0.0f;
        Satisfaction = 1.0f;
        IsBrownout = false;
        StoredEnergy = 0.0f;
        StorageCapacity = 0.0f;
    }
}
