using Orleans.Hosting;
using Orleans.TestingHost;

namespace FunChat.UnitTest
{
    public class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder hostBuilder) =>
            hostBuilder
                .AddMemoryGrainStorage("PubSubStore")
                .AddSimpleMessageStreamProvider("chat")
                ;
    }
}