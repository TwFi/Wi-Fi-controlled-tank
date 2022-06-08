using UnityEngine;

public class PingsData
{
    public int lastLatency;
    public int avgLatency;
    public int minLatency;
    public int maxLatency;
    public int[] receivedToSentRatio;

    public void DebugPrintValues()
    {
        Debug.Log("Latency = " + lastLatency + ", AVG = " + avgLatency +
            ", MIN/MAX = " + minLatency + "/" + maxLatency + ", Ratio = " + receivedToSentRatio[0] + "/" + receivedToSentRatio[1]);
    }

    public PingsData()
    {
        lastLatency = 0;
        avgLatency = 0;
        minLatency = 0;
        maxLatency = 0;
        receivedToSentRatio = new int[2];
    }

    public PingsData GetCopy()
    {
        PingsData copy = new PingsData();
        copy.lastLatency = lastLatency;
        copy.avgLatency = avgLatency;
        copy.minLatency = minLatency;
        copy.maxLatency = maxLatency;
        copy.receivedToSentRatio = receivedToSentRatio;

        return copy;
    }
}