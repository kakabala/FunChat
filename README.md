# Chat Room

![A screenshot of the chat client](screenshot.png)

This sample uses [Orleans Streaming](http://dotnet.github.io/orleans/docs/streaming/index.html) to build a basic chat application. In this application, each client can:

* Login with any login name, the password is the same as the user’s login name 
* Chat with generical channel
* Create private channel with password
* Join and leave a channel
* Send and receive messages in that channel
* List the channel members
* Display the channel's chat history
* [Admin] login admin with 'admin' account
* [Admin] List any channels (include private channels)
* [Admin] List channel members (include private channels)
* [Admin] Delete Channel

Each chat channel has a corresponding `ChannelGrain` which is identified by the channel's name, and a stream which is identified by a `Guid` generated by that grain. Clients connect to the `ChannelGrain` for a channel and then subscribe to the stream identified by the `Guid` returned from the `IChannelGrain.Join` call.

## Running the sample

First, start the server in one terminal window by executing the following:

```PowerShell
dotnet run --project FunChat.Server
```

Then, once the server has started, open one or more terminal windows and execute the following in each:

```PowerShell
dotnet run --project FunChat.Client
```

The clients will print instructions to the terminal which tell you how to interact with the sample.