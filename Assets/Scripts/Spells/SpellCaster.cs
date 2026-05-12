using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpellCaster 
{
    public int max_spells = 4;
    public int mana;
    public int max_mana;
    public int mana_reg;
    public int spell_power;
    public Hittable.Team team;
    public Spell spell;

    private List<Spell> spells;
    private int selectedIndex;

    public int SelectedIndex {get{return selectedIndex;}}
    public int SlotCount {get{return max_spells;}}

    public IEnumerator ManaRegeneration()
    {
        while (true)
        {
            mana += mana_reg;
            mana = Mathf.Min(mana, max_mana);
            yield return new WaitForSeconds(1);
        }
    }

    public SpellCaster(int mana, int mana_reg, Hittable.Team team)
    {
        this.mana = mana;
        this.max_mana = mana;
        this.mana_reg = mana_reg;
        spell_power = 0;
        this.team = team;

        spells = new List<Spell>();
        selectedIndex = 0;

        //basic spell in 0.
        Spell startingSpell = new SpellBuilder().Build(this);
        EquipSpellAt(startingSpell, 0);
    }

    public IEnumerator Cast(Vector3 where, Vector3 target)
    {
        Spell activeSpell = GetCurrentSpell();

        if (activeSpell != null && mana >= activeSpell.GetManaCost() && activeSpell.IsReady())
        {
            mana -= activeSpell.GetManaCost();
            yield return activeSpell.Cast(where, target, team);
        }

        yield break;
    }

    public Spell GetCurrentSpell()
    {
        if (selectedIndex < 0 || selectedIndex >= spells.Count){return null;}
        return spells[selectedIndex];
    }

    public Spell GetSpell(int slot)
    {
        if (slot < 0 || slot >= spells.Count){return null;}
        return spells[slot];
    }

    public int GetEquippedSpellCount()
    {
        int count = 0;
        for (int i = 0; i < spells.Count; i++)
        {
            if (spells[i] != null)
            {
                count++;
            }
        }
        return count;
    }

    public bool CanDropSpell(int slot)
    {
        return GetSpell(slot) != null && GetEquippedSpellCount() > 1;
    }

    public int GetEquipSlotForNextSpell()
    {
        if (selectedIndex >= 0 && selectedIndex < max_spells && GetSpell(selectedIndex) == null)
        {
            return selectedIndex;
        }

        for (int i = 0; i < max_spells; i++)
        {
            if (GetSpell(i) == null)
            {
                return i;
            }
        }

        return Mathf.Clamp(selectedIndex, 0, max_spells - 1);
    }

    public bool SelectSpell(int slot)
    {
        if (slot < 0 || slot >= spells.Count)
        {
            return false;
        }

        if (spells[slot] == null)
        {
            return false;
        }

        selectedIndex = slot;
        spell = spells[selectedIndex];
        return true;
    }

    public bool EquipSpell(Spell nextSpell)
    {
        if (nextSpell == null)
        {
            return false;
        }

        return EquipSpellAt(nextSpell, GetEquipSlotForNextSpell());
    }

    public bool EquipSpellAt(Spell nextSpell, int slot)
    {
        if (nextSpell == null)
        {
            return false;
        }

        if (slot < 0 || slot >= max_spells)
        {
            return false;
        }

        while (spells.Count <= slot)
        {
            spells.Add(null);
        }

        nextSpell.owner = this;
        spells[slot] = nextSpell;

        SelectSpell(slot);
        return true;
    }

    public bool DropSpell(int slot)
    {
        if (!CanDropSpell(slot)){return false;}

        spells[slot] = null;
        if (selectedIndex == slot)
        {
            for (int i = 0; i < spells.Count; i++)
            {
                if (spells[i] != null)
                {
                    SelectSpell(i);
                    return true;
                }
            }
            spell = null;
        }
        return true;
    }
}
