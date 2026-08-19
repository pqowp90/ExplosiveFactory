using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMovementAnimation
{
    //animation hash
    protected static readonly int MoveX = Animator.StringToHash("MoveX");
    protected static readonly int MoveY = Animator.StringToHash("MoveY");
    protected static readonly int MoveZ = Animator.StringToHash("MoveZ");
    protected static readonly int Speed = Animator.StringToHash("Speed");
    protected static readonly int Crouch = Animator.StringToHash("Crouch");
    protected static readonly int Run = Animator.StringToHash("Run");
    protected static readonly int Sprint = Animator.StringToHash("Sprint");
    protected static readonly int Grounded = Animator.StringToHash("Grounded");
    protected static readonly int Jump = Animator.StringToHash("Jump");

    void SetMove(Vector3 move);
    void SetSpeed(float speed);
    void SetCrouch(bool crouch);
    void SetRun(bool crouch);
    void SetSprint(bool sprint);
    void SetGrounded(bool grounded);
    void SetJump();
}
