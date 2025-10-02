using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Klass som hanterar närstridsmekanik: attacker, combo-system, soft-lock osv.
public class CombatController : MonoBehaviour
{
    [Header("--- Combo Settings ---")]
    [SerializeField] private float comboResetTime = 1.2f;

    [Header("--- Targeting ---")]
    [SerializeField] private float softLockRange = 5f;
    [SerializeField] private float softLockAngle = 60f;
    [SerializeField] private float minAttackDistance = 1.5f;
    private GameObject currentTargetUIInstance;

    [Header("--- Aim Assist ---")]
    [SerializeField] private float aimAssistRotationSpeed = 10f;

    [SerializeField] private float edgeCheckDistance = 0.6f; // hur långt framför spelaren vi kollar
[SerializeField] private float groundCheckDistance = 1.2f; // hur långt nedåt vi kollar

    [Header("--- Weapon Trigger ---")]
    [SerializeField] private WeaponTrigger weaponTrigger;

    [Header("--- Weapon Sheath ---")]
    [SerializeField] private float sheathDelay = 10f;
    private float lastCombatActionTime = 0f;
    private bool isSheathed = false;
    public GameObject staffHand;
    public GameObject staffBack;

    private Animator anim;
    private SoundController sound;
    private Transform playerRoot;
    private Transform currentTarget;
    private InputHandler input;

    private int comboStep = 0;
    private float lastClickTime = 0f;
    private float comboEndTime = 0f;

    private bool isAttacking = false;
    private bool comboQueued = false;
    private bool isGrounded = true;
    private bool isDashing = false;

    public bool IsAttacking => isAttacking;

    private void Start()
    {
        sound = GetComponent<SoundController>();
        anim = GetComponent<Animator>();
        input = GetComponent<InputHandler>();
        playerRoot = transform;

        // 🟢 Börja med staff sheathed
        staffHand.SetActive(false);
        staffBack.SetActive(true);
        anim.SetFloat("IsSheathed", 0f);
        anim.SetLayerWeight(1, 0f);
        isSheathed = true;
    }

    void Update()
    {
        HandleCombatInput();
        HandleAttackDirectionAndRotation();

        // Auto-sheath om man inte gör något på länge
        if (!isAttacking && isGrounded && !isSheathed)
        {
            if (Time.time - lastCombatActionTime > sheathDelay)
            {
                anim.SetTrigger("Sheath");
                anim.SetLayerWeight(1, 1f);
                isSheathed = true;
            }
        }
    }

    private void HandleCombatInput()
    {
        if (isDashing) return;

        // --- LIGHT ATTACK INPUT ---
        if (input.MeleePressed)
        {
            if (!isGrounded || isDashing) return;

            // 🟢 Om vi redan attackerar, queuea nästa slag istället för att starta om
            if (isAttacking || anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            {
                comboQueued = true;
                return;
            }

            if (Time.time - comboEndTime < 0.3f && comboStep != 5) return;
            if (Time.time - lastClickTime > comboResetTime) comboStep = 0;

            lastClickTime = Time.time;
            TriggerLightAttack();
        }
    }

    private void TriggerLightAttack()
    {
        if (isDashing) return;
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) return;

        // 🛑 Avbryt sheath om det är igång
        if (anim.GetLayerWeight(1) > 0.9f)
        {
            anim.SetLayerWeight(1, 0f);
            AttackSheath();
            isSheathed = false;
        }

        isAttacking = true;

        // Bara starta om från 1 om vi inte redan är i en combo
        if (comboStep == 0)
            comboStep = 1;

        AttackSheath();
        RegisterCombatAction();

        if (currentTarget == null)
            currentTarget = FindSoftLockTarget();

        Vector3 dir = GetAttackDirection();
        if (dir != Vector3.zero) playerRoot.rotation = Quaternion.LookRotation(dir);

        anim.applyRootMotion = true;
        anim.SetInteger("ComboStep", comboStep);
        anim.SetTrigger("Attack");
    }

