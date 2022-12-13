using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class RigidPlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Transform centre;
    private Animator anim;
    public bool isWallRunning = false;
    public bool isClimbing = false;
    public bool isSliding = false;
    [SerializeField] private float speed = 7.67f;
    [SerializeField] private float wallRunSpeed = 10.0f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float airControl = 1.5f;
    [SerializeField] private float climbHeight = 3.0f;
    [SerializeField] private float wallHeightDec = 0.2f;
    [SerializeField] private const float raycastDistance = 1.03f;
    private int maxJumps = 2;
    private int jumps = 0;
    private float debugMaxSpeed;
    private float hitSpeed;
    private float initialHeight;
    private float halfBodyLength;
    private bool sameWall = false;
    private bool jumped = false;
    private Vector3 wallRunDir;
    private Vector3 vectorFrom;
    private Vector3 climbDir;
    private float wallHeightGain;
    private RaycastHit hit;
    private float x;
    private float z;

    public enum checkDir
    {
        Back,
        Left,
        Right,
        Forward,
        None
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        centre = transform.Find("Centre");
        halfBodyLength = Vector3.Distance(rb.position, centre.position);
    }
    private void Update()
    {
        //debugDistance();
        //debugSpeed(true);
        checkSameWall();
        Debug.DrawRay(centre.position, transform.forward);
        Debug.DrawRay(centre.position, -hit.normal);
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump(x, z);
        }
        else if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Slide();
        }
        wallRunDir = getRunDir(TouchingWall());
        anim.SetBool("isGrounded", isGrounded());
        if (x != 0 || z != 0) { anim.SetBool("isMoving", true); }
        else if (x == 0 && z == 0) { anim.SetBool("isMoving", false); }
    }
    private void FixedUpdate()
    {
        Move();
        Climb();
        WallRun(wallRunDir);
    }

    private void debugSpeed(bool max = false)
    {
        Debug.Log(rb.velocity.magnitude);
        if (max)
        {
            if (rb.velocity.magnitude > debugMaxSpeed)
            {
                debugMaxSpeed = rb.velocity.magnitude;
            }
        }
    }

    private void debugDistance()
    {
        if (!isGrounded())
        {
            jumped = true;
        }
        if (isGrounded())
        {
            if (jumped)
            {
                Debug.Log(Vector3.Distance(vectorFrom, transform.position));
                jumped = false;
            }
            vectorFrom = transform.position;
        }
    }

    private void Move()
    {
        Vector3 inputVector = new Vector3(0, 0, 0);
        if (!isGrounded() && !isWallRunning)
        {
            inputVector = new Vector3(x, 0, 0).normalized * (speed * airControl);
            Vector3 newMag = rb.velocity + inputVector;
            Vector3 balance = new Vector3(0, 0, -(Mathf.Abs(x)));
            rb.AddRelativeForce(inputVector);
            rb.AddRelativeForce(balance);
        }
        else if (isGrounded() && !isWallRunning && !isSliding)
        {
            inputVector = new Vector3(x, 0, z).normalized;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                inputVector *= speed * 1.5f;
                anim.SetBool("isRunning", true);
            }
            else
            {
                inputVector *= speed;
                anim.SetBool("isRunning", false);
            }
            inputVector.y = rb.velocity.y;
            rb.velocity = transform.TransformDirection(inputVector);
        }
    }

    private void Jump(float x, float z)
    {
        if (isGrounded())
        {
            jumps = 0;
            jumps++;
            rb.AddRelativeForce(x * speed, jumpForce, z * speed, ForceMode.Impulse);
        }
        else if (isWallRunning)
        {
            jumps = 0;
            jumps++;
            isWallRunning = false;
            rb.AddRelativeForce(x * speed, jumpForce, z * speed, ForceMode.Impulse);
            rb.AddForce(hit.normal * (speed / 2), ForceMode.Impulse);
        }
        else if (!isGrounded() && jumps < maxJumps)
        {
            if (jumps == 0) { jumps++; }
            jumps++;
            rb.AddRelativeForce(x * speed * 0.5f, jumpForce, z * speed * 0.5f, ForceMode.Impulse);
        }
    }
    private bool isGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, raycastDistance);
    }
    public checkDir TouchingWall(float raycastDist = raycastDistance, bool allDirections = true)
    {
        if (Physics.Raycast(centre.position, -transform.right, raycastDist))
        { return checkDir.Left; }
        else if (Physics.Raycast(centre.position, transform.right, raycastDist))
        { return checkDir.Right; }
        if (allDirections)
        {
            if (Physics.Raycast(centre.position, -transform.forward, raycastDist))
            { return checkDir.Back; }

            else if (Physics.Raycast(centre.position, transform.forward, raycastDist))
            { return checkDir.Forward; }
        }
        return checkDir.None;
    }

    private Vector3 getRunDir(checkDir wall)
    {
        switch(wall)
        {
            case checkDir.Left:
                Physics.Raycast(centre.position, -transform.right, out hit, raycastDistance);
                break;
            case checkDir.Right:
                Physics.Raycast(centre.position, transform.right, out hit, raycastDistance);
                break;
            case checkDir.Forward:
                Physics.Raycast(centre.position, transform.forward, out hit, raycastDistance);
                break;
            default:
                return new Vector3(0, 0, 0);
        }
        Vector3 temp = new Vector3(hit.normal.z, 0, hit.normal.x); //swapped to give travel direction
        if (Vector3.Angle(temp, transform.forward) > 90)
        {
            temp = temp * -1;
        }
        if (Vector3.Angle(transform.forward, -hit.normal) < 15)
        {
            return new Vector3(0, 0, 0);
        }
        return temp;
    }

    private void WallRun(Vector3 runDir)
    {
        Vector3 curDir = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (!isGrounded() && !isWallRunning && runDir != new Vector3(0, 0, 0) && !sameWall && Vector3.Angle(transform.forward, curDir) < 90)
        {
            wallHeightGain = 10f;
            hitSpeed = rb.velocity.magnitude;
            rb.velocity = runDir * hitSpeed;
            isWallRunning = true;
            sameWall = true;
        }
        if (isWallRunning && TouchingWall() == checkDir.None)
        {
            isWallRunning = false;
        }
        if (isWallRunning)
        {
            if (isGrounded())
            {
                isWallRunning = false;
            }
            else
            {
                rb.AddForce(new Vector3(0, (-Physics.gravity.y - 5) + wallHeightGain, 0));
                if (hitSpeed > wallRunSpeed && (Mathf.Pow(rb.velocity.x, 2.0f) + Mathf.Pow(rb.velocity.z, 2.0f)) > Mathf.Pow(wallRunSpeed, 2))
                {
                    rb.AddForce(((rb.velocity.x*-1)/5), 0, ((rb.velocity.z * -1 )/5));
                }
                else if(hitSpeed < wallRunSpeed && (Mathf.Pow(rb.velocity.x, 2.0f) + Mathf.Pow(rb.velocity.z, 2.0f)) < Mathf.Pow(wallRunSpeed, 2))
                {
                    rb.AddForce((rb.velocity.x / 3), 0, (rb.velocity.z / 3));
                }
                wallHeightGain -= wallHeightDec;
            }
        }
        else
        {
            isWallRunning = false;
        }
    }

    private void Climb()
    {
        if (TouchingWall() == checkDir.Forward && !isWallRunning && ((Input.GetKeyDown(KeyCode.Space) || !isGrounded()) && !isClimbing && !sameWall))
        {
            climbDir = transform.forward;
            isClimbing = true;
            initialHeight = transform.position.y;
            Physics.Raycast(centre.position, transform.forward, out hit, raycastDistance);
            sameWall = true;
            Debug.Log(initialHeight);
        }
        if (isClimbing)
        {
            if (transform.position.y < initialHeight + climbHeight && TouchingWall() == checkDir.Forward)
            {
                rb.velocity = new Vector3(0, -Physics.gravity.y - 30f, 0);
            }
            else if (transform.position.y < initialHeight + climbHeight && TouchingWall() != checkDir.Forward)
            {
                Debug.Log("Triggered");
                rb.MovePosition(new Vector3(rb.position.x + 1.5f * climbDir.normalized.x, rb.position.y + halfBodyLength, rb.position.z + 1.5f * climbDir.normalized.z));
                isClimbing = false;
            }
            else
            {
                isClimbing = false;
                Debug.Log("Stopped Climbing");
                Debug.Log(transform.position.y - initialHeight);
            }
        }
    }

    private void Slide()
    {
        if (isGrounded() && !isWallRunning && !isClimbing)
        {
            isSliding = true;
            Debug.Log(rb.velocity);
            rb.velocity = rb.velocity * 1.5f;
        }
        if (isSliding)
        {
            rb.AddRelativeForce(new Vector3(0, 0, 0));
            if (rb.velocity == Vector3.zero || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftControl))
            {
                isSliding = false;
            }
        }
    }

    private void checkSameWall()
    {
        if (sameWall)
        {
            bool hitwall = Physics.Raycast(centre.position, -hit.normal, raycastDistance + 0.1f);
            if (hitwall && sameWall)
            {
                sameWall = true;
            }
            else if (!hitwall && sameWall)
            {
                sameWall = false;
            }
        }
    }
}
