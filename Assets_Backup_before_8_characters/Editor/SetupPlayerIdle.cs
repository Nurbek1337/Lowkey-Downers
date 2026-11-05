using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class SetupPlayerIdle
{
    [MenuItem("Tools/Setup BossPlayer Idle")]
    public static void Setup()
    {
        GameObject player = GameObject.Find("BossPlayer2");

        if (player == null)
        {
            Debug.LogError("Не найден BossPlayer2!");
            return;
        }

        string clipPath = "Assets/Sprites/BossPlayer_Sprites/Ney_Idle.anim";
        AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (idle == null)
        {
            Debug.LogError("Не найдена анимация Ney_Idle.anim!");
            return;
        }

        string folder = "Assets/Animations/Player";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();

        string controllerPath = folder + "/BossPlayerIdle.controller";

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        var stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = null;

        foreach (var child in stateMachine.states)
        {
            if (child.state.name == "Idle")
            {
                idleState = child.state;
                break;
            }
        }

        if (idleState == null)
            idleState = stateMachine.AddState("Idle");

        idleState.motion = idle;
        stateMachine.defaultState = idleState;

        Transform visual = player.transform.Find("PlayerVisual");

        if (visual == null)
        {
            GameObject visualObject = new GameObject("PlayerVisual");
            visual = visualObject.transform;
            visual.SetParent(player.transform);
        }

        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = new Vector3(2f, 2f, 1f);

        SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = visual.gameObject.AddComponent<SpriteRenderer>();

        Animator animator = visual.GetComponent<Animator>();

        if (animator == null)
            animator = visual.gameObject.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(animator);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = visual.gameObject;

        Debug.Log("Idle успешно подключена к BossPlayer2 через PlayerVisual!");
    }
}
