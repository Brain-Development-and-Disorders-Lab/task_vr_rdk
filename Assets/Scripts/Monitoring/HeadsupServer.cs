/**
File: HeadsupServer.cs
Author: Henry Burgess <henry.burgess@wustl.edu>

Adapted from https://gist.github.com/amimaro/10e879ccb54b2cacae4b81abea455b10
*/
using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Monitoring {
    public class Handler : WebSocketBehavior
    {
        private ExperimentManager Experiment;
        private CaptureManager[] CaptureSources;

        public Handler(ExperimentManager manager, CaptureManager[] sources)
        {
            Experiment = manager;
            CaptureSources = sources;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            // Handle received messages and respond accordingly
            if (e.Data == "active")
            {
                // Return active status, "true" if responsive
                Send(JsonConvert.SerializeObject(true));
            }
            else if (e.Data == "kill")
            {
                // Force the experiment to end
                Experiment.ForceEnd();
                Send(JsonConvert.SerializeObject("Done"));
            }
            else if (e.Data == "disable_fixation")
            {
                // Disable the fixation requirement
                Experiment.SetFixationRequired(false);
                Send(JsonConvert.SerializeObject("Fixation Disabled"));
            }
            else if (e.Data == "enable_fixation")
            {
                // Enable the fixation requirement
                Experiment.SetFixationRequired(true);
                Send(JsonConvert.SerializeObject("Fixation Enabled"));
            }
            else if (e.Data == "screenshot")
            {
                // Capture screenshot of current view
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

                Dictionary<string, string> toSend = new() { { "type", "screenshot" }, { "data", JsonConvert.SerializeObject(sourceCaptures) } };
                Send(JsonConvert.SerializeObject(toSend));
            }

            else
            {
                // Default error message
                Debug.LogWarning("Invalid Command: " + e.Data);
                Send(JsonConvert.SerializeObject("Invalid Command"));
            }
        }
    }

    /// <summary>
    /// Server component for Headsup system, enables communication to and from Headsup client over the local
    /// network. Listens on defined prefix address, responds to specific paths. Requires an `ExperimentManager` or similar
    /// class that can provide experiment status data as a `Dictionary<string, string>` type.
    /// </summary>
    public class HeadsupServer : MonoBehaviour
    {
        // Collection of `CaptureManager` instances to retrieve screenshots from
        public CaptureManager[] CaptureSources;
        // Network address to listen on
        public int Port = 4444;
        // `NetworkManager` should also be attached to `ExperimentManager` to get the status of the experiment
        private ExperimentManager Experiment;
        // WebSocket server instance
        private WebSocketServer Server;

        // Queue to manage log messages
        private Queue<string> logsPreflight;

        private float nextUpdateTime = 0.0f;
        public float UpdateInterval = 1.0f;

        void Start()
        {
            Experiment = GetComponent<ExperimentManager>();
            logsPreflight = new Queue<string>();

            Server = new WebSocketServer(Port);
            Server.AddWebSocketService<Handler>("/", () => new Handler(Experiment, CaptureSources));
            Server.Start();

            Application.logMessageReceived += HandleLogMessage;
        }

        void Update()
        {
            // Broadcast the status to all connected clients
            if (Time.time >= nextUpdateTime)
            {
                Dictionary<string, string> status = Experiment != null ?
                    Experiment.GetExperimentStatus() :
                    new Dictionary<string, string>() { { "active_block", "-1" } };
                Dictionary<string, string> toSend = new() { { "type", "status" }, { "data", JsonConvert.SerializeObject(status) } };
                Server.WebSocketServices["/"].Sessions.Broadcast(JsonConvert.SerializeObject(toSend));
                nextUpdateTime += UpdateInterval;
            }

            // Broadcast any log messages to the client interface
            if (logsPreflight.Count > 0)
            {
                Dictionary<string, string> toSend = new() { { "type", "logs" }, { "data", JsonConvert.SerializeObject(logsPreflight.Dequeue()) } };
                Server.WebSocketServices["/"].Sessions.Broadcast(JsonConvert.SerializeObject(toSend));
            }
        }

        /// <summary>
        /// When destroyed, stop the WebSocketServer instance
        /// </summary>
        void OnDestroy()
        {
            Server.Stop();
        }

        /// <summary>
        /// Utility function to enqueue log messages for transmission to client interface
        /// </summary>
        /// <param name="condition">Details of log message</param>
        /// <param name="stackTrace">Stacktrace leading to message</param>
        /// <param name="type">Type of log message / level</param>
        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            logsPreflight.Enqueue(condition);
        }
    }
}

