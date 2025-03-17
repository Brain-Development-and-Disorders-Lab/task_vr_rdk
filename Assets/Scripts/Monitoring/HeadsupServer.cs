/**
File: HeadsupServer.cs
Author: Henry Burgess <henry.burgess@wustl.edu>
*/
using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Monitoring
{
    public class Handler : WebSocketBehavior
    {
        private readonly ExperimentManager _experiment;
        private readonly CaptureManager[] _captureSources;

        public Handler(ExperimentManager manager, CaptureManager[] sources)
        {
            _experiment = manager;
            _captureSources = sources;
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
                _experiment.ForceEnd();
                Send(JsonConvert.SerializeObject("Done"));
            }
            else if (e.Data == "disable_fixation")
            {
                // Disable the fixation requirement
                _experiment.SetFixationRequired(false);
                Send(JsonConvert.SerializeObject("Fixation Disabled"));
            }
            else if (e.Data == "enable_fixation")
            {
                // Enable the fixation requirement
                _experiment.SetFixationRequired(true);
                Send(JsonConvert.SerializeObject("Fixation Enabled"));
            }
            else if (e.Data == "screenshot")
            {
                // Capture screenshot of current view
                // Retrieve screenshots from each of the in-game displays
                List<string> sourceCaptures = new();
                foreach (var source in _captureSources)
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
    /// _server component for Headsup system, enables communication to and from Headsup client over the local
    /// network. Listens on defined prefix address, responds to specific paths. Requires an `ExperimentManager` or similar
    /// class that can provide experiment status data as a `Dictionary<string, string>` type.
    /// </summary>
    public class HeadsupServer : MonoBehaviour
    {
        // Collection of `CaptureManager` instances to retrieve screenshots from
        [SerializeField]
        private CaptureManager[] _captureSources;
        // Network address to listen on
        public int port = 4444;
        // `NetworkManager` should also be attached to `ExperimentManager` to get the status of the experiment
        private ExperimentManager _experiment;
        // WebSocket server instance
        private WebSocketServer _server;

        // Queue to manage log messages
        private Queue<string> _logsPreflight;

        private float _nextUpdateTime = 0.0f;
        [SerializeField]
        private float _updateInterval = 1.0f;

        private void Start()
        {
            _experiment = GetComponent<ExperimentManager>();
            _logsPreflight = new Queue<string>();

            _server = new WebSocketServer(port);
            _server.AddWebSocketService<Handler>("/", () => new Handler(_experiment, _captureSources));
            _server.Start();

            Application.logMessageReceived += HandleLogMessage;
        }

        private void Update()
        {
            // Broadcast the status to all connected clients
            if (Time.time >= _nextUpdateTime)
            {
                var status = _experiment != null ?
                    _experiment.GetExperimentStatus() :
                    new Dictionary<string, string>() { { "active_block", "-1" } };
                Dictionary<string, string> toSend = new() { { "type", "status" }, { "data", JsonConvert.SerializeObject(status) } };
                _server.WebSocketServices["/"].Sessions.Broadcast(JsonConvert.SerializeObject(toSend));
                _nextUpdateTime += _updateInterval;
            }

            // Broadcast any log messages to the client interface
            if (_logsPreflight.Count > 0)
            {
                Dictionary<string, string> toSend = new() { { "type", "logs" }, { "data", JsonConvert.SerializeObject(_logsPreflight.Dequeue()) } };
                _server.WebSocketServices["/"].Sessions.Broadcast(JsonConvert.SerializeObject(toSend));
            }
        }

        /// <summary>
        /// When destroyed, stop the WebSocketServer instance
        /// </summary>
        private void OnDestroy() => _server.Stop();

        /// <summary>
        /// Utility function to enqueue log messages for transmission to client interface
        /// </summary>
        /// <param name="condition">Details of log message</param>
        /// <param name="stackTrace">Stacktrace leading to message</param>
        /// <param name="type">Type of log message / level</param>
        private void HandleLogMessage(string condition, string stackTrace, LogType type) => _logsPreflight.Enqueue(condition);
    }
}

