using Google.Protobuf;
using System.Net;

namespace hms800w2t;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: hms-800w-2t.exe <ipaddress>");
            return;
        }

        if (!IPAddress.TryParse(args[0], out var address))
        {
            Console.WriteLine($"{args[0]} is not a valid IP address.");
            return;
        }

        IHoymilesInverterConnector connector = new InverterConnector(new IPEndPoint(address, Constants.DtuPort));
        if (connector.TryGetRealDataNew(out var response))
        {
            JsonFormatter formatter = new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation());
            Console.WriteLine(formatter.Format(response));
        }
        else
        {
            Console.WriteLine("Could not retrieve data from inverter.");
        }

        Console.ReadKey();
    }
}
