using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("--- Movement Settings ---")]
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("--- Jump Settings ---")]
    [SerializeField] private float jumpHeight = 9f;
    [SerializeField] private float gravity = 20f;

    [Header("--- Dodge Settings ---")]
    [SerializeField] private float dodgeDistance = 5f;
    [SerializeField] private float dodgeDuration = 0.35f;
    [SerializeField] private float dodgeCooldown = 0.8f;
    [SerializeField] private float dodgeStaminaCost = 25f;

    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // fart
    [SerializeField] private AnimationCurve gravityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // gravitation

    [Header("--- Ledge Grab Settings ---")]
    [SerializeField] private float ledgeGrabHeight = 1.3f;
    [SerializeField] private float ledgeCheckDistance = 0.6f;
    [SerializeField] private float hangPositionHeight = 0.6f;
    [SerializeField] private float hangPositionForward = 0.3f;
    [SerializeField] private LayerMask ledgeMask;

    private Vector3 dodgeDirection;
    private float dodgeTimer;
    private bool isDodging;
    private float lastDodgeTime = -999f;

    private bool onGround = true;
    private float vSpeed = -10f;
    private int jumpCount = 0;

    private bool isLedgeGrabbing = false;
    private Vector3 lastLedgeNormal;

    private CombatController combat;
    private Animator anim;
    private CharacterController cont;
    private Camera mainCamera;
    private SoundController sound;
    private PlayerStats playerStats;
    private InputHandler input;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        cont = GetComponent<CharacterController>();
        combat = GetComponent<CombatController>();
        sound = GetComponent<SoundController>();
        playerStats = GetComponent<PlayerStats>();
        input = GetComponent<InputHandler>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!isDodging && !isLedgeGrabbing)
        {
            Movement();
            Jumping();
        }
        else if (isDodging)
        {
            UpdateDodge();
        }

        // ✅ alltid kolla efter kant
        if (!onGround && !isLedgeGrabbing)
            CheckLedgeGrab();

        combat?.SetGrounded(onGround);

        if (input.DodgePressed && !isDodging && !isLedgeGrabbing && Time.time > lastDodgeTime + dodgeCooldown)
        {
            if (playerStats.stamina >= dodgeStaminaCost)
                StartDodge();
        }
    }

    // === Movement ===
    void Movement()
    {
        if (combat.IsAttacking) return;

        Vector3 move = new Vector3(input.MoveInput.x, 0, input.MoveInput.y);
        Vector3 moveDirection = mainCamera.transform.forward * move.z + mainCamera.transform.right * move.x;
        moveDirection.y = 0f;
        moveDirection.Normalize();

        if (moveDirection.sqrMagnitude > 0f)
        {
            if (cont.isGrounded) sound.PlayFootstep();

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            Vector3 localMove = transform.InverseTransformDirection(moveDirection);
            anim.SetFloat("Horizontal", localMove.x, 0.1f, Time.deltaTime);
            anim.SetFloat("Vertical", localMove.z, 0.1f, Time.deltaTime);
        }
        else
        {
            anim.SetFloat("Vertical", 0f, 0.1f, Time.deltaTime);
            anim.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);
        }

        Vector3 movement = moveDirection * runSpeed;
        movement.y = vSpeed;

        cont.Move(movement * Time.deltaTime);
    }

    // === Jumping ===
    void Jumping()
    {
        if (combat.IsAttacking) return;

        bool wasGrounded = onGround;
        onGround = cont.isGrounded || IsActuallyGrounded();
        bool landed = anim.GetBool("Ground") != onGround;
        anim.SetBool("Ground", onGround);

        if (onGround)
        {
            if (landed)
            {
                vSpeed = -10f;
                jumpCount = 0;
            }

            if (input.JumpPressed)
            {
                vSpeed = jumpHeight;
                jumpCount = 1;
            }
        }
        else
        {
            if (wasGrounded && jumpCount == 0)
            {
                vSpeed = 0f;
            }
            else
            {
                vSpeed -= gravity * Time.deltaTime;
            }

            if (jumpCount == 1 && input.JumpPressed)
            {
                anim.SetTrigger("DoubleJump");
                vSpeed = jumpHeight;
                jumpCount = 2;
            }
        }

        if (!onGround && jumpCount == 0)
        {
            jumpCount = 1;
        }
    }

    // === Dodge ===
    void StartDodge()
    {
        dodgeDirection = transform.forward; // alltid framåt
        dodgeTimer = 0f;
        isDodging = true;

        playerStats.UseStamina(dodgeStaminaCost, true);
        lastDodgeTime = Time.time;

        anim.SetTrigger("Dodge");
        sound?.PlayDash();
    }

    void UpdateDodge()
    {
        dodgeTimer += Time.deltaTime;
        float t = dodgeTimer / dodgeDuration;

        if (t < 1f)
        {
            float speedFactor = speedCurve.Evaluate(t);
            float dodgeTopSpeed = dodgeDistance / dodgeDuration;
            float dodgeSpeed = Mathf.Lerp(runSpeed, dodgeTopSpeed, speedFactor);

            // ✅ alltid framåtpush, även i luften
            Vector3 move = dodgeDirection * dodgeSpeed * Time.deltaTime;

            // 👇 gör gravitationen lite snällare i början
            float gravityFactor = gravityCurve.Evaluate(t);
            vSpeed -= gravity * gravityFactor * Time.deltaTime;
            move.y = vSpeed * Time.deltaTime;

            cont.Move(move);
        }
        else
        {
            isDodging = false;
        }
    }


    // === Ledge Grab ===
    void CheckLedgeGrab()
    {
        Vector3 forwardCheck = transform.position + Vector3.up * ledgeGrabHeight + transform.forward * ledgeCheckDistance;
        Vector3 wallCheckStart = transform.position + Vector3.up * (ledgeGrabHeight / 2f);

        bool foundTop = Physics.Raycast(forwardCheck, Vector3.down, out RaycastHit topHit, 1.0f, ledgeMask);
        bool foundWall = Physics.Raycast(wallCheckStart, transform.forward, out RaycastHit wallHit, 1.0f, ledgeMask);

        if (foundTop && foundWall && topHit.normal.y > 0.7f)
        {
            // stoppa dodge om vi fångar kant
            if (isDodging) isDodging = false;

            vSpeed = 0f;
            lastLedgeNormal = wallHit.normal;
            float grabOffsetY = 1.35f;
            Vector3 grabPoint = new(wallHit.point.x, topHit.point.y - grabOffsetY, wallHit.point.z);

            StartLedgeClimb(grabPoint);
        }
    }

    void StartLedgeClimb(Vector3 ledgePoint)
    {
        cont.enabled = false;
        isLedgeGrabbing = true;
        anim.SetTrigger("LedgeClimb");
        anim.SetBool("Ground", false);
        vSpeed = 0f;

        Vector3 faceDir = -lastLedgeNormal;
        transform.rotation = Quaternion.LookRotation(faceDir);

        if (transform.position.y > ledgePoint.y)
        {
            transform.position = new Vector3(transform.position.x, ledgePoint.y - 0.05f, transform.position.z);
        }

        float grabOffsetY = hangPositionHeight;
        float controllerRadius = cont.radius;
        float forwardOffset = Mathf.Max(hangPositionForward, controllerRadius + 0.05f);
        Vector3 offset = lastLedgeNormal * forwardOffset + Vector3.down * grabOffsetY;
        Vector3 finalHangPos = ledgePoint + offset;

        StartCoroutine(SmoothLedgeGrabAndEnableRootMotion(finalHangPos));
    }

    public void EndLedgeClimb()
    {
        if (!isLedgeGrabbing) return;

        isLedgeGrabbing = false;
        vSpeed = -10f;

        StartCoroutine(MoveOverLedge(0.1f));
    }

    IEnumerator MoveOverLedge(float duration)
    {
        cont.enabled = true;
        anim.applyRootMotion = true;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            Vector3 rootPos = anim.rootPosition;
            Vector3 delta = rootPos - transform.position;
            Vector3 forwardPush = transform.forward * 0.05f;

            cont.Move(delta + forwardPush);

            yield return null;
        }

        anim.applyRootMotion = false;
    }

    IEnumerator SmoothLedgeGrabAndEnableRootMotion(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;

        cont.enabled = false;
        anim.applyRootMotion = true;

        yield return new WaitForSeconds(0.2f);

        float timeout = 0.2f;
        float timer = 0f;

        while (!anim.GetCurrentAnimatorStateInfo(0).IsName("LedgeClimb") && timer < timeout)
        {
            if (!anim.IsInTransition(0))
                timer += Time.deltaTime;

            yield return null;
        }

        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("LedgeClimb"))
        {
            Debug.LogWarning("❌ LedgeClimb animation failed — forcing recovery");

            anim.ResetTrigger("LedgeClimb");
            anim.applyRootMotion = false;
            isLedgeGrabbing = false;
            cont.enabled = true;
            vSpeed = -10f;

            anim.SetTrigger("ForceIdle");
        }
    }

    // === Ground Check ===
    bool IsActuallyGrounded()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.05f, Vector3.down);
        return Physics.Raycast(ray, out RaycastHit hit, 0.3f);
    }
}
