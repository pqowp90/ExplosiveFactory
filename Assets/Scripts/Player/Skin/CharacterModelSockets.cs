using UnityEngine;

/// <summary>
/// 각 3D 캐릭터 모델 프리팹 루트에 부착되어,
/// 3인칭 손 소켓(오른손/왼손)의 트랜스폼 레퍼런스를 직렬화 필드로 관리하는 컴포넌트입니다.
/// 모델마다 손 모양과 회전축이 다르므로, 이 소켓의 위치/각도를 씬 뷰에서 자유롭게 튜닝할 수 있습니다.
/// </summary>
public class CharacterModelSockets : MonoBehaviour
{
    [Header("Hand Sockets")]
    [Tooltip("오른손 아이템 부착 소켓 트랜스폼")]
    [SerializeField] private Transform _rightHandSocket;

    [Tooltip("왼손 아이템 부착 소켓 트랜스폼")]
    [SerializeField] private Transform _leftHandSocket;

    public Transform RightHandSocket => _rightHandSocket;
    public Transform LeftHandSocket => _leftHandSocket;

    public Transform GetSocket(PlayerHandyType handyType)
    {
        return handyType switch
        {
            PlayerHandyType.Right => _rightHandSocket,
            PlayerHandyType.Left => _leftHandSocket,
            _ => _rightHandSocket
        };
    }

    private void Awake()
    {
        NormalizeSocketsWorldScale();
    }

    /// <summary>
    /// 손 본의 스케일과 상관없이, 소켓 자체의 실제 월드 스케일(lossyScale)이 정확히 (1, 1, 1)이 되도록 로컬 스케일을 역보정합니다.
    /// </summary>
    public void NormalizeSocketsWorldScale()
    {
        NormalizeSocket(_rightHandSocket);
        NormalizeSocket(_leftHandSocket);
    }

    public static void NormalizeSocket(Transform socket)
    {
        if (socket == null || socket.parent == null) return;

        Vector3 parentLossy = socket.parent.lossyScale;
        socket.localScale = new Vector3(
            Mathf.Abs(parentLossy.x) > 0.0001f ? 1f / parentLossy.x : 1f,
            Mathf.Abs(parentLossy.y) > 0.0001f ? 1f / parentLossy.y : 1f,
            Mathf.Abs(parentLossy.z) > 0.0001f ? 1f / parentLossy.z : 1f
        );
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Setup Sockets")]
    private void Reset()
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand != null && _rightHandSocket == null)
            {
                var socket = rightHand.Find("ItemSocket_Right");
                if (socket == null)
                {
                    var newSocket = new GameObject("ItemSocket_Right");
                    newSocket.transform.SetParent(rightHand, false);
                    socket = newSocket.transform;
                }
                _rightHandSocket = socket;
                NormalizeSocket(socket);
            }

            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (leftHand != null && _leftHandSocket == null)
            {
                var socket = leftHand.Find("ItemSocket_Left");
                if (socket == null)
                {
                    var newSocket = new GameObject("ItemSocket_Left");
                    newSocket.transform.SetParent(leftHand, false);
                    socket = newSocket.transform;
                }
                _leftHandSocket = socket;
                NormalizeSocket(socket);
            }
        }
    }
#endif
}
