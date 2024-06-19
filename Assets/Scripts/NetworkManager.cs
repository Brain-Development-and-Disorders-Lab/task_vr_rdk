using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

public class NetworkManager : MonoBehaviour
{
  public CaptureManager[] CaptureSources;
  private ExperimentManager Experiment;
  private HttpListener listener;
  private Thread listenerThread;
  private Queue<string> logQueue;
  private bool isListening = false;

  void Start()
  {
    Experiment = GetComponent<ExperimentManager>();

    // Setup logging capture
    logQueue = new();
    Application.logMessageReceived += AddMessage;

    listener = new HttpListener();
    listener.Prefixes.Add("http://*:4444/");
    listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
    listener.Start();

    isListening = true;
    listenerThread = new Thread(startListener);
    listenerThread.Start();
    Debug.Log("Server Started");
  }

  void OnDestroy()
  {
    isListening = false;
  }

  private void AddMessage(string condition, string stackTrace, LogType type)
  {
    logQueue.Enqueue(condition);
  }

  private IEnumerable<string> GetLogChunk(int maxLength)
  {
    for (int i = 0; i < maxLength && logQueue.Count > 0; i++)
    {
      yield return logQueue.Dequeue();
    }
  }

  private void startListener()
  {
    while (isListening) {
      var result = listener.BeginGetContext(ListenerCallback, listener);
      result.AsyncWaitHandle.WaitOne();
    }
  }

  private void ListenerCallback(IAsyncResult result)
  {
    var context = listener.EndGetContext(result);

    // Enable CORS
    if (context.Request.HttpMethod == "OPTIONS")
    {
        context.Response.AddHeader("Access-Control-Allow-Headers", "*");
        context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
        context.Response.AddHeader("Access-Control-Max-Age", "1728000");
    }
    context.Response.AppendHeader("Access-Control-Allow-Origin", "*");

    // Handle request methods
    if (context.Request.HttpMethod == "GET")
    {
      byte[] buffer;
      if (context.Request.Url.LocalPath == "/active")
      {
        // Return active status, "true" if responsive
        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(true));
      }
      else if (context.Request.Url.LocalPath == "/status")
      {
        // Return experiment status information
        string responseValue = JsonConvert.SerializeObject(new Dictionary<string, string>(){
          { "active_block", "-1" },
        });
        if (Experiment != null)
        {
          Dictionary<string, string> status = Experiment.GetExperimentStatus();
          responseValue = JsonConvert.SerializeObject(status);
        }
        buffer = System.Text.Encoding.UTF8.GetBytes(responseValue);
      }
      else if (context.Request.Url.LocalPath == "/logs")
      {
        List<string> messages = GetLogChunk(10).ToList();
        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messages));
      }
      else if (context.Request.Url.LocalPath == "/screen")
      {
        List<string> sourceCaptures = new();
        foreach (CaptureManager source in CaptureSources)
        {
          source.CaptureScreenshot();
          byte[] screenshot = source.GetLastScreenshot();
          string bufferContents = Convert.ToBase64String(screenshot);
          sourceCaptures.Add(bufferContents);
        }

        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sourceCaptures));
      }
      else
      {
        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("Invalid path"));
      }

      context.Response.ContentLength64 = buffer.Length;
      Stream output = context.Response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
    }

    context.Response.Close();
  }
}
