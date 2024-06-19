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
  public CaptureManager CaptureSource;
  private HttpListener listener;
  private Thread listenerThread;
  private Queue<string> logQueue;

  void Start()
  {
    logQueue = new();
    Application.logMessageReceived += AddMessage;

    listener = new HttpListener();
    listener.Prefixes.Add("http://*:4444/");
    listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
    listener.Start();

    listenerThread = new Thread (startListener);
    listenerThread.Start();
    Debug.Log("Server Started");
  }

  private void AddMessage(string condition, string stackTrace, LogType type)
  {
    logQueue.Enqueue(stackTrace);
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
    while (true) {
      var result = listener.BeginGetContext(ListenerCallback, listener);
      result.AsyncWaitHandle.WaitOne();
    }
  }

  private void ListenerCallback(IAsyncResult result)
  {
    var context = listener.EndGetContext(result);

    if (context.Request.HttpMethod == "OPTIONS")
    {
        context.Response.AddHeader("Access-Control-Allow-Headers", "*");
        context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
        context.Response.AddHeader("Access-Control-Max-Age", "1728000");
    }
    context.Response.AppendHeader("Access-Control-Allow-Origin", "*");

    if (context.Request.HttpMethod == "GET")
    {
      byte[] buffer;
      if (context.Request.Url.LocalPath == "/active")
      {
        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(true));
      }
      else if (context.Request.Url.LocalPath == "/status")
      {
        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Dictionary<string, string>(){
          { "active_block", "1" },
          { "elapsed_time", "1000.023" },
        }));
      }
      else if (context.Request.Url.LocalPath == "/logs")
      {
        List<string> messages = GetLogChunk(10).ToList();
        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messages));
      }
      else if (context.Request.Url.LocalPath == "/screen")
      {
        if (CaptureSource)
        {
          CaptureSource.CaptureScreenshot();
          buffer = CaptureSource.GetLastScreenshot();
        }
        else
        {
          buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("Capture source configuration error"));
        }
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
