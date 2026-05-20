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
        variables["power"] = owner == null ? 0 : owner.GetSpellPower();

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
    public int secondaryProjectileCount;
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
    protected SpellDefinition baseDefinition;
    protected List<SpellDefinition> modifiers;

    public Spell(SpellCaster owner)
        : this(owner, null, new List<SpellDefinition>())
    {
    }

    public Spell(SpellCaster owner, SpellDefinition baseDefinition, List<SpellDefinition> modifiers)
    {
        this.owner = owner;
        this.baseDefinition = baseDefinition;
        this.modifiers = modifiers == null ? new List<SpellDefinition>() : new List<SpellDefinition>(modifiers);
    }

    public string GetName()
    {
        SpellCastProfile profile = BuildProfile();
        return profile.name;
    }

    public int GetManaCost()
    {
        return BuildProfile().manaCost;
    }

    public int GetDamage()
    {
        return BuildProfile().damage;
    }

    public string GetDescription()
    {
        return BuildProfile().description;
    }

    public float GetCooldown()
    {
        return BuildProfile().cooldown;
    }

    public virtual int GetIcon()
    {
        return BuildProfile().icon;
    }

    public string GetBaseId()
    {
        return baseDefinition == null ? "" : baseDefinition.id;
    }

    public List<string> GetModifierNames()
    {
        List<string> names = new List<string>();
        foreach (SpellDefinition modifier in modifiers)
        {
            names.Add(modifier.GetString("name", modifier.id));
        }
        return names;
    }

    public bool IsReady()
    {
        return (last_cast + GetCooldown() < Time.time);
    }

    public virtual IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        this.team = team;
        last_cast = Time.time;
        SpellCastProfile profile = BuildProfile();
        yield return CastVolley(where, target, team, profile);
        yield return new WaitForEndOfFrame();
    }

    protected SpellCastProfile BuildProfile()
    {
        SpellCastProfile profile = new SpellCastProfile();

        if (baseDefinition == null)
        {
            profile.name = "Bolt";
            profile.description = "A straight-flying bolt.";
            profile.icon = 0;
            profile.damage = 100;
            profile.manaCost = 10;
            profile.cooldown = 0.75f;
            profile.projectile = new ProjectileSpellData(0, "straight", 15f);
            return profile;
        }

        profile.name = baseDefinition.GetString("name", baseDefinition.id);
        profile.description = baseDefinition.GetString("description", "");
        profile.icon = baseDefinition.EvaluateInt("icon", owner, 0);

        JObject damage = baseDefinition.GetObject("damage");
        profile.damage = SpellDefinition.EvaluateInt(damage == null ? null : damage["amount"], owner, 10);
        profile.damageType = Damage.TypeFromString(damage == null ? "arcane" : damage["type"] == null ? "arcane" : damage["type"].ToString());
        profile.manaCost = baseDefinition.EvaluateInt("mana_cost", owner, 10);
        profile.cooldown = baseDefinition.EvaluateFloat("cooldown", owner, 1f);
        profile.projectile = ProjectileSpellData.FromJson(baseDefinition.GetObject("projectile"), owner, new ProjectileSpellData(0, "straight", 8f));

        int n = Mathf.Max(1, baseDefinition.EvaluateInt("N", owner, 1));
        if (baseDefinition.attributes["spray"] != null)
        {
            profile.projectileCount = n;
            profile.projectileSpreadDegrees = baseDefinition.EvaluateFloat("spray", owner, 0.3f) * Mathf.Rad2Deg;
        }

        if (baseDefinition.GetObject("secondary_projectile") != null)
        {
            profile.secondaryProjectile = ProjectileSpellData.FromJson(baseDefinition.GetObject("secondary_projectile"), owner, profile.projectile);
            profile.secondaryDamage = baseDefinition.EvaluateInt("secondary_damage", owner, Mathf.Max(1, profile.damage / 4));
            profile.secondaryProjectileCount = n;
        }

        ApplyModifiers(profile);
        return profile;
    }

    protected void ApplyModifiers(SpellCastProfile profile)
    {
        foreach (SpellDefinition modifier in modifiers)
        {
            profile.name = modifier.GetString("name", modifier.id) + " " + profile.name;
            string modifierDescription = modifier.GetString("description", "");
            if (!string.IsNullOrWhiteSpace(modifierDescription))
            {
                if (!string.IsNullOrWhiteSpace(profile.description))
                {
                    profile.description += "\n";
                }
                profile.description += modifier.GetString("name", modifier.id) + ": " + modifierDescription;
            }

            profile.damage = ApplyIntModifier(profile.damage, modifier, "damage_multiplier", "damage_adder", 1);
            profile.manaCost = ApplyIntModifier(profile.manaCost, modifier, "mana_multiplier", "mana_adder", 0);
            profile.cooldown = ApplyFloatModifier(profile.cooldown, modifier, "cooldown_multiplier", "cooldown_adder", 0.05f);

            if (profile.projectile != null)
            {
                profile.projectile.speed = ApplyFloatModifier(profile.projectile.speed, modifier, "speed_multiplier", "speed_adder", 0.1f);

                string trajectory = modifier.GetString("projectile_trajectory", "");
                if (!string.IsNullOrWhiteSpace(trajectory))
                {
                    profile.projectile.trajectory = trajectory;
                }
            }

            if (modifier.attributes["delay"] != null)
            {
                profile.repeatCount += 1;
                profile.repeatDelay = Mathf.Max(profile.repeatDelay, modifier.EvaluateFloat("delay", owner, 0.5f));
            }

            if (modifier.attributes["angle"] != null)
            {
                profile.volleyCount += 1;
                profile.volleySpreadDegrees += modifier.EvaluateFloat("angle", owner, 10f);
            }

            if (modifier.attributes["on_hit_projectiles"] != null)
            {
                profile.onHitProjectiles += Mathf.Max(0, modifier.EvaluateInt("on_hit_projectiles", owner, 0));
                profile.onHitDamage += Mathf.Max(0, modifier.EvaluateInt("on_hit_damage", owner, Mathf.Max(1, profile.damage / 5)));
                profile.onHitProjectile = ProjectileSpellData.FromJson(modifier.GetObject("on_hit_projectile"), owner, profile.projectile);
            }
        }
    }

    protected int ApplyIntModifier(int current, SpellDefinition modifier, string multiplierKey, string adderKey, int minimum)
    {
        float value = current;
        if (modifier.attributes[multiplierKey] != null)
        {
            value *= modifier.EvaluateFloat(multiplierKey, owner, 1f);
        }
        if (modifier.attributes[adderKey] != null)
        {
            value += modifier.EvaluateFloat(adderKey, owner, 0);
        }
        return Mathf.Max(minimum, Mathf.RoundToInt(value));
    }

    protected float ApplyFloatModifier(float current, SpellDefinition modifier, string multiplierKey, string adderKey, float minimum)
    {
        float value = current;
        if (modifier.attributes[multiplierKey] != null)
        {
            value *= modifier.EvaluateFloat(multiplierKey, owner, 1f);
        }
        if (modifier.attributes[adderKey] != null)
        {
            value += modifier.EvaluateFloat(adderKey, owner, 0);
        }
        return Mathf.Max(minimum, value);
    }

    protected IEnumerator CastVolley(Vector3 where, Vector3 target, Hittable.Team hitTeam, SpellCastProfile profile)
    {
        for (int repeat = 0; repeat < profile.repeatCount; repeat++)
        {
            if (repeat > 0 && profile.repeatDelay > 0)
            {
                yield return new WaitForSeconds(profile.repeatDelay);
            }

            for (int volley = 0; volley < profile.volleyCount; volley++)
            {
                Vector3 direction = target - where;
                float offset = GetSpreadOffset(volley, profile.volleyCount, profile.volleySpreadDegrees);
                CastProjectiles(where, Rotate(direction, offset), hitTeam, profile);
            }
        }
    }

    protected void CastProjectiles(Vector3 where, Vector3 direction, Hittable.Team hitTeam, SpellCastProfile profile)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.right;
        }

        int count = Mathf.Max(1, profile.projectileCount);
        for (int i = 0; i < count; i++)
        {
            float offset = GetSpreadOffset(i, count, profile.projectileSpreadDegrees);
            ProjectileSpellData projectile = profile.projectile ?? new ProjectileSpellData(0, "straight", 8f);
            CreateProjectile(projectile, where, Rotate(direction, offset), (other, impact) => OnHit(other, impact, hitTeam, profile));
        }
    }

    protected void CreateProjectile(ProjectileSpellData projectile, Vector3 where, Vector3 direction, Action<Hittable, Vector3> onHit)
    {
        if (GameManager.Instance.projectileManager == null)
        {
            Debug.LogWarning("Cannot cast spell because no ProjectileManager is registered.");
            return;
        }

        if (projectile.lifetime > 0)
        {
            GameManager.Instance.projectileManager.CreateProjectile(projectile.sprite, projectile.trajectory, where, direction, projectile.speed, onHit, projectile.lifetime);
        }
        else
        {
            GameManager.Instance.projectileManager.CreateProjectile(projectile.sprite, projectile.trajectory, where, direction, projectile.speed, onHit);
        }
    }

    protected void OnHit(Hittable other, Vector3 impact, Hittable.Team hitTeam, SpellCastProfile profile)
    {
        if (other.team == hitTeam) return;

        other.Damage(new Damage(profile.damage, profile.damageType));
        CastSecondaryProjectiles(impact, hitTeam, profile);
        CastOnHitProjectiles(impact, hitTeam, profile);
    }

    protected void OnSecondaryHit(Hittable other, Vector3 impact, Hittable.Team hitTeam, int damage, Damage.Type damageType)
    {
        if (other.team == hitTeam) return;
        other.Damage(new Damage(damage, damageType));
    }

    protected void CastSecondaryProjectiles(Vector3 impact, Hittable.Team hitTeam, SpellCastProfile profile)
    {
        if (profile.secondaryProjectile == null || profile.secondaryDamage <= 0 || profile.secondaryProjectileCount <= 0) return;

        for (int i = 0; i < profile.secondaryProjectileCount; i++)
        {
            float angle = 360f * i / profile.secondaryProjectileCount;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
            CreateProjectile(profile.secondaryProjectile, impact, direction,
                (other, secondaryImpact) => OnSecondaryHit(other, secondaryImpact, hitTeam, profile.secondaryDamage, profile.damageType));
        }
    }

    protected void CastOnHitProjectiles(Vector3 impact, Hittable.Team hitTeam, SpellCastProfile profile)
    {
        if (profile.onHitProjectile == null || profile.onHitDamage <= 0 || profile.onHitProjectiles <= 0) return;

        for (int i = 0; i < profile.onHitProjectiles; i++)
        {
            float angle = 360f * i / profile.onHitProjectiles;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
            CreateProjectile(profile.onHitProjectile, impact, direction,
                (other, secondaryImpact) => OnSecondaryHit(other, secondaryImpact, hitTeam, profile.onHitDamage, profile.damageType));
        }
    }

    protected float GetSpreadOffset(int index, int count, float spreadDegrees)
    {
        if (count <= 1 || Mathf.Abs(spreadDegrees) < Mathf.Epsilon) return 0;
        return Mathf.Lerp(-spreadDegrees * 0.5f, spreadDegrees * 0.5f, index * 1.0f / (count - 1));
    }

    protected Vector3 Rotate(Vector3 direction, float degrees)
    {
        return Quaternion.Euler(0, 0, degrees) * direction;
    }
}
