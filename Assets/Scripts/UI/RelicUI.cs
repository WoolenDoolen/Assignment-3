using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelicUI : MonoBehaviour
{
    public PlayerController player;
    public int index;

    public Image icon;
    public GameObject highlight;
    public TextMeshProUGUI label;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        Refresh();
    }

    public void Apply(PlayerController player, int index)
    {
        this.player = player;
        this.index = index;
        Refresh();
    }

    void Refresh()
    {
        if (player == null || player.relics == null || index < 0 || index >= player.relics.Count) return;

        Relic r = player.relics[index];
        if (icon != null && GameManager.Instance.relicIconManager != null)
        {
            GameManager.Instance.relicIconManager.PlaceSprite(r.sprite, icon);
        }
        if (label != null)
        {
            label.text = r.GetLabel();
        }
        if (highlight != null)
        {
            highlight.SetActive(r.IsActive());
        }
    }
}
