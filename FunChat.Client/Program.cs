using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FunChat;
using FunChat.Client;
using FunChat.Common;
using Orleans;
using Orleans.Hosting;
using Spectre.Console;

//To make this sample simple
//In this sample, one client can only join one channel, hence we have a static variable of one channel name.
//client can send messages to the channel , and receive messages sent to the channel/stream from other clients.
var client = new ClientBuilder()
    .UseLocalhostClustering()
    .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IChannelGrain).Assembly).WithReferences())
    .AddSimpleMessageStreamProvider("chat")
    .Build();

PrintUsage();

await AnsiConsole.Status().StartAsync("Connecting to server", async ctx =>
{
    ctx.Spinner(Spinner.Known.Dots);
    ctx.Status = "Connecting...";

    await client.Connect(async error =>
    {
        AnsiConsole.MarkupLine("[bold red]Error:[/] error connecting to server!");
        AnsiConsole.WriteException(error);
        ctx.Status = "Waiting to retry...";
        await Task.Delay(TimeSpan.FromSeconds(2));
        ctx.Status = "Retrying connection...";
        return true;
    });

    ctx.Status = "Connected!";
});

string currentChannel = null;
string defaultChannel = "generic";

var userName = AnsiConsole.Ask<string>("What is your [aqua]name[/]?");
while (!Helpers.IsValidUserName(userName))
{
    userName = AnsiConsole.Ask<string>("[aqua]The name[/] must be alphanumeric characters and 3 ~ 10 characters.");
}

var password = AnsiConsole.Ask<string>("What is your [aqua]password[/]?");
while (!string.Equals(userName, password))
{
    password = AnsiConsole.Ask<string>("[aqua]The password[/] is the same as your [aqua]name[/]");
}

await JoinChannel();

string input = null;
do
{
    input = Console.ReadLine();

    await ValidateCurrentChannel();

    if (string.IsNullOrWhiteSpace(input)) continue;

    if (input.StartsWith("/j"))
    {
        string secureString = AnsiConsole.Ask<string>("What is room's [aqua]secure string[/]?");
        await JoinPrivateRoom(secureString, input.Replace("/j", "").Trim());
    }
    else if (input.StartsWith("/n"))
    {
        string secure = input.Replace("/n", "").Trim();
        while (!Helpers.IsValidSecure(secure))
        {
            secure = AnsiConsole.Ask<string>("[aqua]The password[/] must be alphanumeric characters and 6 ~ 18 characters.");
        }

        await CreatePrivateRoom(secure);
        AnsiConsole.MarkupLine("[dim][[STATUS]][/] You are at [lime]{0}[/]", currentChannel);
    }
    else if (input.StartsWith("/l"))
    {
        await LeaveChannel();
    }
    else if (input.StartsWith("/h"))
    {
        await ShowCurrentChannelHistory();
    }
    else if (input.StartsWith("/m"))
    {
        await ShowChannelMembers();
    }
    else if (input.StartsWith("/g"))
    {
        await SwitchTo(input.Replace("/g", "").Trim());
    }
    else if (Helpers.IsAdmin(userName) &&
        input.StartsWith("/adc"))
    {
        await ShowChannels();
    }
    else if (Helpers.IsAdmin(userName) &&
             input.StartsWith("/adm"))
    {
        await ShowChannelMembers(input.Replace("/adm", "").Trim());
    }
    else if (Helpers.IsAdmin(userName) &&
             input.StartsWith("/adr"))
    {
        await DeletePrivateChannel(input.Replace("/adr", "").Trim());
    }
    else if (!input.StartsWith("/exit"))
    {
        await SendMessage(input);
    }
    else
    {
        if (AnsiConsole.Confirm("Do you really want to exit?"))
        {
            break;
        }
    }
} while (input != "/exit");

await AnsiConsole.Status().StartAsync("Disconnecting...", async ctx =>
{
    ctx.Spinner(Spinner.Known.Dots);
    await client.Close();
});

