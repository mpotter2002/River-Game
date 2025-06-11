using UnityEngine;
using TMPro;

public class FloatingScorePopup : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float lifetime = 1.5f;
    private float currentLifetime;
    private Camera mainCamera;

    private void Awake()
    {
        Debug.Log("[FloatingScorePopup] Awake called.");
        // Get the TextMeshProUGUI component from children
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh == null)
        {
            Debug.LogError("[FloatingScorePopup] TextMeshProUGUI component is missing from children!");
            Destroy(gameObject);
            return;
        }

        // Configure the text
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.enableWordWrapping = false;
        textMesh.raycastTarget = false;

        // Set the RectTransform of the text
        RectTransform rectTransform = textMesh.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(50, 30);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        mainCamera = Camera.main;
        currentLifetime = lifetime;
        Debug.Log("[FloatingScorePopup] Setup complete.");
    }

    private void Update()
    {
        if (mainCamera == null) return;

        // Move upward
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Make text face the camera
        transform.rotation = mainCamera.transform.rotation;

        // Fade out
        currentLifetime -= Time.deltaTime;
        float alpha = currentLifetime / lifetime;
        Color color = textMesh.color;
        color.a = alpha;
        textMesh.color = color;

        // Destroy when lifetime is up
        if (currentLifetime <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void SetScore(int score)
    {
        Debug.Log($"[FloatingScorePopup] SetScore called with value: {score}");
        if (textMesh != null)
        {
            if (score == 10)
            {
                textMesh.text = "+10 sec";
                // Set to purple #440077
                textMesh.color = new Color(0.266f, 0.0f, 0.466f); // #440077
            }
            else
            {
                textMesh.text = score > 0 ? $"+{score}" : score.ToString();
                // Use a darker green for other positive scores
                Color darkGreen = new Color(0.0f, 0.5f, 0.0f); // RGB (0,128,0)
                textMesh.color = score > 0 ? darkGreen : Color.red;
            }
            Debug.Log($"[FloatingScorePopup] Text set to: {textMesh.text}, color: {textMesh.color}");
        }
        else
        {
            Debug.LogWarning("[FloatingScorePopup] textMesh is null in SetScore!");
        }
    }
} 