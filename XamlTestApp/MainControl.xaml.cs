namespace XamlTestApp
{
  using SimpleUwpTwoWayComms;
  using System;
  using System.ComponentModel;
  using System.Runtime.CompilerServices;
  using System.Threading.Tasks;
  using Windows.UI;
  using Windows.UI.Xaml.Controls;

  public sealed partial class MainControl : UserControl, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    public MainControl()
    {
      this.InitializeComponent();
      this.BackgroundColour = Colors.White;
      this.IsConnected = false;
    }
    public Color BackgroundColour
    {
      get {
        return (this.backgroundColour);
      }
      set
      {
        if (this.backgroundColour != value)
        {
          this.backgroundColour = value;
          this.FirePropertyChanged();
        }
      }
    }
    Color backgroundColour;

    public bool IsConnected
    {
      get
      {
        return (this.isConnected);
      }
      set
      {
        if (this.isConnected != value)
        {
          this.isConnected = value;
          this.FirePropertyChanged();
        }
      }
    }
    bool isConnected;

    public void OnAdvertise()
    {
      this.OnInitialise();
    }
    public void OnConnect()
    {
      this.OnInitialise(false);
    }
    public async void OnInitialise(bool advertise = true)
    {
      this.pipe = new AutoConnectMessagePipe(advertise);

      await this.pipe.WaitForConnectionAsync(TimeSpan.FromMilliseconds(-1));

      this.IsConnected = this.pipe.IsConnected;

      if (this.IsConnected)
      {
        await this.pipe.ReadAndDispatchMessageLoopAsync(this.MessageHandler);
      }
    }
    public async void OnRed()
    {
      await this.OnColourAsync(Colors.Red);
    }
    public async void OnGreen()
    {
      await this.OnColourAsync(Colors.Green);
    }
    public async void OnBlue()
    {
      await this.OnColourAsync(Colors.Blue);
    }
    async Task OnColourAsync(Color colour)
    {
      await this.pipe.SendBytesAsync(
        new byte[] { colour.R, colour.G, colour.B });
    }
    void MessageHandler(MessageType messageType, object messageBody)
    {
      // We just handle byte arrays here.
      if (messageType == MessageType.Buffer)
      {
        var bits = (byte[])messageBody;
        this.BackgroundColour = Color.FromArgb(0xFF, bits[0], bits[1], bits[2]);
      }
    }
    void FirePropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this,
        new PropertyChangedEventArgs(propertyName));
    }
    AutoConnectMessagePipe pipe;
  }
}
