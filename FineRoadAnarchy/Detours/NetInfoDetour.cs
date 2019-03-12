using FineRoadAnarchy.Redirection;

namespace FineRoadAnarchy.Detours
{
    [TargetType(typeof(NetInfo))]
    public class NetInfoDetour : NetInfo
    {
        [RedirectMethod]
        new public float GetMinNodeDistance()
        {
            return 3f;
        }
    }
}
