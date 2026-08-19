using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhoneTimeUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI timeText;
    [SerializeField] private TMPro.TextMeshProUGUI dateText;
    private void Update()
    {
        if (timeText == null) return;
        timeText.text = System.DateTime.Now.ToString("HH:mm");
        if (dateText == null) return;
        dateText.text = System.DateTime.Now.ToString("dd/MM/yyyy");
    }
}
