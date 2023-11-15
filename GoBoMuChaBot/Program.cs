using Discord;
using Discord.Net;
using Discord.WebSocket;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;

class Program
{
    private DiscordSocketClient _client;
    private ImageAlert[] alerts;
 
    public static Task Main(string[] args) => new Program().MainAsync();

    private Task AddAlert(IMessageChannel channel, long time, string tags, long count, long score)
    {
        if (alerts != null)
        {
            ImageAlert[] buffer = alerts;
            alerts = new ImageAlert[buffer.Length + 1];
            for (int i = 0; i < buffer.Length; i++)
            {
                alerts[i] = buffer[i];
            }
            alerts[^1] = new ImageAlert(_client, channel, time, tags, count, score);
        }
        else
        {
            alerts = new ImageAlert[1];
            alerts[0] = new ImageAlert(_client, channel, time, tags, count, score);
        }
        return Task.CompletedTask;
    }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();

        _client.Log += Log;

        var token = "MTE3MzUzNTAxNTAwMzQ5MjM2Mg.Gcbtqb.84tU8RL-qcEZO8krvc9T2M25Njzu_Sno5n8ggA";

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _client.Ready += Client_Ready;

        while (_client.LoginState == LoginState.LoggedIn || _client.LoginState == LoginState.LoggingIn)
        {
            await Task.Delay(10);
            if (alerts != null && alerts.Length > 0)
            {
                foreach (var item in alerts)
                {
                    await item.Handle();
                }
            }
        }
        Console.Write("Press Enter to continue...");
        Console.ReadLine();
    }

    public async Task Client_Ready()
    {
        var r34Command = new SlashCommandBuilder()
            .WithName("r34s")
            .WithDescription("Shedule r34 random images from r34 site with set tags.")
            .WithNsfw(true)
            .AddOption("time", ApplicationCommandOptionType.Integer, "How often to post nswf(in minutes)", isRequired: true)
            .AddOption("tags", ApplicationCommandOptionType.String, "Specify tags from which to find imgs", isRequired: true)
            .AddOption("count", ApplicationCommandOptionType.Integer, "How many images to post at once", isRequired: true)
            .AddOption("min_score", ApplicationCommandOptionType.Integer, "Minimum score of imgs", isRequired: true);

        var r34xCommand = new SlashCommandBuilder()
            .WithName("r34xyz")
            .WithDescription("Shedule r34 random images from r34xyz site with set tags.")
            .WithNsfw(true)
            .AddOption("time", ApplicationCommandOptionType.Integer, "How often to post nswf(in minutes)", isRequired: true)
            .AddOption("tags", ApplicationCommandOptionType.String, "Specify tags from which to find imgs", isRequired: true);

        var StopCommand = new SlashCommandBuilder()
            .WithName("stop")
            .WithDescription("Stop the bot.");


        try
        {
            await _client.CreateGlobalApplicationCommandAsync(r34Command.Build());
            await _client.CreateGlobalApplicationCommandAsync(r34xCommand.Build());
            await _client.CreateGlobalApplicationCommandAsync(StopCommand.Build());
            _client.SlashCommandExecuted += CommandHandler;
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

            Console.WriteLine(json);
        }
    }

    private async Task CommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "r34s":
                await HandleR34S(command);
                break;
            case "stop":
                await command.RespondAsync("Goodbye.");
                await _client.LogoutAsync();
                await _client.StopAsync();
                break;
        }
    }

    private async Task HandleR34S(SocketSlashCommand command)
    {
        var time = (long)command.Data.Options.First().Value;
        string tags = (string)command.Data.Options.ElementAt(1).Value;
        var count = (long)command.Data.Options.ElementAt(2).Value;
        var score = (long)command.Data.Options.ElementAt(3).Value;
        await AddAlert(await command.GetChannelAsync(), time, tags.Replace(',', '+'), count, score);
        Console.WriteLine("{0}, {1}", time, tags);
        await command.RespondAsync("Command executed.");
    }
}

class ImageAlert
{
    private IMessageChannel _channel;
    private long _time;
    private string _tags;
    private DiscordSocketClient _client;
    private long full_time;
    private long _count;
    private long _score;
    public ImageAlert(DiscordSocketClient client, IMessageChannel channel, long time, string tags, long count, long score)
    {
        _channel = channel;
        _time = time;
        _tags = tags;
        _client = client;
        _count = count;
        _score = score;
    }

    private HtmlDocument GetDocument(string URL)
    {
        try
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(URL);
            return doc;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Cannot get document on URL: {URL}");
            return null;
        }
    }

    public async Task Handle()
    {
        if (full_time == 0)
        {
            await _channel.SendMessageAsync($"I'm here with tags {_tags} score {_score} and count {_count}");

            HtmlDocument page = GetDocument($"https://rule34.xxx/index.php?page=post&s=list&tags={_tags}&pid=0");
            HtmlNodeCollection page_collection = page.GetElementbyId("paginator").SelectNodes("div/a");
            HtmlNode last_page = page_collection.Last();
            long last_page_num = long.Parse(last_page.Attributes.First().Value.Split("pid=", 2)[^1]) / 42;

            Random rnd = new Random();
            long page_num;
            int img_num;
            HtmlNodeCollection imgs;
            HtmlDocument image_page;
            int score;
            for (int i = 0; i < _count; i++)
            {
                img_num = rnd.Next(42);
                do
                {
                    page_num = rnd.NextInt64(last_page_num + 1);
                    page = GetDocument($"https://rule34.xxx/index.php?page=post&s=list&tags={_tags}&pid={page_num * 42}");
                    //await _channel.SendMessageAsync($"https://rule34.xxx/index.php?page=post&s=list&tags={_tags}&pid={page_num * 42}");
                    //await _channel.SendMessageAsync($"https://rule34.xxx/index.php?page=post&s=list&tags={_tags}&pid=0");
                    imgs = page.GetElementbyId("post-list").SelectNodes("div")[1].SelectNodes("div");
                } while (imgs.Count <= 1);

                imgs = imgs[0].SelectNodes("span/a");
                image_page = GetDocument($"https://rule34.xxx{imgs[img_num].Attributes["href"].Value}");

                score = int.Parse(image_page.GetElementbyId($"psc{imgs[img_num].Attributes["href"].Value.Split("id=")[^1]}").InnerText);
                if (score < _score)
                {
                    i--;
                    continue;
                }

                try
                {
                    await _channel.SendMessageAsync(image_page.GetElementbyId("image").Attributes["src"].Value);
                }
                catch (Exception ex)
                {
                    i--;
                }
            }

            full_time = _time * 50000;
        }
        else
        {
            full_time -= 10;
        }
    }
}
