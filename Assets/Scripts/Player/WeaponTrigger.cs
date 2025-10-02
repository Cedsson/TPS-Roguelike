using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTrigger : MonoBehaviour
{
    public GameObject hitEffectPrefab;
    public float shakeAmount = 0.02f;
    public float hitstopDuration = 0.05f;
    public LayerMask hitLayer;
    public SoundController sound;

    private List<Collider> alreadyHit = new();
    private CameraController cam;
    private BoxCollider boxCollider;
    private bool isHitstopActive = false;
    private bool isSwinging = false;

    private PlayerStats playerStats;

    void Start()
    {
        sound = GetComponentInParent<SoundController>();
        cam = FindObjectOfType<CameraController>();
        boxCollider = GetComponent<BoxCollider>();
        playerStats = GetComponentInParent<PlayerStats>();

        if (boxCollider == null)
        {
            Debug.LogWarning($"⚠️ Ingen BoxCollider hittades på {gameObject.name}");
        }
        else
        {
            boxCollider.enabled = false;
        }
    }

    void Update()
    {
        if (isSwinging && boxCollider != null)
            CheckHits();
    }

    void CheckHits()
    {
        foreach (var hit in Physics.OverlapBox(
            transform.TransformPoint(boxCollider.center),
            boxCollider.size * 0.5f,
            transform.rotation,
            hitLayer))
        {
            HandleHit(hit);
        }
    }

    void HandleHit(Collider hit)
    {
        if (!hit.CompareTag("Enemy") || alreadyHit.Contains(hit)) return;

        alreadyHit.Add(hit);
        sound?.PlaySwordHit();
        cam?.DoCameraShake(shakeAmount, 0.1f);

        // 🔴 Blink-effekt
        DamageFlash flash = hit.GetComponentInChildren<DamageFlash>();
        if (flash != null) flash.Flash();

        // 💥 Träffeffekt
        if (hitEffectPrefab != null)
        {
            Vector3 hitPoint = hit.ClosestPoint(transform.position);
            Quaternion rotation = Quaternion.LookRotation(-transform.forward);
            Instantiate(hitEffectPrefab, hitPoint, rotation);
        }

        if (!isHitstopActive)
            StartCoroutine(HitstopCoroutine(hitstopDuration));
    }

    IEnumerator HitstopCoroutine(float duration)
    {
        isHitstopActive = true;
        Animator anim = GetComponentInParent<Animator>();
        if (anim != null) anim.speed = 0f;
        yield return new WaitForSecondsRealtime(duration);
        if (anim != null) anim.speed = 1f;
        isHitstopActive = false;
    }

    public void StartSwingFromAnimation()
    {
        alreadyHit.Clear();
        isSwinging = true;
    }

    public void EndSwingFromAnimation()
    {
        isSwinging = false;
    }

    void OnDrawGizmosSelected()
    {
        var bc = GetComponent<BoxCollider>();
        if (bc == null) return;

        Gizmos.color = Color.red;
        Vector3 origin = transform.TransformPoint(bc.center);
        Quaternion rotation = transform.rotation;
        Gizmos.matrix = Matrix4x4.TRS(origin, rotation, bc.size);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
