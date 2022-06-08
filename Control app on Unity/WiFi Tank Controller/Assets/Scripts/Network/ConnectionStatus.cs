public class ConnectionStatus
{
    public ConnectionStatusType type;
    public PingsData pingData;


    public ConnectionStatus()
    {
        type = ConnectionStatusType.NONE;
        pingData = new PingsData();
    }
      
    public void PingDataSetToZero()
    {
        pingData = new PingsData();
    }
}
