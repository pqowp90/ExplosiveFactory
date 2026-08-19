using System;
using UnityEngine;

public class AnimationTriggerEventHolder : MonoBehaviour
{
    private Action<int> _onAnimationTriggerEvent;
    public void SetOnAnimationTriggerEvent(Action<int> onAnimationTriggerEvent)
    {
        _onAnimationTriggerEvent = onAnimationTriggerEvent;
    }
    public void OnAnimationTriggerEvent(int triggerID)
    {
        _onAnimationTriggerEvent?.Invoke(triggerID);
    }
}
