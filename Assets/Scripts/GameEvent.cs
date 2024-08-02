using System;

[Serializable]
public class GameEvent
{
    public string Type { get; }
    public string Data { get; }

    public GameEvent(string type, string data)
    {
        Type = type;
        Data = data;
    }
}