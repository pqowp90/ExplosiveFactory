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
    public override void OnSpawned(Player player)
    {
        base.OnSpawned(player);
        _isOn = false;
        OnOffFlashLight();
    }
    private void OnOffFlashLight()
    {
        _light.gameObject.SetActive(_isOn);
    }
}
