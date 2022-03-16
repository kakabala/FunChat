using System;
using System.Threading.Tasks;
using FunChat.Common;
using FunChat.Server;
using Orleans.TestingHost;
using Xunit;

namespace FunChat.UnitTest
{
    [Collection(ClusterCollection.Name)]
    public class UnitTest
    {
        private readonly TestCluster _cluster;

        public UnitTest(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }
        
        [Fact]
        public async Task Join()
        {
            var hello = _cluster.GrainFactory.GetGrain<IChannelGrain>("generic");
            var greeting = await hello.Join("test");

            Assert.IsType<Guid>(greeting);
        }
        
        [Fact]
        public async Task Leave()
        {
            var hello = _cluster.GrainFactory.GetGrain<IChannelGrain>("generic");
            var greeting = await hello.Leave("test");

            Assert.IsType<Guid>(greeting);
        }
        
        [Fact]
        public async Task GetMembers()
        {
            var hello = _cluster.GrainFactory.GetGrain<IChannelGrain>("generic");
            var greeting = await hello.GetMembers();

            Assert.IsType<string[]>(greeting);
        }
    }
}