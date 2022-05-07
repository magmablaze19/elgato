using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class sync_position : MonoBehaviourPunCallbacks, IPunObservable
{
    public Vector3 position;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting){
            stream.SendNext(transform.position);
        }
        else {
            transform.position = (Vector3)stream.ReceiveNext();
        }
    }
}
