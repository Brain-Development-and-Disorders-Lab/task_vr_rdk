/**
  File: HeadsupServer.cs
  Author: Henry Burgess <henry.burgess@wustl.edu>
  Description: Server component for Headsup system, enables communication to and from Headsup client over the local
  network. Listens on defined prefix address, responds to specific paths. Requires an `ExperimentManager` or similar
  class that can provide experiment status data as a `Dictionary<string, string>` type.

  Adapted from https://gist.github.com/amimaro/10e879ccb54b2cacae4b81abea455b10
*/
using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

public class HeadsupServer : MonoBehaviour
{
  // Collection of `CaptureManager` instances to retrieve screenshots from
  public CaptureManager[] CaptureSources;
  // Network address to listen on
  public string Prefix = "http://*:4444/";
  // `NetworkManager` should also be attached to `ExperimentManager` to get the status of the experiment
  private ExperimentManager Experiment;

  // Network-related variables
  private HttpListener listener;
  private Thread listenerThread;
  private bool isListening = false;

  // Queue to manage log messages
  private Queue<string> logQueue;

  void Start()
  {
    Experiment = GetComponent<ExperimentManager>();

    // Setup logging capture
    logQueue = new();
    Application.logMessageReceived += AddMessage;

    // Setup listener
    listener = new HttpListener();
    listener.Prefixes.Add(Prefix);
    listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
    listener.Start();

    // Start a thread for the listener
    isListening = true;
    listenerThread = new Thread(StartListener);
    listenerThread.Start();

    Debug.Log("Headsup server started, listening on: " + Prefix);
  }

  /// <summary>
  /// When destroyed, set the `isListening` flag to `false`, ensuring thread exits
  /// </summary>
  void OnDestroy()
  {
    isListening = false;
  }

  /// <summary>
  /// Utility function to enqueue log messages for transmission to client interface
  /// </summary>
  /// <param name="condition">Details of log message</param>
  /// <param name="stackTrace">Stacktrace leading to message</param>
  /// <param name="type">Type of log message / level</param>
  private void AddMessage(string condition, string stackTrace, LogType type)
  {
    logQueue.Enqueue(condition);
  }

  /// <summary>
  /// Retrieves a chunk of log messages, up to the defined maximum count
  /// </summary>
  /// <param name="maxLength">Maximum number of messages to include</param>
  /// <returns></returns>
  private IEnumerable<string> GetLogChunk(int maxLength)
  {
    for (int i = 0; i < maxLength && logQueue.Count > 0; i++)
    {
      yield return logQueue.Dequeue();
    }
  }

  /// <summary>
  /// Loop function to setup the listener and handle results
  /// </summary>
  private void StartListener()
  {
    while (isListening) {
      var result = listener.BeginGetContext(ListenerCallback, listener);
      result.AsyncWaitHandle.WaitOne();
    }
  }

  /// <summary>
  /// Callback function handling network results
  /// </summary>
  /// <param name="result">Network result</param>
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
        // Collate a chunk of log messages
        List<string> messages = GetLogChunk(10).ToList();
        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messages));
      }
      else if (context.Request.Url.LocalPath == "/screen")
      {
        // Retrieve screenshots from each of the in-game displays
        List<string> sourceCaptures = new();
        foreach (CaptureManager source in CaptureSources)
        {
          // For each source, capture the screenshots and convert to base64 string for network communication
          source.CaptureScreenshot();
          byte[] screenshot = source.GetLastScreenshot();
          string bufferContents = Convert.ToBase64String(screenshot);
          sourceCaptures.Add(bufferContents);
        }

        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sourceCaptures));
      }
      else
      {
        // Default error message
        buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("Invalid path"));
      }

      // Write the output in response
      context.Response.ContentLength64 = buffer.Length;
      Stream output = context.Response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
    }

    context.Response.Close();
  }
}
