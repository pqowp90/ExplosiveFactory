using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 최상위 네트워크 엔티티 및 컴포넌트 허브(Context Container).
/// - 이동, 회전, 애니메이션, 입력, 아이템 등 하위 서브시스템 컴포넌트들을 참조하고 조율합니다.
/// - 1인칭 다리 및 Foot IK 설정은 FirstPersonLegsSetup / FirstPersonLegsController에 위임되어 있습니다.
/// </summary>
public class Player : NetworkBehaviour, IPoolable
{
    [Header("Player Components")]
    public ItemHolder ItemHolder { get; private set; }
    public PlayerMove PlayerMove { get; private set; }
    public PlayerRotate PlayerRotate { get; private set; }
    public PlayerAnimation PlayerAnimation { get; private set; }
    public PlayerInput PlayerInput { get; private set; }
    public InputController InputController { get; private set; }
    public FirstPersonLegsSetup LegsSetup { get; private set; }
    public CustomNetworkAnimator CustomNetworkAnimator { get; private set; }
    public LookAtController LookAtController { get; private set; }
    public FootIKController FootIKController { get; private set; }
    public PlayerSkinController PlayerSkinController { get; private set; }

    [Header("Model Transforms")]
    public Transform PlayerBodyTransform;
    public Transform PlayerHandTransform;
    [HideInInspector]
    public Transform PlayerLegTransform;

    [Header("Cameras")]
    public Camera Camera;
    public Camera HandCamera;

    private void Awake()
    {
        ItemHolder = GetComponent<ItemHolder>();
        PlayerMove = GetComponent<PlayerMove>();
        PlayerRotate = GetComponent<PlayerRotate>();
        PlayerAnimation = GetComponent<PlayerAnimation>();
        PlayerInput = GetComponent<PlayerInput>();
        InputController = GetComponent<InputController>();
        LegsSetup = GetComponent<FirstPersonLegsSetup>() ?? gameObject.AddComponent<FirstPersonLegsSetup>();
        CustomNetworkAnimator = GetComponent<CustomNetworkAnimator>() ?? GetComponentInChildren<CustomNetworkAnimator>();
        LookAtController = GetComponent<LookAtController>() ?? GetComponentInChildren<LookAtController>();
        FootIKController = GetComponent<FootIKController>() ?? GetComponentInChildren<FootIKController>();
        PlayerSkinController = GetComponent<PlayerSkinController>();
    }

    /// <summary>
    /// 플레이어 3인칭 모델링을 교체(스왑)하고 모든 애니메이션 및 IK 서브시스템을 새 모델에 맞게 일괄 갱신합니다.
    /// </summary>
    public void SetPlayerBody(Transform newBodyTransform)
    {
        PlayerBodyTransform = newBodyTransform;
        if (newBodyTransform == null) return;

        var newAnimator = newBodyTransform.GetComponentInChildren<Animator>();

        // AnimatorIKForwarder 보장
        if (newAnimator != null && newAnimator.GetComponent<AnimatorIKForwarder>() == null)
        {
            newAnimator.gameObject.AddComponent<AnimatorIKForwarder>();
        }

        if (CustomNetworkAnimator != null)
        {
            CustomNetworkAnimator.SetAnimator(newAnimator);
        }

        if (LookAtController != null)
        {
            LookAtController.RebindModel(newBodyTransform, newAnimator);
        }

        if (FootIKController != null)
        {
            FootIKController.RebindModel(newAnimator);
        }

        if (PlayerAnimation != null)
        {
            PlayerAnimation.RebindBodyAnimator(CustomNetworkAnimator);
        }
    }

    public void OnSpawned()
    {
    }

    public void OnDespawned()
    {
    }
}
