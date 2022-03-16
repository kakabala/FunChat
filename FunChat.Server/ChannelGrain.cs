using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FunChat.Common;
using Orleans;
using Orleans.Streams;

namespace FunChat.Server
{
    public class ChannelGrain : Grain, IChannelGrain
    {
        private const string NameRoomDefault = "generic";
        private readonly List<ChatMsg> messages = new List<ChatMsg>(100);
        private readonly List<string> onlineMembers = new List<string>(10);
        // private readonly Dictionary<string, IClusterClient> onlineMembers = new Dictionary<string, IClusterClient>(10);
        private IAsyncStream<ChatMsg> stream;
        private string secureString;
        private List<string> privateChannels = new List<string>();
        
        public override Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider("chat");

            stream = streamProvider.GetStream<ChatMsg>(Guid.NewGuid(), "default");
            return base.OnActivateAsync();
        }

        public async Task<Guid> Join(string nickname)
        {
            onlineMembers.Add(nickname);

            await stream.OnNextAsync(new ChatMsg("System", $"{nickname} joins the chat '{this.GetPrimaryKeyString()}' ..."));

            return stream.Guid;
        }
        
        public async Task<Guid> Leave(string nickname)
        {
            onlineMembers.Remove(nickname);
            await stream.OnNextAsync(new ChatMsg("System", $"{nickname} leaves the chat..."));

            return stream.Guid;
        }

        public async Task<bool> Message(ChatMsg msg)
        {
            messages.Add(msg);
            await stream.OnNextAsync(msg);

            return true;
        }

        public Task<string[]> GetMembers()
        {
            return Task.FromResult(onlineMembers.ToArray());
        }

        public Task<string[]> GetPrivateChannels()
        {
            return Task.FromResult(privateChannels.ToArray());
        }
        
        public Task<ChatMsg[]> ReadHistory(int numberOfMessages)
        {
            return Task.FromResult(messages
                .OrderByDescending(x => x.Created)
                .Take(numberOfMessages)
                .OrderBy(x => x.Created)
                .ToArray());
        }

        public Task SetSecureString(string secure)
        {
            secureString = secure;
            return Task.CompletedTask;
        }

        public Task<bool> IsValidSecure(string secure)
        {
            return Task.FromResult(secure == secureString);
        }
        public Task AddPrivateChannel(string nameChannel)
        {
            privateChannels.Add(nameChannel);
            return Task.CompletedTask;
        }

        public override Task OnDeactivateAsync()
        {
            stream = null;

            return base.OnDeactivateAsync();
        }

        public async Task Delete()
        {
            await OnDeactivateAsync();
        }

        public Task<bool> IsDeleted()
        {
            return Task.FromResult(stream == null);
        }
    }
}