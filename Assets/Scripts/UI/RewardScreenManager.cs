using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class RewardScreenManager : MonoBehaviour
{
    public GameObject rewardUI;
    private TextMeshProUGUI buttonText;
    private TextMeshProUGUI messageText;
    private SpellBuilder spellBuilder;
    private Button[] relicButtons;
    private Image[] relicButtonImages;
    private TextMeshProUGUI[] relicButtonTexts;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spellBuilder = new SpellBuilder();
        buttonText = rewardUI.GetComponentInChildren<TextMeshProUGUI>(true);
        GameObject message = new GameObject("Reward Message");
        message.transform.SetParent(rewardUI.transform, false);
        messageText = message.AddComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.fontSize = 24;
        messageText.textWrappingMode = TextWrappingModes.Normal;
        messageText.color = Color.black;

        RectTransform rect = messageText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.45f);
        rect.anchorMax = new Vector2(0.85f, 0.85f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CreateRelicButtons();
        rewardUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        bool shouldShow = GameManager.Instance.state == GameManager.GameState.WAVEEND ||
                          GameManager.Instance.state == GameManager.GameState.GAMEOVER ||
                          GameManager.Instance.state == GameManager.GameState.VICTORY;

        if (rewardUI.activeSelf != shouldShow)
        {
            rewardUI.SetActive(shouldShow);
        }

        if (shouldShow && buttonText != null)
        {
            PlayerController player = GetPlayer();
            EnsurePendingReward(player);
            buttonText.text = GameManager.Instance.state == GameManager.GameState.WAVEEND ? GetRewardButtonText(player) : "Return to Start";
            messageText.text = GetMessage(player);
            UpdateRelicButtons();
        }
        else
        {
            SetRelicButtonsVisible(false);
        }
    }

    PlayerController GetPlayer()
    {
        GameObject playerObject = GameManager.Instance.player;
        return playerObject == null ? null : playerObject.GetComponent<PlayerController>();
    }

    void EnsurePendingReward(PlayerController player)
    {
        if (GameManager.Instance.state != GameManager.GameState.WAVEEND) return;

        if (ShouldOfferRelicReward())
        {
            EnsurePendingRelicRewards(player);
            if (GameManager.Instance.pendingRelicRewards != null &&
                GameManager.Instance.pendingRelicRewardWave == GameManager.Instance.wave)
            {
                GameManager.Instance.ClearPendingSpellReward();
                return;
            }
        }

        GameManager.Instance.ClearPendingRelicRewards();
        EnsurePendingRewardSpell(player);
    }

    void EnsurePendingRewardSpell(PlayerController player)
    {
        if (GameManager.Instance.state != GameManager.GameState.WAVEEND) return;
        if (GameManager.Instance.pendingSpellReward != null &&
            GameManager.Instance.pendingSpellRewardWave == GameManager.Instance.wave)
        {
            return;
        }

        if (player == null || player.spellcaster == null) return;

        GameManager.Instance.SetPendingSpellReward(
            spellBuilder.BuildReward(player.spellcaster, GameManager.Instance.wave),
            GameManager.Instance.wave);
    }

    void EnsurePendingRelicRewards(PlayerController player)
    {
        if (GameManager.Instance.pendingRelicRewards != null &&
            GameManager.Instance.pendingRelicRewardWave == GameManager.Instance.wave)
        {
            return;
        }

        if (player == null) return;

        List<Relic> choices = player.BuildRelicRewardChoices(3);
        if (choices.Count > 0)
        {
            GameManager.Instance.SetPendingRelicRewards(choices, GameManager.Instance.wave);
        }
    }

    bool ShouldOfferRelicReward()
    {
        return GameManager.Instance.wave > 0 && GameManager.Instance.wave % 3 == 0;
    }

    string GetMessage(PlayerController player)
    {
        if (GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            if (GameManager.Instance.pendingRelicRewards != null)
            {
                return "Wave " + GameManager.Instance.wave + " complete\nEnemies defeated: " +
                       GameManager.Instance.enemiesDefeated + "/" + GameManager.Instance.enemiesSpawned +
                       "\n\nRelic Reward\n" + GetRelicRewardDescription();
            }

            return "Wave " + GameManager.Instance.wave + " complete\nEnemies defeated: " +
                   GameManager.Instance.enemiesDefeated + "/" + GameManager.Instance.enemiesSpawned +
                   "\n\nReward Spell\n" + GetRewardDescription(player);
        }
        return GameManager.Instance.resultMessage + "\nEnemies defeated: " +
               GameManager.Instance.enemiesDefeated + "/" + GameManager.Instance.enemiesSpawned;
    }

    string GetRewardButtonText(PlayerController player)
    {
        Relic selectedRelic = GameManager.Instance.GetSelectedRelicReward();
        if (selectedRelic != null) return "Take " + selectedRelic.name;

        if (player == null || player.spellcaster == null) return "Take Spell";

        return "Take Spell (Slot " + (player.spellcaster.GetEquipSlotForNextSpell() + 1) + ")";
    }

    string GetRewardDescription(PlayerController player)
    {
        Spell reward = GameManager.Instance.pendingSpellReward;
        if (reward == null) return "No spell reward generated.";

        return reward.GetName() +
               "\nDamage: " + reward.GetDamage() +
               "   Mana: " + reward.GetManaCost() +
               "   Cooldown: " + reward.GetCooldown().ToString("0.0") + "s" +
               "\n" + GetRewardSlotDescription(player) +
               "\n" + reward.GetDescription();
    }

    string GetRewardSlotDescription(PlayerController player)
    {
        if (player == null || player.spellcaster == null) return "Slot: unavailable";

        int slot = player.spellcaster.GetEquipSlotForNextSpell();
        Spell current = player.spellcaster.GetSpell(slot);
        if (current == null)
        {
            return "Slot " + (slot + 1) + ": empty";
        }

        return "Slot " + (slot + 1) + ": replacing " + current.GetName();
    }

    string GetRelicRewardDescription()
    {
        Relic selectedRelic = GameManager.Instance.GetSelectedRelicReward();
        if (selectedRelic == null) return "No relic reward available.";

        return "Selected: " + selectedRelic.name + "\n" + selectedRelic.GetDescription();
    }

    void CreateRelicButtons()
    {
        relicButtons = new Button[3];
        relicButtonImages = new Image[3];
        relicButtonTexts = new TextMeshProUGUI[3];

        for (int i = 0; i < relicButtons.Length; i++)
        {
            GameObject choice = new GameObject("Relic Choice " + (i + 1));
            choice.transform.SetParent(rewardUI.transform, false);
            relicButtonImages[i] = choice.AddComponent<Image>();
            relicButtonImages[i].color = new Color(1f, 1f, 1f, 0.72f);
            relicButtons[i] = choice.AddComponent<Button>();
            int choiceIndex = i;
            relicButtons[i].onClick.AddListener(() => GameManager.Instance.SelectPendingRelicReward(choiceIndex));

            RectTransform rect = choice.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.16f + 0.23f * i, 0.13f);
            rect.anchorMax = new Vector2(0.34f + 0.23f * i, 0.38f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            GameObject label = new GameObject("Label");
            label.transform.SetParent(choice.transform, false);
            relicButtonTexts[i] = label.AddComponent<TextMeshProUGUI>();
            relicButtonTexts[i].alignment = TextAlignmentOptions.Center;
            relicButtonTexts[i].fontSize = 18;
            relicButtonTexts[i].textWrappingMode = TextWrappingModes.Normal;
            relicButtonTexts[i].color = Color.black;

            RectTransform labelRect = relicButtonTexts[i].GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8, 8);
            labelRect.offsetMax = new Vector2(-8, -8);

            choice.SetActive(false);
        }
    }

    void UpdateRelicButtons()
    {
        List<Relic> choices = GameManager.Instance.pendingRelicRewards;
        if (GameManager.Instance.state != GameManager.GameState.WAVEEND || choices == null)
        {
            SetRelicButtonsVisible(false);
            return;
        }

        for (int i = 0; i < relicButtons.Length; i++)
        {
            bool hasChoice = i < choices.Count;
            relicButtons[i].gameObject.SetActive(hasChoice);
            if (!hasChoice) continue;

            relicButtonTexts[i].text = choices[i].name + "\n" + choices[i].GetDescription();
            relicButtonImages[i].color = i == GameManager.Instance.selectedRelicRewardIndex
                ? new Color(0.82f, 0.96f, 1f, 0.9f)
                : new Color(1f, 1f, 1f, 0.72f);
        }
    }

    void SetRelicButtonsVisible(bool visible)
    {
        if (relicButtons == null) return;

        foreach (Button button in relicButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }
    }
}
