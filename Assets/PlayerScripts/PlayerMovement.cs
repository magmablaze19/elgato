using System;
using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviour {

    public PlayerControls C = new PlayerControls(KeyCode.W, KeyCode.S, KeyCode.D, KeyCode.A, KeyCode.Space, KeyCode.Q, KeyCode.E, KeyCode.LeftControl, KeyCode.LeftShift);

    //Cringe ass networking shit
    public PhotonView view;

    public float velocity;

    //Assingables
    public Transform playerCam;
    public Transform orientation;
    public Animator animator;
    public GameObject playercapsule;
    
    //Other
    private Rigidbody rb;
    private CapsuleCollider CapCol;

    //Rotation and look
    private float xRotation;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;
    
    //Movement
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public float topSpeed = 40;
    public bool grounded;
    public bool prevgrounded;
    public LayerMask whatIsGround;
    
    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    //Crouch & Slide
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideForce = 400;
    public float slideCounterMovement = 2f;

    //Jumping
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 1700f;
    
    //WallRunning
    public bool WallRunning = false;
    public bool CanWallRun = true;
    public float WallRunDirection;
    public float WallRunModifier;

    //Input
    float x, y;
    public bool jumping, sprinting, crouching;
    
    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    //Vaulting
    private float playerHeight;
    private Vector3 verticalCenterOffset;
    [Range(0,1)]
    public float vaultHeightMax = 0.8f, vaultHeightMin = 0.1f;
    public float maxVaultDistance = 2;
    public int vaultVariety = 6;
    public float fallBound;
    public bool vaulting = false;

    //Grappling Comms
    private Grapplehook hook1;
    private Grapplehook hook2;


    void Awake() {
        rb = GetComponent<Rigidbody>();
        CapCol = GetComponentInChildren<CapsuleCollider>();
    }
    
    void Start() {
        playerScale =  transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        view = GetComponent<PhotonView>();
        playerHeight = CapCol.height * transform.lossyScale.y;
        if (view.IsMine)
        {
            Camera.main.transform.SetParent(playerCam, false);
            Camera.main.transform.localPosition = Vector3.zero;
            playercapsule.layer = 2;
        }
    }


    
    private void FixedUpdate() {
        if (view.IsMine)
        {
            Movement();
        }
    }

    private void Update() {
        velocity = rb.velocity.magnitude;
        if (view.IsMine)
        {
            MyInput();
            Look();
            Animate();
        }
    }

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    private void MyInput() {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetKey(C.jump); 
        crouching = Input.GetKey(KeyCode.LeftControl);
      
        //Crouching
        if (Input.GetKeyDown(KeyCode.LeftControl))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.LeftControl))
            StopCrouch();
    }

    private void StartCrouch() {
        CapCol.height = 1;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        if (grounded && rb.velocity.magnitude > 2f) {
            rb.AddForce(orientation.transform.forward * slideForce);
        }
        if (!grounded) {
        }
    }

    private void StopCrouch() {
        CapCol.height = 2;
    }

    private void GroundCheck() {
            prevgrounded = grounded;
            RaycastHit hitInfo;
            if (Physics.Raycast(rb.transform.position, Vector3.down, out hitInfo, (CapCol.height/2)+.5f))
            {
                grounded = true;
            }
            else
            {
                grounded = false;
            }
            if (!prevgrounded && grounded && jumping)
            {
                jumping = false;
            }
    }

    private void Movement() {

        GroundCheck();
        
        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);
        
        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();

        //Set max speed
        float maxSpeed = this.maxSpeed;
        
        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump) {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;
        
        // Movement in air
        if (!grounded && !WallRunning) {
            rb.AddForce(Vector3.down * Time.deltaTime * (9.8f * rb.mass* 110));
        }
        
        // Movement while sliding I think this is what causes the no friction while crouching?
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);

        WallRun();
    }

    private void Jump() {
        if (grounded && readyToJump) {
            readyToJump = false;

            //Add jump forces
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);
            
            //If jumping while falling, reset y velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0) 
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
            
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }
    
    private void WallRun() {
        //add camera tilt. 
        if (Input.GetKey(C.forward) && Input.GetKey(C.left) && !grounded && Physics.Raycast(rb.position, playerCam.transform.right, 2f) && CanWallRun) {
            if (WallRunning == false){
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            WallRunDirection = -1;
            WallRunning = true;
            rb.useGravity = false;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(orientation.forward * rb.velocity.magnitude);
            rb.AddForce(orientation.forward * 150f);
        }
        else if (Input.GetKey(C.forward) && Input.GetKey(C.right) && !grounded && Physics.Raycast(rb.position, -playerCam.transform.right, 2f) && CanWallRun) {
            if (WallRunning == false){
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            WallRunDirection = 1;
            WallRunning = true;
            rb.useGravity = false;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(orientation.forward * rb.velocity.magnitude);
            rb.AddForce(orientation.forward * 150f);
        }
        else {
            WallRunDirection = 0;
            rb.useGravity = true;
            WallRunning = false;
        }

        if (WallRunning && Input.GetKey(C.jump) && readyToJump)
        {
            float modifier = WallRunModifier;
            rb.useGravity = true;
            //walljump
            Vector3 f = Vector3.up;
            if (WallRunDirection == 1){f = orientation.right.normalized + Vector3.up;}
            else if (WallRunDirection == -1){f = -orientation.right.normalized + Vector3.up;};
            WallRunning = false;
            rb.AddForce(f * jumpForce*1.5f);
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetWallrun(){
        CanWallRun = true;
    }

    private void ResetJump() {
        readyToJump = true;
    }
    

    private float desiredX;
    private void Look() {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;
        
        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);

    }

    private void CounterMovement(float x, float y, Vector2 mag) {
        if (!grounded || jumping) return;

        //Slow down sliding
        if (crouching && grounded) {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0)) {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0)) {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }
        
        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed) {
            float fallspeed = rb.velocity.y;
            //Vector3 n = rb.velocity.normalized * maxSpeed;
            //rb.velocity = new Vector3(n.x, fallspeed, n.z);
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * 1.5f);
        }

    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    public Vector2 FindVelRelativeToLook() {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);
        
        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v) {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private void Animate() {
        if (rb.velocity.magnitude > 1f && grounded) {
            animator.SetBool("Walking", true);
        }
        else{
            animator.SetBool("Walking", false);
        }
        animator.SetBool("Grounded", grounded);
        animator.SetBool("Crouching", crouching);

    }

    private bool cancellingGrounded;
    
    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other) {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++) {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal)) {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }
    }

    private void StopGrounded() {
        grounded = false;
    }
        
    }
[System.Serializable]
public class PlayerControls
{
    public KeyCode forward, backward, left, right, jump, grapple1, grapple2, crouch, sprint;

    public PlayerControls(KeyCode _f, KeyCode _b, KeyCode _l, KeyCode _r, KeyCode _j, KeyCode _g1, KeyCode _g2, KeyCode _c, KeyCode _s)
    {
        forward = _f;
        backward = _b;
        left = _l;
        right = _r;
        jump = _j;
        grapple1 = _g1;
        grapple2 = _g2;
        crouch = _c;
    }
};