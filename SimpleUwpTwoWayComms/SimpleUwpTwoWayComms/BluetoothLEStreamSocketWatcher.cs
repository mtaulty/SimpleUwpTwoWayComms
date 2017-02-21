namespace SimpleUwpTwoWayComms
{
  using System;
  using System.Collections.Generic;
  using System.Net;
  using Windows.Devices.Bluetooth.Advertisement;

  class BluetoothLEStreamSocketWatcher
  {
    public event EventHandler<BluetoothLEStreamSocketDiscoveredEventArgs> StreamSocketDiscovered;

    public BluetoothLEStreamSocketWatcher(IPAddress localAddress)
    {
      this.localAddress = localAddress;
      this.uniqueAdvertisements = new List<BluetoothLEStreamSocketAdvertisement>();
    }
    public void Start()
    {
      if (this.bluetoothWatcher != null)
      {
        throw new InvalidOperationException("Already started");
      }
      // Listen for remote advertisements.
      this.bluetoothWatcher = new BluetoothLEAdvertisementWatcher();
      this.bluetoothWatcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(
         new BluetoothLEManufacturerData()
         {
           CompanyId = BluetoothLEStreamSocketAdvertisement.MS_BLUETOOTH_LE_ID
         }
      );
      this.bluetoothWatcher.Received += OnBluetoothAdvertisementSpotted;
      this.bluetoothWatcher.Start();
    }
    public void Stop()
    {
      if (this.bluetoothWatcher != null)
      {
        this.bluetoothWatcher.Received -= this.OnBluetoothAdvertisementSpotted;
        this.bluetoothWatcher.Stop();
        this.bluetoothWatcher = null;
      }
      else
      {
        throw new InvalidOperationException("Not watching");
      }
      this.uniqueAdvertisements.Clear();
    }
    void OnBluetoothAdvertisementSpotted(
      BluetoothLEAdvertisementWatcher sender,
      BluetoothLEAdvertisementReceivedEventArgs args)
    {
      foreach (var item in args.Advertisement.GetManufacturerDataByCompanyId(
        BluetoothLEStreamSocketAdvertisement.MS_BLUETOOTH_LE_ID))
      {
        var advertisement = BluetoothLEStreamSocketAdvertisement.ReadFromBuffer(item.Data);

        if (advertisement != null)
        {
          if ((advertisement.Address.ToString() != this.localAddress.ToString()) &&
            !IsRepeatAdvertisement(advertisement))
          {
            this.StreamSocketDiscovered?.Invoke(this,
              new BluetoothLEStreamSocketDiscoveredEventArgs()
              {
                Advertisement = advertisement
              });

            this.uniqueAdvertisements.Add(advertisement);
          }
        }
      }
    }
    bool IsRepeatAdvertisement(BluetoothLEStreamSocketAdvertisement advertisement)
    {
      return (
        this.uniqueAdvertisements.Exists(
          e => ((e.Port == advertisement.Port) &&
                (e.Address.ToString() == advertisement.Address.ToString()))));
    }
    BluetoothLEAdvertisementWatcher bluetoothWatcher;
    List<BluetoothLEStreamSocketAdvertisement> uniqueAdvertisements;
    IPAddress localAddress;
  }
}
