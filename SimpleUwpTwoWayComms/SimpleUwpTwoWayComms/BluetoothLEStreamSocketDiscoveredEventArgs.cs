namespace SimpleUwpTwoWayComms
{
  using System;

  public class BluetoothLEStreamSocketDiscoveredEventArgs : EventArgs
  {
    public BluetoothLEStreamSocketAdvertisement Advertisement { get; internal set; }
  }
}
