using System.Collections;
using UnityEngine;

public class PlayerCamera : MonoBehaviour {

    public bool isAskew = false;
    public float speed;
    private Vector3 offset;         //Private variable to store the offset distance between the player and camera

    void Start()
    {
        speed = .1f;
        if(isAskew)
            transform.Rotate(0, 0, 45);
    }

    // LateUpdate is called after Update each frame
    void Update()
    {
        float horizontalSpeed = Input.GetAxis("Horizontal") * speed;
        float verticalSpeed = Input.GetAxis("Vertical") * speed;
        transform.Translate(horizontalSpeed, verticalSpeed, 0);
    }
}