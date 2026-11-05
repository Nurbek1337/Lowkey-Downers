using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class RhythmLayoutSetup
{
    [MenuItem("Tools/Rhythm Boss/Arrange Lanes")]
    public static void ArrangeLanes()
    {
        GameObject area = GameObject.Find("NoteArea");

        if (area == null)
        {
            Debug.LogError("NoteArea не найден!");
            return;
        }

        RectTransform areaRect = area.GetComponent<RectTransform>();
        areaRect.sizeDelta = new Vector2(1400, 900);

        string[] names =
        {
            "Lane_D",
            "Lane_F",
            "Lane_Space",
            "Lane_J",
            "Lane_K"
        };

        Color[] colors =
        {
            new Color(0.2f, 0.5f, 1f, 0.3f),
            new Color(0.2f, 0.8f, 0.6f, 0.3f),
            new Color(1f, 0.7f, 0.2f, 0.3f),
            new Color(0.7f, 0.4f, 1f, 0.3f),
            new Color(1f, 0.3f, 0.4f, 0.3f)
        };

        for (int i = 0; i < names.Length; i++)
        {
            Transform lane = area.transform.Find(names[i]);

            if (lane == null)
            {
                Debug.LogWarning(names[i] + " не найден!");
                continue;
            }

            RectTransform rect = lane.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.anchoredPosition = new Vector2(-400 + i * 200, 0);
            rect.sizeDelta = new Vector2(160, 900);

            Image image = lane.GetComponent<Image>();

            if (image != null)
                image.color = colors[i];
        }

        Transform hitLine = area.transform.Find("HitLine");

        if (hitLine != null)
        {
            RectTransform rect = hitLine.GetComponent<RectTransform>();

            rect.anchoredPosition = new Vector2(0, -80);
            rect.sizeDelta = new Vector2(500, 5);

            hitLine.SetAsLastSibling();
        }

        Transform note = area.transform.Find("Note");

        if (note != null)
        {
            RectTransform rect = note.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(35, 35);
            rect.anchoredPosition = new Vector2(0, 100);
        }

        EditorUtility.SetDirty(area);
        Debug.Log("–асстановка Rhythm Boss завершена!");
    }
}