using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject playerPrefab;

    public float MiX;
    public float MaX;
    public float MiY;
    public float MaY;


    // Start is called before the first frame update
    private void Start()
    {
        Vector3 position = new Vector3(Random.Range(MiX,MaX), Random.Range(MiY,MaY), 5);
        PhotonNetwork.Instantiate(playerPrefab.name, position, Quaternion.identity);
    }

    public void Respawn(PhotonView nuts)
    {
        PhotonNetwork.Destroy(nuts);
        Vector3 position = new Vector3(Random.Range(MiX,MaX), Random.Range(MiY,MaY), 5);
        PhotonNetwork.Instantiate(playerPrefab.name, position, Quaternion.identity);
    }

}
