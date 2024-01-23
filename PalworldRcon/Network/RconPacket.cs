using System;
using System.Text;
using PalworldRcon.Logging;

namespace PalworldRcon.Network;

public class RconPacket
{
    public int ID { get; set; }
    public PacketType Type { get; set; }
    public string Body { get; set; }
    public bool IsSuccess { get; set; }

    public RconPacket(byte[] data)
    {
        var intSize = sizeof(int);

        try
        {
            //We're assuming we have valid data from the framer now
            ID = BitConverter.ToInt32(data, 0);
            Type = (PacketType)BitConverter.ToInt32(data, intSize);
            Body = Encoding.UTF8.GetString(data, intSize * 2, (data.Length-intSize*2)-2);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public RconPacket(int id, PacketType type, string body)
    {
        ID = id;
        Type = type;
        Body = body;
    }

    public byte[] GetBytes()
    {
        byte[] resultBytes;

        if (!string.IsNullOrWhiteSpace(Body))
        {
            //We have a body to encode
            var stringBytes = Encoding.ASCII.GetBytes(Body  + '\0');
            resultBytes = new byte[sizeof(int) * 2 + stringBytes.Length];
            Buffer.BlockCopy(stringBytes, 0, resultBytes, sizeof(int)*2, stringBytes.Length);
        }
        else
        {
            //No body, add the additional byte for the null terminator
            resultBytes = new byte[sizeof(int) * 2 + 1];
            resultBytes[^1] = 0;
        }

        var idBytes = BitConverter.GetBytes(ID);
        var typeBytes = BitConverter.GetBytes((int)Type);

        Buffer.BlockCopy(idBytes, 0, resultBytes, 0, idBytes.Length);
        Buffer.BlockCopy(typeBytes, 0, resultBytes, sizeof(int), typeBytes.Length);

        return resultBytes;
    }
}

public enum PacketType
{
    Response = 0,
    AuthRespExec = 2,
    ClientAuth = 3
}