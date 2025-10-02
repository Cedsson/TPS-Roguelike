using System.Collections;
using UnityEngine;

// Gör att objekt blinkar rött när de träffas
public class DamageFlash : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.red;

    private Renderer[] renderers;
    private Color[] originalColors;
    private MaterialPropertyBlock propBlock;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();

        originalColors = new Color[renderers.Length];
        propBlock = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(propBlock);
            if (propBlock.HasProperty("_Color"))
            {
                originalColors[i] = propBlock.GetColor("_Color");
            }
            else
            {
                originalColors[i] = Color.white; // fallback
            }
        }
    }

    public void Flash()
    {
        StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", flashColor);
            renderers[i].SetPropertyBlock(propBlock);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", originalColors[i]);
            renderers[i].SetPropertyBlock(propBlock);
        }
    }
}
