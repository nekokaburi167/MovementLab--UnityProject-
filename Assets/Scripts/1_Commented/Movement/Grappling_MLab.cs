using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Dave MovementLab - Grappling
///
// Content:
/// - swinging ability
/// - grappling ability
/// 
// Note:
/// This script handles starting and stopping the swinging and grappling ability, as well as moving the player
/// The grappling rope is drawn and animated by the GrapplingRope_MLab script
/// 
/// If you don't understand the difference between swinging and grappling, please read the documentation
/// 
// Also, the swinging ability is based on Danis tutorial
// Credits: https://youtu.be/Xgh4v1w5DxU


public class Grappling_MLab: MonoBehaviour
{
    [Header("ToggleAbilites")]
    public bool EnableSwingingWithForces = true;
    public GrappleMode grappleMode = GrappleMode.Precise;

    [Header("References")]
    public Transform orientation;

    [Header("Swinging")]
    public LayerMask whatIsGrappleable; // you can grapple & swing on all objects that are in this layermask
    public Transform gunTip;
    public Transform cam;
    public float maxSwingDistance = 25f; // max distance you're able hit objects for swinging ability

    private SpringJoint joint; // for swining we use Unitys SpringJoint component
    public float spring = 4.5f; // spring of the SpringJoint component
    public float damper = 7f; // damper of the SpringJoint component
    public float massScale = 4.5f; // massScale of the SpringJoint component

    [Header("Grappling")]
    public float maxGrappleDistance = 25f; // max distance you're able to grapple onto objects
    public float grappleDelayTime = 0.5f; // the time you freeze in the air before grappling
    public float grappleForce = 20f;
    public float grappleUpwardForce = 5f;
    public float grappleDistanceMultiplier = 0.1f; // how much more force you gain when grappling toward objects that are further away

    public float grapplingCd = 2.5f; // cooldown of your grappling ability
    private float grapplingCdTimer;

    public float overshootYAxis = 2f; // adjust the trajectory hight of the player when grappling (only in precise mode)

    public enum GrappleMode
    {
        Basic,
        Precise
    }

    [Header("Input")]
    //public KeyCode swingKey = KeyCode.Mouse0;
    public KeyCode grappleKey = KeyCode.Mouse1;

    private Rigidbody rb;

    private Vector3 grapplePoint; // the point you're grappling to / swinging on

    private bool tracking;

    private bool grappleExecuted;

    private PlayerMovement_MLab pm;

    private bool grappling;

    private void Start()
    {
        // if you don't set whatIsGrappleable to anything, it's automatically set to Default
        if (whatIsGrappleable.value == 0)
            whatIsGrappleable = LayerMask.GetMask("Default");

        // get references
        pm = GetComponent<PlayerMovement_MLab>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // cooldown timer
        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;

        // make sure MyInput() is called every frame
        MyInput();

        if (EnableSwingingWithForces && joint != null) OdmGearMovement();
    }

    private void MyInput()
    {
        // StartSwing() is called when you press down the swingKey
        //if (Input.GetKeyDown(swingKey)) StartSwing();
        // StopSwing() is called when you release the swingKey
        //if (Input.GetKeyUp(swingKey)) StopSwing();

        // StartGrapple() is called when you press down the grappleKey
        if (Input.GetKeyDown(grappleKey)) StartGrapple();
        // TryStopGrapple() is called when you release the grappleKey
        if (Input.GetKeyUp(grappleKey)) TryStopGrapple();
    }

    #region Swinging

