using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_2 : MonoBehaviour {
    private Rigidbody2D playerRigidbody;
    private Collision playerCollision;
    private Animator playerAnimator;
    private PlayerInputAction playerInputAction;
    private CancellationTokenSource wallGrabToken;
    private UniTask wallGrabTask;

    private Vector2 normalMoveInput;
    private Vector2 moveInput;
    private bool canJump, canWallJump;
    private bool canGrab, isWallGrabbing, hasGrabbed; // Before, During, and After
    private bool isJumpPressed, isJumpCancelling;
    private float coyoteTime, jumpBufferTime;
    private float wallJumpDirection;

    private bool isReadMovementInput = true;

    [SerializeField] private DebugCanvas debugCanvas;

    [Header("Movement Configs")]
    [SerializeField] private float maxSpeed;
    [Tooltip("How fast the object accelerates to max speed")]
    [SerializeField] private float accelerationTime;
    [Tooltip("How fast the object decelerates to zero speed")]
    [SerializeField] private float decelerationTime;

    [Header("Jump Configs")]
    [SerializeField] private float jumpHeight;
    [SerializeField] private Vector2 wallJumpStrength;
    [SerializeField] private float maxFallSpeed;
    [SerializeField] private float coyoteTimeMax;
    [SerializeField] private float jumpBufferTimeMax;
    [SerializeField] private float taskAfterJumpDelay;

    [Header("Apex Configs")]
    [SerializeField] private float jumpApexThreshold;

    [Header("Gravity Configs")]
    [SerializeField] private float normalGravity;
    [SerializeField] private float lowGravityMultiplier;
    [SerializeField] private float fallGravityMultiplier;
    [SerializeField] private float jumpCancelMultiplier;

    [Header("Wall Grab Configs")]
    [SerializeField] private float maxWallGrabTime;
    [SerializeField] private float wallGrabGravityTime;

    private void Awake() {
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerCollision = GetComponent<Collision>();
        playerAnimator = GetComponent<Animator>();

        playerInputAction = new PlayerInputAction();
    }

    private void FixedUpdate() {
        CanPerformActionCheck();
        HandleWallGrab();
        HandleMove();
        HandleJump();
        AdjustGravity();
    }

    private void OnEnable() {
        playerInputAction.Enable();
        playerInputAction.Player.Movement.performed += OnMove;
        playerInputAction.Player.Movement.canceled += OnMove;
        playerInputAction.Player.Jump.performed += OnJump;
        playerInputAction.Player.Jump.canceled += OnJump;
    }

    private void OnDisable() {

        playerInputAction.Player.Movement.performed -= OnMove;
        playerInputAction.Player.Movement.canceled -= OnMove;
        playerInputAction.Player.Jump.performed -= OnJump;
        playerInputAction.Player.Jump.canceled -= OnJump;
        playerInputAction.Disable();
        wallGrabToken.Cancel();
    }

    private void OnMove(InputAction.CallbackContext obj) {
        normalMoveInput = obj.ReadValue<Vector2>();

        /*
         * Key of concept
         * 
         * -1 ------ 0 ------ +1
         * If jump direction is left (-1) : player prohibited go to left. So the Input min is 0.
         * If jump direction is right (1) : player prohibited go to right. So the Input max is 0.
         */
        if (!isReadMovementInput) moveInput = wallJumpDirection == -1 ? new Vector2(Mathf.Min(0, normalMoveInput.x), normalMoveInput.y) : new Vector2(Mathf.Max(0, normalMoveInput.x), normalMoveInput.y);
        else moveInput = normalMoveInput;


        string debugText = $"Is Read Movement Input : {isReadMovementInput}\nNormal Move Input : {normalMoveInput}\nMove Input : {moveInput}";
        debugCanvas.UpdateText(debugText);
    }

    private void OnJump(InputAction.CallbackContext obj) {
        isJumpPressed = obj.ReadValueAsButton();
        jumpBufferTime = jumpBufferTimeMax;
    }

    private void CanPerformActionCheck() {
        /*
         * Key of concept
         * Jump :
         *   - true : isOnGround
         *   - false : !isOnGround
         * 
         * canWallJump :
         *   - true : !isOnGround && isWallGrabbing
         *   - false : isOnGround || !isWallGrabbing
         */

        // ? Should i put coyote time in canWallJump?

        canGrab = moveInput.x != 0;
        canJump = isJumpPressed && (playerCollision.isOnGround || coyoteTime > 0) && jumpBufferTime > 0;
        canWallJump = isJumpPressed && isWallGrabbing && !playerCollision.isOnGround && jumpBufferTime > 0;
    }

    private void HandleMove() {
        if (moveInput.x != 0) {
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
            playerAnimator.SetBool("isWalking", true);
        } else {
            playerAnimator.SetBool("isWalking", false);
        }
        float accel = moveInput.x != 0 ? accelerationTime : decelerationTime;
        float newVelocityX = Mathf.MoveTowards(playerRigidbody.velocity.x, moveInput.x * maxSpeed, (maxSpeed / accel) * Time.fixedDeltaTime);

        // Resetting grab wall chance when jump, wall jump, or touching ground
        playerRigidbody.velocity = new Vector2(newVelocityX, playerRigidbody.velocity.y);
    }

    private void HandleJump() {
        if (canJump || canWallJump) {
            coyoteTime = 0;
            jumpBufferTime = 0;

            if (canJump) {
                float jumpForce = Mathf.Sqrt(2 * jumpHeight * Mathf.Abs(Physics2D.gravity.y) * playerRigidbody.mass * normalGravity * normalGravity);
                playerRigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                TaskAfterJump().Forget();
            } else if (canWallJump) {
                wallJumpDirection = -Mathf.Sign(transform.localScale.x);
                transform.localScale = new Vector3(wallJumpDirection, 1, 1);
                Vector2 jumpForce = new Vector2(wallJumpStrength.x * wallJumpDirection, wallJumpStrength.y) * Mathf.Sqrt(2 * jumpHeight * Mathf.Abs(Physics2D.gravity.y) * playerRigidbody.mass * normalGravity * normalGravity);
                playerRigidbody.gravityScale = normalGravity;
                playerRigidbody.AddForce(jumpForce, ForceMode2D.Impulse);
                TaskAfterJump(true).Forget();
            }
        }

        // Cancel Jump
        if (!isJumpPressed && playerRigidbody.velocity.y > 0) {
            isJumpCancelling = true;
            playerRigidbody.gravityScale = normalGravity * jumpCancelMultiplier;
        }

        coyoteTime = Mathf.Max(0, coyoteTime - Time.fixedDeltaTime);
        jumpBufferTime = Mathf.Max(0, jumpBufferTime - Time.fixedDeltaTime);
    }

    private void HandleWallGrab() {
        /*
         * Key of Concept
         * Player can grabbing wall and stay still when not on ground + touching wall + moving to that wall
         * hasGrabbed : Player only have 1 chance to grab wall, resets everytime player jump, wall jump, or touching ground
         */

        if (playerCollision.isOnWall && !playerCollision.isOnGround && canGrab && !hasGrabbed) {
            hasGrabbed = true;
            isWallGrabbing = true;
            if (wallGrabTask.Status != UniTaskStatus.Pending) {
                playerRigidbody.velocity = Vector2.zero;
                playerRigidbody.gravityScale = 0;
                wallGrabToken?.Cancel();
                wallGrabToken = new CancellationTokenSource();
                wallGrabTask = GravityOnGrabbingWall();
            }
        }

        // Resetting wall grab gravity
        if (!playerCollision.isOnWall || !canGrab) {
            wallGrabToken?.Cancel();
            isWallGrabbing = false;

            // Why inside !playerCollision.isOnWall || !canGrab ? Because this line should be executed when player can't grabbing wall (input vector x isn't towards wall)
            // If player cancelling jump and then grabbing wall, gravity should be zero otherwise change it to normal gravity value
            if (!canGrab && !isJumpCancelling) playerRigidbody.gravityScale = normalGravity;
        }

        // Resetting grab wall chance when jump, wall jump, or touching ground
        if (playerCollision.isOnGround) hasGrabbed = false;


    }

    private void AdjustGravity() {
        if (playerCollision.isOnGround) {
            playerRigidbody.gravityScale = normalGravity;
            isJumpCancelling = false;
            return; // No need to adjust gravity anymore
        }

        // Actually, "return" is optional since "if (isJumpCancelling || isWallGrabbing) return" handle that
        if (isWallGrabbing) {
            isJumpCancelling = false;
            return;
        }

        /*
         * isJumpCancelling => No lerping gravity
         * isWallGrabbing => Don't change gravity (set to 0 while grabbing) to any other gravity value
         */
        if (isJumpCancelling || isWallGrabbing) return;

        //To prevent bug: Flying in air.This code wouldn't affect isWallGrabbing gravity
        if (Mathf.Approximately(playerRigidbody.gravityScale, 0)) playerRigidbody.gravityScale = normalGravity;

        float velocityY = playerRigidbody.velocity.y;

        if (velocityY > 0) { // Apex Threshold to Zero
            playerRigidbody.gravityScale = normalGravity * ChangeGravityOnApex(jumpApexThreshold, 0, velocityY);
        } else {
            if (velocityY > -jumpApexThreshold) { // Zero to Apex Threshold
                playerRigidbody.gravityScale = normalGravity * ChangeGravityOnApex(jumpApexThreshold, 0, velocityY);
            } else { // Apex Threshold to ground
                playerRigidbody.gravityScale = normalGravity * fallGravityMultiplier;
            }
        }

        // Speed protection
        if (velocityY < -maxFallSpeed) playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, -maxFallSpeed);
    }

    private float ChangeGravityOnApex(float threshold1, float threshold2, float value) {
        float apexPoint = Mathf.InverseLerp(threshold1, threshold2, Mathf.Abs(value));
        return Mathf.Lerp(1, lowGravityMultiplier, apexPoint);
    }

    private async UniTask GravityOnGrabbingWall() {
        await UniTask.Delay((int)(maxWallGrabTime * 1000), cancellationToken: wallGrabToken.Token);
        while (isWallGrabbing && !wallGrabToken.IsCancellationRequested) {
            playerRigidbody.gravityScale = Mathf.MoveTowards(playerRigidbody.gravityScale, normalGravity, (1 / wallGrabGravityTime) * Time.fixedDeltaTime);
            await UniTask.Yield(wallGrabToken.Token);
            if (Mathf.Approximately(playerRigidbody.gravityScale, normalGravity)) break;
        }
        playerRigidbody.gravityScale = normalGravity;
    }

    private async UniTaskVoid TaskAfterJump(bool isWallJumping = false) {
        if (isWallJumping) {
            isReadMovementInput = false;
            moveInput = Vector2.zero;
        }
        await UniTask.Delay((int)(taskAfterJumpDelay * 1000));
        hasGrabbed = false;
        isReadMovementInput = true;
        moveInput = normalMoveInput;
    }
}
