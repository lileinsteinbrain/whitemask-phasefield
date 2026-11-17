using UnityEngine;
using TMPro;

public class PhaseFieldController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Renderer phaseRenderer;
    [SerializeField] private TextMeshProUGUI stateLabel;

    [Header("Quantum Phase Parameters")]
    [Range(0f, 1f)] public float lambda_M = 0.5f; // Measurement collapse strength
    [Range(0f, 1f)] public float lambda_A = 0.5f; // Agency phase recovery
    [Range(0f, 2f)] public float eta_t = 1f;      // Temporal noise amplitude

    private Color baseColor = Color.white;
    private float coherence = 1f; // ψ coherence index
    private bool isObserved = false;

    void Start()
    {
        if (phaseRenderer == null)
            phaseRenderer = GetComponent<Renderer>();

        UpdateVisual();
    }

    void Update()
    {
        // Left click (or tap) -> simulate measurement collapse
        if (Input.GetMouseButtonDown(0))
        {
            Measure();
        }

        // Long press (or sustained gaze) -> simulate re-phasing (regaining coherence)
        if (Input.GetMouseButton(0))
        {
            RePhase();
        }
    }

    void Measure()
    {
        isObserved = true;
        coherence -= lambda_M * Random.Range(0.4f, 0.8f);
        coherence = Mathf.Clamp01(coherence);
        UpdateVisual();

        if (stateLabel != null)
            stateLabel.text = $"M·ψ → Collapse ({coherence:F2})";
    }

    void RePhase()
    {
        isObserved = false;
        coherence += lambda_A * Time.deltaTime;
        coherence = Mathf.Clamp01(coherence);
        UpdateVisual();

        if (stateLabel != null)
            stateLabel.text = $"A·ψ → Re-Phase ({coherence:F2})";
    }

    void UpdateVisual()
    {
        if (phaseRenderer == null) return;

        float brightness = Mathf.Lerp(0.2f, 1.0f, coherence);
        float fog = Mathf.Lerp(0.6f, 0.05f, coherence);

        Color newColor = baseColor * brightness;
        newColor.a = 1f - fog; // use alpha to represent "visibility"
        phaseRenderer.material.color = newColor;
    }
}