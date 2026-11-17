using UnityEngine;
using TMPro;

public class WhiteMaskController : MonoBehaviour
{
    [Header("Refs")]
    public Renderer phaseFieldRenderer; // 内在相位场 (PhaseFieldSphere)
    public Renderer faceRenderer;       // 外壳脸 (FaceProxy)
    public TextMeshProUGUI uiText;      // 屏幕上的文字

    [Header("Face Materials")]
    public Material matIdleSafe;        // 你自己决定的样子 / 稳定自持
    public Material matExposed;         // 别人的读法 / 被迫单一化
    public Material matAbstract;        // 抽象化防御 / 重新掌控解释权

    [Header("State Scalars")]
    [Range(0f,1f)] public float agencyIndex = 1.0f;   // “我还能决定我是谁”的程度
    [Range(0f,1f)] public float coherence   = 1.0f;   // “我还整合吗，还是碎成别人版本”
    private float agencyTarget    = 1.0f;
    private float coherenceTarget = 1.0f;

    [Header("Dynamics / Speeds")]
    public float collapseSpeed = 4.0f;   // λ_M: 被看时往下掉多快
    public float recoverSpeed  = 0.6f;   // λ_A: 我重新夺回时往上爬多快
    public float idleSmooth    = 0.3f;   // 完全安全时的小幅收敛

    [Header("Breath / Flicker")]
    public float noiseAmp    = 0.05f;    // 呼吸幅度
    public float noiseFreqHz = 1.2f;     // 呼吸频率

    [Header("Stochastic Measurement")]
    [Tooltip("随机被观测的泊松率 λ_M（越大越容易随机触发被看见）")]
    public float lambdaM = 0.8f;

    [Header("State Entry Pulse & Dwell")]
    [Tooltip("进入任一状态时的入场脉冲时长(秒)")]
    public float stateEnterDuration = 0.20f; // 200ms
    [Tooltip("Observed 最短驻留(秒)")]
    public float minDwellObserved = 0.25f;
    [Tooltip("RePhase 最短驻留(秒)")]
    public float minDwellRephase  = 0.40f;

    [Header("Stability / Debug")]
    [Tooltip("Calm Mode 开启后：无随机被观测、无呼吸抖动、无入场脉冲")]
    public bool calmMode = true;

    private bool beingObserved = false;

    private enum ViewState { IdleSafe, ObservedCollapse, RePhaseOverride }
    private ViewState currentState = ViewState.IdleSafe;
    private ViewState lastState    = ViewState.IdleSafe;

    // 进入态计时 & 驻留计时
    private float stateEnterTimer = 0f;
    private float stateTime       = 0f;

    private string headline = "stable self-held state";

