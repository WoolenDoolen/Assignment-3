using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

public class PlayerController : MonoBehaviour
{
    public Hittable hp;
    public HealthBar healthui;
    public ManaBar manaui;

    public SpellCaster spellcaster;
    public SpellUI spellui;

    public int speed;
    public string playerClass = "mage";

    public Unit unit;
    private Coroutine manaRoutine;
    private JObject classConfig;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;
    }

    public void StartLevel()
    {
        unit.movement = Vector2.zero;
        if (manaRoutine != null)
        {
            StopCoroutine(manaRoutine);
        }

        spellcaster = new SpellCaster(125, 8, Hittable.Team.PLAYER);
        manaRoutine = StartCoroutine(spellcaster.ManaRegeneration());

        hp = new Hittable(100, Hittable.Team.PLAYER, gameObject);
        hp.OnDeath += Die;
        hp.team = Hittable.Team.PLAYER;
        ApplyWaveStats();

        // tell UI elements what to show
        healthui.SetHealth(hp);
        manaui.SetSpellCaster(spellcaster);
        spellui.SetSpell(spellcaster.spell);
    }

    public void EquipSpell(Spell spell)
    {
        if (spellcaster == null || spell == null) return;

        spellcaster.EquipSpell(spell);
        if (spellui != null)
        {
            spellui.SetSpell(spellcaster.spell);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnAttack(InputValue value)
    {
        if (GameManager.Instance.state != GameManager.GameState.INWAVE) return;
        Vector2 mouseScreen = Mouse.current.position.value;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0;
        StartCoroutine(spellcaster.Cast(transform.position, mouseWorld));
    }

    void OnMove(InputValue value)
    {
        if (GameManager.Instance.state != GameManager.GameState.COUNTDOWN &&
            GameManager.Instance.state != GameManager.GameState.INWAVE)
        {
            unit.movement = Vector2.zero;
            return;
        }
        unit.movement = value.Get<Vector2>()*speed;
    }

    public void ApplyWaveStats()
    {
        JObject attributes = GetClassAttributes();
        int maxHP = EvaluateClassValue(attributes, "health", 100);
        int maxMana = EvaluateClassValue(attributes, "mana", 125);
        int manaRegeneration = EvaluateClassValue(attributes, "mana_regeneration", 8);
        int spellPower = EvaluateClassValue(attributes, "spellpower", 0);
        int moveSpeed = EvaluateClassValue(attributes, "speed", 5);

        if (hp != null)
        {
            hp.SetMaxHP(maxHP);
        }

        if (spellcaster != null)
        {
            float manaPercent = spellcaster.max_mana <= 0 ? 1 : spellcaster.mana * 1.0f / spellcaster.max_mana;
            spellcaster.max_mana = maxMana;
            spellcaster.mana = Mathf.Clamp(Mathf.RoundToInt(maxMana * manaPercent), 0, maxMana);
            spellcaster.mana_reg = manaRegeneration;
            spellcaster.spell_power = spellPower;
        }

        speed = moveSpeed;
    }

    JObject GetClassAttributes()
    {
        if (classConfig == null)
        {
            classConfig = LoadClassConfig();
        }

        if (classConfig == null) return null;
        JObject attributes = classConfig[playerClass] as JObject;
        if (attributes == null)
        {
            attributes = classConfig["mage"] as JObject;
        }
        return attributes;
    }

    JObject LoadClassConfig()
    {
        TextAsset asset = Resources.Load<TextAsset>("classes");
        string json = asset == null ? "" : asset.text;

        if (string.IsNullOrWhiteSpace(json))
        {
            string path = "./Assets/Resources/classes.json";
            if (File.Exists(path))
            {
                json = File.ReadAllText(path);
            }
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("Could not find classes.json. Falling back to hardcoded player stats.");
            return null;
        }

        return JObject.Parse(json);
    }

    int EvaluateClassValue(JObject attributes, string key, int fallback)
    {
        if (attributes == null || attributes[key] == null) return fallback;

        Dictionary<string, int> variables = new Dictionary<string, int>();
        variables["wave"] = Mathf.Max(1, GameManager.Instance.wave);

        try
        {
            return Mathf.RoundToInt(RPNEvaluator.RPNEvaluator.Evaluatef(attributes[key].ToString(), variables));
        }
        catch
        {
            Debug.LogWarning("Could not evaluate class stat '" + key + "'. Using fallback value.");
            return fallback;
        }
    }

    void Die()
    {
        unit.movement = Vector2.zero;
        GameManager.Instance.resultMessage = "You were defeated on wave " + GameManager.Instance.wave + ".";
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
    }

}
