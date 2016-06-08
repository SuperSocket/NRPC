using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NRPC.Channels.Pipe;
using Xunit;

namespace NRPC.Test
{
    public class PipeChannelTest
    {
        
        [Fact]
        public async Task TestChannelConnection()
        {
            var options = Options.Create(new NamePipeConfig
            {
                ServerName = ".",
                PipeName = "TestServerChannel"
            });

            using(var serverChannel = new NamePipeServerChannel(options))
            using(var clientChannel = new NamePipeClientChannel(options))
            {
                await Task.WhenAll(new Task[]
                    {
                        serverChannel.Start(),
                        clientChannel.Start()
                    });

                Console.WriteLine("Connection established.");
            }
        }

        [Fact]
        public async Task TestOneToManyChannels()
        {
            var options = Options.Create(new NamePipeConfig
            {
                ServerName = ".",
                PipeName = "TestServerChannel"
            });

            using(var serverChannel = new NamePipeServerChannel(options))
            using(var clientChannelA = new NamePipeClientChannel(options))
            using(var clientChannelB = new NamePipeClientChannel(options))
            {
                await Task.WhenAll(new Task[]
                    {
                        serverChannel.Start(),
                        clientChannelA.Start()
                    });

                Console.WriteLine("clientChannelA established.");

                var connectTask = clientChannelB.Start();

                Assert.Equal(connectTask, await Task.WhenAny(connectTask, Task.Delay(1000 * 5)));
                Console.WriteLine("clientChannelB established.");
            }
        }
    }
}