    void Update()
    {
        // ======= 0) 计算“有效参数”（受 calmMode 控制）=======
        float effLambdaM   = calmMode ? 0f : lambdaM;
        float effNoiseAmp  = calmMode ? 0f : noiseAmp;
        float effNoiseFreq = calmMode ? 0f : noiseFreqHz;
        float effEnterDur  = calmMode ? 0.0001f : Mathf.Max(0.0001f, stateEnterDuration);

        // ======= 1) 计时推进 =======
        stateEnterTimer += Time.deltaTime;
        stateTime       += Time.deltaTime;

        // ======= 2) 输入：G/H 键模拟外部凝视 vs 自我反抗 =======
        bool keyGDown = Input.GetKey(KeyCode.G);
        bool keyGUp   = Input.GetKeyUp(KeyCode.G);
        bool keyH     = Input.GetKeyDown(KeyCode.H);

        if (keyGDown) SetObserved();
        if (keyGUp)   SetRephase();
        if (keyH)     SetRephase();

        // ——(A) 随机观测事件（泊松过程）——
        if (!beingObserved && currentState != ViewState.ObservedCollapse)
        {
            if (Random.value < effLambdaM * Time.deltaTime)
            {
                SetObserved();
            }
        }

        // ——(B) 驻留/迟滞——
        if (currentState == ViewState.ObservedCollapse &&
            stateTime >= minDwellObserved &&
            !Input.GetKey(KeyCode.G))
        {
            SetRephase(true);
        }

        if (currentState == ViewState.RePhaseOverride &&
            stateTime >= minDwellRephase &&
            !beingObserved &&
            agencyIndex > 0.95f && coherence > 0.95f)
        {
            SetIdle();
        }

        // ======= 3) 目标缓慢推向 1.0 / 1.0（仅 RePhase 时自动上扬）=======
        if (!beingObserved && currentState == ViewState.RePhaseOverride)
        {
            agencyTarget    = Mathf.MoveTowards(agencyTarget,    1.0f, 0.2f * Time.deltaTime);
            coherenceTarget = Mathf.MoveTowards(coherenceTarget, 1.0f, 0.2f * Time.deltaTime);
        }

        // ======= 4) 实际数值向 target 逼近 =======
        switch (currentState)
        {
            case ViewState.ObservedCollapse:
                agencyIndex = Mathf.MoveTowards(agencyIndex, agencyTarget, collapseSpeed * Time.deltaTime);
                coherence   = Mathf.MoveTowards(coherence,   coherenceTarget, collapseSpeed * Time.deltaTime);
                break;
            case ViewState.RePhaseOverride:
                agencyIndex = Mathf.MoveTowards(agencyIndex, agencyTarget, recoverSpeed * Time.deltaTime);
                coherence   = Mathf.MoveTowards(coherence,   coherenceTarget, recoverSpeed * Time.deltaTime);
                break;
            case ViewState.IdleSafe:
                agencyIndex = Mathf.MoveTowards(agencyIndex, 1.0f, idleSmooth * Time.deltaTime);
                coherence   = Mathf.MoveTowards(coherence,   1.0f, idleSmooth * Time.deltaTime);
                break;
        }

        // （保底）若完全恢复则 Idle
        if (!beingObserved && currentState != ViewState.IdleSafe &&
            agencyIndex > 0.99f && coherence > 0.99f)
        {
            SetIdle();
        }

        // ======= 5) 呼吸/抖动（内在相位场 & UI）=======
        float t = Time.time * effNoiseFreq;
        float sinusBreath = Mathf.Sin(t * 2f * Mathf.PI) * (effNoiseAmp * 0.5f);
        float microJitter = UnityEngine.Random.Range(-effNoiseAmp, effNoiseAmp) * 0.1f;
        float flicker     = sinusBreath + microJitter;

        float brightnessVisual = Mathf.Clamp01(agencyIndex + flicker);
        float stabVisual       = Mathf.Clamp01(coherence   + flicker * 0.25f);

        if (phaseFieldRenderer != null)
        {
            var matInner = phaseFieldRenderer.material;
            matInner.SetFloat("_Brightness", brightnessVisual);
            matInner.SetFloat("_Coherence",  stabVisual);
        }

        // ======= 6) FaceProxy：状态基线 + 入场脉冲（受 calmMode 抑制）=======
        if (faceRenderer != null)
        {
            // 切外层材质
            Material chosen = matIdleSafe;
            if (currentState == ViewState.ObservedCollapse)      chosen = matExposed;
            else if (currentState == ViewState.RePhaseOverride)  chosen = matAbstract;
            faceRenderer.material = chosen;

            // 进入态脉冲 (0→1→0)
            float u = Mathf.Clamp01(stateEnterTimer / effEnterDur);
            float enterPulse = (calmMode ? 0f : (1f - u) * Mathf.Sin(u * Mathf.PI)); // calmMode=0

            // 基线
            float baseBright, baseSharp, baseLines, basePhase, baseOpacity;
            switch (currentState)
            {
                case ViewState.ObservedCollapse:
                    baseBright  = 0.95f;
                    baseSharp   = 0.95f;
                    baseLines   = Mathf.Lerp(0.6f, 1.0f, 1f - coherence);
                    basePhase   = 0.03f;
                    baseOpacity = 0.00f;
                    break;
                case ViewState.RePhaseOverride:
                    float rephaseK = Mathf.InverseLerp(0.6f, 1f, agencyIndex);
                    baseBright  = 0.45f;
                    baseSharp   = 0.45f;
                    baseLines   = 0.10f;
                    basePhase   = Mathf.Lerp(0.4f, 0.8f, rephaseK);
                    baseOpacity = 0.35f;
                    break;
                default: // IdleSafe
                    baseBright  = 0.58f;
                    baseSharp   = 0.55f;
                    baseLines   = 0.00f;
                    basePhase   = 0.08f;
                    baseOpacity = 0.05f;
                    break;
            }

            // 叠加内在偏移
            float brightness = Mathf.Clamp01(baseBright + (agencyIndex - 0.58f) * 0.40f);
            float sharpness  = Mathf.Clamp01(baseSharp  + (coherence   - 0.55f) * 0.50f);
            float lines      = baseLines;
            float phaseN     = basePhase;
            float opacityS   = baseOpacity;

            // 入场脉冲
            if (currentState == ViewState.ObservedCollapse)
            {
                brightness += 0.25f * enterPulse;
                lines      += 0.25f * enterPulse;
            }
            else if (currentState == ViewState.RePhaseOverride)
            {
                phaseN     += 0.25f * enterPulse;
                opacityS   += 0.25f * enterPulse;
            }

            var faceMat = faceRenderer.material;
            faceMat.SetFloat("_Brightness",    brightness);
            faceMat.SetFloat("_Sharpness",     sharpness);
            faceMat.SetFloat("_CollapseLines", lines);
            faceMat.SetFloat("_PhaseNoise",    phaseN);
            faceMat.SetFloat("_OpacityShift",  opacityS);
        }

        // ======= 7) UI =======
        if (currentState != lastState)
        {
            switch (currentState)
            {
                case ViewState.ObservedCollapse: headline = "you are being defined / measured"; break;
                case ViewState.RePhaseOverride:  headline = "re-phase / rewriting self";        break;
                case ViewState.IdleSafe:         headline = "stable self-held state";           break;
            }
        }

        if (uiText != null)
        {
            float visAgency    = Mathf.Clamp01(agencyIndex + flicker);
            float visCoherence = Mathf.Clamp01(coherence   + flicker * 0.25f);
            uiText.text = headline + "\n" +
                          "agency=" + visAgency.ToString("0.00") +
                          " | coherence=" + visCoherence.ToString("0.00") +
                          "\nstate=" + currentState.ToString() +
                          (calmMode ? "\n[calm mode]" : "");
        }

        lastState = currentState;

        // ======= 8) Debug 日志 =======
        Debug.Log("[" + Time.time.ToString("0.00") + "s] " +
                  "agency=" + agencyIndex.ToString("0.00") + ", " +
                  "coherence=" + coherence.ToString("0.00") + ", " +
                  "state=" + currentState.ToString() +
                  (calmMode ? " (calm)" : ""));
    }

    // ======================== 状态切换封装 ========================
    private void SetObserved()
    {
        beingObserved = true;
        currentState  = ViewState.ObservedCollapse;
        agencyTarget    = 0.0f;
        coherenceTarget = 0.2f;
        ResetStateTimers();
    }

    private void SetRephase(bool autoFromObserved = false)
    {
        beingObserved = false;
        currentState  = ViewState.RePhaseOverride;
        agencyTarget    = 0.6f;
        coherenceTarget = 0.6f;
        ResetStateTimers();
    }

    private void SetIdle()
    {
        beingObserved = false;
        currentState  = ViewState.IdleSafe;
        agencyTarget    = 1.0f;
        coherenceTarget = 1.0f;
        ResetStateTimers();
    }

    private void ResetStateTimers()
    {
        stateEnterTimer = 0f;
        stateTime       = 0f;
    }
}