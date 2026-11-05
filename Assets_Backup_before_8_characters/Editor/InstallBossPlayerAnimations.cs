using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class InstallBossPlayerAnimations
{
    const string Sprites = "Assets/Sprites/BossPlayer_Sprites";
    const string Output = "Assets/Animations/Player";

    [MenuItem("Tools/Install BossPlayer Animations")]
    public static void Install()
    {
        GameObject player = GameObject.Find("BossPlayer2");

        if (player == null)
        {
            Debug.LogError("Не найден BossPlayer2 в открытой сцене!");
            return;
        }

        Directory.CreateDirectory(Output);

        string[] names =
        {
            "Idle", "Run", "Jump", "Fall", "Land",
            "Dash", "Hurt", "Death", "Parry", "DaggerThrow"
        };

        Dictionary<string, AnimationClip> clips =
            new Dictionary<string, AnimationClip>();

        foreach (string name in names)
        {
            string path = Sprites + "/BossPlayer_" + name + ".png";

            if (!File.Exists(path))
            {
                Debug.LogWarning("Не найден файл: " + path);
                continue;
            }

            AssetDatabase.ImportAsset(path);

            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .Where(s => s.rect.width >= 20 && s.rect.height >= 18)
                .OrderBy(s => s.rect.x)
                .ToArray();

            if (frames.Length == 0)
            {
                Debug.LogWarning("Нет кадров для " + name);
                continue;
            }

            AnimationClip clip = new AnimationClip();
            clip.frameRate = name == "Idle" || name == "Run" ? 10f : 12f;

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keys =
                new ObjectReferenceKeyframe[frames.Length];

            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = frames[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            bool loop =
                name == "Idle" || name == "Run" ||
                name == "Fall" || name == "Parry";

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clip);

            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string clipPath = Output + "/Player_" + name + ".anim";

            AssetDatabase.DeleteAsset(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);

            clips[name] = clip;

            Debug.Log(name + ": " + frames.Length + " кадров");
        }

        string controllerPath = Output + "/BossPlayer.controller";

        AssetDatabase.DeleteAsset(controllerPath);

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        var stateMachine = controller.layers[0].stateMachine;

        foreach (var pair in clips)
        {
            AnimatorState state = stateMachine.AddState(pair.Key);
            state.motion = pair.Value;

            if (pair.Key == "Idle")
                stateMachine.defaultState = state;
        }

        Transform visual = player.transform.Find("PlayerVisual");

        if (visual == null)
        {
            GameObject visualObject = new GameObject("PlayerVisual");
            visual = visualObject.transform;
            visual.SetParent(player.transform, false);
        }

        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = new Vector3(2f, 2f, 1f);

        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();

        if (sr == null)
            sr = visual.gameObject.AddComponent<SpriteRenderer>();

        sr.enabled = true;
        sr.sortingOrder = 10;

        Animator animator = visual.GetComponent<Animator>();

        if (animator == null)
            animator = visual.gameObject.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (visual.GetComponent<PlayerVisualAnimator>() == null)
            visual.gameObject.AddComponent<PlayerVisualAnimator>();

        SpriteRenderer rootRenderer = player.GetComponent<SpriteRenderer>();

        if (rootRenderer != null)
            rootRenderer.enabled = false;

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(visual.gameObject);

        AssetDatabase.SaveAssets();

        Selection.activeGameObject = visual.gameObject;

        Debug.Log("Анимации подключены к BossPlayer2!");
    }
}
