using Google.Protobuf;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;

namespace hms800w2t;

public class InverterConnector : IHoymilesInverterConnector
{
    private readonly IPEndPoint _inverterEndPoint;

    private ushort _sequence = 0;

    public InverterConnector(IPEndPoint inverterEndPoint)
    {
        _inverterEndPoint = inverterEndPoint;
    }

    public bool TryGetRealDataNew(out RealDataNewReqDTO? response)
    {
        // Build the request message
        var request = new RealDataNewResDTO()
        {
            TimeYmdHms = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Time = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds),
            Offset = 28800
        };

        if (TrySendRequest(
            Constants.CommandGetRealDataNew,
            request.ToByteArray(), 
            out byte[] responseBytes) && responseBytes.Length > 10)
        {
            response = RealDataNewReqDTO.Parser.ParseFrom(responseBytes);
            return true;
        }

        response = null;
        return false;
    }

    private bool TrySendRequest(ushort command, byte[] request, out byte[] response)
    {
        _sequence = (ushort)((_sequence + 1) & 0xFFFF);                             // Increment the sequence number with every request sent

        var crc16 = ModbusCrc.CalculateCRC(request);                                // Calculate the CRC for the protobuf message
        var len = (ushort)(request.Length + 10);                                    // Length of the request = length of protobuf message + 10 bytes (header)
        var commandBytes = BitConverter.GetBytes(Constants.CommandHeader).Reverse() // Build the command byte sequence from the command header and the command code
                            .Concat(BitConverter.GetBytes(command).Reverse())
                            .ToArray();

        // Create a memorystream that contains all bytes to be sent to the inverter
        // Endianess of values has to be changed -> reverse the byte order
        var message = new MemoryStream();
        message.Write(commandBytes, 0, commandBytes.Length);                        // Header defines which command should be executed
        message.Write(BitConverter.GetBytes(_sequence).Reverse().ToArray(), 0, 2);  // Sequence number of the message
        message.Write(BitConverter.GetBytes(crc16).Reverse().ToArray(), 0, 2);      // CRC16 checksum of the protobuf message
        message.Write(BitConverter.GetBytes(len).Reverse().ToArray(), 0, 2);        // Length of the full message
        message.Write(request, 0, request.Length);

        try
        {
            using (var client = new TcpClient())
            {
                client.Connect(_inverterEndPoint);

                using (var stream = client.GetStream())
                {
                    stream.WriteTimeout = 5000;
                    stream.ReadTimeout = 5000;

                    stream.Write(message.ToArray(), 0, (int)message.Length);

                    var buf = new byte[1024];
                    var read = stream.Read(buf, 0, buf.Length);

                    response = buf.Skip(10).Take(read - 10).ToArray();

                    return true;
                }
            }
        }
        catch (Exception e)
        {
            response = Array.Empty<byte>();
            return false;
        }
    }
}
