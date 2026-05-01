using UnityEngine;

public class PlayerCollision : MonoBehaviour
{

    public PlayerMovement movement;
     void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.collider.name);
        if(collision.collider.tag == "obstacle")
        {
            movement.enabled = false;
        }
    }
}
