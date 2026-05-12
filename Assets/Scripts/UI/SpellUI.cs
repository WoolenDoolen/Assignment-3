using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellUI : MonoBehaviour
{
    public GameObject icon;
    public RectTransform cooldown;
    public TextMeshProUGUI manacost;
    public TextMeshProUGUI damage;
    public GameObject highlight;
    public Spell spell;
    float last_text_update;
    const float UPDATE_DELAY = 1;
    public GameObject dropbutton;
    public int slotIndex;
    public PlayerController player;

    private SpellUIContainer container;
    private bool dropButtonBound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        last_text_update = 0;
        BindDropButton();
        UpdateDropButton();
    }

    public void Setup(SpellUIContainer container, PlayerController player, int slotIndex)
    {
        this.container = container;
        this.player = player;
        this.slotIndex = slotIndex;
        BindDropButton();
        UpdateDropButton();
    }

    public void SetSpell(Spell spell){
        this.spell = spell;

        if (spell == null){
            if (icon != null)
            {icon.SetActive(false);}

            if (manacost != null)
            {manacost.text = "-";}

            if (damage != null)
            {damage.text = "-";}

            if (cooldown != null)
            {cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);}

            UpdateDropButton();
            return;
        }

        if (icon != null){
            icon.SetActive(true);
            GameManager.Instance.spellIconManager.PlaceSprite(spell.GetIcon(), icon.GetComponent<Image>());
        }
        RefreshText();
        last_text_update = Time.time;
        UpdateDropButton();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (spell == null) return;
        if (Time.time > last_text_update + UPDATE_DELAY)
        {
            RefreshText();
            last_text_update = Time.time;
        }
        
        float since_last = Time.time - spell.last_cast;
        float perc;
        if (since_last > spell.GetCooldown())
        {
            perc = 0;
        }
        else
        {
            perc = 1-since_last / spell.GetCooldown();
        }
        if (cooldown != null)
        {
            cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48 * perc);
        }
    }

    void RefreshText()
    {
        if (manacost != null)
        {
            manacost.text = spell.GetManaCost().ToString();
        }

        if (damage != null)
        {
            damage.text = spell.GetDamage().ToString();
        }
    }

    void BindDropButton()
    {
        if (dropbutton == null || dropButtonBound) return;

        Button button = dropbutton.GetComponent<Button>();
        if (button == null) return;

        button.onClick.AddListener(DropSpell);
        dropButtonBound = true;
    }

    void UpdateDropButton()
    {
        if (dropbutton == null) return;

        bool canDrop = spell != null &&
                       player != null &&
                       player.spellcaster != null &&
                       player.spellcaster.CanDropSpell(slotIndex);
        dropbutton.SetActive(canDrop);
    }

    public void DropSpell()
    {
        if (container != null)
        {
            container.DropSlot(slotIndex);
            return;
        }

        if (player != null)
        {
            player.DropSpell(slotIndex);
        }
    }
}
