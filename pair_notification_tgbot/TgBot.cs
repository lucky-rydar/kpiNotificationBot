using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace pair_notification_tgbot
{
    class TgBot
    {
        TelegramBotClient tgBotClient;
        Dictionary<string, PageData> allGroupsPairData;
        DatabaseController db;

        public TgBot()
        {
            tgBotClient = new TelegramBotClient("1309652518:AAGBoah0faywqPCSYLAjp8X-e6Z6DXj92nk");
            tgBotClient.OnMessage += Bot_OnMessage;

            db = new DatabaseController();
            allGroupsPairData = new Dictionary<string, PageData>();

            List<Task<DateTime>> tasks = new List<Task<DateTime>>();
            var groups = db.GetSubGroups();
            
            for (int i = 0; i < groups.Count; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() => getDataForAsync(db.GetSubGroups()[index])));
            }
            Task.WaitAll(tasks.ToArray());
        }

        public void Run()
        {
            DateTime tempTime = DateTime.Now;
            while (tempTime.Minute % 5 != 0)
            {
                tempTime = DateTime.Now;
                Thread.Sleep(1000);
            }
            
            tgBotClient.StartReceiving();
            Console.WriteLine($"[INFO {DateTime.Now.TimeOfDay}] Bot have started receiving.");

            DateTime currentTime = DateTime.Now;
            DateTime waituntil = DateTime.Now;
            while (true)
            {
                currentTime = DateTime.Now;
                

                if (currentTime.Hour == waituntil.Hour && currentTime.Minute == waituntil.Minute)
                {
                    currentTime = waituntil;
                    waituntil = waituntil.AddMinutes(5);

                    var allSubs = db.GetAllData();

                    for(int i = 0; i < allSubs.Count; i++)
                    {
                        if(allGroupsPairData[allSubs[i].group].isToday && allGroupsPairData[allSubs[i].group].groupExist)
                        {
                            if (DateTime.Now.Hour == allGroupsPairData[allSubs[i].group].closestPairTime.AddMinutes(-10).Hour && 
                                DateTime.Now.Minute == allGroupsPairData[allSubs[i].group].closestPairTime.AddMinutes(-10).Minute)
                            {
                                if(allGroupsPairData.ContainsKey(allSubs[i].group))
                                {
                                    tgBotClient.SendTextMessageAsync(allSubs[i].id, "Через 10 хвилин " + "\n" +
                                    allGroupsPairData[allSubs[i].group].pairNum + " пара" + "\n" +
                                    "О " + allGroupsPairData[allSubs[i].group].closestPairTime.Hour + ":" + allGroupsPairData[allSubs[i].group].closestPairTime.Minute + "\n" +
                                    "Предмет: " + allGroupsPairData[allSubs[i].group].subjectName + "\n" +
                                    "Викладач: " + allGroupsPairData[allSubs[i].group].teacher + "\n");
                                    Console.WriteLine($"[INFO {DateTime.Now.TimeOfDay}] Sent notifocation to | {allSubs[i].index} | {allSubs[i].group} | {allSubs[i].id}.");
                                }
                            }
                        }
                    }
                    Console.WriteLine($"[INFO {DateTime.Now.TimeOfDay}] Sent notifocation.");

                    var groups = db.GetSubGroups();
                    for(int i = 0; i < groups.Count; i++)
                    {
                        var index = i;
                        Task.Run(() => getDataForAsync(groups[index]));
                    }
                    Console.WriteLine($"[INFO {DateTime.Now.TimeOfDay}] Updated data for groups.");
                }
                Thread.Sleep(1000);
            }
        }

        async void Bot_OnMessage(object sender, MessageEventArgs m)
        {
            if (m == null)
                return;
            
            if (m.Message.Text == "/start")
            {
                await tgBotClient.SendTextMessageAsync(m.Message.Chat.Id, "Цей бот буде нагадувати вам про наступні пари.");
            }
            else if (m.Message.Text == "/help")
            {
                await tgBotClient.SendTextMessageAsync(m.Message.Chat.Id, "Список команд:\n" +
                    "/groups - виведе всі групи, на нагадування яких ви підписані в даний момент.\n" +
                    "/help - виведе це повідомлення.\n" +
                    "Для того, щоб підписатися на розсилку нагадувань або відписатися від неї, " +
                    "потрібно на українській мові відправити боту назву групи, нагадування " +
                    "про пари якої ви хочете отримувати(приклад: ІТ-02, іт-02, Іт-02, іТ-02).");
            }
            else if(m.Message.Text == "/groups")
            {
                var groupsById = db.GetGroupsById(m.Message.Chat.Id);

                if(groupsById.Count > 0)
                {
                    string toSend = new string("Ваші групи: \n");
                    for (int i = 0; i < groupsById.Count; i++)
                        toSend += $"{i+1}. {groupsById[i]}\n";

                    await tgBotClient.SendTextMessageAsync(m.Message.Chat.Id, toSend);
                }
                else if(groupsById.Count == 0)
                {
                    await tgBotClient.SendTextMessageAsync(m.Message.Chat.Id, "Ви ще не підписані на жодну групу");
                }
            }
            else if (Regex.Match(m.Message.Text, @"([а-яА-ЯЄІЇєії][а-яА-ЯЄІЇєії]\-\d{2})").Value == m.Message.Text)
            {
                string group = m.Message.Text.ToUpper();
                long id = m.Message.Chat.Id;
                
                if (!db.ContainsSub(group, id))
                {
                    db.AddSub(group, id);

                    await tgBotClient.SendTextMessageAsync(id, "Зачекайте будь-ласка, намагаюся знайти групу...");
                    Console.WriteLine($"[INFO {DateTime.Now.TimeOfDay}] {m.Message.Chat.FirstName} {m.Message.Chat.LastName} trying to subscribe on {group}");

                    var pageParser = new PageParser();
                    var pageData = pageParser.getData(group);
                    ///TODO checking if such group exist
                    
                    if(pageData.groupExist)
                    {
                        allGroupsPairData[group] = pageData;
                        if (!db.ContainsSub(group, id))
                        {
                            allGroupsPairData.Remove(group);
                        }
                        else if (db.ContainsSub(group, id))
                        {
                            await tgBotClient.SendTextMessageAsync(id, $"Тепер ви будете отримувати нагадування про пари групи '{group}'.");
                            Console.WriteLine($"[INFO {DateTime.Now.TimeOfDay}] {m.Message.Chat.FirstName} {m.Message.Chat.LastName} subscribed on {group}");
                        }
                    }
                    else
                    {
                        await tgBotClient.SendTextMessageAsync(id, $"Група '{group}' не існує, не можу добавити її у ваш список.");
                        Console.WriteLine($"[INFO {DateTime.Now.TimeOfDay}] {m.Message.Chat.FirstName} {m.Message.Chat.LastName} could not be subscribed on {group}");
                        db.RemoveSub(group, id);
                    }
                        

                    
                }
                else if(db.ContainsSub(group, id))
                {
                    db.RemoveSub(group, id);
                    await tgBotClient.SendTextMessageAsync(id, $"Ви більше не будете отримувати нагадуування про пари групи '{group}'.");
                    Console.WriteLine($"[INFO {DateTime.Now.TimeOfDay}] {m.Message.Chat.FirstName} {m.Message.Chat.LastName} unsubscribed from {group}");
                }
            }
            else
            {
                await tgBotClient.SendTextMessageAsync(m.Message.Chat.Id, "Нема такої команди або групи");
                Console.WriteLine($"[INFO {DateTime.Now.TimeOfDay}] Not found command for {m.Message.Chat.FirstName} {m.Message.Chat.LastName}");
            }
            
        }
        
        private async Task<DateTime> getDataForAsync(string group)
        {
            var pageParser = new PageParser();

            var tempPageData = pageParser.getData(group);
            
            allGroupsPairData[group] = tempPageData;    

            GC.Collect(); // COLLECTOR

            return DateTime.Now;
        }
    }
}
