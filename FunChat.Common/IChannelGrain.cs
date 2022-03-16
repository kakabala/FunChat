using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;

namespace FunChat.Common
{
    public interface IChannelGrain : IGrainWithStringKey
    {
        Task<Guid> Join(string nickname);
        Task<Guid> Leave(string nickname);
        Task<bool> Message(ChatMsg msg);
        Task<ChatMsg[]> ReadHistory(int numberOfMessages);
        Task<string[]> GetMembers();
        Task<string[]> GetPrivateChannels();
        Task SetSecureString(string secure);
        Task<bool> IsValidSecure(string secure);
        Task AddPrivateChannel(string nameChannel);
        Task Delete();
        Task<bool> IsDeleted();
    }
}