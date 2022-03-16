using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace FunChat.UnitTest
{
    public class ClientConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => 
            clientBuilder.AddSimpleMessageStreamProvider("chat");
    }
}