void PrintUsage()
{
    AnsiConsole.WriteLine();
    using var logoStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FunChat.Client.logo.png");
    var logo = new CanvasImage(logoStream)
    {
        MaxWidth = 25
    };

    var table = new Table()
    {
        Border = TableBorder.None,
        Expand = true,
    }.HideHeaders();
    table.AddColumn(new TableColumn("One"));

    var header = new FigletText("Orleans")
    {
        Color = Color.Fuchsia
    };
    var header2 = new FigletText("Chat Room")
    {
        Color = Color.Aqua
    };

    var markup = new Markup(
       "[bold fuchsia]/j[/] [aqua]<channel>[/] to [underline green]join[/] a private channel\n"
       + "[bold fuchsia]/n[/] [aqua]<secure>[/] to create your [underline green]private channel[/]\n"
       + "[bold fuchsia]/l[/] to [underline green]leave[/] the current channel\n"
       + "[bold fuchsia]/h[/] to re-read the current channel [underline green]history[/]\n"
       + "[bold fuchsia]/m[/] to query [underline green]members[/] in the current channel\n"
       + "[bold fuchsia]/g[/] to go to [underline green]the specific private channel[/]\n"
       + "[[[bold red]administrator[/]]][bold fuchsia] /adc[/] to query [underline green]list of all channels[/]\n"
       + "[[[bold red]administrator[/]]][bold fuchsia] /adm[/] [aqua]<channel>[/] to query [underline green]list of members in the channel.[/]\n"
       + "[[[bold red]administrator[/]]][bold fuchsia] /adr[/] [aqua]<channel>[/] to delete [underline green]the channel.[/]\n"
       + "[bold fuchsia]/exit[/] to exit\n"
       + "[bold aqua]<message>[/] to send a [underline green]message[/]\n");
    table.AddColumn(new TableColumn("Two"));
    var rightTable = new Table().HideHeaders().Border(TableBorder.None).AddColumn(new TableColumn("Content"));
    rightTable.AddRow(header).AddRow(header2).AddEmptyRow().AddEmptyRow().AddRow(markup);
    table.AddRow(logo, rightTable);

    AnsiConsole.Render(table);
    AnsiConsole.WriteLine();
}

async Task ShowChannelMembers(string nameChannel = "")
{
    if (string.IsNullOrEmpty(nameChannel))
        nameChannel = currentChannel;

    var room = client.GetGrain<IChannelGrain>(nameChannel);
    var members = await room.GetMembers();

    AnsiConsole.Render(new Rule($"Members for '{nameChannel}'")
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });

    foreach (var member in members)
    {
        AnsiConsole.MarkupLine("[bold yellow]{0}[/]", member);
    }

    AnsiConsole.Render(new Rule()
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });
}

async Task ShowCurrentChannelHistory()
{
    var room = client.GetGrain<IChannelGrain>(currentChannel);
    var history = await room.ReadHistory(1000);

    AnsiConsole.Render(new Rule($"History for '{currentChannel}'")
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });

    foreach (var chatMsg in history)
    {
        AnsiConsole.MarkupLine("[[[dim]{0}[/]]] [bold yellow]{1}:[/] {2}", chatMsg.Created.LocalDateTime, chatMsg.Author, chatMsg.Text);
    }

    AnsiConsole.Render(new Rule()
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });
}

async Task SendMessage(string messageText)
{
    var room = client.GetGrain<IChannelGrain>(currentChannel);
    await room.Message(new ChatMsg(userName, messageText));
}

async Task JoinChannel()
{
    currentChannel = defaultChannel;

    AnsiConsole.MarkupLine("[bold aqua]Joining default channel [/]{0}", currentChannel);

    await AnsiConsole.Status().StartAsync("Joining default channel...", async ctx =>
    {
        var room = client.GetGrain<IChannelGrain>(currentChannel);
        var streamId = await room.Join(userName);
        var stream = client.GetStreamProvider("chat").GetStream<ChatMsg>(streamId, "default");
        //subscribe to the stream to receiver furthur messages sent to the chatroom
        await stream.SubscribeAsync(new StreamObserver(currentChannel));
    });
    AnsiConsole.MarkupLine("[bold aqua]Joined default channel [/]{0}", currentChannel);
}

async Task LeaveChannel()
{
    if (currentChannel == defaultChannel)
    {
        AnsiConsole.MarkupLine("[bold olive]Can not leaving channel [/]{0}", currentChannel);
        return;
    }

    AnsiConsole.MarkupLine("[bold olive]Leaving channel [/]{0}", currentChannel);
    await AnsiConsole.Status().StartAsync("Leaving channel...", async ctx =>
    {
        var room = client.GetGrain<IChannelGrain>(currentChannel);
        var streamId = await room.Leave(userName);
        var stream = client.GetStreamProvider("chat").GetStream<ChatMsg>(streamId, "default");

        //unsubscribe from the channel/stream since client left, so that client won't
        //receive furture messages from this channel/stream
        var subscriptionHandles = await stream.GetAllSubscriptionHandles();
        foreach (var handle in subscriptionHandles)
        {
            await handle.UnsubscribeAsync();
        }
    });

    AnsiConsole.MarkupLine("[bold olive]Left channel [/]{0}", currentChannel);

    currentChannel = defaultChannel;
}