    private Transform grappleObject;
    public void StartSwing()
    {
        // this will cause the PlayerMovement script to enter MovementMode.swinging
        pm.swinging = true;

        /// shoot a raycast from your cameras position forward, store the hit date in the RaycashHit variable called "hit",
        /// use your maxSwingDistance as distance for the raycast and check if the object is in the whatIsGrappleable layer
        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxSwingDistance, whatIsGrappleable))
        {
            // the grappleObject is the object the raycast hit
            grappleObject = hit.transform;
            tracking = true;

            // the exact point where you swing on
            grapplePoint = hit.point;

            // add a springJoint component to your player
            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;

            // set the anchor of the springJoint
            joint.connectedAnchor = grapplePoint;

            // calculate the distance to the grapplePoint
            float distanceFromPoint = Vector3.Distance(transform.position, grapplePoint);

            // the distance grapple will try to keep from grapple point.
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;

            // adjust these values to fit your game
            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;
        }
    }

    public void StopSwing()
    {
        pm.swinging = false;

        tracking = false;

        // destroy the SpringJoint again after you stopped swinging 
        Destroy(joint);
    }

    #endregion

    /// Here you'll find all of the code specificly needed for the grappling ability
    #region Grappling

    public void StartGrapple()
    {
        // in cooldown
        if (grapplingCdTimer > 0) return;

        // set cooldown
        grapplingCdTimer = grapplingCd;

        grappling = true;

        // shoot a raycast (same as above with swinging ability)
        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            // this will cause the PlayerMovement script to change to MovemementMode.freeze
            /// -> therefore the player will freeze mid-air for some time before grappling
            pm.freeze = true;

            // same stuff as in StartSwing() function
            grappleObject = hit.transform;
            tracking = true;

            grapplePoint = hit.point;

            // call the ExecuteGrapple() function after the grappleDelayTime is over
            Invoke(nameof(ExcecuteGrapple), grappleDelayTime);
        }

        // if the raycast didn't hit anything:
        else
        {
            // we still want to freeze the player for a bit
            pm.freeze = true;

            // the grapple point is now just a point in the air
            /// calculated by taking your cameras position + the forwardDirection times your maxGrappleDistance
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;

            // call the StopGrapple() function after the grappleDelayTime is over
            Invoke(nameof(StopGrapple), grappleDelayTime);
        }
    }

    public void ExcecuteGrapple()
    {
        // make sure that the player can move again
        pm.freeze = false;

        if(grappleMode == GrappleMode.Precise)
        {
            // find the lowest point of the player
            Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

            // calculate how much higher the grapple point is relative to the player
            float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
            // calculate the highest y position that the player should reach when grappling
            float highestPointOfArc = grapplePointRelativeYPos + overshootYAxis;

            // no upwards force when point is below player
            if (grapplePointRelativeYPos < 0) highestPointOfArc = overshootYAxis;

            print("trying to grapple to " + grapplePointRelativeYPos + " which arc " + highestPointOfArc);

            pm.JumpToPosition(grapplePoint, highestPointOfArc, default, 3f);
        }

        if(grappleMode == GrappleMode.Basic)
        {
            // calculate the direction from the player to the grapplePoint
            Vector3 direction = (grapplePoint - transform.position).normalized;

            // reset the y velocity of your rigidbody
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // the further the grapple point is away, the higher the distanceBoost should be
            float distanceBoost = Vector3.Distance(transform.position, grapplePoint) * grappleDistanceMultiplier;

            // apply force to your rigidbody in the direction towards the grapplePoint
            rb.AddForce(direction * grappleForce, ForceMode.Impulse);
            // also apply upwards force that scales with the distanceBoost
            rb.AddForce(Vector3.up * grappleUpwardForce * distanceBoost, ForceMode.Impulse);
            /// -> make sure to use ForceMode.Impulse because you're only applying force once
        }

        // Stop grapple after a second, (by this time you'll already have travelled most of the distance anyway)
        Invoke(nameof(StopGrapple), 1f);

        grappleExecuted = true;
    }

    /// called on right click release
    public void TryStopGrapple()
    {
        // if release the right click before the grapple was executed nothing happens
        /// -> the grapple ability will still be called
        if (!grappleExecuted) return;

        StopGrapple();
    }

    private void StopGrapple()
    {
        // make sure player can move
        if(pm.freeze) pm.freeze = false;

        // reset the grappleExecuted bool
        grappleExecuted = false;

        grappling = false;
    }

    public void OnObjectTouch()
    {
        if (grappleExecuted) StopGrapple();
    }

    #endregion

    #region Odm Gear

    [Header("OdmGear")]
    public float horizontalThrustForce;
    public float forwardThrustForce;
    public float extendCableSpeed;
    private void OdmGearMovement()
    {
        if (Input.GetKey(KeyCode.D)) rb.AddForce(orientation.right * horizontalThrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.A)) rb.AddForce(-orientation.right * horizontalThrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.W))
        {
            Vector3 directionToPoint = grapplePoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.deltaTime);

            // calculate the distance to the grapplePoint
            float distanceFromPoint = Vector3.Distance(transform.position, grapplePoint);

            // the distance grapple will try to keep from grapple point.
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            // calculate the distance to the grapplePoint
            float distanceFromPoint = Vector3.Distance(transform.position, grapplePoint) + extendCableSpeed;

            // the distance grapple will try to keep from grapple point.
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
        }
    }

    #endregion

    #region Tracking Objects

    // Important Note: function currently not being used, I'll implement that soon
    private void TrackObject()
    {
        ///Calculate direction
        Vector3 direction = transform.position - grappleObject.position;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, maxSwingDistance))
        {
            grapplePoint = hit.point;
            joint.connectedAnchor = grapplePoint;
        }
    }

    #endregion

    #region Getters

    private Vector3 currentGrapplePosition;

    // a bool to check if we're currently swinging or grappling
    /// function needed and called from the GrapplingRope_MLab script
    public bool IsGrappling()
    {
        return joint != null || grappling;
    }

    // a Vetor3 to quickly access the grapple point
    /// function needed and called from the GrapplingRope_MLab script
    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }

    #endregion

    /// just to visualize the maxGrappleRange inside of Unity
    #region Gizmos Visualisation

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Vector3 direction = (grapplePoint - transform.position).normalized;
        Gizmos.DrawRay(transform.position, direction * maxGrappleDistance);
    }

    #endregion
}
