public struct ConnectionMode
{
    public ConnectionType connectionType;
    public string IP;
    public string messageText;
    public MessageController.MessageType messageType;

    public ConnectionMode(ConnectionType connectionType, string IP, string messageText, MessageController.MessageType messageType)
    {
        this.connectionType = connectionType;
        this.IP = IP;
        this.messageText = messageText;
        this.messageType = messageType;
    }
}