async Task JoinPrivateRoom(string secureString, string nameChannel)
{
    AnsiConsole.MarkupLine("[bold aqua]Joining channel [/]{0}", nameChannel);

    // Validation: maximum join 2 channels
    string[] joinedPrivateChannels = await GetJoinedPrivateChannel();
    if (joinedPrivateChannels.Length > 1)
    {
        AnsiConsole.MarkupLine(
            "You can [bold aqua]maximum[/] join 2 private channels. Your joined private channels are: {0}.",
            string.Join(",", joinedPrivateChannels));
        return;
    }

    await AnsiConsole.Status().StartAsync("Joining channel...", async ctx =>
    {
        var room = client.GetGrain<IChannelGrain>(nameChannel);
        var isValid = await room.IsValidSecure(secureString);
        if (!isValid)
        {
            AnsiConsole.MarkupLine("[bold aqua]Channel's password is incorrect.[/]");
            return;
        }
        currentChannel = nameChannel;
        var streamId = await room.Join(userName);
        var stream = client.GetStreamProvider("chat").GetStream<ChatMsg>(streamId, "default");
        //subscribe to the stream to receiver furthur messages sent to the chatroom
        await stream.SubscribeAsync(new StreamObserver(currentChannel));
    });
    AnsiConsole.MarkupLine("[bold aqua]Joined channel [/]{0}", currentChannel);
}

async Task CreatePrivateRoom(string secureString)
{
    // Validation: maximum join 2 channels
    string[] joinedPrivateChannels = await GetJoinedPrivateChannel();
    if (joinedPrivateChannels.Length > 1)
    {
        AnsiConsole.MarkupLine(
            "You can [bold aqua]maximum[/] join 2 private channels. Your joined private channels are: {0}.",
            string.Join(",", joinedPrivateChannels));
        return;
    }

    string nameChannel = Helpers.GetUniqChannelName();

    currentChannel = nameChannel;

    AnsiConsole.MarkupLine("[bold aqua]Creating channel [/]{0}", currentChannel);

    await AnsiConsole.Status().StartAsync("Creating channel...", async ctx =>
    {
        var room = client.GetGrain<IChannelGrain>(nameChannel);
        await room.SetSecureString(secureString);
        var streamId = await room.Join(userName);
        var stream = client.GetStreamProvider("chat").GetStream<ChatMsg>(streamId, "default");
        //subscribe to the stream to receiver furthur messages sent to the chatroom
        await stream.SubscribeAsync(new StreamObserver(currentChannel));
    });
    AnsiConsole.MarkupLine("[bold aqua]Created channel [/]{0}", currentChannel);

    // adding private channel
    var room = client.GetGrain<IChannelGrain>(defaultChannel);
    await room.AddPrivateChannel(nameChannel);
}

async Task SwitchTo(string nameChannel)
{
    var room = client.GetGrain<IChannelGrain>(nameChannel);
    var members = await room.GetMembers();
    if (!members.Contains(nameChannel))
    {
        AnsiConsole.MarkupLine("[bold aqua]{0}[/] does belong to [bold aqua]{1} channel[/].", userName, nameChannel);
        return;
    }

    currentChannel = nameChannel;
}

// Administrator

async Task ShowChannels()
{
    string[] privateChannels = await client.GetGrain<IChannelGrain>(defaultChannel).GetPrivateChannels();

    AnsiConsole.Render(new Rule($"Private channels for '{userName}'")
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });

    AnsiConsole.MarkupLine("[bold red]{0}[/]", defaultChannel);
    foreach (var privateChannel in privateChannels)
    {
        AnsiConsole.MarkupLine("[bold yellow]{0}[/]", privateChannel);
    }

    AnsiConsole.Render(new Rule()
    {
        Alignment = Justify.Center,
        Style = Style.Parse("darkgreen")
    });
}

async Task DeletePrivateChannel(string nameChannel)
{
    if (nameChannel == defaultChannel)
    {
        AnsiConsole.MarkupLine("[bold olive]Can not delete {0} channel [/]", nameChannel);
        return;
    }

    AnsiConsole.MarkupLine("[bold olive]Deleting channel [/]{0}", nameChannel);
    await AnsiConsole.Status().StartAsync("Deleting channel...", async ctx =>
    {
        var room = client.GetGrain<IChannelGrain>(nameChannel);
        await room.Delete();
    });

    AnsiConsole.MarkupLine("[bold olive]Deleted channel [/]{0}", nameChannel);

    // go to default channel
    if (currentChannel == nameChannel)
    {
        currentChannel = defaultChannel;
    }
}

async Task<string[]> GetJoinedPrivateChannel()
{
    List<string> joinedPrivateChannels = new List<string>();
    var privateChannels = await client.GetGrain<IChannelGrain>(defaultChannel).GetPrivateChannels();
    foreach (var privateChannel in privateChannels)
    {
        if (client.GetGrain<IChannelGrain>(privateChannel).GetMembers().Result.All(_ => _ != userName)) continue;
        joinedPrivateChannels.Add(privateChannel);
    }

    return joinedPrivateChannels.ToArray();
}

async Task ValidateCurrentChannel()
{
    var room = client.GetGrain<IChannelGrain>(currentChannel);
    var isDeleted = await room.IsDeleted();
    if (isDeleted)
    {
        AnsiConsole.MarkupLine("[bold red]{0} channel[/] is not found. You have been move to [bold olive]{1} channel[/].", currentChannel, defaultChannel);
        currentChannel = defaultChannel;
    }
}