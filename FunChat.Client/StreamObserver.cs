using System;
using System.Threading.Tasks;
using FunChat.Common;
using Orleans.Streams;
using Spectre.Console;

namespace FunChat.Client
{
    public class StreamObserver : IAsyncObserver<ChatMsg>
    {
        private readonly string _roomName;
        public StreamObserver(string roomName) => _roomName = roomName;

        public Task OnCompletedAsync() => Task.CompletedTask;

        public Task OnErrorAsync(Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return Task.CompletedTask;
        }

        public Task OnNextAsync(ChatMsg item, StreamSequenceToken token = null)
        {
            AnsiConsole.MarkupLine("[[[dim]{0}[/]]] [[[bold red]{3}[/]]] [bold yellow]{1}:[/] {2}", item.Created.LocalDateTime, item.Author, item.Text, _roomName);
            return Task.CompletedTask;
        }
    }
}