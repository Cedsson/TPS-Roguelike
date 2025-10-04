using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject soundPrefab; // 🎵 Prefab med AudioSource (utan AutoDestroy)

    [Header("Footstep Sounds")]
    public AudioClip[] steps;
    public float footstepCooldown = 0.4f;
    public float footStepsVolume = 0.1f;

    [Header("Action Sounds")]
    public float actionVolume = 1.0f;
    public AudioClip jumpSound;
    public AudioClip dashSound;
    public AudioClip meleeSwingSound;

    private float footstepTimer = 0f;

    public void PlayFootstep()
    {
        if (Time.time < footstepTimer || steps.Length == 0) return;
        PlayClip(steps[Random.Range(0, steps.Length)], footStepsVolume, Random.Range(0.9f, 1.1f));
        footstepTimer = Time.time + footstepCooldown;
    }

    public void PlayJump() => PlayClip(jumpSound, actionVolume);
    public void PlayDash() => PlayClip(dashSound, actionVolume);
    public void PlayMeleeSwing() => PlayClip(meleeSwingSound, actionVolume);

    private void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || soundPrefab == null) return;

        GameObject go = Instantiate(soundPrefab, transform.position, Quaternion.identity);
        AudioSource source = go.GetComponent<AudioSource>();

        if (source != null)
        {
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }

        // 🔴 förstör efter 2 sekunder
        Destroy(go, 2f);
    }
}
