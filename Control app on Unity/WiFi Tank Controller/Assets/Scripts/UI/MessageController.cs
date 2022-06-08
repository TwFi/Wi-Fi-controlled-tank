using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageController : MonoBehaviour
{
    public Text mText;
    public enum MessageType
    {
        success,
        warning,
        fail,
        none
    }

    private const float show_time = 3;//s
    private Color32 success_color = new Color32(20, 255, 20, 255);
    private Color32 warning_color = new Color32(255, 255, 20, 255);
    private Color32 fail_color = new Color32(255, 20, 20, 255);
    private Color32 none_color = new Color32(255, 255, 255, 255);

    private float timer_end_show;
    private bool message_is_active = false;

    private void Awake()
    {
        EventManager.OnMessage.AddListener(ShowMessage);
    }

    void Update()
    {
       UpdateMessage();
    }

    public void ShowMessage(string _text, MessageType m_type)
    {
        timer_end_show = Time.unscaledTime + show_time;
        message_is_active = true;

        mText.text = _text.ToUpper();
        switch(m_type)
        {
            case MessageType.success:
                mText.color = success_color;
                break;
            case MessageType.warning:
                mText.color = warning_color;
                break;
            case MessageType.fail:
                mText.color = fail_color;
                break;
            case MessageType.none:
                mText.color = none_color;
                break;
            default:
                mText.color = Color.white;
                Debug.Log("Incorrect message color!");
                break;
        }
        mText.gameObject.SetActive(true);
    }

    private void UpdateMessage()
    {
        if (message_is_active && Time.unscaledTime > timer_end_show)
        {
            message_is_active = false;

            mText.text = "";
            mText.gameObject.SetActive(false);
        }
    }
}
