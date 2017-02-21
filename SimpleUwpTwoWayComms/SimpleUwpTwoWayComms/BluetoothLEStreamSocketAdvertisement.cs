namespace SimpleUwpTwoWayComms
{
  using System;
  using System.Net;
  using System.Runtime.InteropServices;
  using Windows.Storage.Streams;

  public class BluetoothLEStreamSocketAdvertisement
  {
    private BluetoothLEStreamSocketAdvertisement()
    {

    }
    public BluetoothLEStreamSocketAdvertisement(IPAddress address, ushort port)
    {
      this.Address = address;
      this.Port = port;
    }
    public IBuffer WriteToBuffer()
    {
      DataWriter writer = new DataWriter();
      writer.WriteUInt32(MAGIC_HEADER);

      var bits = this.Address.GetAddressBytes();
      writer.WriteUInt16((ushort)bits.Length);
      writer.WriteBytes(bits);
      writer.WriteUInt16(this.Port);
      return (writer.DetachBuffer());
    }
    public static BluetoothLEStreamSocketAdvertisement ReadFromBuffer(IBuffer buffer)
    {
      BluetoothLEStreamSocketAdvertisement returnValue = null;

      var dataReader = DataReader.FromBuffer(buffer);

      if (dataReader.UnconsumedBufferLength >= Marshal.SizeOf<UInt32>())
      {
        var magicHeader = dataReader.ReadUInt32();

        if (magicHeader == MAGIC_HEADER)
        {
          returnValue = new BluetoothLEStreamSocketAdvertisement();

          var bitLength = dataReader.ReadUInt16();
          var bits = new byte[bitLength];
          dataReader.ReadBytes(bits);
          returnValue.Address = new IPAddress(bits);
          returnValue.Port = dataReader.ReadUInt16();
        }
      }
      return (returnValue);
    }
    public IPAddress Address { get; private set; }
    public ushort Port { get; private set; }

    static readonly UInt32 MAGIC_HEADER = 0xFEEDF00D;

    // Note: this may not be the best idea in terms of using the Microsoft bluetooth
    // id here, I just picked one. 
    internal static readonly ushort MS_BLUETOOTH_LE_ID = 6;
  }
}
