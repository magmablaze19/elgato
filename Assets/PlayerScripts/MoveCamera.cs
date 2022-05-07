using UnityEngine;
using Photon.Pun;

public class MoveCamera : MonoBehaviour {

    public Transform player;
    public PlayerMovement pm;
    private Quaternion desiredRotation;
    private Quaternion currentrotation;
    
    void Update() {
        transform.position = player.transform.position;
        if (pm.WallRunning) {
            if (pm.WallRunDirection == 1){
                    transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, -10);
            }
            if (pm.WallRunDirection == -1){
                    transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, 10);
            }
        }
        else {
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, 0);
        }
    }
}