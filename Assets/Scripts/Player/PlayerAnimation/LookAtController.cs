using System.Collections;
using System.Collections.Generic;

using Mirror;

using UnityEngine;

public class LookAtController : MonoBehaviour
{

    public Transform objectToLookAt;
    public float headWeight;
    public float bodyWeight;
    private CustomNetworkAnimator _networkAnimator;
    private Player _player;

    private float _curRotation = 0f;
    private float _realRotation = 0f;

    private void Start()
    {
        _player = GetComponentInParent<Player>();
        _networkAnimator = GetComponent<CustomNetworkAnimator>();
        _curRotation = transform.parent.eulerAngles.y;
    }
    private void Update()
    {
        if (!_player.PlayerAnimation.IsMoving)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(_realRotation, transform.parent.eulerAngles.y)) > 60f)
            {
                _curRotation = transform.parent.eulerAngles.y;
            }
            var deltaAngle = Mathf.DeltaAngle(_realRotation, _curRotation);
            if (Mathf.Abs(deltaAngle) < 3f)
            {
                _player.PlayerAnimation.SetTurn(0);
                _realRotation += deltaAngle * Time.deltaTime * 14f;
            }
            else
            {
                _player.PlayerAnimation.SetTurn(deltaAngle < 0 ? -1 : 1);
                _realRotation += deltaAngle * Time.deltaTime * 7f;
            }
        }
        else
        {
            _curRotation = transform.parent.eulerAngles.y;
            var deltaAngle = Mathf.DeltaAngle(_realRotation, _curRotation);
            _realRotation += deltaAngle * Time.deltaTime * 10f;
        }
        transform.eulerAngles = new Vector3(0, _realRotation, 0);



    }
    private void OnAnimatorIK(int layerIndex)
    {
        if (_networkAnimator != null && objectToLookAt != null)
        {
            _networkAnimator.SetLookAtPosition(objectToLookAt.position);
            _networkAnimator.SetLookAtWeight(1, headWeight, bodyWeight);
        }
    }
}
