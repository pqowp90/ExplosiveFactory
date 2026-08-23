using UnityEngine;

/// <summary>
/// 유니티 Animator가 부착된 GameObject에서 OnAnimatorIK 이벤트를 받아,
/// 부모의 LookAtController 및 FootIKController로 안전하게 중계(Forwarding)하는 프록시 컴포넌트.
/// - 3인칭 모델링이 교체되어도 자식 모델에 본 컴포넌트만 존재하면 부모의 IK 시스템이 정상 작동합니다.
/// </summary>
[DisallowMultipleComponent]
public class AnimatorIKForwarder : MonoBehaviour
{
    private LookAtController _lookAtController;
    private FootIKController _footIKController;

    private void Awake()
    {
        CacheControllers();
    }

    public void CacheControllers()
    {
        _lookAtController = GetComponent<LookAtController>() ?? GetComponentInParent<LookAtController>();
        _footIKController = GetComponent<FootIKController>() ?? GetComponentInParent<FootIKController>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (_lookAtController == null || _footIKController == null)
        {
            CacheControllers();
        }

        if (_lookAtController != null)
        {
            _lookAtController.OnForwardedAnimatorIK(layerIndex);
        }

        if (_footIKController != null)
        {
            _footIKController.OnForwardedAnimatorIK(layerIndex);
        }
    }
}
