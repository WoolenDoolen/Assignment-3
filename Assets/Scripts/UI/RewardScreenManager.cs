using UnityEngine;
using TMPro;

public class RewardScreenManager : MonoBehaviour
{
    public GameObject rewardUI;
    private TextMeshProUGUI buttonText;
    private TextMeshProUGUI messageText;
    private SpellBuilder spellBuilder;

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
        messageText.enableWordWrapping = true;
        messageText.color = Color.black;

        RectTransform rect = messageText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.45f);
        rect.anchorMax = new Vector2(0.85f, 0.85f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

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
            EnsurePendingRewardSpell();
            buttonText.text = GameManager.Instance.state == GameManager.GameState.WAVEEND ? "Take Spell" : "Return to Start";
            messageText.text = GetMessage();
        }
    }

    void EnsurePendingRewardSpell()
    {
        if (GameManager.Instance.state != GameManager.GameState.WAVEEND) return;
        if (GameManager.Instance.pendingSpellReward != null &&
            GameManager.Instance.pendingSpellRewardWave == GameManager.Instance.wave)
        {
            return;
        }

        GameObject playerObject = GameManager.Instance.player;
        PlayerController player = playerObject == null ? null : playerObject.GetComponent<PlayerController>();
        if (player == null || player.spellcaster == null) return;

        GameManager.Instance.SetPendingSpellReward(
            spellBuilder.BuildReward(player.spellcaster, GameManager.Instance.wave),
            GameManager.Instance.wave);
    }

    string GetMessage()
    {
        if (GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            return "Wave " + GameManager.Instance.wave + " complete\nEnemies defeated: " +
                   GameManager.Instance.enemiesDefeated + "/" + GameManager.Instance.enemiesSpawned +
                   "\n\nReward Spell\n" + GetRewardDescription();
        }
        return GameManager.Instance.resultMessage + "\nEnemies defeated: " +
               GameManager.Instance.enemiesDefeated + "/" + GameManager.Instance.enemiesSpawned;
    }

    string GetRewardDescription()
    {
        Spell reward = GameManager.Instance.pendingSpellReward;
        if (reward == null) return "No spell reward generated.";

        return reward.GetName() +
               "\nDamage: " + reward.GetDamage() +
               "   Mana: " + reward.GetManaCost() +
               "   Cooldown: " + reward.GetCooldown().ToString("0.0") + "s" +
               "\n" + reward.GetDescription();
    }
}
