using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SensorUIHeadConnection : SensorUIElement
{
    Color32 goodColor = new Color32(20, 255, 20, 255);
    Color32 lossColor = new Color32(255, 255, 20, 255);
    Color32 noneColor = new Color32(255, 20, 20, 255);

    Image iconImage;
    Text labelText;

    private new void Awake()
    {
        isHead = true;
        base.Awake();
        iconImage = this.transform.Find("Icon").GetComponent<Image>();
        if (iconImage == null)
            Debug.Log("Icon is null!");
        labelText = this.transform.Find("Label").GetComponent<Text>();
        if (labelText == null)
            Debug.Log("Label is null!");

        NetManager.OnConnectionStatusChanged.AddListener(ConnectionStatusUpdate);
    }

    private void ConnectionStatusUpdate(ConnectionStatus status)
    {
        if (status.type == ConnectionStatusType.GOOD || status.type == ConnectionStatusType.LOSS)
        {
            if (status.type == ConnectionStatusType.GOOD)
            {
                iconImage.color = goodColor;
                labelText.text = "Connection GOOD";
            }
            else
            {
                iconImage.color = lossColor;
                labelText.text = "Connection LOSS";
            }

            PingsData  p = status.pingData;

            valueText.text = p.lastLatency.ToString() + " ms " + p.receivedToSentRatio[0].ToString() + "/" + p.receivedToSentRatio[1].ToString();
        }
        else if (status.type == ConnectionStatusType.NONE)
        {
            iconImage.color = noneColor;
            labelText.text = "Connection NO";
            valueText.text = "-/-";
        }
    }
}
