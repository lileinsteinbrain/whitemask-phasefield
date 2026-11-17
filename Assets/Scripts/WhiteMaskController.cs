using UnityEngine;
using TMPro;

public class WhiteMaskController : MonoBehaviour
{
    [Header("Refs")]
    public Renderer phaseFieldRenderer; // inner phase field (PhaseFieldSphere)
    public Renderer faceRenderer;       // outer shell face (FaceProxy)
    public TextMeshProUGUI uiText;      // on-screen text

    [Header("Face Materials")]
    public Material matIdleSafe;        // self-determined appearance / stable and self-held
    public Material matExposed;         // read by others / forced into a single version
    public Material matAbstract;        // abstraction as defence / reclaiming interpretation

    [Header("State Scalars")]
    [Range(0f,1f)] public float agencyIndex = 1.0f;   // how much "I can still decide who I am"
    [Range(0f,1f)] public float coherence   = 1.0f;   // "am I still whole, or fragmented into others' versions"
    private float agencyTarget    = 1.0f;
    private float coherenceTarget = 1.0f;

    [Header("Dynamics / Speeds")]
    public float collapseSpeed = 4.0f;   // λ_M: how fast it drops when observed
    public float recoverSpeed  = 0.6f;   // λ_A: how fast it climbs back when reclaimed
    public float idleSmooth    = 0.3f;   // slight convergence when fully safe

    [Header("Breath / Flicker")]
    public float noiseAmp    = 0.05f;    // breathing amplitude
    public float noiseFreqHz = 1.2f;     // breathing frequency

    [Header("Stochastic Measurement")]
    [Tooltip("Poisson rate λ_M of random observation (higher = more likely to be 'seen' at random)")]
    public float lambdaM = 0.8f;

    [Header("State Entry Pulse & Dwell")]
    [Tooltip("Entry pulse duration when entering any state (seconds)")]
    public float stateEnterDuration = 0.20f; // 200ms
    [Tooltip("Minimum dwell time in Observed (seconds)")]
    public float minDwellObserved = 0.25f;
    [Tooltip("Minimum dwell time in RePhase (seconds)")]
    public float minDwellRephase  = 0.40f;

    [Header("Stability / Debug")]
    [Tooltip("Calm Mode: no random observation, no breathing jitter, no entry pulse")]
    public bool calmMode = true;

    private bool beingObserved = false;

    private enum ViewState { IdleSafe, ObservedCollapse, RePhaseOverride }
    private ViewState currentState = ViewState.IdleSafe;
    private ViewState lastState    = ViewState.IdleSafe;

    // Entry-state timer & dwell timer
    private float stateEnterTimer = 0f;
    private float stateTime       = 0f;

    private string headline = "stable self-held state";

    void Update()
    {
        // ======= 0) Compute effective parameters (affected by calmMode) =======
        float effLambdaM   = calmMode ? 0f : lambdaM;
        float effNoiseAmp  = calmMode ? 0f : noiseAmp;
        float effNoiseFreq = calmMode ? 0f : noiseFreqHz;
        float effEnterDur  = calmMode ? 0.0001f : Mathf.Max(0.0001f, stateEnterDuration);

        // ======= 1) Advance timers =======
        stateEnterTimer += Time.deltaTime;
        stateTime       += Time.deltaTime;

        // ======= 2) Input: G/H keys simulate external gaze vs. self-resistance =======
        bool keyGDown = Input.GetKey(KeyCode.G);
        bool keyGUp   = Input.GetKeyUp(KeyCode.G);
        bool keyH     = Input.GetKeyDown(KeyCode.H);

        if (keyGDown) SetObserved();
        if (keyGUp)   SetRephase();
        if (keyH)     SetRephase();

        // --(A) Random observation events (Poisson process)--
        if (!beingObserved && currentState != ViewState.ObservedCollapse)
        {
            if (Random.value < effLambdaM * Time.deltaTime)
            {
                SetObserved();
            }
        }

        // --(B) Dwell / hysteresis--
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

        // ======= 3) Targets drift slowly toward 1.0 / 1.0 (only rises automatically in RePhase) =======
        if (!beingObserved && currentState == ViewState.RePhaseOverride)
        {
            agencyTarget    = Mathf.MoveTowards(agencyTarget,    1.0f, 0.2f * Time.deltaTime);
            coherenceTarget = Mathf.MoveTowards(coherenceTarget, 1.0f, 0.2f * Time.deltaTime);
        }

        // ======= 4) Move actual values toward targets =======
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

        // (Fallback) return to Idle once fully recovered
        if (!beingObserved && currentState != ViewState.IdleSafe &&
            agencyIndex > 0.99f && coherence > 0.99f)
        {
            SetIdle();
        }

        // ======= 5) Breathing / jitter (inner phase field & UI) =======
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

        // ======= 6) FaceProxy: state baseline + entry pulse (suppressed in calmMode) =======
        if (faceRenderer != null)
        {
            // Switch outer material
            Material chosen = matIdleSafe;
            if (currentState == ViewState.ObservedCollapse)      chosen = matExposed;
            else if (currentState == ViewState.RePhaseOverride)  chosen = matAbstract;
            faceRenderer.material = chosen;

            // Entry pulse (0 -> 1 -> 0)
            float u = Mathf.Clamp01(stateEnterTimer / effEnterDur);
            float enterPulse = (calmMode ? 0f : (1f - u) * Mathf.Sin(u * Mathf.PI)); // calmMode=0

            // Baseline
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

            // Add inner offset
            float brightness = Mathf.Clamp01(baseBright + (agencyIndex - 0.58f) * 0.40f);
            float sharpness  = Mathf.Clamp01(baseSharp  + (coherence   - 0.55f) * 0.50f);
            float lines      = baseLines;
            float phaseN     = basePhase;
            float opacityS   = baseOpacity;

            // Entry pulse
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

        // ======= 8) Debug log =======
        Debug.Log("[" + Time.time.ToString("0.00") + "s] " +
                  "agency=" + agencyIndex.ToString("0.00") + ", " +
                  "coherence=" + coherence.ToString("0.00") + ", " +
                  "state=" + currentState.ToString() +
                  (calmMode ? " (calm)" : ""));
    }

    // ======================== State transition helpers ========================
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