using UnityEngine;

public interface RelicTrigger
{
    void Register(Relic relic, PlayerController player);
    void Unregister();
}

public interface RelicEffect
{
    void Apply(Relic relic, PlayerController player);
    void Clear();
    bool IsActive();
}

public static class RelicFactory
{
    public static RelicTrigger CreateTrigger(RelicDefinition definition)
    {
        string type = definition.GetTriggerType();
        if (type == "take-damage") return new TakeDamageRelicTrigger();
        if (type == "on-kill") return new EnemyKillRelicTrigger();
        if (type == "stand-still") return new StandStillRelicTrigger(definition);
        if (type == "wave-start") return new WaveStartRelicTrigger();
        if (type == "wave-complete") return new WaveCompleteRelicTrigger();
        if (type == "move-distance") return new MoveDistanceRelicTrigger(definition);
        return new NullRelicTrigger(type);
    }

    public static RelicEffect CreateEffect(RelicDefinition definition)
    {
        string type = definition.GetEffectType();
        if (type == "gain-mana") return new GainManaRelicEffect(definition);
        if (type == "gain-spellpower") return new GainSpellPowerRelicEffect(definition);
        if (type == "gain-max-health") return new GainMaxHealthRelicEffect(definition);
        return new NullRelicEffect(type);
    }
}

public class TakeDamageRelicTrigger : RelicTrigger
{
    private Relic relic;
    private PlayerController player;

    public void Register(Relic relic, PlayerController player)
    {
        this.relic = relic;
        this.player = player;
        EventBus.Instance.OnPlayerDamaged += OnPlayerDamaged;
    }

    public void Unregister()
    {
        EventBus.Instance.OnPlayerDamaged -= OnPlayerDamaged;
    }

    void OnPlayerDamaged(PlayerController damagedPlayer, Damage damage)
    {
        if (damagedPlayer == player)
        {
            relic.Trigger();
        }
    }
}

public class EnemyKillRelicTrigger : RelicTrigger
{
    private Relic relic;

    public void Register(Relic relic, PlayerController player)
    {
        this.relic = relic;
        EventBus.Instance.OnEnemyKilled += OnEnemyKilled;
    }

    public void Unregister()
    {
        EventBus.Instance.OnEnemyKilled -= OnEnemyKilled;
    }

    void OnEnemyKilled(Hittable enemy)
    {
        relic.Trigger();
    }
}

public class StandStillRelicTrigger : RelicTrigger
{
    private Relic relic;
    private PlayerController player;
    private float requiredSeconds;
    private float stillSeconds;
    private bool triggered;

    public StandStillRelicTrigger(RelicDefinition definition)
    {
        requiredSeconds = Mathf.Max(0.1f, definition.Evaluate(definition.trigger, "amount", null, 3));
    }

    public void Register(Relic relic, PlayerController player)
    {
        this.relic = relic;
        this.player = player;
        EventBus.Instance.OnPlayerMovementTick += OnPlayerMovementTick;
        EventBus.Instance.OnPlayerMoved += OnPlayerMoved;
    }

    public void Unregister()
    {
        EventBus.Instance.OnPlayerMovementTick -= OnPlayerMovementTick;
        EventBus.Instance.OnPlayerMoved -= OnPlayerMoved;
    }

    void OnPlayerMovementTick(PlayerController tickPlayer, float deltaTime, bool isMoving)
    {
        if (tickPlayer != player || GameManager.Instance.state != GameManager.GameState.INWAVE) return;

        if (isMoving)
        {
            Reset();
            return;
        }

        stillSeconds += deltaTime;
        if (!triggered && stillSeconds >= requiredSeconds)
        {
            triggered = true;
            relic.Trigger();
        }
    }

    void OnPlayerMoved(PlayerController movedPlayer, float distance)
    {
        if (movedPlayer == player)
        {
            Reset();
        }
    }

    void Reset()
    {
        stillSeconds = 0;
        triggered = false;
    }
}

public class WaveStartRelicTrigger : RelicTrigger
{
    private Relic relic;

    public void Register(Relic relic, PlayerController player)
    {
        this.relic = relic;
        EventBus.Instance.OnWaveStarted += OnWaveStarted;
    }

    public void Unregister()
    {
        EventBus.Instance.OnWaveStarted -= OnWaveStarted;
    }

    void OnWaveStarted(int wave)
    {
        relic.Trigger();
    }
}

public class WaveCompleteRelicTrigger : RelicTrigger
{
    private Relic relic;

    public void Register(Relic relic, PlayerController player)
    {
        this.relic = relic;
        EventBus.Instance.OnWaveCompleted += OnWaveCompleted;
    }

    public void Unregister()
    {
        EventBus.Instance.OnWaveCompleted -= OnWaveCompleted;
    }

    void OnWaveCompleted(int wave)
    {
        relic.Trigger();
    }
}

public class MoveDistanceRelicTrigger : RelicTrigger
{
    private Relic relic;
    private PlayerController player;
    private float requiredDistance;
    private float movedDistance;

