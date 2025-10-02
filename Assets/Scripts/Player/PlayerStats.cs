using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("--- Health ---")]
    public float HP = 100f;
    public float maxHP = 100f;

    [Header("--- Healthbar UI ---")]
    public Image healthFill;

    [Header("--- Stamina ---")]
    public float stamina = 100f;
    [HideInInspector] public float displayedStamina = 100f;
    [SerializeField] private float staminaRegenDelay = 1f;
    [SerializeField] private float staminaRegenRate = 20f;
    private float lastStaminaUseTime = -Mathf.Infinity;

    [Header("--- Damage ---")]
    public float damage = 10f;

    private CameraController cam;

    void Start()
    {
        cam = Camera.main.GetComponent<CameraController>();

        if (HP <= 0 || HP > maxHP)
            HP = maxHP;

        displayedStamina = stamina;

        UpdateHealthBar();
    }

    void Update()
    {
        if (Time.time - lastStaminaUseTime >= staminaRegenDelay && stamina < 100f)
        {
            stamina += staminaRegenRate * Time.deltaTime;
            if (stamina > 100f) stamina = 100f;
        }

        float speed = stamina < displayedStamina ? 15f : 5f;
        displayedStamina = Mathf.Lerp(displayedStamina, stamina, Time.deltaTime * speed);

        if (Input.GetKeyDown(KeyCode.T)) TakeDamage(20f);
        if (Input.GetKeyDown(KeyCode.Y)) RegainHP(20f);
        if (Input.GetKeyDown(KeyCode.P)) UpgradeHP(20);
    }

    void UpdateHealthBar()
    {
        if (HP > maxHP) HP = maxHP;
        if (HP < 0) HP = 0;

        if (healthFill != null)
            healthFill.fillAmount = maxHP > 0 ? HP / maxHP : 0f;
    }

    public void UseStamina(float amount, bool instant = false)
    {
        if (instant)
        {
            stamina -= amount;
            if (stamina < 0) stamina = 0;
            lastStaminaUseTime = Time.time;
        }
        else
        {
            StopCoroutine("SmoothDrainStamina");
            StartCoroutine(SmoothDrainStamina(amount));
        }
    }

    IEnumerator SmoothDrainStamina(float amount)
    {
        float drained = 0f;
        float drainSpeed = 100f;

        while (drained < amount)
        {
            float delta = drainSpeed * Time.deltaTime;
            float remaining = amount - drained;

            float drainThisFrame = Mathf.Min(delta, remaining);
            stamina -= drainThisFrame;
            drained += drainThisFrame;

            if (stamina < 0f) stamina = 0f;

            lastStaminaUseTime = Time.time;
            yield return null;
        }
    }

    public void TakeDamage(float amount)
    {
        CombatController combat = GetComponent<CombatController>();
        if (combat != null)
        {
            if (stamina > 0f)
            {
                // stamina drain baserat på attackens styrka
                UseStamina(amount * 0.5f, true);

                // reducera skada till 25% (75% block)
                float reducedDamage = amount * 0.25f;
                HP -= reducedDamage;

                cam.DoCameraShake(0.05f, 0.15f);
                UpdateHealthBar();
                return;
            }
        }

        // 🟥 vanlig damage
        cam.DoCameraShake(0.05f, 0.2f);
        HP -= amount;
        if (HP < 0) HP = 0;
        UpdateHealthBar();
    }

    public void RegainHP(float amount)
    {
        HP += amount;
        if (HP > maxHP) HP = maxHP;
        UpdateHealthBar();
    }

    public void RegainStamina(float amount)
    {
        stamina += amount;
        if (stamina > 100f) stamina = 100f;
    }

    public void UpgradeHP(int upgradeAmount)
    {
        maxHP += upgradeAmount;
        HP += upgradeAmount;
        UpdateHealthBar();
    }

    public void UpgradeDamage(int upgradeAmount)
    {
        damage += upgradeAmount;
    }

    public void DealDamage(GameObject target)
    {
        PlayerStats targetStats = target.GetComponent<PlayerStats>();
        if (targetStats != null)
            targetStats.TakeDamage(damage);
    }
}
