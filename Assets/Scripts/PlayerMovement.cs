using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.UI.Image;

public class PlayerMovement : MonoBehaviour
{
    [Header("Locomotion")]
    public float groundAcceleration;
    public float airAcceleration;
    public float groundDeceleration;
    public float airDeceleration;
    public float speed;
    private bool facingRight;
    private bool shifting;
    public float shiftSpeedMult;
    public float shiftJumpMult;
    public float safeShiftRatio;
    public Vector2 shiftSize;
    public Vector2 shiftOffset;
    private Vector2 standingSize;
    private Vector2 standingOffset;
    private Vector2 lastGroundedPosition;
    [SerializeField] private bool isGrounded;
    private bool wasGrounded;

    [Header("Jump")]
    public float jump;
    public float fallMultiplier;
    private float originalGravityScale;
    private bool jumpRequested;
    private bool jumpReleaseRequested;
    public float jumpCancelDeceleration;
    [SerializeField ] private bool isJumping;
    private bool wasFalling;
    public float hangTime;
    private float hangTimeStart;
    public float coyoteTime;
    private float coyoteTimeStart;
    public float jumpBuffer;
    private float jumpBufferStart;

    [Header("Actions")]
    private bool attackRequested;

    [Header("Other")]
    public BoxCollider2D bodyCol;
    public BoxCollider2D feetCol;

    private PlayerInputActions inputActions;
    private PlayerAbilityHandler abilityHandler;
    private Vector2 moveInput;
    private Rigidbody2D rb;
    public LayerMask collidable;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        abilityHandler = GetComponent<PlayerAbilityHandler>();
        inputActions = new PlayerInputActions();
        facingRight = true;
        standingSize = bodyCol.size;
        standingOffset = bodyCol.offset;
    }

    private void Start()
    {
        originalGravityScale = rb.gravityScale;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;
        inputActions.Player.Jump.performed += _ => RequestJump();
        inputActions.Player.Jump.canceled += _ => jumpReleaseRequested = true;
        inputActions.Player.Attack.performed += _ => abilityHandler.Fire(); 
        inputActions.Player.Sprint.performed += _ => shifting = true;
        inputActions.Player.Sprint.canceled += _ => shifting = false;
        inputActions.Player.Next.performed += _ => abilityHandler.NextItem();
        inputActions.Player.Previous.performed += _ => abilityHandler.PrevItem();
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    void FixedUpdate()
    {
        wasGrounded = isGrounded;
        isGrounded = CheckGrounded();
        if (isGrounded && !wasGrounded) // just landed
        {
            isJumping = false;
            wasFalling = false;
            jumpReleaseRequested = false;
            rb.gravityScale = originalGravityScale;
        }
        else if (!isGrounded && wasGrounded && !isJumping) // if just walked off a platform WITHOUT JUMPING
        {
            coyoteTimeStart = Time.time;
        }

        // hang time
        if (isJumping)
        {
            if (jumpReleaseRequested && rb.linearVelocity.y > 0)
            {
                rb.linearVelocityY = Mathf.Lerp(rb.linearVelocityY, 0, jumpCancelDeceleration * Time.fixedDeltaTime);
            }
            else
            {
                if (!wasFalling && rb.linearVelocity.y < 0)
                {
                    wasFalling = true;
                    hangTimeStart = Time.time;
                }
            }

            if (hangTimeStart + hangTime >= Time.time)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.gravityScale = 0;
            }
            else
            {
                rb.gravityScale = originalGravityScale * fallMultiplier;
            }
        }

        // flip player if you turn around
        if (facingRight && moveInput.x < 0)
        {
            Turn(false);
        }
        else if (!facingRight && moveInput.x > 0)
        {
            Turn(true);
        }

        // movement
        float targetXVelocity = moveInput.x * speed;
        float acceleration = moveInput.x != 0 ? (isGrounded ? groundAcceleration : airAcceleration) : (isGrounded ? groundDeceleration : airDeceleration);
        float xVel;
        if (shifting)
        {
            if (moveInput.x != 0)
            {
                targetXVelocity *= shiftSpeedMult;
            }

            xVel = moveInput.x != 0 ? Mathf.Lerp(rb.linearVelocity.x, targetXVelocity, acceleration * Time.fixedDeltaTime) : Mathf.Lerp(rb.linearVelocity.x, 0, acceleration * Time.fixedDeltaTime);

            if (isGrounded) // safeSneak
            {
                float dist = 0.05f;
                Vector2 origin = new Vector2(transform.position.x - feetCol.size.x / 2 * transform.localScale.x, feetCol.bounds.min.y - dist);
                if (facingRight)
                    origin.x += (1 - safeShiftRatio)/2;
                RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right, feetCol.size.x * safeShiftRatio * transform.localScale.x, collidable);
                if (hit.collider == null)
                {
                    xVel = 0;
                }
            }
        }
        else
            xVel = moveInput.x != 0 ? Mathf.Lerp(rb.linearVelocity.x, targetXVelocity, acceleration * Time.fixedDeltaTime) : Mathf.Lerp(rb.linearVelocity.x, 0, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(xVel, rb.linearVelocity.y);

        // do jump
        if (jumpRequested && !isJumping && (isGrounded || coyoteTimeStart + coyoteTime >= Time.time))
        {
            rb.AddForceY(jump * (shifting ? shiftJumpMult : 1), ForceMode2D.Impulse);
            isJumping = true;
            rb.gravityScale *= fallMultiplier;
            jumpRequested = false;
        }

        // use item
        if (attackRequested)
        {
            abilityHandler.Fire();
            attackRequested = false;
        }

        // shift logic (hitbox & safewalk)
        if (shifting) // LERP SIZES/OFFSETS FOR SMOOTHER COLLISIONS WHEN STANDING UP INTO SOMETHING
        {
            bodyCol.size = shiftSize;
            bodyCol.offset = new Vector2(standingOffset.x, (standingSize.y - shiftSize.y)/-2 + standingOffset.y);
        }
        else
        {
            bodyCol.size = standingSize;
            bodyCol.offset = standingOffset;
        }

        // disable jump if time since pressing jump was longer than the jump buffer
        if (jumpRequested && jumpBufferStart + jumpBuffer < Time.time)
        {
            jumpRequested = false;
        }
    }

    private bool CheckGrounded()
    {
        float detectionRayLength = 0.05f;
        Vector2 origin = new(transform.position.x, feetCol.bounds.min.y);
        Vector2 size = new(feetCol.bounds.size.x, detectionRayLength);
        return Physics2D.BoxCast(origin, size, 0, Vector2.down, detectionRayLength, collidable);
    }

    private void Turn(bool facingR)
    {
        facingRight = facingR;
        transform.Rotate(0, 180 * (facingR ? 1 : -1), 0);
    }

    private void RequestJump()
    {
        jumpRequested = true;
        jumpBufferStart = Time.time;
    }
}
