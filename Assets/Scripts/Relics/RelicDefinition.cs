using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

public class RelicDefinition
{
    public string id;
    public string name;
    public int sprite;
    public JObject trigger;
    public JObject effect;

    public RelicDefinition(string id, string name, int sprite, JObject trigger, JObject effect)
    {
        this.id = id;
        this.name = name;
        this.sprite = sprite;
        this.trigger = trigger;
        this.effect = effect;
    }

    public string GetTriggerType()
    {
        return GetString(trigger, "type");
    }

    public string GetEffectType()
    {
        return GetString(effect, "type");
    }

    public string GetDescription()
    {
        string triggerDescription = GetString(trigger, "description");
        string effectDescription = GetString(effect, "description");
        if (string.IsNullOrWhiteSpace(triggerDescription)) return effectDescription;
        if (string.IsNullOrWhiteSpace(effectDescription)) return triggerDescription;
        return triggerDescription + ", " + effectDescription;
    }

    public int Evaluate(JObject data, string key, SpellCaster owner, int fallback = 0)
    {
        JToken token = data == null ? null : data[key];
        if (token == null) return fallback;

        Dictionary<string, int> variables = new Dictionary<string, int>();
        variables["wave"] = Mathf.Max(1, GameManager.Instance.wave);
        variables["power"] = owner == null ? 0 : owner.spell_power;

        try
        {
            return Mathf.RoundToInt(RPNEvaluator.RPNEvaluator.Evaluatef(token.ToString(), variables));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not evaluate relic expression '" + token + "': " + e.Message);
            return fallback;
        }
    }

    public static string GetString(JObject data, string key, string fallback = "")
    {
        JToken token = data == null ? null : data[key];
        return token == null ? fallback : token.ToString();
    }
}

public class Relic
{
    public string id;
    public string name;
    public int sprite;
    public RelicDefinition definition;

    public Relic(RelicDefinition definition)
    {
        this.definition = definition;
        id = definition.id;
        name = definition.name;
        sprite = definition.sprite;
    }

    public string GetLabel()
    {
        return name;
    }

    public string GetDescription()
    {
        return definition.GetDescription();
    }

    public bool IsActive()
    {
        return false;
    }
}

public static class RelicLibrary
{
    public static List<Relic> LoadAll()
    {
        List<Relic> relics = new List<Relic>();
        foreach (RelicDefinition definition in LoadDefinitions())
        {
            relics.Add(new Relic(definition));
        }
        return relics;
    }

    public static List<RelicDefinition> LoadDefinitions()
    {
        List<RelicDefinition> relics = new List<RelicDefinition>();
        string json = LoadJson();
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("Could not find relics.json.");
            return relics;
        }

        try
        {
            JArray data = JArray.Parse(json);
            for (int i = 0; i < data.Count; i++)
            {
                JObject relic = data[i] as JObject;
                if (relic == null) continue;

                string name = RelicDefinition.GetString(relic, "name", "Relic " + (i + 1));
                string id = RelicDefinition.GetString(relic, "id", MakeId(name));
                int sprite = 0;
                if (relic["sprite"] != null)
                {
                    int.TryParse(relic["sprite"].ToString(), out sprite);
                }

                relics.Add(new RelicDefinition(
                    id,
                    name,
                    sprite,
                    relic["trigger"] as JObject,
                    relic["effect"] as JObject));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not parse relics.json: " + e.Message);
        }

        return relics;
    }

    static string LoadJson()
    {
        TextAsset asset = Resources.Load<TextAsset>("relics");
        if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
        {
            return asset.text;
        }

        string path = "./Assets/Resources/relics.json";
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }

    static string MakeId(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "relic";

        char[] chars = name.ToLowerInvariant().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
            {
                chars[i] = '-';
            }
        }

        return new string(chars).Trim('-');
    }
}
