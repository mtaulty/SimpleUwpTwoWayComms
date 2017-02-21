namespace SimpleUwpTwoWayComms
{
  using System;
  using System.Diagnostics;
  using System.Net;
  using System.Threading.Tasks;
  using Windows.Devices.Bluetooth.Advertisement;
  using Windows.Networking;
  using Windows.Networking.Sockets;

  public class StreamSocketEventArgs : EventArgs
  {
    public StreamSocket Socket { get; internal set; }
  }
  public class BluetoothLEStreamSocketAdvertiser
  {
    public event EventHandler<StreamSocketEventArgs> ConnectionReceived;

    public BluetoothLEStreamSocketAdvertiser()
    {
    }
    public BluetoothLEStreamSocketAdvertiser(
      IPAddress address, 
      StreamSocketListener listener) :this()
    {
      this.address = address;
      this.SocketListener = listener;
    }
    public async Task StartAsync()
    {
      if (this.address == null)
      {
        await this.CreateForLocalInternetProfileAsync();
      }
      this.SocketListener.ConnectionReceived += OnConnectionReceived;

      // Advertise it over Bluetooth LE.
      var advert = new BluetoothLEStreamSocketAdvertisement(
        address, 
        ushort.Parse(this.SocketListener.Information.LocalPort));

      var advertBuffer = advert.WriteToBuffer();

      var manufacturerData = new BluetoothLEManufacturerData(
        BluetoothLEStreamSocketAdvertisement.MS_BLUETOOTH_LE_ID, advertBuffer);

      this.bluetoothPublisher = new BluetoothLEAdvertisementPublisher();

      this.bluetoothPublisher.Advertisement.ManufacturerData.Add(manufacturerData);

      this.bluetoothPublisher.Start();
    }
    void OnConnectionReceived(
      StreamSocketListener sender, 
      StreamSocketListenerConnectionReceivedEventArgs args)
    {
      this.ConnectionReceived?.Invoke(this,
        new StreamSocketEventArgs()
        {
          Socket = args.Socket
        }
      );
    }

    public void Stop()
    {
      if (this.SocketListener != null)
      {
        this.SocketListener.ConnectionReceived -= this.OnConnectionReceived;
      }
      if (this.ownsListener)
      {
        this.SocketListener.Dispose();
        this.SocketListener = null;
      }
      if (this.bluetoothPublisher != null)
      {
        this.bluetoothPublisher.Stop();
        this.bluetoothPublisher = null;
      }      
    }
    async Task CreateForLocalInternetProfileAsync()
    {
      this.SocketListener = new StreamSocketListener();
      this.ownsListener = true;

      this.address = IPAddressExtensions.GetForLocalInternetProfile();

      await this.SocketListener.BindEndpointAsync(
        new HostName(this.address.ToString()), string.Empty);
    }
    public StreamSocketListener SocketListener
    {
      get; private set;
    }
    bool ownsListener;
    IPAddress address;
    BluetoothLEAdvertisementPublisher bluetoothPublisher;
  }
}
