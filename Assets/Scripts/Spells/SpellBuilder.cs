using UnityEngine;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;


public class SpellBuilder 
{
    private Dictionary<string, SpellDefinition> definitions;
    private List<SpellDefinition> baseSpells;
    private List<SpellDefinition> modifierSpells;

    public Spell Build(SpellCaster owner)
    {
        SpellDefinition arcaneBolt = GetDefinition("arcane_bolt");
        SpellDefinition baseSpell = arcaneBolt ?? (baseSpells.Count > 0 ? baseSpells[0] : null);
        return new Spell(owner, baseSpell, new List<SpellDefinition>());
    }

    public Spell BuildRandom(SpellCaster owner, int maxModifiers = 3)
    {
        SpellDefinition baseSpell = baseSpells.Count == 0 ? null : baseSpells[Random.Range(0, baseSpells.Count)];
        List<SpellDefinition> modifiers = new List<SpellDefinition>();

        while (modifiers.Count < maxModifiers && modifierSpells.Count > 0)
        {
            float chance = modifiers.Count == 0 ? 0.7f : 0.45f;
            if (Random.value > chance) break;
            modifiers.Add(modifierSpells[Random.Range(0, modifierSpells.Count)]);
        }

        return new Spell(owner, baseSpell, modifiers);
    }

    public Spell BuildWithModifiers(SpellCaster owner, string baseId, params string[] modifierIds)
    {
        SpellDefinition baseSpell = GetDefinition(baseId) ?? (baseSpells.Count > 0 ? baseSpells[0] : null);
        List<SpellDefinition> modifiers = new List<SpellDefinition>();
        foreach (string modifierId in modifierIds)
        {
            SpellDefinition modifier = GetDefinition(modifierId);
            if (modifier != null && !modifier.IsBaseSpell())
            {
                modifiers.Add(modifier);
            }
        }
        return new Spell(owner, baseSpell, modifiers);
    }
   
    public SpellBuilder()
    {
        LoadDefinitions();
    }

    private SpellDefinition GetDefinition(string id)
    {
        if (id == null || !definitions.ContainsKey(id)) return null;
        return definitions[id];
    }

    private void LoadDefinitions()
    {
        definitions = new Dictionary<string, SpellDefinition>();
        baseSpells = new List<SpellDefinition>();
        modifierSpells = new List<SpellDefinition>();

        string spellData = LoadSpellData();
        if (string.IsNullOrWhiteSpace(spellData)) return;

        JObject root = JObject.Parse(spellData);
        foreach (JProperty property in root.Properties())
        {
            JObject attributes = property.Value as JObject;
            if (attributes == null) continue;

            SpellDefinition definition = new SpellDefinition(property.Name, attributes);
            definitions[property.Name] = definition;
            if (definition.IsBaseSpell())
            {
                baseSpells.Add(definition);
            }
            else
            {
                modifierSpells.Add(definition);
            }
        }
    }

    private string LoadSpellData()
    {
        TextAsset asset = Resources.Load<TextAsset>("spells");
        if (asset != null) return asset.text;

        string path = "./Assets/Resources/spells.json";
        if (File.Exists(path)) return File.ReadAllText(path);

        Debug.LogWarning("Could not find spells.json.");
        return "";
    }
}
