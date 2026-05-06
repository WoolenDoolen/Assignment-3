using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class SpellDefinition
{
    public string id;
    public JObject attributes;

    public SpellDefinition(string id, JObject attributes)
    {
        this.id = id;
        this.attributes = attributes;
    }

    public bool IsBaseSpell()
    {
        return attributes["damage"] != null && attributes["projectile"] != null;
    }

    public string GetString(string key, string fallback = "")
    {
        JToken token = attributes[key];
        return token == null ? fallback : token.ToString();
    }

    public JObject GetObject(string key)
    {
        return attributes[key] as JObject;
    }

    public int EvaluateInt(string key, SpellCaster owner, int fallback = 0)
    {
        return Mathf.RoundToInt(EvaluateFloat(attributes[key], owner, fallback));
    }

    public float EvaluateFloat(string key, SpellCaster owner, float fallback = 0)
    {
        return EvaluateFloat(attributes[key], owner, fallback);
    }

    public static int EvaluateInt(JToken token, SpellCaster owner, int fallback = 0)
    {
        return Mathf.RoundToInt(EvaluateFloat(token, owner, fallback));
    }

    public static float EvaluateFloat(JToken token, SpellCaster owner, float fallback = 0)
    {
        if (token == null) return fallback;

        string expression = token.ToString();
        if (string.IsNullOrWhiteSpace(expression)) return fallback;

        Dictionary<string, int> variables = new Dictionary<string, int>();
        variables["wave"] = GameManager.Instance.wave;
        variables["power"] = owner == null ? 0 : owner.spell_power;

        try
        {
            return RPNEvaluator.RPNEvaluator.Evaluatef(expression, variables);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not evaluate spell expression '" + expression + "': " + e.Message);
            return fallback;
        }
    }
}

public class ProjectileSpellData
{
    public int sprite;
    public string trajectory;
    public float speed;
    public float lifetime;

    public ProjectileSpellData(int sprite, string trajectory, float speed, float lifetime = 0)
    {
        this.sprite = sprite;
        this.trajectory = trajectory;
        this.speed = speed;
        this.lifetime = lifetime;
    }

    public ProjectileSpellData Copy()
    {
        return new ProjectileSpellData(sprite, trajectory, speed, lifetime);
    }

    public static ProjectileSpellData FromJson(JObject data, SpellCaster owner, ProjectileSpellData fallback)
    {
        if (data == null)
        {
            return fallback == null ? null : fallback.Copy();
        }

        int sprite = SpellDefinition.EvaluateInt(data["sprite"], owner, fallback == null ? 0 : fallback.sprite);
        string trajectory = data["trajectory"] == null ? (fallback == null ? "straight" : fallback.trajectory) : data["trajectory"].ToString();
        float speed = SpellDefinition.EvaluateFloat(data["speed"], owner, fallback == null ? 8 : fallback.speed);
        float lifetime = SpellDefinition.EvaluateFloat(data["lifetime"], owner, fallback == null ? 0 : fallback.lifetime);
        return new ProjectileSpellData(sprite, trajectory, speed, lifetime);
    }
}

public class SpellCastProfile
{
    public string name;
    public string description;
    public int icon;
    public int damage;
    public Damage.Type damageType;
    public int manaCost;
    public float cooldown;
    public ProjectileSpellData projectile;
    public int projectileCount;
    public float projectileSpreadDegrees;
    public int repeatCount;
    public float repeatDelay;
    public int volleyCount;
    public float volleySpreadDegrees;
    public int secondaryDamage;
    public ProjectileSpellData secondaryProjectile;
    public int onHitProjectiles;
    public int onHitDamage;
    public ProjectileSpellData onHitProjectile;

    public SpellCastProfile()
    {
        projectileCount = 1;
        repeatCount = 1;
        volleyCount = 1;
        damageType = Damage.Type.ARCANE;
    }
}

public class Spell 
{
    public float last_cast;
    public SpellCaster owner;
    public Hittable.Team team;

    public Spell(SpellCaster owner)
    {
        this.owner = owner;
    }

    public string GetName()
    {
        return "Bolt";
    }

    public int GetManaCost()
    {
        return 10;
    }

    public int GetDamage()
    {
        return 100;
    }

    public float GetCooldown()
    {
        return 0.75f;
    }

    public virtual int GetIcon()
    {
        return 0;
    }

    public bool IsReady()
    {
        return (last_cast + GetCooldown() < Time.time);
    }

    public virtual IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        this.team = team;
        GameManager.Instance.projectileManager.CreateProjectile(0, "straight", where, target - where, 15f, OnHit);
        yield return new WaitForEndOfFrame();
    }

    void OnHit(Hittable other, Vector3 impact)
    {
        if (other.team != team)
        {
            other.Damage(new Damage(GetDamage(), Damage.Type.ARCANE));
        }

    }

}
