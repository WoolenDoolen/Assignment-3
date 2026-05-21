using UnityEngine;
using System;

public class EventBus 
{
    private static EventBus theInstance;
    public static EventBus Instance
    {
        get
        {
            if (theInstance == null)
                theInstance = new EventBus();
            return theInstance;
        }
    }

    public event Action<Vector3, Damage, Hittable> OnDamage;
    public event Action<PlayerController, Damage> OnPlayerDamaged;
    public event Action<Hittable> OnEnemyKilled;
    public event Action<SpellCaster, Spell> OnSpellCast;
    public event Action<PlayerController, float> OnPlayerMoved;
    public event Action<PlayerController, float, bool> OnPlayerMovementTick;
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<Relic> OnRelicPickedUp;
    public event Action OnRelicsCleared;
    
    public void DoDamage(Vector3 where, Damage dmg, Hittable target)
    {
        OnDamage?.Invoke(where, dmg, target);
    }

    public void DoPlayerDamaged(PlayerController player, Damage damage)
    {
        OnPlayerDamaged?.Invoke(player, damage);
    }

    public void DoEnemyKilled(Hittable enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
    }

    public void DoSpellCast(SpellCaster caster, Spell spell)
    {
        OnSpellCast?.Invoke(caster, spell);
    }

    public void DoPlayerMoved(PlayerController player, float distance)
    {
        OnPlayerMoved?.Invoke(player, distance);
    }

    public void DoPlayerMovementTick(PlayerController player, float deltaTime, bool isMoving)
    {
        OnPlayerMovementTick?.Invoke(player, deltaTime, isMoving);
    }

    public void DoWaveStarted(int wave)
    {
        OnWaveStarted?.Invoke(wave);
    }

    public void DoWaveCompleted(int wave)
    {
        OnWaveCompleted?.Invoke(wave);
    }

    public void DoRelicPickedUp(Relic relic)
    {
        OnRelicPickedUp?.Invoke(relic);
    }

    public void DoRelicsCleared()
    {
        OnRelicsCleared?.Invoke();
    }

}
