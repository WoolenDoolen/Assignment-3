using UnityEngine;

public class SpellUIContainer : MonoBehaviour
{
    public GameObject[] spellUIs;
    public PlayerController player;

    void Start()
    {
        ConfigureSlots();
        ShowAllSlots();
        Refresh();
    }

    void Update()
    {
        ShowAllSlots();
        Refresh();
    }

    void ConfigureSlots()
    {
        if (spellUIs == null) return;

        for (int i = 0; i < spellUIs.Length; i++)
        {
            if (spellUIs[i] == null) continue;

            SpellUI ui = spellUIs[i].GetComponent<SpellUI>();
            if (ui == null) continue;

            ui.Setup(this, player, i);
        }
    }

    void ShowAllSlots()
    {
        if (spellUIs == null) return;

        for (int i = 0; i < spellUIs.Length; i++)
        {
            if (spellUIs[i] != null)
            {
                spellUIs[i].SetActive(true);
            }
        }
    }

    //reload after changes
    void Refresh()
    {
        if (spellUIs == null) return;
        if (player == null) return;
        if (player.spellcaster == null) return;

        for (int i = 0; i < spellUIs.Length; i++)
        {
            //null checks
            if (spellUIs[i] == null) continue;
            SpellUI ui = spellUIs[i].GetComponent<SpellUI>();
            if (ui == null) continue;

            Spell spell = player.spellcaster.GetSpell(i);
            ui.SetSpell(spell);

            if (ui.highlight != null){ui.highlight.SetActive(i == player.spellcaster.SelectedIndex);}
        }
    }

    public void SelectSlot0()
    {
        SelectSlot(0);
    }

    public void SelectSlot1()
    {
        SelectSlot(1);
    }

    public void SelectSlot2()
    {
        SelectSlot(2);
    }

    public void SelectSlot3()
    {
        SelectSlot(3);
    }

    public void SelectSlot(int slot)
    {
        if (player == null) return;

        player.SelectSpell(slot);
        Refresh();
    }

    public void DropSlot(int slot)
    {
        if (player == null) return;

        player.DropSpell(slot);
        Refresh();
    }
}
