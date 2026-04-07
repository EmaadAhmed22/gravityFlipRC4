using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public Rigidbody rb;
    public bool flipped = false;

    void Start()
    {
        rb.freezeRotation = true;
    }

    void Update()
    {
           float x = 0;
            float z = 0;

        if (Input.GetKey(KeyCode.D))
            x = 1;
        if (Input.GetKey(KeyCode.A))
            x = -1;
        if (Input.GetKey(KeyCode.W))
            z = 1;
        if (Input.GetKey(KeyCode.S))
            z = -1;
        if (Input.GetKey(KeyCode.Space))
            FlipPlayer();
        Vector3 move = (transform.forward * z + transform.right * x) * speed;
        move.y = rb.linearVelocity.y;
        rb.linearVelocity = move;

        if (Input.GetKeyDown(KeyCode.Space) && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        transform.position += Vector3.forward * Time.deltaTime * speed;    }
     void FlipPlayer()
    {
        flipped = !flipped;
        if (flipped)
        {
            transform.rotation = Quaternion.Euler(0, 0, 100);

        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, 100);

        }
    }

}