    private void HandleAttackDirectionAndRotation()
    {
        if (isAttacking || anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            // stoppa rörelse både vid kant och vid för nära target
            LimitDistanceToTarget();
            LimitMovementNearEdge();
            HandleDirectionalTargeting();
            SmoothRotateTowardTarget();
        }
    }

    public void ComboWindowEnd()
    {
        if (isDashing) return;

        if (comboQueued && comboStep < 5)
        {
            comboStep++;
            anim.SetInteger("ComboStep", comboStep);
            anim.SetTrigger("Attack");
            comboQueued = false;

            RegisterCombatAction();
        }
        else
        {
            if (comboStep == 5)
            {
                comboStep = 1;
                anim.SetInteger("ComboStep", comboStep);
                anim.SetTrigger("Attack");

                RegisterCombatAction();
            }
            else
            {
                comboStep = 0;
                anim.SetInteger("ComboStep", 0);
                isAttacking = false;
                comboQueued = false;
                currentTarget = null;
                anim.applyRootMotion = false;
                comboEndTime = Time.time;

                RegisterCombatAction();
            }
        }
    }

    private void HandleDirectionalTargeting()
    {
        Vector3 inputDir = new(input.MoveInput.x, 0f, input.MoveInput.y);
        if (inputDir == Vector3.zero) return;

        Vector3 desiredDir = Camera.main.transform.TransformDirection(inputDir);
        desiredDir.y = 0f;
        desiredDir.Normalize();

        Transform newTarget = FindClosestEnemyInDirection(desiredDir);
        if (newTarget != null && newTarget != currentTarget)
            currentTarget = newTarget;
    }

