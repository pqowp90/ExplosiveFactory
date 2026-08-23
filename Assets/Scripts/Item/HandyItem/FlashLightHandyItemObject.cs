using UnityEngine;

public class FlashLightHandyItemObject : HandyItemObject
{
    [SerializeField] private bool _isOn = false;
    [SerializeField] private Light _light;

    public override void OnAnimationTriggerEvent(int triggerID)
    {
        base.OnAnimationTriggerEvent(triggerID);
        if (triggerID == 0)
        {
            _isOn = !_isOn;
            OnOffFlashLight();
        }
    }

    protected override void OnSetupFirstPerson(Player player)
    {
        base.OnSetupFirstPerson(player);
        _isOn = false;
        OnOffFlashLight();
    }

    protected override void OnSetupThirdPerson(Player player)
    {
        base.OnSetupThirdPerson(player);
        _isOn = false;
        OnOffFlashLight();
    }

    protected override void OnSetupShadowOnly(Player player)
    {
        base.OnSetupShadowOnly(player);
        _isOn = false;
        // 그림자 오브젝트에서는 라이트를 완전히 꺼서 중복 조명 및 1인칭 화면 간섭 방지
        if (_light != null)
        {
            _light.gameObject.SetActive(false);
        }
    }

    private void OnOffFlashLight()
    {
        if (_light != null)
        {
            // 그림자 전용 모드에서는 라이트를 켜지 않음
            if (CurrentAttachMode == HandyAttachMode.ShadowOnly)
            {
                _light.gameObject.SetActive(false);
                return;
            }

            _light.gameObject.SetActive(_isOn);
        }
    }
}
