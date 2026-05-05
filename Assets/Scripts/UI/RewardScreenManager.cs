using UnityEngine;
using TMPro;

public class RewardScreenManager : MonoBehaviour
{
    public GameObject rewardUI;
    private TextMeshProUGUI buttonText;
    private TextMeshProUGUI messageText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        buttonText = rewardUI.GetComponentInChildren<TextMeshProUGUI>(true);
        GameObject message = new GameObject("Reward Message");
        message.transform.SetParent(rewardUI.transform, false);
        messageText = message.AddComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.fontSize = 32;
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
            buttonText.text = GameManager.Instance.state == GameManager.GameState.WAVEEND ? "Next Wave" : "Return to Start";
            messageText.text = GetMessage();
        }
    }

    string GetMessage()
    {
        if (GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            return "Wave " + GameManager.Instance.wave + " complete\nEnemies defeated: " +
                   GameManager.Instance.enemiesDefeated + "/" + GameManager.Instance.enemiesSpawned;
        }
        return GameManager.Instance.resultMessage + "\nEnemies defeated: " +
               GameManager.Instance.enemiesDefeated + "/" + GameManager.Instance.enemiesSpawned;
    }
}
