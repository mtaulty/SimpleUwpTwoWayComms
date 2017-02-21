using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_UWP && !UNITY_EDITOR
using SimpleUwpTwoWayComms;
using System.Threading.Tasks;
#endif

public class Placeholder : MonoBehaviour
{
  public GameObject panelConnection;
  public GameObject panelColours;
  public GameObject cube;

  public void OnAdvertise()
  {
#if UNITY_UWP && !UNITY_EDITOR
    this.OnInitialiseAsync();
#endif
  }
  public void OnConnect()
  {
#if UNITY_UWP && !UNITY_EDITOR
    this.OnInitialiseAsync(false);
#endif
  }
  public void OnRed()
  {
    this.OnColour(Color.red);
  }
  public void OnGreen()
  {
    this.OnColour(Color.green);
  }
  public void OnBlue()
  {
    this.OnColour(Color.blue);
  }
  public void OnColour(Color colour)
  {
    // Convert 0 to 1 values into bytes so that we can be compatible with the 2D XAML
    // app.
    var message = new byte[]
    {
      (byte)(colour.r * 255.0f),
      (byte)(colour.g * 255.0f),
      (byte)(colour.b * 255.0f)
    };

#if UNITY_UWP && !UNITY_EDITOR
    this.OnColourAsync(message);
#endif
  }
  void Dispatch(Action action)
  {
    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
    {
      action();
    },
    false);
  }
#if UNITY_UWP && !UNITY_EDITOR
  async Task OnColourAsync(byte[] bits)
  {
    await this.pipe.SendBytesAsync(bits);
  }

  async Task OnInitialiseAsync(bool advertise = true)
  {
    if (this.pipe == null)
    {
      this.pipe = new AutoConnectMessagePipe(advertise);
    }

    await this.pipe.WaitForConnectionAsync(TimeSpan.FromMilliseconds(-1));

    if (pipe.IsConnected)
    {
      this.TogglePanels(false);
      await this.pipe.ReadAndDispatchMessageLoopAsync(this.MessageHandler);
      this.TogglePanels(true);
    }
  }
  void TogglePanels(bool connectionPanel)
  {
    this.Dispatch(
      () =>
      {
        this.panelConnection.SetActive(connectionPanel);
        this.panelColours.SetActive(!connectionPanel);
      }
    );
  }
  void MessageHandler(MessageType messageType, object messageBody)
  {
    // We just handle byte arrays here.
    if (messageType == MessageType.Buffer)
    {
      var bits = (byte[])messageBody;

      if (bits != null)
      {
        this.Dispatch(() =>
          {
            this.cube.GetComponent<Renderer>().material.color =
              new Color(
                (float)(bits[0]) / 255.0f,
                (float)(bits[1]) / 255.0f,
                (float)(bits[2] / 255.0f));
          }
        );
      }
    }
  }
  AutoConnectMessagePipe pipe;
#endif
}