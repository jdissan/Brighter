#region Licence
/* The MIT License (MIT)
Copyright � 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the �Software�), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED �AS IS�, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using nUnitShouldAdapter;
using NUnit.Framework;
using paramore.brighter.commandprocessor.tests.nunit.CommandProcessors.TestDoubles;
using paramore.brighter.commandprocessor.tests.nunit.MessageDispatch.TestDoubles;
using paramore.brighter.serviceactivator;
using paramore.brighter.serviceactivator.TestHelpers;

namespace paramore.brighter.commandprocessor.tests.nunit.MessageDispatch
{
    [TestFixture]
    public class MessageDispatcherRoutingTests
    {
        private Dispatcher _dispatcher;
        private FakeChannel _channel;
        private SpyCommandProcessor _commandProcessor;

        [SetUp]
        public void Establish()
        {
            _channel = new FakeChannel();
            _commandProcessor = new SpyCommandProcessor();

            var messageMapperRegistry = new MessageMapperRegistry(new SimpleMessageMapperFactory(() => new MyEventMessageMapper()));
            messageMapperRegistry.Register<MyEvent, MyEventMessageMapper>();

            var connection = new Connection(
                name: new ConnectionName("test"), 
                dataType: typeof(MyEvent), 
                noOfPerformers: 1, 
                timeoutInMilliseconds: 1000, 
                channelFactory: new InMemoryChannelFactory(_channel),
                channelName: new ChannelName("fakeChannel"), 
                routingKey: "fakekey");
            _dispatcher = new Dispatcher(_commandProcessor, messageMapperRegistry, new List<Connection> { connection });

            var @event = new MyEvent();
            var message = new MyEventMessageMapper().MapToMessage(@event);
            _channel.Add(message);

            _dispatcher.State.ShouldEqual(DispatcherState.DS_AWAITING);
            _dispatcher.Receive();
        }


        [Test]
        public void When_A_Message_Dispatcher_Is_Asked_To_Connect_A_Channel_And_Handler()
        {
            Task.Delay(1000).Wait();
            _dispatcher.End().Wait();


            //_should_have_consumed_the_messages_in_the_channel
            _channel.Length.ShouldEqual(0);
            //_should_have_a_stopped_state
            _dispatcher.State.ShouldEqual(DispatcherState.DS_STOPPED);
            //_should_have_dispatched_a_request
            _commandProcessor.Observe<MyEvent>().ShouldNotBeNull();
            //_should_have_published_async
            _commandProcessor.Commands.Any(ctype => ctype == CommandType.Publish).ShouldBeTrue();
        }
    }
}