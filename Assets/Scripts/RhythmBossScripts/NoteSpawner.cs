using UnityEngine;
using System.Collections;

public class NoteSpawner : MonoBehaviour
{
    [Header("Настройки")]
    public GameObject notePrefab;
    public Transform noteArea;
    public RhythmBattleManager battleManager;

    public float spawnInterval = 1f;

    // Последовательность дорожек:
    // 0 = D, 1 = F, 2 = Space, 3 = J, 4 = K
    private int[] sequence =
    {
        0, 1, 2, 3, 4,
        0, 2, 4, 1, 3,
        4, 3, 2, 1, 0
    };

    private string[] laneNames =
{
    "Lane_D",
    "Lane_F",
    "Lane_Space",
    "Lane_J",
    "Lane_K"
};

    private void Start()
    {
        Debug.Log("NoteSpawner запущен!");
        StartCoroutine(SpawnNotes());
    }

    IEnumerator SpawnNotes()
    {
        foreach (int lane in sequence)
        {
            SpawnNote(lane);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnNote(int lane)
    {
        GameObject laneObject = GameObject.Find(laneNames[lane]);

        if (laneObject == null)
        {
            Debug.LogError("Не найдена дорожка: " + laneNames[lane]);
            return;
        }

        GameObject note = Instantiate(notePrefab, laneObject.transform);

        RectTransform noteRect = note.GetComponent<RectTransform>();

        noteRect.anchorMin = new Vector2(0.5f, 0.5f);
        noteRect.anchorMax = new Vector2(0.5f, 0.5f);
        noteRect.pivot = new Vector2(0.5f, 0.5f);

        noteRect.anchoredPosition = new Vector2(0, 450);
        noteRect.sizeDelta = new Vector2(55, 55);

        NoteController controller = note.GetComponent<NoteController>();

        controller.battleManager = battleManager;
        controller.laneIndex = lane;
    }
}