using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class headtracking : MonoBehaviour
{
    public Transform playerCam;
    private Transform transform;
    // Start is called before the first frame update
    void Start(){
        transform = GetComponent<Transform>();
    }

    void Update()
    {
        transform.localRotation = Quaternion.Euler(playerCam.eulerAngles.x,0,0);
    }
}
