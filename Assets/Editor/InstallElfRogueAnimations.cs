using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class InstallElfRogueAnimations
{
    private const string Source = "Assets/Sprites/Elf_Rogue_Unity_Animations";
    private const string Output = "Assets/Animations/Player";

    [MenuItem("Tools/Install Elf Rogue Animations")]
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
            "Walk", "Dash", "Jump", "Hurt",
            "Death", "Dagger_Throw", "Parry"
        };

        Dictionary<string, AnimationClip> clips =
            new Dictionary<string, AnimationClip>();

        foreach (string name in names)
        {
            string path = Source + "/" + name + ".png";

            if (!File.Exists(path))
            {
                Debug.LogError("Не найден файл: " + path);
                return;
            }

            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                Debug.LogError("Не удалось импортировать: " + path);
                return;
            }

            int frameCount = importer.maxTextureSize;
            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
            {
                Debug.LogError("Не удалось загрузить: " + path);
                return;
            }

            if (texture.width % 64 != 0 || texture.height != 64)
            {
                Debug.LogError("Неверный размер листа: " + path);
                return;
            }

            frameCount = texture.width / 64;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            SpriteMetaData[] metadata = new SpriteMetaData[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                metadata[i] = new SpriteMetaData
                {
                    name = name + "_" + i,
                    rect = new Rect(i * 64, 0, 64, 64),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f)
                };
            }

            importer.spritesheet = metadata;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();

            if (frames.Length != frameCount)
            {
                Debug.LogError("Неверное количество кадров: " + name);
                return;
            }

            AnimationClip clip = new AnimationClip();
            clip.name = "Player_" + name;
            clip.frameRate = name == "Walk" ? 12f : 14f;

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

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clip);

            settings.loopTime =
                name == "Walk" || name == "Parry";

            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string clipPath = Output + "/Player_" + name + ".anim";

            AssetDatabase.DeleteAsset(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);

            clips[name] = clip;

            Debug.Log(name + ": " + frames.Length + " кадров");
        }

        // Idle использует первый кадр Walk
        AnimationClip idle = new AnimationClip();
        idle.name = "Player_Idle";
        idle.frameRate = 1f;

        Sprite[] walkFrames = AssetDatabase.LoadAllAssetsAtPath(
            Source + "/Walk.png"
        ).OfType<Sprite>().OrderBy(s => s.name).ToArray();

        EditorCurveBinding idleBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(
            idle,
            idleBinding,
            new[]
            {
                new ObjectReferenceKeyframe
                {
                    time = 0f,
                    value = walkFrames[0]
                }
            }
        );

        string idlePath = Output + "/Player_Idle.anim";
        AssetDatabase.DeleteAsset(idlePath);
        AssetDatabase.CreateAsset(idle, idlePath);
        clips["Idle"] = idle;

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
        AssetDatabase.Refresh();

        Selection.activeGameObject = visual.gameObject;

        Debug.Log("Новые Elf Rogue анимации подключены к BossPlayer2!");
    }
}
