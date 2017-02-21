namespace SimpleUwpTwoWayComms
{
  using System.Linq;
  using System.Net;
  using Windows.Networking.Connectivity;

  internal static class IPAddressExtensions
  {
    public static IPAddress GetForLocalInternetProfile()
    {
      // TODO: Find a better way of doing everything in this function :-)
      var internetProfile = NetworkInformation.GetInternetConnectionProfile();

      var hostName =
        NetworkInformation.GetHostNames().SingleOrDefault(
          name =>
            name.IPInformation?.NetworkAdapter?.NetworkAdapterId ==
              internetProfile.NetworkAdapter.NetworkAdapterId);

      return (IPAddress.Parse(hostName.ToString()));
    }
  }
}
