using UnityEngine;
using System.Collections.Generic;

public class RelicUIManager : MonoBehaviour
{
    public GameObject relicUIPrefab;
    public PlayerController player;
    private List<GameObject> relicViews;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        relicViews = new List<GameObject>();
        EventBus.Instance.OnRelicPickedUp += OnRelicPickedUp;
        EventBus.Instance.OnRelicsCleared += ClearRelicViews;
        ResolvePlayer();
    }

    void OnDestroy()
    {
        EventBus.Instance.OnRelicPickedUp -= OnRelicPickedUp;
        EventBus.Instance.OnRelicsCleared -= ClearRelicViews;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnRelicPickedUp(Relic relic)
    {
        ResolvePlayer();
        if (player == null || player.relics == null || relicUIPrefab == null) return;

        int index = player.relics.IndexOf(relic);
        if (index < 0)
        {
            index = player.relics.Count - 1;
        }
        if (index < 0) return;

        GameObject rui = Instantiate(relicUIPrefab, transform);
        relicViews.Add(rui);
        rui.transform.localPosition = new Vector3(-450 + 40 * index, 0, 0);
        RelicUI ruic = rui.GetComponent<RelicUI>();
        if (ruic != null)
        {
            ruic.Apply(player, index);
        }
    }

    void ResolvePlayer()
    {
        if (player != null) return;

        GameObject playerObject = GameManager.Instance.player;
        player = playerObject == null ? null : playerObject.GetComponent<PlayerController>();
    }

    void ClearRelicViews()
    {
        if (relicViews == null)
        {
            relicViews = new List<GameObject>();
            return;
        }

        foreach (GameObject relicView in relicViews)
        {
            if (relicView != null)
            {
                Destroy(relicView);
            }
        }

        relicViews.Clear();
    }
}
