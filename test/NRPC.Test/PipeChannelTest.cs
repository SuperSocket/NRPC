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

            using(var serverChannelListener = new NamePipeChannelListener(options))
            using(var clientChannel = new NamePipeClientChannel(options))
            {
                await Task.WhenAll(new Task[]
                    {
                        serverChannelListener.ListenAsync(),
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

            using(var serverChannelListener = new NamePipeChannelListener(options))
            using(var clientChannelA = new NamePipeClientChannel(options))
            using(var clientChannelB = new NamePipeClientChannel(options))
            {
                await Task.WhenAll(new Task[]
                    {
                        serverChannelListener.ListenAsync(),
                        clientChannelA.Start()
                    });

                Console.WriteLine("clientChannelA established.");

                await Task.WhenAll(new Task[]
                    {
                        serverChannelListener.ListenAsync(),
                        clientChannelB.Start()
                    });

                Console.WriteLine("clientChannelB established.");
            }
        }
    }
}