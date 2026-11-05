using UnityEngine;
using System.Collections.Generic;

public enum GolemPose
{
    Idle, Walk, SlamWindup, Slam, Crouch, Airborne, Land,
    SpinWindup, Spin, Roar, Stunned, FinalCharge, Exhausted, Dead
}

/// <summary>
/// Процедурная скелетная анимация каменного голема из отдельных частей (cut-out rig).
/// Части ищутся по именам внутри Visual: Body, Torso, Head, ArmL/ArmR, ForeL/ForeR, LegL/LegR, Pelvis,
/// а также светящиеся слои (имена содержат "Crack", "Glow" или "Eyes"). Отсутствующие части пропускаются.
/// Состояния: Idle, Walk, подготовка атаки, удар, прыжок, вращение, рёв (переход фазы),
/// оглушение, финальная атака, истощение, смерть. Урон — вспышка/тряска (без прерывания атаки).
/// </summary>
public class GolemRig : MonoBehaviour
{
    [Header("Цвета свечения трещин и глаз")]
    public Color phase1Glow = new Color(1f, 0.72f, 0.28f, 1f);
    public Color phase2Glow = new Color(1f, 0.32f, 0.12f, 1f);
    public Color finalGlow = new Color(1f, 0.95f, 0.75f, 1f);
    [Tooltip("Оттенок камня: фаза 1 / фаза 2 (нагрев).")]
    public Color stoneTint1 = new Color(0.95f, 0.93f, 0.92f, 1f);
    public Color stoneTint2 = new Color(1f, 0.82f, 0.74f, 1f);

    [Header("Анимация")]
    public float walkFrequency = 4.2f;

    private Transform visual, body, torso, head, armL, armR, foreL, foreR, legL, legR, pelvis;
    private readonly List<SpriteRenderer> glows = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> stones = new List<SpriteRenderer>();
    private readonly List<Transform> allParts = new List<Transform>();
    private readonly List<Vector3> partRestPos = new List<Vector3>();

    private GolemPose pose = GolemPose.Idle;
    private float poseTime;
    private float faceSign = 1f;
    private int phase = 1;
    private float hitUntil;
    private float glowBoost;
    private float spinPhase;
    private bool dead;
    private float deathTime;
    private readonly List<Vector2> deathVel = new List<Vector2>();
    private float lastStepSign = 1f;
    private float nextAmbient;

    // текущие значения позы (сглаживаются)
    private float bodyY, bodyRot, torsoRot, headRot, armLa, armRa, foreLa, foreRa, legLa, legRa, squash, glowLevel;
    private float baseBodyY;

    public float FaceSign => faceSign;
    public GolemPose Pose => pose;

    private void Awake()
    {
        visual = FindDeep(transform, "Visual");
        if (visual == null) visual = transform;
        body = FindDeep(visual, "Body"); torso = FindDeep(visual, "Torso"); head = FindDeep(visual, "Head");
        armL = FindDeep(visual, "ArmL"); armR = FindDeep(visual, "ArmR");
        foreL = FindDeep(visual, "ForeL"); foreR = FindDeep(visual, "ForeR");
        legL = FindDeep(visual, "LegL"); legR = FindDeep(visual, "LegR"); pelvis = FindDeep(visual, "Pelvis");
        if (body != null) baseBodyY = body.localPosition.y;

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            string n = sr.name;
            if (n.Contains("Crack") || n.Contains("Glow") || n.Contains("Eyes")) glows.Add(sr);
            else stones.Add(sr);
            allParts.Add(sr.transform);
            partRestPos.Add(sr.transform.localPosition);
        }
        SetPhase(1);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    // ------------------------------------------------------------------
    public void SetPose(GolemPose p)
    {
        if (dead || p == pose) return;
        pose = p; poseTime = 0f;
    }

    public void SetMoving(bool moving)
    {
        if (pose == GolemPose.Idle && moving) SetPose(GolemPose.Walk);
        else if (pose == GolemPose.Walk && !moving) SetPose(GolemPose.Idle);
    }

