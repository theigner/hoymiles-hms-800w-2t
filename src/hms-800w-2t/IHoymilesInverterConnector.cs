namespace hms800w2t;

public interface IHoymilesInverterConnector
{
    bool TryGetRealDataNew(out RealDataNewReqDTO? response);
}
