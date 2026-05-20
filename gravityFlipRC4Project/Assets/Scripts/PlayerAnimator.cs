using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator anim;
    private PlayerMovement player;

    private void Start()
    {
        anim = GetComponent<Animator>();
        player = GetComponentInParent<PlayerMovement>();
    }

    private void Update()
    {
        anim.SetBool("isGrounded", player.IsGrounded);
    }
}