    private void SmoothRotateTowardTarget()
    {
        if (currentTarget != null)
        {
            Vector3 toTarget = currentTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget == Vector3.zero) return;

            Quaternion targetRot = Quaternion.LookRotation(toTarget);
            playerRoot.rotation = Quaternion.Slerp(playerRoot.rotation, targetRot, Time.deltaTime * aimAssistRotationSpeed);
        }
        else
        {
            Vector3 inputDir = new(input.MoveInput.x, 0f, input.MoveInput.y);
            if (inputDir.sqrMagnitude > 0.01f)
            {
                Vector3 desiredDir = Camera.main.transform.TransformDirection(inputDir);
                desiredDir.y = 0f;
                desiredDir.Normalize();

                Quaternion targetRot = Quaternion.LookRotation(desiredDir);
                playerRoot.rotation = Quaternion.Slerp(playerRoot.rotation, targetRot, Time.deltaTime * aimAssistRotationSpeed);
            }
        }
    }

    private Transform FindClosestEnemyInDirection(Vector3 desiredDirection)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, softLockRange);
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Vector3 toTarget = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(desiredDirection, toTarget);
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (angle < softLockAngle && distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = hit.transform;
            }
        }
        return bestTarget;
    }

    private Transform FindSoftLockTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, softLockRange);
        Transform bestTarget = null;
        float bestAngle = softLockAngle;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Vector3 toTarget = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toTarget);
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (angle < bestAngle && distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = hit.transform;
            }
        }
        return bestTarget;
    }

    private Vector3 GetAttackDirection()
    {
        currentTarget = FindSoftLockTarget();

        if (currentTarget != null)
        {
            Vector3 dir = (currentTarget.position - playerRoot.position).normalized;
            dir.y = 0;
            return dir;
        }

        Vector3 inputDir = new(input.MoveInput.x, 0f, input.MoveInput.y);
        if (inputDir != Vector3.zero)
        {
            Vector3 moveDir = Camera.main.transform.TransformDirection(inputDir);
            moveDir.y = 0;
            return moveDir.normalized;
        }

        return Vector3.zero;
    }

    private void LimitDistanceToTarget()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance < minAttackDistance)
        {
            anim.applyRootMotion = false;
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null && cc.velocity.magnitude > 0.01f)
                cc.Move(Vector3.zero);
        }
        else if (!anim.applyRootMotion)
        {
            anim.applyRootMotion = true;
        }
    }

    private void RegisterCombatAction()
    {
        lastCombatActionTime = Time.time;
        if (isSheathed) isSheathed = false;
    }

    public void CancelAttack()
    {
        if (!isAttacking) return;

        isAttacking = false;
        comboStep = 0;
        anim.SetInteger("ComboStep", 0);
        anim.ResetTrigger("Attack");
        anim.applyRootMotion = false;
        comboQueued = false;
        currentTarget = null;
    }

    public void CancelSheath()
    {
        // Om sheath-animationen är aktiv → stoppa
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Sheath"))
        {
            anim.ResetTrigger("Sheath");
            anim.SetLayerWeight(1, 0f);
            isSheathed = false;

            staffHand.SetActive(true);
            staffBack.SetActive(false);
        }
    }

    public void SetGrounded(bool grounded) => isGrounded = grounded;
    public void SetDodging(bool dashing) => isDashing = dashing;

    public void ActivateWeaponHitbox()
    {
        sound.PlaySwordSwing(); // TODO: byt till staff-ljud
        weaponTrigger?.StartSwingFromAnimation();
    }

    public void DeactivateWeaponHitbox() => weaponTrigger?.EndSwingFromAnimation();

    public void EndSheath()
    {
        anim.SetLayerWeight(1, 0f);
        FadeSheath(0f);
        staffHand.SetActive(false);
        staffBack.SetActive(true);
    }

    public void AttackSheath()
    {
        FadeSheath(1f);
        staffHand.SetActive(true);
        staffBack.SetActive(false);
    }

    private Coroutine sheathFadeCoroutine;
    private void FadeSheath(float targetValue)
    {
        if (sheathFadeCoroutine != null)
            StopCoroutine(sheathFadeCoroutine);
        sheathFadeCoroutine = StartCoroutine(FadeSheathCoroutine(targetValue));
    }

    private IEnumerator FadeSheathCoroutine(float targetValue)
    {
        float startValue = anim.GetFloat("IsSheathed");
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float newValue = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            anim.SetFloat("IsSheathed", newValue);
            elapsed += Time.deltaTime;
            yield return null;
        }

        anim.SetFloat("IsSheathed", targetValue);
    }
    private void LimitMovementNearEdge()
    {
        if (IsNearEdge())
        {
            anim.applyRootMotion = false;
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null && cc.velocity.magnitude > 0.01f)
                cc.Move(Vector3.zero);
        }
        else if (!anim.applyRootMotion && isAttacking) // ✅ bara återställ rootmotion under attack
        {
            anim.applyRootMotion = true;
        }
    }
    private bool IsNearEdge()
    {
        // Kolla en punkt lite framför spelaren
        Vector3 origin = transform.position + transform.forward * edgeCheckDistance;
        Ray ray = new(origin + Vector3.up * 0.1f, Vector3.down);

        // Om vi inte träffar mark = nära en kant
        if (!Physics.Raycast(ray, groundCheckDistance))
        {
            return true;
        }

        return false;
    }
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // --- Edge-check gizmo ---
        Vector3 origin = transform.position + transform.forward * edgeCheckDistance;
        Vector3 end = origin + Vector3.down * groundCheckDistance;
        Gizmos.color = IsNearEdge() ? Color.red : Color.green;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawSphere(end, 0.05f);

        // --- Soft lock arc ---
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f); // blå transparent
#if UNITY_EDITOR
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.DrawSolidArc(
            transform.position,
            Vector3.up,
            Quaternion.Euler(0, -softLockAngle * 0.5f, 0) * transform.forward,
            softLockAngle,
            softLockRange
        );
#endif

        // --- Rita bollar över fiender inom soft lock ---
        Collider[] hits = Physics.OverlapSphere(transform.position, softLockRange);
        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Vector3 toTarget = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toTarget);

            if (angle <= softLockAngle)
            {
                // Liten blå boll över alla potentiella fiender
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(hit.transform.position + Vector3.up * 2f, 0.2f);
            }
        }

        // --- Extra markör på currentTarget ---
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentTarget.position + Vector3.up * 2.5f, 0.3f);
        }
    }


}