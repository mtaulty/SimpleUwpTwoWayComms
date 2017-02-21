namespace XamlTestApp
{
  using Windows.ApplicationModel.Activation;
  using Windows.UI.Xaml;

  sealed partial class App : Application
  {
    public App()
    {
      this.InitializeComponent();
    }
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
      if (e.PrelaunchActivated == false)
      {
        if (Window.Current.Content == null)
        {
          Window.Current.Content = new MainControl();
        }
        Window.Current.Activate();
      }
    }
  }
}