    public void Face(float dir)
    {
        if (Mathf.Abs(dir) > 0.01f) faceSign = dir > 0f ? 1f : -1f;
    }

    public void SetPhase(int p)
    {
        phase = p;
        Color tint = p >= 2 ? stoneTint2 : stoneTint1;
        foreach (SpriteRenderer s in stones) if (s != null) s.color = tint;
        if (p >= 2) CombatFx.Flash(transform.position + Vector3.up * 3.5f, new Color(1f, 0.5f, 0.2f, 0.9f), 6f, 0.6f);
    }

    public void OnHit()
    {
        hitUntil = Time.time + 0.09f;
        glowBoost = 0.9f;
    }

    public void PlayDeath()
    {
        if (dead) return;
        dead = true; deathTime = 0f;
        deathVel.Clear();
        foreach (Transform t in allParts)
            deathVel.Add(new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(1.5f, 6f)));
        glowBoost = 2f;
    }

    // ------------------------------------------------------------------
    private void Update()
    {
        float dt = Time.deltaTime;
        if (dead) { UpdateDeath(dt); return; }

        poseTime += dt;
        float t = Time.time;
        glowBoost = Mathf.MoveTowards(glowBoost, 0f, dt * 2.5f);

        // целевые значения
        float tBodyY = 0, tBodyRot = 0, tTorso = 0, tHead = 0, tArmL = 4, tArmR = 4, tForeL = 6, tForeR = 6, tLegL = 0, tLegR = 0, tSquash = 0, tGlow = 0.55f;
        float k = 9f;
        float flipX = faceSign;

        switch (pose)
        {
            case GolemPose.Idle:
                tBodyY = Mathf.Sin(t * 1.8f) * 0.04f;
                tArmL = 4 + Mathf.Sin(t * 1.8f) * 3; tArmR = 4 + Mathf.Sin(t * 1.8f + 0.6f) * 3;
                tTorso = Mathf.Sin(t * 0.9f) * 1.2f; tHead = Mathf.Sin(t * 0.7f) * 2f;
                tGlow = 0.6f + 0.15f * Mathf.Sin(t * 2.2f);
                break;
            case GolemPose.Walk:
            {
                float s = Mathf.Sin(poseTime * walkFrequency);
                tLegL = s * 20f; tLegR = -s * 20f;
                tBodyY = Mathf.Abs(s) * 0.06f; tTorso = s * 2f;
                tArmL = 6 - s * 10; tArmR = 6 + s * 10;
                tGlow = 0.65f;
                if (Mathf.Sign(s) != lastStepSign && Mathf.Abs(s) < 0.2f)
                {
                    lastStepSign = Mathf.Sign(s);
                    CombatFx.Dust(transform.position + new Vector3(0f, 0.1f, 0f), 2, 0.8f, 0.5f);
                    CombatFx.Shake(0.025f, 0.08f);
                }
                k = 12f;
                break;
            }
            case GolemPose.SlamWindup:
                tArmL = 165; tArmR = 165; tForeL = 25; tForeR = 25; tBodyY = 0.12f; tTorso = -5; tHead = 6; tSquash = -0.04f;
                tGlow = 0.9f + 0.4f * Mathf.Sin(poseTime * 14f); k = 7f;
                break;
            case GolemPose.Slam:
                tArmL = 12; tArmR = 12; tForeL = 0; tForeR = 0; tBodyY = -0.18f; tTorso = 9; tHead = 10; tSquash = 0.12f; tGlow = 1.5f; k = 34f;
                break;
            case GolemPose.Crouch:
                tSquash = 0.28f; tBodyY = -0.1f; tArmL = 35; tArmR = 35; tForeL = 40; tForeR = 40; tLegL = 14; tLegR = -14; tTorso = 8; tGlow = 0.9f; k = 9f;
                break;
            case GolemPose.Airborne:
                tSquash = -0.1f; tArmL = 125; tArmR = 125; tForeL = 15; tForeR = 15; tLegL = 18; tLegR = -18; tTorso = -3; tGlow = 1.1f; k = 12f;
                break;
            case GolemPose.Land:
                tSquash = 0.3f; tBodyY = -0.2f; tArmL = 45; tArmR = 45; tForeL = 10; tForeR = 10; tTorso = 10; tGlow = 1.5f; k = 28f;
                break;
            case GolemPose.SpinWindup:
                tArmL = 90; tArmR = 90; tForeL = 0; tForeR = 0; tBodyY = 0.1f + Mathf.Sin(poseTime * 40f) * 0.03f; tGlow = 1.0f + 0.5f * Mathf.Sin(poseTime * 16f); k = 9f;
                break;
            case GolemPose.Spin:
                tArmL = 90; tArmR = 90; tBodyY = 0.05f; tGlow = 1.4f; k = 12f;
                spinPhase += dt * 16f;
                flipX = faceSign * Mathf.Max(0.22f, Mathf.Abs(Mathf.Cos(spinPhase)));
                break;
            case GolemPose.Roar:
                tArmL = 75; tArmR = 75; tForeL = 25; tForeR = 25; tHead = -22; tTorso = -6; tBodyY = 0.12f + Mathf.Sin(poseTime * 30f) * 0.03f; tGlow = 1.8f; k = 9f;
                break;
            case GolemPose.Stunned:
                tHead = 30 + Mathf.Sin(t * 5f) * 3f; tTorso = 7; tBodyY = -0.14f; tArmL = 12 + Mathf.Sin(t * 3f) * 3; tArmR = 12 - Mathf.Sin(t * 3f) * 3; tSquash = 0.08f;
                tGlow = 0.25f + 0.25f * (Mathf.Sin(t * 22f) > 0.2f ? 1f : 0f); k = 11f;
                break;
            case GolemPose.FinalCharge:
                tArmL = 172; tArmR = 172; tForeL = 10; tForeR = 10; tBodyY = 0.22f + Mathf.Sin(t * 9f) * 0.04f; tHead = -12; tTorso = -4;
                tGlow = 1.7f + 0.4f * Mathf.Sin(t * 13f); k = 8f;
                break;
            case GolemPose.Exhausted:
                tSquash = 0.22f; tBodyY = -0.22f; tHead = 36; tTorso = 12; tArmL = 6; tArmR = 6; tLegL = 12; tLegR = -12;
                tGlow = 0.25f + 0.3f * (Mathf.Sin(t * 9f) > 0.5f ? 1f : 0f); k = 7f;
                break;
        }

        float a = 1f - Mathf.Exp(-k * dt);
        bodyY = Mathf.Lerp(bodyY, tBodyY, a); bodyRot = Mathf.Lerp(bodyRot, tBodyRot, a);
        torsoRot = Mathf.Lerp(torsoRot, tTorso, a); headRot = Mathf.Lerp(headRot, tHead, a);
        armLa = Mathf.Lerp(armLa, tArmL, a); armRa = Mathf.Lerp(armRa, tArmR, a);
        foreLa = Mathf.Lerp(foreLa, tForeL, a); foreRa = Mathf.Lerp(foreRa, tForeR, a);
        legLa = Mathf.Lerp(legLa, tLegL, a); legRa = Mathf.Lerp(legRa, tLegR, a);
        squash = Mathf.Lerp(squash, tSquash, a);
        glowLevel = Mathf.Lerp(glowLevel, tGlow, 1f - Mathf.Exp(-10f * dt));

        ApplyPose(flipX);
        ApplyGlow();
        AmbientParticles();
    }

    private void ApplyPose(float flipX)
    {
        if (visual != null && visual != transform)
            visual.localScale = new Vector3(flipX * (1f + squash * 0.35f), 1f - squash, 1f);

        float shake = Time.time < hitUntil ? 0.06f : 0f;
        Vector3 jitter = shake > 0f ? (Vector3)(Random.insideUnitCircle * shake) : Vector3.zero;
        if (body != null)
        {
            body.localPosition = new Vector3(jitter.x, baseBodyY + bodyY + jitter.y, 0f);
            body.localRotation = Quaternion.Euler(0f, 0f, bodyRot);
        }
        if (torso != null) torso.localRotation = Quaternion.Euler(0f, 0f, -torsoRot);
        if (head != null) head.localRotation = Quaternion.Euler(0f, 0f, headRot);
        if (armL != null) armL.localRotation = Quaternion.Euler(0f, 0f, -armLa);
        if (armR != null) armR.localRotation = Quaternion.Euler(0f, 0f, armRa);
        if (foreL != null) foreL.localRotation = Quaternion.Euler(0f, 0f, -foreLa);
        if (foreR != null) foreR.localRotation = Quaternion.Euler(0f, 0f, foreRa);
        if (legL != null) legL.localRotation = Quaternion.Euler(0f, 0f, legLa);
        if (legR != null) legR.localRotation = Quaternion.Euler(0f, 0f, legRa);
    }

    private void ApplyGlow()
    {
        Color baseCol = pose == GolemPose.FinalCharge ? finalGlow : (phase >= 2 ? phase2Glow : phase1Glow);
        float level = Mathf.Clamp(glowLevel + glowBoost, 0f, 2.5f) * (phase >= 2 ? 1.15f : 1f);
        foreach (SpriteRenderer g in glows)
        {
            if (g == null) continue;
            Color c = baseCol;
            bool halo = g.name.Contains("Glow") && !g.name.Contains("Eyes");
            c.a = Mathf.Clamp01(level * (halo ? 0.35f : 1f));
            g.color = c;
        }
    }

    private void AmbientParticles()
    {
        if (Time.time < nextAmbient) return;
        nextAmbient = Time.time + (phase >= 2 ? 0.22f : 0.45f);
        Vector3 p = transform.position + new Vector3(Random.Range(-1.8f, 1.8f), Random.Range(1.5f, 5.5f), 0f);
        // Пыль и мелкие камешки осыпаются с тела; во второй фазе — тлеющие искры.
        CombatFx.Debris(p, 1, 0.6f, new Color(0.7f, 0.62f, 0.55f, 1f), 0.8f);
        if (phase >= 2) CombatFx.Flash(p, new Color(1f, 0.45f, 0.15f, 0.8f), 0.35f, 0.5f);
    }

    private void UpdateDeath(float dt)
    {
        deathTime += dt;
        for (int i = 0; i < allParts.Count; i++)
        {
            Transform t = allParts[i];
            if (t == null) continue;
            Vector2 v = deathVel[i];
            v.y -= 12f * dt;
            deathVel[i] = v;
            if (deathTime > 0.5f) t.position += (Vector3)(v * dt);
            if (t.position.y < -3.7f && deathTime > 0.5f) { t.position = new Vector3(t.position.x, -3.7f, t.position.z); deathVel[i] = new Vector2(v.x * 0.3f, 0f); }
        }
        foreach (SpriteRenderer g in glows)
            if (g != null) { Color c = phase2Glow; c.a = deathTime < 0.5f ? 1f : Mathf.Clamp01(1f - (deathTime - 0.5f)); g.color = c; }
        if (deathTime > 1.6f)
            foreach (SpriteRenderer s in stones)
                if (s != null) { Color c = s.color; c.a = Mathf.Clamp01(1f - (deathTime - 1.6f) / 1.4f); s.color = c; }
        if (Random.value < 0.4f) CombatFx.Debris(transform.position + new Vector3(Random.Range(-2f, 2f), Random.Range(0.5f, 4f), 0f), 2, 4f, new Color(0.7f, 0.62f, 0.55f, 1f));
    }
}
