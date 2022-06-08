using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

public class MJPEG_DownloadWebHandler : DownloadHandlerScript
{
    public UnityEvent<byte[]> OnMJPEG_Received = new UnityEvent<byte[]>();

    private const byte startCommandByte = 0xFF;
    private const byte startImageByte = 0xD8;
    private const byte endImageByte = 0xD9;

    private List<byte> frameBytes = new List<byte>();
    private bool frameIsWrite = false;
    private bool frameIsReady = false;
    private byte lastByte;

    public MJPEG_DownloadWebHandler() : base() { }
    public MJPEG_DownloadWebHandler(byte[] buffer) : base(buffer) {}
    protected override byte[] GetData() { return null;}

    protected override bool ReceiveData(byte[] byteFromServer, int dataLength)
    {
        if (byteFromServer == null || byteFromServer.Length < 1)
        {
            return false;
        }
        byte currentByte;
        for (int i = 0; i < byteFromServer.Length; i++)
        {
            currentByte = byteFromServer[i];

            if (lastByte == startCommandByte)
            {
                if (currentByte == startImageByte)
                {
                    if (frameIsWrite)
                        frameBytes.Clear();

                    frameBytes.Add(lastByte);
                    frameIsWrite = true;
                }
                else if (currentByte == endImageByte)
                    frameIsReady = true;
            }

            if (frameIsWrite)
            {
                frameBytes.Add(currentByte);
                if (frameIsReady)
                    frameIsWrite = false;
            }

            if (frameIsReady)
            {
                OnMJPEG_Received.Invoke(frameBytes.ToArray());
                frameBytes.Clear();
                frameIsReady = false;
                GetData();
            }

            lastByte = currentByte;
        }

        return true;
    }
}

