using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SensorUIHeadMoveSide : SensorUIElement
{
    public Sprite arrowSprite;
    public Sprite stopSprite;

    Image iconImage;
    RectTransform iconTrans;
    Dictionary<MoveSide, int> iconAngles = new Dictionary<MoveSide, int>();

    private new void Awake()
    {
        isHead = true;
        base.Awake();
        iconImage = this.transform.Find("Icon").GetComponent<Image>();
        iconTrans = this.transform.Find("Icon").GetComponent<RectTransform>();
        if (iconImage == null)
            Debug.Log("Image transform is null!");
        if (iconTrans == null)
            Debug.Log("Rect transform is null!");

        iconAngles.Add(MoveSide.FORWARD, 0);
        iconAngles.Add(MoveSide.BACK, 180);
        iconAngles.Add(MoveSide.LEFT, 90);
        iconAngles.Add(MoveSide.RIGHT, -90);
        iconAngles.Add(MoveSide.FORWARD_LEFT, 45);
        iconAngles.Add(MoveSide.FORWARD_RIGHT, -45);
        iconAngles.Add(MoveSide.BACK_LEFT, 135);
        iconAngles.Add(MoveSide.BACK_RIGHT, -135);

        HTTP_Manager.OnControlDataSended.AddListener(MoveSideUpdate);
    }

    private void Start()
    {
        MoveSideUpdate(((int)MoveSide.NONE).ToString());
    }

    private void MoveSideUpdate(string moveSide)
    {
        MoveSide side = (MoveSide)int.Parse(moveSide);

        if (side != MoveSide.NONE && side != MoveSide.STOP)
        {
            if (!iconImage.enabled)
                iconImage.enabled = true;
            if (iconImage.sprite != arrowSprite)
                iconImage.sprite = arrowSprite;

            iconTrans.eulerAngles = new Vector3(0, 0, iconAngles[side]);
        }
        else if (side == MoveSide.STOP)
        {
            if (!iconImage.enabled)
                iconImage.enabled = true;
            if (iconImage.sprite != stopSprite)
                iconImage.sprite = stopSprite;
        }
        else if (side == MoveSide.NONE)
        {
            if (iconImage.enabled)
                iconImage.enabled = false;
        }
    }
}
