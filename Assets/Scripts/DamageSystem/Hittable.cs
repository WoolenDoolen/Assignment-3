using UnityEngine;
using System;

public class Hittable
{

    public enum Team { PLAYER, MONSTERS }
    public Team team;

    public int hp;
    public int max_hp;

    public GameObject owner;

    public void Damage(Damage damage)
    {
        if (hp <= 0) return;

        EventBus.Instance.DoDamage(owner.transform.position, damage, this);
        hp -= damage.amount;
        if (team == Team.PLAYER && owner != null)
        {
            EventBus.Instance.DoPlayerDamaged(owner.GetComponent<PlayerController>(), damage);
        }
        if (hp <= 0)
        {
            hp = 0;
            if (team == Team.MONSTERS)
            {
                EventBus.Instance.DoEnemyKilled(this);
            }
            OnDeath?.Invoke();
        }
    }

    public event Action OnDeath;

    public Hittable(int hp, Team team, GameObject owner)
    {
        this.hp = hp;
        this.max_hp = hp;
        this.team = team;
        this.owner = owner;
    }

    public void SetMaxHP(int max_hp)
    {
        float perc = this.hp * 1.0f / this.max_hp;
        this.max_hp = max_hp;
        this.hp = Mathf.RoundToInt(perc * max_hp);
    }
}