    public MoveDistanceRelicTrigger(RelicDefinition definition)
    {
        requiredDistance = Mathf.Max(0.5f, definition.Evaluate(definition.trigger, "amount", null, 25));
    }

    public void Register(Relic relic, PlayerController player)
    {
        this.relic = relic;
        this.player = player;
        EventBus.Instance.OnPlayerMoved += OnPlayerMoved;
    }

    public void Unregister()
    {
        EventBus.Instance.OnPlayerMoved -= OnPlayerMoved;
    }

    void OnPlayerMoved(PlayerController movedPlayer, float distance)
    {
        if (movedPlayer != player || GameManager.Instance.state != GameManager.GameState.INWAVE) return;

        movedDistance += distance;
        if (movedDistance >= requiredDistance)
        {
            movedDistance -= requiredDistance;
            relic.Trigger();
        }
    }
}

public class GainManaRelicEffect : RelicEffect
{
    private RelicDefinition definition;

    public GainManaRelicEffect(RelicDefinition definition)
    {
        this.definition = definition;
    }

    public void Apply(Relic relic, PlayerController player)
    {
        if (player == null || player.spellcaster == null) return;

        int amount = definition.Evaluate(definition.effect, "amount", player.spellcaster, 0);
        player.spellcaster.AddMana(amount);
    }

    public void Clear()
    {
    }

    public bool IsActive()
    {
        return false;
    }
}

public class GainMaxHealthRelicEffect : RelicEffect
{
    private RelicDefinition definition;

    public GainMaxHealthRelicEffect(RelicDefinition definition)
    {
        this.definition = definition;
    }

    public void Apply(Relic relic, PlayerController player)
    {
        if (player == null || player.hp == null) return;

        int amount = definition.Evaluate(definition.effect, "amount", player.spellcaster, 0);
        if (amount > 0)
        {
            player.hp.SetMaxHP(player.hp.max_hp + amount);
        }
    }

    public void Clear()
    {
    }

    public bool IsActive()
    {
        return false;
    }
}

public class GainSpellPowerRelicEffect : RelicEffect
{
    private RelicDefinition definition;
    private SpellCaster activeCaster;
    private PlayerController activePlayer;
    private int activeAmount;
    private string until;

    public GainSpellPowerRelicEffect(RelicDefinition definition)
    {
        this.definition = definition;
        until = RelicDefinition.GetString(definition.effect, "until");
    }

    public void Apply(Relic relic, PlayerController player)
    {
        if (player == null || player.spellcaster == null) return;

        int amount = definition.Evaluate(definition.effect, "amount", player.spellcaster, 0);
        if (string.IsNullOrWhiteSpace(until))
        {
            player.spellcaster.spell_power += amount;
            return;
        }

        Clear();

        activePlayer = player;
        activeCaster = player.spellcaster;
        activeAmount = amount;
        activeCaster.AddTemporarySpellPower(activeAmount);

        if (until == "move")
        {
            EventBus.Instance.OnPlayerMoved += OnPlayerMoved;
            EventBus.Instance.OnPlayerMovementTick += OnPlayerMovementTick;
        }
        else if (until == "cast-spell")
        {
            EventBus.Instance.OnSpellCast += OnSpellCast;
        }
    }

    public void Clear()
    {
        if (activeCaster != null && activeAmount > 0)
        {
            activeCaster.RemoveTemporarySpellPower(activeAmount);
        }

        EventBus.Instance.OnPlayerMoved -= OnPlayerMoved;
        EventBus.Instance.OnPlayerMovementTick -= OnPlayerMovementTick;
        EventBus.Instance.OnSpellCast -= OnSpellCast;
        activeCaster = null;
        activePlayer = null;
        activeAmount = 0;
    }

    public bool IsActive()
    {
        return activeAmount > 0;
    }

    void OnPlayerMoved(PlayerController player, float distance)
    {
        if (player == activePlayer)
        {
            Clear();
        }
    }

    void OnPlayerMovementTick(PlayerController player, float deltaTime, bool isMoving)
    {
        if (player == activePlayer && isMoving)
        {
            Clear();
        }
    }

    void OnSpellCast(SpellCaster caster, Spell spell)
    {
        if (caster == activeCaster)
        {
            Clear();
        }
    }
}

public class NullRelicTrigger : RelicTrigger
{
    private string type;

    public NullRelicTrigger(string type)
    {
        this.type = type;
    }

    public void Register(Relic relic, PlayerController player)
    {
        Debug.LogWarning("Unknown relic trigger type: " + type);
    }

    public void Unregister()
    {
    }
}

public class NullRelicEffect : RelicEffect
{
    private string type;

    public NullRelicEffect(string type)
    {
        this.type = type;
    }

    public void Apply(Relic relic, PlayerController player)
    {
        Debug.LogWarning("Unknown relic effect type: " + type);
    }

    public void Clear()
    {
    }

    public bool IsActive()
    {
        return false;
    }
}
