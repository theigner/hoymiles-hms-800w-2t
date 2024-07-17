using Google.Protobuf;
using System.Net;
using System.Net.Sockets;

namespace hms800w2t;

public class InverterConnector
{
    private readonly IPEndPoint _inverterEndPoint;

    private ushort _sequence = 0;

    public InverterConnector(IPEndPoint inverterEndPoint)
    {
        _inverterEndPoint = inverterEndPoint;
    }

    public bool TryGetInverterState(out RealDataNewReqDTO? response)
    {
        // Build the request message
        var request = new RealDataNewResDTO() 
        {
            TimeYmdHms = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Time = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds),
            Offset = 28800
        };

        // Increment the sequence number
        _sequence = (ushort)((_sequence + 1) & 0xFFFF);

        var requestAsBytes = request.ToByteArray();
        var crc16 = ModbusCrc.CalculateCRC(requestAsBytes);
        var len = (ushort)(requestAsBytes.Length + 10);

        var header = new byte[] { 0x48, 0x4d, 0xa3, 0x11 };            

        var message = new MemoryStream();
        message.Write(header, 0, header.Length);
        message.Write(BitConverter.GetBytes(_sequence).Reverse().ToArray(), 0, 2);
        message.Write(BitConverter.GetBytes(crc16).Reverse().ToArray(), 0, 2);
        message.Write(BitConverter.GetBytes(len).Reverse().ToArray(), 0, 2);
        message.Write(requestAsBytes, 0, requestAsBytes.Length);

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

                    response = RealDataNewReqDTO.Parser.ParseFrom(buf.Skip(10).Take(read - 10).ToArray());

                    return true;
                }
            }
        }
        catch (Exception e)
        {
            // SocketException is thrown when the inverter is offline

            response = null;
            return false;
        }
    }
}
