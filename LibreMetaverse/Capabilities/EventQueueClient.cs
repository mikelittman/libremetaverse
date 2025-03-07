/*
 * Copyright (c) 2006-2016, openmetaverse.co
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the openmetaverse.co nor the names
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Net;
using System.Threading;
using OpenMetaverse.StructuredData;

namespace OpenMetaverse.Http
{
    public class EventQueueClient
    {
        private const string REQUEST_CONTENT_TYPE = "application/llsd+xml";

        /// <summary>Viewer defauls to 30 for main grid, 60 for others</summary>
        public const int REQUEST_TIMEOUT = 60 * 1000;

        /// <summary>For exponential backoff on error.</summary>
        public const int REQUEST_BACKOFF_SECONDS = 15 * 1000; // 15 seconds start
        public const int REQUEST_BACKOFF_SECONDS_INC = 5 * 1000; // 5 seconds increase
        public const int REQUEST_BACKOFF_SECONDS_MAX = 5 * 60 * 1000; // 5 minutes

        public delegate void ConnectedCallback();
        public delegate void EventCallback(string eventName, OSDMap body);

        public ConnectedCallback OnConnected;
        public EventCallback OnEvent;

        public bool Running => _Running;

        protected Uri _Address;
        protected bool _Dead;
        protected bool _Running;
        protected HttpWebRequest _Request;

        /// <summary>Number of times we've received an unknown CAPS exception in series.</summary>
        private int _errorCount;

        public EventQueueClient(Uri eventQueueLocation)
        {
            _Address = eventQueueLocation;
        }

        public void Start()
        {
            _Dead = false;

            // Create an EventQueueGet request
            OSDMap request = new OSDMap {["ack"] = new OSD(), ["done"] = OSD.FromBoolean(false)};

            byte[] postData = OSDParser.SerializeLLSDXmlBytes(request);

            _Request = CapsBase.PostDataAsync(_Address, null, REQUEST_CONTENT_TYPE, postData, REQUEST_TIMEOUT, OpenWriteHandler, null, RequestCompletedHandler);
        }

        public void Stop(bool immediate)
        {
            _Dead = true;

            if (immediate)
                _Running = false;

            _Request?.Abort();
        }

        void OpenWriteHandler(HttpWebRequest request)
        {
            _Running = true;
            _Request = request;

            Logger.DebugLog("Capabilities event queue connected");

            // The event queue is starting up for the first time
            if (OnConnected != null)
            {
                try { OnConnected(); }
                catch (Exception ex) { Logger.Log(ex.Message, Helpers.LogLevel.Error, ex); }
            }
        }

        void RequestCompletedHandler(HttpWebRequest request, HttpWebResponse response, byte[] responseData, Exception error)
        {
            // We don't care about this request now that it has completed
            _Request = null;

            OSDArray events = null;
            int ack = 0;

            if (responseData != null)
            {
                _errorCount = 0;
                // Got a response
                if (OSDParser.DeserializeLLSDXml(responseData) is OSDMap result)
                {
                    events = result["events"] as OSDArray;
                    ack = result["id"].AsInteger();
                }
                else
                {
                    Logger.Log("Got an unparseable response from the event queue: \"" +
                        System.Text.Encoding.UTF8.GetString(responseData) + "\"", Helpers.LogLevel.Warning);
                }
            }
            else if (error != null)
            {
                #region Error handling

                HttpStatusCode code = HttpStatusCode.OK;

                if (error is WebException webException)
                {
                    // Filter out some of the status requests to skip handling
                    switch (webException.Status)
                    {
                        case WebExceptionStatus.RequestCanceled:
                        case WebExceptionStatus.KeepAliveFailure:
                            goto HandlingDone;
                    }

                    if (webException.Response != null)
                        code = ((HttpWebResponse)webException.Response).StatusCode;
                }

                switch (code)
                {
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.Gone:
                        Logger.Log($"Closing event queue at {_Address} due to missing caps URI", Helpers.LogLevel.Info);

                        _Running = false;
                        _Dead = true;
                        break;
                    case (HttpStatusCode)499: // weird error returned occasionally, ignore for now
						// I believe this is the timeout error invented by LL for LSL HTTP-out requests (gwyneth 20220413)
						Logger.Log($"Possible HTTP-out timeout error from {_Address}, no need to continue", Helpers.LogLevel.Debug);

						_Running = false;
						_Dead = true;
						break;
					case HttpStatusCode.InternalServerError:
						// As per LL's instructions, we ought to consider this a
						// 'request to close client' (gwyneth 20220413)
						Logger.Log($"Grid sent a {code} at {_Address}, closing connection", Helpers.LogLevel.Debug);

						// ... but do we happen to have an InnerException? Log it!
						if (error.InnerException != null)
						{
							// unravel the whole inner error message, so we finally figure out what it is!
							// (gwyneth 20220414)
							Logger.Log($"Unrecognized internal caps exception from {_Address}: '{error.InnerException.Message}'",																					Helpers.LogLevel.Warning);
							Logger.Log("\nMessage ---\n{error.Message}",		Helpers.LogLevel.Warning);
							Logger.Log("\nHelpLink ---\n{ex.HelpLink}",			Helpers.LogLevel.Warning);
							Logger.Log("\nSource ---\n{error.Source}",			Helpers.LogLevel.Warning);
							Logger.Log("\nStackTrace ---\n{error.StackTrace}",  Helpers.LogLevel.Warning);
							Logger.Log("\nTargetSite ---\n{error.TargetSite}",  Helpers.LogLevel.Warning);
							if (error.Data.Count > 0)
							{
								Logger.Log("  Extra details:",					Helpers.LogLevel.Warning);
								foreach (DictionaryEntry de in error.Data)
									Logger.Log(String.Format("    Key: {0,-20}      Value: '{1}'",
										de.Key, de.Value),
										Helpers.LogLevel.Warning);
							}
							// but we'll nevertheless close this connection (gwyneth 20220414)
						}

						_Running = false;
						_Dead = true;
						break;
                    case HttpStatusCode.BadGateway:
                        // This is not good (server) protocol design, but it's normal.
                        // The EventQueue server is a proxy that connects to a Squid
                        // cache which will time out periodically. The EventQueue server
                        // interprets this as a generic error and returns a 502 to us
                        // that we ignore
						//
						// Note: if this condition persists, it _might_ be the grid trying to request
						// that the client closes the connection, as per LL's specs (gwyneth 20220414)
						Logger.Log($"Grid sent a Bad Gateway Error at {_Address}; probably a time-out from the grid's EventQueue server (normal) -- ignoring and continuing", Helpers.LogLevel.Debug);
                        break;
                    default:
                        ++_errorCount;

                        // Try to log a meaningful error message
                        if (code != HttpStatusCode.OK)
                        {
                            Logger.Log($"Unrecognized caps connection problem from {_Address}: {code}",
                                Helpers.LogLevel.Warning);
                        }
                        else if (error.InnerException != null)
                        {
							// see comment above (gwyneth 20220414)
							Logger.Log($"Unrecognized internal caps exception from {_Address}: '{error.InnerException.Message}'",							Helpers.LogLevel.Warning);
							Logger.Log("\nMessage ---\n{error.Message}",		Helpers.LogLevel.Warning);
							Logger.Log("\nHelpLink ---\n{ex.HelpLink}",			Helpers.LogLevel.Warning);
							Logger.Log("\nSource ---\n{error.Source}",			Helpers.LogLevel.Warning);
							Logger.Log("\nStackTrace ---\n{error.StackTrace}",  Helpers.LogLevel.Warning);
							Logger.Log("\nTargetSite ---\n{error.TargetSite}",	Helpers.LogLevel.Warning);
							if (error.Data.Count > 0)
							{
								Logger.Log("  Extra details:",					Helpers.LogLevel.Warning);
								foreach (DictionaryEntry de in error.Data)
									Logger.Log(String.Format("    Key: {0,-20}      Value: {1}",
										"'" + de.Key + "'", de.Value),
										Helpers.LogLevel.Warning);
							}
                        }
                        else
                        {
                            Logger.Log($"Unrecognized caps exception from {_Address}: {error.Message}",
                                Helpers.LogLevel.Warning);
                        }
                        break;
                }	// end switch

                #endregion Error handling
            }
            else
            {
                ++_errorCount;

                Logger.Log("No response from the event queue but no reported error either", Helpers.LogLevel.Warning);
            }

        HandlingDone:

            #region Resume the connection

            if (_Running)
            {
                OSDMap osdRequest = new OSDMap();
                if (ack != 0) osdRequest["ack"] = OSD.FromInteger(ack);
                else osdRequest["ack"] = new OSD();
                osdRequest["done"] = OSD.FromBoolean(_Dead);

                byte[] postData = OSDParser.SerializeLLSDXmlBytes(osdRequest);

                if (_errorCount > 0) // Exponentially back off, so we don't hammer the CPU
                    Thread.Sleep(Math.Min(REQUEST_BACKOFF_SECONDS + _errorCount * REQUEST_BACKOFF_SECONDS_INC, REQUEST_BACKOFF_SECONDS_MAX));

                // Resume the connection. The event handler for the connection opening
                // just sets class _Request variable to the current HttpWebRequest
                CapsBase.PostDataAsync(_Address, null, REQUEST_CONTENT_TYPE, postData, REQUEST_TIMEOUT,
                    delegate(HttpWebRequest newRequest) { _Request = newRequest; }, null, RequestCompletedHandler);

                // If the event queue is dead at this point, turn it off since
                // that was the last thing we want to do
                if (_Dead)
                {
                    _Running = false;
                    Logger.DebugLog("Sent event queue shutdown message");
                }
            }

            #endregion Resume the connection

            #region Handle incoming events

            if (OnEvent == null || events == null || events.Count <= 0) return;
            // Fire callbacks for each event received
            foreach (var osd in events)
            {
                var evt = (OSDMap) osd;
                string msg = evt["message"].AsString();
                OSDMap body = (OSDMap)evt["body"];

                try { OnEvent(msg, body); }
                catch (Exception ex) { Logger.Log(ex.Message, Helpers.LogLevel.Error, ex); }
            }

            #endregion Handle incoming events
        }
    }
}
