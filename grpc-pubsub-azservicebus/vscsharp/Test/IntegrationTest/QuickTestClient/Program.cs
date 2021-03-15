﻿// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Copyright © 2021 Solid Value Software, LLC

using System;
using System.Threading.Tasks;
using ConfigHelpers;
using Grpc.Net.Client;
using ServiceA;
using ServiceHelpers;

namespace QuickTestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // The below USING disposes of the channel when execution goes outside the current set of curly braces.
            using GrpcChannel serviceAChannel = GrpcChannelFactory.MakeGrpcChannel(GrpcSettings.ServiceAClientHttpAddressString);
            
            // Make the protobuf client side proxy to allow calling protobuf rpc methods on ServiceADemo.
            SvcADemo.SvcADemoClient serviceADemoProxy = new SvcADemo.SvcADemoClient(serviceAChannel);

            bool keepRunning = true;
            while (keepRunning)
            {
                Console.WriteLine("Input a-bft arg1 arg2 ENTER to execute ServiceA.DoBasicFunctionalTestAsync(message).");
                //Console.WriteLine("Input b-bft arg1 arg2 ENTER to execute ServiceB.PerformBftAsync(message).");
                Console.WriteLine("Or cmds = a-pubevent,...");
                Console.WriteLine("     OR Press ENTER to quit.");

                string userInput = Console.ReadLine();
                if (string.IsNullOrEmpty(userInput))
                {
                    keepRunning = false;
                }

                if (keepRunning)
                {
                    SvcAPublishEventRequest request;
                    SvcAStringReply pubEventReply;
                    ParsedUserInput cmdNArgs = ParseUserInput(userInput);
                    string resultsMsg = "No results returned from the service!  Error?";
                    switch (cmdNArgs.Cmd)
                    {
                        case "a-bft":
                            SvcABftRequest svcABftRequest =
                                new SvcABftRequest
                                {
                                    TestData1 = $"SvcADemo.SvcADemoClient request: Cmd={cmdNArgs.Cmd} Arg1={cmdNArgs.Arg1}, Arg2={cmdNArgs.Arg2}.",
                                    TestData2 = $"Test data, test data, test data, ...."
                                };
                            SvcABftReply svcABftReply = await serviceADemoProxy.DoBasicFunctionalTestAsync(svcABftRequest);

                            resultsMsg = $"SvcADemo.DoBasicFunctionalTestAsync() response =\n{svcABftReply.Message}\n";
                            Console.WriteLine(resultsMsg);
                            break;

                        case "a-pubevent": // Usage: a-pubevent  pubsubkind  pubsubname  topicname  somepayloadstring

                            // Example:   a-pubevent netcodeconnstr pubsub svcADemoEvents1 payload=tttttt

                            // PubSubKind can be netcodeconnstr, netcodeclicred, netcodeclicert, or daprpubsub
                            
                            request = new SvcAPublishEventRequest();
                            request.PubSubKind = cmdNArgs.Arg1;
                            request.PubSubName = cmdNArgs.Arg2;
                            request.TopicName = cmdNArgs.Arg3;
                            request.EventPayload = cmdNArgs.Arg4;
                            
                            // TODO -- ALWAYS USE the ASYNC version of serviceAProxy.PublishEventViaDapr as shown below.
                            // TODO -- Using the non-async version results in GetAwaiter() error.
                            pubEventReply = await serviceADemoProxy.PublishEventAsync(request);

                            resultsMsg = $"Service response =\n  {pubEventReply.Message}\n";
                            Console.WriteLine(resultsMsg);
                            break;

                        //case "a-pubevent": // Usage: a-pubevent pubsubname topicname somepayloadstring
                        //    SvcAPublishEventRequest request = new SvcAPublishEventRequest();
                        //    request.PubSubName = cmdNArgs.Arg1;
                        //    request.TopicName = cmdNArgs.Arg2;

                        //    // The EventPayload string needs first be converted into a PubSubTest object, a DTO that is
                        //    // passed between services.  Then the PubSubTest object needs to be serialized into a json string.
                        //    // Json is required since the SvcAPublishEventRequest is defined via Protobuf which does not
                        //    // support generic types or inheritance (Marc Gravell).
                        //    // TODO Above is a huge limitation. What will I do about it?  Compensate with use of json.
                        //    // The SvcAPublishEventRequest.EventPayload should be a generic type T, but since that is not
                        //    // available I use// a .NET object (PubSubTestData) that is serialized into a json string.
                        //    // This approach will handle all .NET types.
                        //    PubSubTestData psTestData = new PubSubTestData {Msg1 = cmdNArgs.Arg3};

                        //    // One MUST use the [JsonProperty("camelCasePropertyName")] attribute on each property
                        //    // of each object (DTO) that is serialized and deserialized since the SERVICE CODE EXPECTS
                        //    // camelCase due to its use of that naming policy in its JsonSerializerOptions.
                        //    request.EventPayload = JsonConvert.SerializeObject(psTestData);

                        //    // TODO Can I make the EventPayload field a struct (i.e. object) and use T in
                        //    // the service call?  NO since the serviceProxy (below) is GENERATED CODE.

                        //    // TODO -- ALWAYS USE the ASYNC version of serviceAProxy.PublishEventViaDapr as shown below.
                        //    // TODO -- Using the non-async version results in GetAwaiter() error.
                        //    SvcAStringReply pubEventReply = await serviceAProxy.PublishEventViaDaprAsync(request);

                        //    resultsMsg = $"AllSvcsRequestCount = {allSvcsRequestCount++}, SvcARequestCount = {svcARequestCount++}," +
                        //                 $"Service response =\n  {pubEventReply.Message}\n";
                        //    Console.WriteLine(resultsMsg);
                        //    break;

                        case "a-pubeventmulti": 
                            
                            // Usage: a-pubeventmulti  pubsubkind  pubsubname  topicname  somepayloadstring nevents delaymsec

                            // Example:   a-pubeventmulti daprpubsub svs-pubsub-asbtopic svcADemoEvents1 payload=tttttt 100 20
                            // Above example will send 100 events (nevents = 100) with a time delay of 20 millisec between
                            // each send (delaymsec = 20);

                            // The only pubsubkind supported in this sample is daprpubsub
                            // The only pubsubname supported in this sample is svs-pubsub-asbtopic, although it is easy
                            // for YOU to add the code to ServiceB to support the default dapr redis pubsubname = pubsub.
                            
                            request = new SvcAPublishEventRequest();
                            request.PubSubKind = cmdNArgs.Arg1;
                            request.PubSubName = cmdNArgs.Arg2;
                            request.TopicName = cmdNArgs.Arg3;
                            request.EventPayload = cmdNArgs.Arg4;

                            int nEvents;
                            bool isParsed = Int32.TryParse(cmdNArgs.Arg5, out nEvents);
                            if (!isParsed)
                            {
                                Console.WriteLine("\n** ERROR: Input value 'nevents' is <= 0. Please try again.");
                            }
                            int delayMsec;
                            Int32.TryParse(cmdNArgs.Arg6, out delayMsec);

                            for (int i = 1; i < nEvents+1; i++)
                            {
                                // TODO -- ALWAYS USE the ASYNC version of serviceAProxy.PublishEventViaDapr as shown below.
                                // TODO -- Using the non-async version results in GetAwaiter() error.
                                string senderSequenceNumber = i.ToString("D9");
                                string adjustedEventPayload = senderSequenceNumber + ", " + request.EventPayload;
                                pubEventReply = await serviceADemoProxy.PublishEventAsync(request);

                                Console.WriteLine($"Sent Message number {i} of {nEvents} messages to send.\n\t\t\t\t\tMsg = {adjustedEventPayload}");
                                resultsMsg = $"Service response =\n  {pubEventReply.Message}\n";
                                Console.WriteLine(resultsMsg);

                                await Task.Delay(delayMsec);
                            }
                            break;

                        default:
                            Console.WriteLine($"Unrecognized cmd. userInput = {userInput}. Try again!");
                            break;
                    } // End switch.
                }
                else
                {
                    Console.WriteLine($"QuickTestClient quitting on user pressing Enter with no other input.");
                }
            } // End while keep running.
            Console.WriteLine("QuickTestClient EXITING!");
            await Task.Delay(1000);
        }
        private class ParsedUserInput
        {
            public string Cmd { get; set; } = string.Empty;
            public string Arg1 { get; set; } = string.Empty;
            public string Arg2 { get; set; } = string.Empty;
            public string Arg3 { get; set; } = string.Empty;
            public string Arg4 { get; set; } = string.Empty;
            public string Arg5 { get; set; } = string.Empty;
            public string Arg6 { get; set; } = string.Empty;
        }
        private static ParsedUserInput ParseUserInput(string userInput)
        {
            string[] inputItems = userInput.Split(' ');
  
            ParsedUserInput cmdNArgs = new ParsedUserInput();
            try
            {
                cmdNArgs.Cmd = inputItems[0];
                cmdNArgs.Arg1 = inputItems[1];
                cmdNArgs.Arg2 = inputItems[2];
                cmdNArgs.Arg3 = inputItems[3];
                cmdNArgs.Arg4 = inputItems[4];
                cmdNArgs.Arg5 = inputItems[5];
                cmdNArgs.Arg6 = inputItems[6];
            }
            catch (Exception ex)
            {
            }
            return cmdNArgs;
        }
    }
}
