using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnergyCircleUI : MonoBehaviour
{
    public Transform target; // Spelaren
    public Vector3 verticalOffset = new(0, 2, 0); // Ovanför spelaren
    public float leftOffset = 1.5f; // Hur långt åt vänster om spelaren (ur kamerans perspektiv)

    public Image fillImage;
    public PlayerStats stats;

    public float fadeSpeed = 4f;

    private Camera cam;
    private CanvasGroup canvasGroup;

    void Start()
    {
        cam = Camera.main;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    void LateUpdate()
    {
        if (target == null || cam == null || stats == null) return;

        float percent = stats.displayedStamina / 100f;
        fillImage.fillAmount = percent;

        // 🎨 Mjuk färgövergång baserat på stamina
        Color targetColor;

        if (percent > 0.5f)
        {
            // Mellan grön och gul
            float t = Mathf.InverseLerp(0.5f, 1f, percent); // 0 → gul, 1 → grön
            targetColor = Color.Lerp(Color.yellow, Color.green, t);
        }
        else
        {
            // Mellan röd och gul
            float t = Mathf.InverseLerp(0f, 0.5f, percent); // 0 → röd, 1 → gul
            targetColor = Color.Lerp(Color.red, Color.yellow, t);
        }

        fillImage.color = Color.Lerp(fillImage.color, targetColor, Time.deltaTime * 10f);

        // 🟢 Fade ut när stamina är 100%
        float targetAlpha = percent < 0.995f ? 1f : 0f;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }

        // 🎯 Position & rotation
        Vector3 left = -cam.transform.right * leftOffset;
        Vector3 worldPos = target.position + verticalOffset + left;
        transform.position = worldPos;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }

}
