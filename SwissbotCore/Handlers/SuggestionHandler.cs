﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;

namespace SwissbotCore.Handlers
{
    [DiscordHandler]
    class SuggestionHandler
    {
        public static DiscordSocketClient client { get; set; }
        public static List<Suggestion> CurrentSuggestions { get; set; }
        public SuggestionHandler(DiscordSocketClient _client)
        {
            client = _client;
            CurrentSuggestions = Global.LoadSuggestions();
        }

        public class Suggestion
        {
            public string SuggestionText { get; set; }
            public ulong MessageID { get; set; }
            public State ReviewType { get; set; } 
            public Status CurrentStatus { get; set; }
            public string ReviewText { get; set; }
            public ulong AuthorID { get; set; }
            public List<Updaters> Updaters { get; set; }
            public ulong ReviewerID { get; set; }
            public DateTime UTCTime { get; set; }
        }
        public enum State
        {
            Accepted,
            Denied,
            NotReviewed
        }
        public enum Status
        {
            InTheWorks,
            Completed,
            NeedsStaffReview,
            NeedsApproval,
            NotReviewed
        }
        public class Updaters
        {
            public Status status { get; set; }
            public ulong updaterID { get; set; }
            public DateTime Time { get; set; }
        }
        //<InTheWorks/Completed/NeedsStaffReview/NeedsApproval/NotReviewed>
        [DiscordCommandClass()]
        public class SuggestionCommands : CommandModuleBase
        {
            [DiscordCommand("suggest", BotCanExecute = false, description = "Suggest somthing to the server", commandHelp = "Usage - `(PREFIX)suggest <suggestion>`", RequiredPermission = false)]
            public async Task suggest(params string[] args)
            {
                if (args.Length == 0)
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = "Erm..",
                        Color = Color.Red,
                        Description = "What do you want to suggest? like i cant read minds *yet*"
                    }.Build());
                    return;
                }
                if (CurrentSuggestions.Any(x => (DateTime.UtcNow - x.UTCTime).TotalHours < 1 && x.AuthorID == Context.Message.Author.Id))
                {
                    var us = CurrentSuggestions.Find(x => (DateTime.UtcNow - x.UTCTime).TotalHours < 1 && x.AuthorID == Context.Message.Author.Id);
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = "Wait a bit please",
                        Color = Color.Red,
                        Description = $"You can only post suggestions once per hour. Please wait another **{60 - ((int)(DateTime.UtcNow - us.UTCTime).TotalMinutes)}** Minutes <3"
                    }.Build());
                    return;
                }
                string suggestmsg = string.Join(' ', args);
                Suggestion s = new Suggestion();
                s.AuthorID = Context.Message.Author.Id;
                s.Updaters = new List<Updaters>();
                s.SuggestionText = suggestmsg;
                s.UTCTime = DateTime.UtcNow;
                s.ReviewType = State.NotReviewed;
                var msg = await Context.Guild.GetTextChannel(Global.SuggestionChannelID).SendMessageAsync("creating...");
                await msg.ModifyAsync(x => 
                {
                    x.Content = "";
                    x.Embed = new EmbedBuilder()
                    {
                        Title = "User Suggestion!",
                        Color = Color.Teal,
                        Description = suggestmsg,
                        Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "Author",
                            Value = Context.Message.Author.Mention + $"\nID: ({Context.Message.Author.Id})          ",
                            IsInline = true
                        },
                        new EmbedFieldBuilder()
                        {
                            Name = "Reviewed?",
                            Value = "Not yet!",
                            IsInline = true
                        },
                        new EmbedFieldBuilder()
                        {
                            Name = "Status",
                            Value = Status.NotReviewed,
                        },
                        new EmbedFieldBuilder()
                        {
                            Name = "Message ID",
                            Value = msg.Id,
                        }
                    }
                    }.Build();
                }) ;
                await msg.AddReactionsAsync(new IEmote[] { new Emoji("✅"), new Emoji("❌") });
                s.MessageID = msg.Id;
                CurrentSuggestions.Add(s);
                Global.SaveSuggestions();
                await Context.Channel.SendMessageAsync($"Congrats {Context.Message.Author.Mention}, We added your suggestion! We'll DM you when a staff reviews it!");
            }
            
            [DiscordCommand("suggestion",
                RequiredPermission = true,
                description = "Admins use this command to accept or deny suggestions",
                BotCanExecute = false,
                commandHelp = "Usage - `(PREFIX)suggestion <accept/deny/delete/update> <MessageID> <Reason>` or `(PREFIX)suggestion status <MessageID> <InTheWorks/Completed/NeedsStaffReview/NeedsApproval/NotReviewed> <Reason>`")]
            public async Task suggestions(params string[] args)
            {
                if (!HasExecutePermission)
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = "Hol up",
                        Color = Color.Red,
                        Description = "You do not have the correct authority ma'am/sir!"
                    }.Build());
                    return;
                }
                if(Context.User.Id == 259053800755691520 | Context.User.Id == 393448221944315915) { goto skip; }
                if(Context.Guild.GetUser(Context.Message.Author.Id).Hierarchy < Context.Guild.GetRole(592755793808588840).Position)
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = "Hol up",
                        Color = Color.Red,
                        Description = "You do not have the correct authority ma'am/sir!"
                    }.Build());
                    return;
                }
                skip:
                if(args.Length == 0)
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = "bch daquafk??!!?!?!?",
                        Color = Color.Red,
                        Description = "what you doing? what you want me to do? i aint no magician?",
                        ImageUrl = "https://i.chzbgr.com/full/6113771776/hEF22755C/dafuk-was-that"
                    }.Build());
                    return;
                }
                State state = State.NotReviewed;
                Status status = Status.NotReviewed;
                bool dlt = false;
                bool isUpdate = false;
                bool isStatus = false;
                switch (args[0].ToLower())
                {
                    case "accept":
                        state = State.Accepted;
                        break;
                    case "deny":
                        state = State.Denied;
                        break;
                    case "delete":
                        dlt = true;
                        break;
                    case "update":
                        isUpdate = true;
                        break;
                    case "status":
                        {
                            isStatus = true;
                            //<InTheWorks/Completed/NeedsStaffReview/NeedsApproval/NotReviewed>
                            switch (args[2].ToLower())
                            {
                                case "InTheWorks":
                                    status = Status.InTheWorks;
                                    break;
                                case "Completed":
                                    status = Status.Completed;
                                    break;
                                case "NeedsStaffReview":
                                    status = Status.NeedsStaffReview;
                                    break;
                                case "NeedsApproval":
                                    status = Status.NeedsApproval;
                                    break;
                                case "NotReviewed":
                                    status = Status.NotReviewed;
                                    break;
                                default:
                                    {
                                        await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                                        {
                                            Title = "What?",
                                            Color = Color.Red,
                                            Description = $"Sorry but you need to follow this format `*suggestion status <MessageID> <InTheWorks/Completed/NeedsStaffReview/NeedsApproval/NotReviewed> <Reason>",
                                            ImageUrl = "https://i.chzbgr.com/full/6113771776/hEF22755C/dafuk-was-that"
                                        }.Build());
                                        return;
                                    }
                            }
                        }
                        break;
                    default:
                        {
                            await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                            {
                                Title = "What?",
                                Color = Color.Red,
                                Description = $"Sorry but you need to either `Accept` or `Deny` a suggestion. i dont know what {args[0].ToLower()} means..",
                                ImageUrl = "https://i.chzbgr.com/full/6113771776/hEF22755C/dafuk-was-that"
                            }.Build());
                            return;
                        }
                }
                if(args.Length == 1)
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = "What suggestion do you want to accept or deny?",
                        Color = Color.Red,
                        Description = "Please provide a message ID",
                    }.Build());
                    return;
                }
                if (args.Length == 2 && !dlt)
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = "Whats the Reason?",
                        Color = Color.Red,
                        Description = "Please provide a reason!",
                    }.Build());
                    return;
                }

                string reason = string.Join(' ', args.Skip(2));
                ulong id = 0;
                try
                {
                    id = ulong.Parse(args[1]);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = "H u h ?",
                        Color = Color.Red,
                        Description = "That Message ID is Invalid",
                    }.Build());
                    return;
                }
                if (!CurrentSuggestions.Any(x => x.MessageID == id))
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = "Sorry, I can't find that Message :/",
                        Color = Color.Red,
                        Description = "looks like that ID is not on record",
                    }.Build());
                    return;
                }
                var suggestion = CurrentSuggestions.Find(x => x.MessageID == id);
                if(suggestion.ReviewType != State.NotReviewed && !isUpdate && !isStatus)
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = $"Sorry, you cant {state} that :/",
                        Color = Color.Red,
                        Description = $"Someones already {suggestion.ReviewType} that suggestion, try updating it or changing the status",
                    }.Build());
                    return;
                }
                if (isUpdate)
                {
                    //update card

                }
                else if (isStatus)
                {
                    //update status
                }
                else
                {
                    int inx = CurrentSuggestions.IndexOf(suggestion);
                    suggestion.ReviewText = reason == null ? "" : reason;
                    suggestion.ReviewType = state;
                    suggestion.ReviewerID = Context.Message.Author.Id;
                    CurrentSuggestions[inx] = suggestion;
                    Global.SaveSuggestions();
                    //try to get message
                    RestUserMessage msg = (RestUserMessage)await Context.Guild.GetTextChannel(Global.SuggestionChannelID).GetMessageAsync(suggestion.MessageID);
                    if (msg == null)
                    {
                        await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                        {
                            Title = "Sorry, I can't find that Message :/",
                            Color = Color.Red,
                            Description = "looks like that message is either deleted or is too old",
                        }.Build());
                        return;
                    }
                    if (dlt)
                    {
                        if((DateTime.UtcNow - msg.Timestamp.UtcDateTime).TotalDays >= 14) {
                            await Context.Channel.SendMessageAsync("Suggestion cannot be deleted as it is older than 14 days \n" + msg.GetJumpUrl());
                            CurrentSuggestions.Remove(suggestion);
                            return;
                        }
                        await msg.DeleteAsync();
                        await Context.Channel.SendMessageAsync("Deleted!");
                        CurrentSuggestions.Remove(suggestion);
                        Global.SaveSuggestions();
                        return;
                    }
                    await msg.ModifyAsync(x => x.Embed = new EmbedBuilder()
                    {
                        Title = "User Suggestion!",
                        Color = state == State.Accepted ? Color.Green : state == State.Denied ? Color.Red : Color.Red,
                        Description = suggestion.SuggestionText +
                        $"\n\n-----------**{state}**-----------\n\n**{state} For:** {reason}",
                        Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "Author",
                            Value = $"<@{suggestion.AuthorID}>" + $"\nID: ({suggestion.AuthorID})          ",
                            IsInline = true
                        },
                        new EmbedFieldBuilder()
                        {
                            Name = "Reviewed?",
                            Value = $"**{state}** by {Context.Message.Author.Mention}\n  ",
                            IsInline = true
                        },
                        new EmbedFieldBuilder()
                        {
                            Name = "Message ID",
                            Value = msg.Id,
                        }
                    }
                    }.Build());

                    try
                    {
                        //dm user
                        var usr = Context.Guild.GetUser(suggestion.AuthorID).SendMessageAsync($"Your Suggestion \"{suggestion.SuggestionText}\" was **{state}** by <@{suggestion.ReviewerID}>");
                    }
                    catch { }
                    await Context.Channel.SendMessageAsync("Success <3");
                }
            }
        }
    }
}
