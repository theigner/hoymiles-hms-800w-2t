namespace hms800w2t;

public static class ModbusCrc
{
    public static ushort CalculateCRC(Memory<byte> buffer)
    {
        var span = buffer.Span;
        ushort crc = 0xFFFF;

        foreach (var value in span)
        {
            crc ^= value;

            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }

        return crc;
    }
}
