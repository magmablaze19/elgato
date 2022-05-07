using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Grapplehook : MonoBehaviourPunCallbacks, IPunObservable
{

    //Layermask to ignore grapples
    public LayerMask IgnoreMe;

    //Player camera object, can be the actual camera or the object the camera is attached to
    public Transform playercamera;

    //player rigidbody
    public Rigidbody rb;

    //player movement script
    public PlayerMovement pm;

    //Variable to store keycode to use grapple
    public UnityEngine.KeyCode pc;

    //variable used to store the hit target
    public Rigidbody target;

    //Object to return hooks to and draw line from
    public Transform startpoint;

    //To be honest? No idea
    private Transform desiredRotation;

    //Dunno what rotationspeed is, but pullspeed is the speed that the grapples pull the player
    private float rotationSpeed = 5f, pullSpeed = 35;

    //state bools
    public bool isgrappling = false;

    public bool ispulling = false;

    public bool isprimary = true;

    //Linerenderer to render grapple lines
    public LineRenderer lineRenderer;

    //Previous grapple velocity for reasons
    private Vector3 prevgrapplevelocity = new Vector3(0f, 0f, 0f);

    //point to grapple to, can probably be removed? 
    public Vector3 grapplepoint;

    //Hook of the grapple, allows linerendering to work in multiplayer and allows player to follow moving objects
    public Transform Obunga;
    private Vector3 prevObungaPosition;

    // Start is called before the first frame update
    void Start()
    {
        //determines if this grapple is the primary or secondary grapple to assign the proper keybind.
        if (!isprimary){
            pc = pm.C.grapple1;
        }
        else{
            pc = pm.C.grapple2;
        }
        lineRenderer.positionCount = 2;
    }

    // Update is called once per frame
    void Update(){
        if (pm.view.IsMine){
            RaycastHit hit;
            if (Input.GetKeyDown(pc))
            {
                if (Physics.Raycast(playercamera.transform.position, playercamera.transform.forward, out hit, 100f, ~IgnoreMe)){
                    target = hit.transform.GetComponent<Rigidbody>();
                    isgrappling = true;
                    grapplepoint = hit.point;
                    //set the parent to adopt the point to and move grapple point by relative vector to parent in isgrappling && !ispulling
                    Obunga.position = grapplepoint;
                    Obunga.SetParent(hit.collider.transform);
                    if (target != null && target.mass < rb.mass)
                    {
                    ispulling = true;
                    }
                }
            }
            else if (Input.GetKeyUp(pc) && isgrappling)
            {
                isgrappling = false;
                ispulling = false;
                prevgrapplevelocity = new Vector3(0f, 0f, 0f);
                Obunga.SetParent(startpoint);
                Obunga.localPosition = new Vector3(0f, 0f, 0f);
                Obunga.localScale = new Vector3(.25f, .25f, .25f);
            }
        }
        lineRenderer.SetPosition(0, startpoint.transform.position);
        lineRenderer.SetPosition(1, Obunga.position);
    }
    void FixedUpdate()
    {
        if (pm.view.IsMine){
            if (isgrappling){
                grapple();
            }
        }
    }

    void grapple()
    {
        if (!ispulling)
        {
            Vector3 grapplevelocity = (grapplepoint - rb.position).normalized;
            Vector3 newvelocity = (rb.velocity + (grapplevelocity.normalized * 1.5f));
            if (newvelocity.x > pullSpeed)
            {
                newvelocity.x = pullSpeed;
            }
            if (newvelocity.y > pullSpeed)
            {
                newvelocity.y = pullSpeed;
            }
            if (newvelocity.z > pullSpeed)
            {
                newvelocity.z = pullSpeed;
            }
            if (newvelocity.x < -pullSpeed)
            {
                newvelocity.x = -pullSpeed;
            }
            if (newvelocity.y < -pullSpeed)
            {
                newvelocity.y = -pullSpeed;
            }
            if (newvelocity.z < -pullSpeed)
            {
                newvelocity.z = -pullSpeed;
            }
            rb.velocity = newvelocity;
            grapplepoint = Obunga.position;
        }
        else if (ispulling)
        {
            Vector3 grapplevelocity = (transform.position - target.position);
            Vector3 newvelocity = ((target.velocity - prevgrapplevelocity) + grapplevelocity.normalized / 2) * 5f;
            if (newvelocity.x > pullSpeed)
            {
                newvelocity.x = pullSpeed;
            }
            if (newvelocity.y > pullSpeed)
            {
                newvelocity.y = pullSpeed;
            }
            if (newvelocity.z > pullSpeed)
            {
                newvelocity.z = pullSpeed;
            }
            if (newvelocity.x < -pullSpeed)
            {
                newvelocity.x = -pullSpeed;
            }
            if (newvelocity.y < -pullSpeed)
            {
                newvelocity.y = -pullSpeed;
            }
            if (newvelocity.z < -pullSpeed)
            {
                newvelocity.z = -pullSpeed;
            }
            target.velocity = newvelocity;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting){
            stream.SendNext(Obunga.position);
            stream.SendNext(isgrappling);
            prevObungaPosition = Obunga.position;
        }
        else {
            Vector3 newObungaPosition = (Vector3)stream.ReceiveNext();
            if ((bool)stream.ReceiveNext()){
                Obunga.parent = null;
                Obunga.position = newObungaPosition;
            }
            else{
                Obunga.parent = startpoint;
                Obunga.localPosition = new Vector3(0f,0f,0f);
            }
        }
    }
}
