using UnityEngine;
using TMPro;

public class MenuSelectorController : MonoBehaviour
{
    public TextMeshProUGUI label;
    public string level;
    public string playerClass;
    public EnemySpawner spawner;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLevel(string text)
    {
        level = text;
        playerClass = "";
        label.text = "Level: " + text;
    }

    public void SetClass(string text, bool selected)
    {
        playerClass = text;
        level = "";
        label.text = (selected ? "> " : "") + "Class: " + text;
    }

    public void StartLevel()
    {
        if (!string.IsNullOrWhiteSpace(level))
        {
            spawner.StartLevel(level);
            return;
        }

        if (!string.IsNullOrWhiteSpace(playerClass))
        {
            spawner.SelectClass(playerClass);
        }
    }
}
