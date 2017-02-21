namespace SimpleUwpTwoWayComms
{
  using Newtonsoft.Json;
  using System;
  using System.Runtime.InteropServices;
  using System.Runtime.InteropServices.WindowsRuntime;
  using System.Runtime.Serialization;
  using System.Text;
  using System.Threading.Tasks;
  using Windows.Networking;
  using Windows.Networking.Sockets;
  using Windows.Storage.Streams;
  using MessageTypePair = System.Tuple<MessageType, object>;

  public enum MessageType : byte
  {
    Buffer,
    String,
    SerializedObject
  }
  public class MessageBase
  {
  }
  public class AutoConnectMessagePipe
  {
    public AutoConnectMessagePipe(bool advertise = true)
    {
      this.advertise = advertise;
    }
    public async Task<bool> WaitForConnectionAsync(TimeSpan timeOut)
    {
      bool connected = false;
      this.connectionMadeTask = new TaskCompletionSource<bool>();

      if (this.advertise)
      {
        this.advertiser = new BluetoothLEStreamSocketAdvertiser();
        this.advertiser.ConnectionReceived += OnRemotePipeConnected;
        await this.advertiser.StartAsync();
      }
      else
      {
        this.watcher = new BluetoothLEStreamSocketWatcher(
          IPAddressExtensions.GetForLocalInternetProfile());

        this.watcher.StreamSocketDiscovered += OnRemotePipeDiscovered;

        this.watcher.Start();
      }
      var completed = await Task.WhenAny(
        this.connectionMadeTask.Task,
        Task.Delay(timeOut));

      connected = completed == this.connectionMadeTask.Task;

      this.StopAdvertisingWatching();

      if (connected && !this.advertise)
      {
        connected = await this.ConnectToRemotePipeAsync();
      }
      return (connected);
    }
    public bool IsConnected
    {
      get
      {
        return (this.socket != null);
      }
    }
    void OnRemotePipeConnected(object sender, StreamSocketEventArgs e)
    {
      this.socket = e.Socket;
      this.connectionMadeTask.SetResult(true);
    }
    async Task<bool> ConnectToRemotePipeAsync()
    {
      bool connected = false;

      try
      {
        this.socket = new StreamSocket();

        await this.socket.ConnectAsync(
          new EndpointPair(
            null,
            string.Empty,
            new HostName(this.advertisement.Address.ToString()),
            this.advertisement.Port.ToString()));

        connected = true;
      }
      catch // TBD: what to catch here?
      {
        this.socket.Dispose();
        this.socket = null;
      }
      return (connected);
    }
    public async Task ReadAndDispatchMessageLoopAsync(
      Action<MessageType, object> handler)
    {
      while (true)
      {
        try
        {
          var msg = await this.ReadMessageAsync();
          handler(msg.Item1, msg.Item2);
        }
        catch
        {
          break;
        }
      }
    }
    public async Task<MessageTypePair> ReadMessageAsync()
    {
      // TODO: lots of allocations of potentially large size, might
      // be better to allocate some big buffer and keep re-using it?
      byte[] bits = new byte[Marshal.SizeOf<int>()];

      await this.socket.InputStream.ReadAsync(bits.AsBuffer(),
        (uint)bits.Length, InputStreamOptions.None);

      if (BitConverter.IsLittleEndian)
      {
        Array.Reverse(bits);
      }

      int size = BitConverter.ToInt32(bits, 0);

      bits = new byte[size + 1];

      await this.socket.InputStream.ReadAsync(bits.AsBuffer(),
        (uint)bits.Length, InputStreamOptions.None);

      var messageType = (MessageType)bits[bits.Length - 1];
      var returnValue = (object)null;

      switch (messageType)
      {
        case MessageType.Buffer:
          Array.Resize<byte>(ref bits, bits.Length - 1);
          returnValue = bits;
          break;
        case MessageType.String:
          returnValue = UTF8Encoding.UTF8.GetString(bits, 0, bits.Length - 1);
          break;
        case MessageType.SerializedObject:
          var strObject = UTF8Encoding.UTF8.GetString(bits, 0, bits.Length - 1);
          returnValue = JsonConvert.DeserializeObject<MessageBase>(strObject, jsonSettings);
          break;
        default:
          break;
      }
      return (Tuple.Create(messageType, (object)returnValue));
    }
    public async Task SendStringAsync(string data)
    {
      await this.SendStringAsync(MessageType.String, data);
    }
    async Task SendStringAsync(MessageType messageType, string data)
    {
      var bits = UTF8Encoding.UTF8.GetBytes(data);
      await this.SendBytesAsync(messageType, bits);
    }
    public async Task SendBytesAsync(byte[] bits)
    {
      await this.SendBytesAsync(MessageType.Buffer, bits);
    }
    public async Task SendObjectAsync(MessageBase data)
    {
      var serialized = JsonConvert.SerializeObject(data, jsonSettings);
      await this.SendStringAsync(MessageType.SerializedObject, serialized);
    }
    public async Task SendStreamAsync(MessageType messageType, InMemoryRandomAccessStream bits)
    {
      await this.SendAsync(
        messageType,
        (int)bits.Size,
        async () =>
        {
          await RandomAccessStream.CopyAsync(bits, this.socket.OutputStream);
        }
      );
      bits.Dispose();
    }
    public void Close()
    {
      if (this.socket != null)
      {
        this.socket.Dispose();
        this.socket = null;
      }
    }
    void OnRemotePipeDiscovered(object sender,
      BluetoothLEStreamSocketDiscoveredEventArgs e)
    {
      this.advertisement = e.Advertisement;
      this.connectionMadeTask.SetResult(true);
    }
    void StopAdvertisingWatching()
    {
      if (this.watcher != null)
      {
        this.watcher.Stop();
        this.watcher.StreamSocketDiscovered -= this.OnRemotePipeDiscovered;
        this.watcher = null;
      }
      if (this.advertiser != null)
      {
        this.advertiser.Stop();
        this.advertiser = null;
      }
      this.connectionMadeTask = null;
    }
    async Task SendBytesAsync(MessageType messageType, byte[] bits)
    {
      await this.SendAsync(
        messageType, 
        bits.Length,
        async () =>
        {
          await this.socket.OutputStream.WriteAsync(bits.AsBuffer());     
        }
      );
    }
    async Task SendAsync(MessageType messageType, int length,
      Func<Task> writeBitsAsync)
    {
      if (this.socket == null)
      {
        throw new InvalidOperationException("Socket not connected");
      }
      var data = BitConverter.GetBytes(length);

      if (BitConverter.IsLittleEndian)
      {
        Array.Reverse(data);
      }
      await this.socket.OutputStream.WriteAsync(data.AsBuffer());

      await writeBitsAsync();

      data = new byte[] { (byte)messageType };
      await this.socket.OutputStream.WriteAsync(data.AsBuffer());

      await this.socket.OutputStream.FlushAsync();
    }
    static JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
    {
      TypeNameHandling = TypeNameHandling.All
    };

    bool advertise;
    StreamSocket socket;
    BluetoothLEStreamSocketAdvertisement advertisement;
    BluetoothLEStreamSocketAdvertiser advertiser;
    BluetoothLEStreamSocketWatcher watcher;
    TaskCompletionSource<bool> connectionMadeTask;
  }
}