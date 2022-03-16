using System;
using Orleans.TestingHost;

namespace FunChat.UnitTest
{
    public class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            var builder = new TestClusterBuilder();
            Cluster = builder.AddSiloBuilderConfigurator<SiloConfigurator>()
                .AddClientBuilderConfigurator<ClientConfigurator>()
                .Build();
            Cluster.Deploy();
        }
        
        public void Dispose()
        {
            this.Cluster.StopAllSilos();
        }
        
        public TestCluster Cluster { get; private set; }
    }
}