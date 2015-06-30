﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using DapperWare;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace DapperWare
{
    [TestClass]
    public class WampSessionTests
    {
        public class MockTransportFactory : ITransportFactory
        {
            private IWampTransport instance;

            public MockTransportFactory(IWampTransport instance)
            {
                this.instance = instance;
            }

            public IWampTransport Create()
            {
                return instance;
            }
        }

        public class MockWampTransport : IWampTransport
        {

            public event EventHandler Closed;

            public void Send(Newtonsoft.Json.Linq.JToken array)
            {
                if (this.SendProxy != null)
                    this.SendProxy(array);
            }

            public Action<JToken> SendProxy { get; set; }

            public event EventHandler<WampMessageEventArgs> Message;

            internal void RaiseMessage(JArray result)
            {
                if (Message != null)
                {
                    this.Message(this, new WampMessageEventArgs(result));
                }
            }


            public Task ConnectAsync(string url)
            {
                RaiseMessage(new JArray(0, "mysessionid", 1, "test server 1.0"));
                return Task.FromResult(true);
            }


            public event EventHandler Error;


            public void Send(IEnumerable<object> message)
            {
                if (this.SendProxy != null)
                    this.SendProxy(JArray.FromObject(message));
            }
        }

        private ITransportFactory mockTransportFactory;
        private MockWampTransport mockTransport;
        private WampSession connection;


        [TestInitialize]
        public void Setup()
        {
            mockTransport = new MockWampTransport();
            mockTransportFactory = new MockTransportFactory(mockTransport);
            var connectionTask = WampClient.ConnectAsync("ws://localhost:3000", mockTransportFactory);
            connectionTask.Wait();
            this.connection = connectionTask.Result;
        }

        [TestMethod]
        public void TestInitializedSession()
        {
            Assert.AreEqual(connection.SessionId, "mysessionid");
        }

        [TestMethod]
        public void TestPublishEvent()
        {
            connection.Publish("test/topic", 5);
        }

        [TestMethod]
        public void TestSubscribe()
        {
            var t = connection.Subscribe<int>("test/topic");
            Assert.AreEqual(t.Topic, "test/topic");
            Assert.AreEqual(connection.Subscriptions.Count(), 1);
        }

        [TestMethod]
        public void TestUnsubscribe()
        {
            connection.Subscribe<int>("test/topic");
            using (var t = connection.Subscribe<int>("test/topic2"))
            {

                Assert.AreEqual(connection.Subscriptions.Count(), 2);

                connection.Unsubscribe("test/topic");

                Assert.AreEqual(connection.Subscriptions.Count(), 1);
            }

            Assert.AreEqual(connection.Subscriptions.Count(), 0);
        }

        [TestMethod]
        public void TestCallMethod()
        {
            mockTransport.SendProxy = arr =>
            {
                JArray call = (JArray)arr;
                //Must capture the call id in order finish the call
                JArray result = new JArray(MessageType.CALLRESULT, call[1], 8);
                mockTransport.RaiseMessage(result);
            };


            var callTask = connection.Call<int>("test/method", 5, 3);

            callTask.Wait();

            Assert.AreEqual(callTask.Result, 8);
        }

        [TestMethod]
        public void TestCallWithError()
        {
            mockTransport.SendProxy = arr =>
            {
                JArray call = (JArray)arr;
                JArray result = new JArray(MessageType.CALLERROR, call[1], 8);
                mockTransport.RaiseMessage(result);
            };

            var callTask = connection.Call<int>("test/method", 5, 3);

            Assert.ThrowsException<AggregateException>(() => callTask.Wait());

        }

        [TestMethod]
        public void TestDeserializeMultiMessage()
        {
            string message = "[3][\"test\"]";

            using (var reader = new JsonTextReader(new StreamReader(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(message)))))
            {
                
                reader.SupportMultipleContent = true;
                var parsed = JArray.Load(reader);
                reader.Read();
                var parsed2 = JArray.Load(reader);

                Assert.AreEqual(1, parsed.Count);
                Assert.AreEqual(1, parsed2.Count);
            }
        }

        [TestMethod]
        public void TestDeserializeMessage()
        {
            string message = "[3]";

            using (var reader = new JsonTextReader(new StreamReader(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(message)))))
            {
                List<JArray> messages = new List<JArray>();
                reader.SupportMultipleContent = true;

                while (reader.Read())
                {
                    messages.Add(JArray.Load(reader));
                }

                Assert.AreEqual(1, messages.Count);
            }
        }
    }
}
