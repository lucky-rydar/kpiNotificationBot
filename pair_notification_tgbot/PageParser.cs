using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using HtmlAgilityPack;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace pair_notification_tgbot
{
    class PageParser
    {
        private  IWebDriver browser;
        private const string schedule_url = "http://rozklad.kpi.ua/Schedules/ScheduleGroupSelection.aspx";
        private PageData tempData;

        public PageParser()
        {
            browser = new PhantomJSDriver();
        }

        public PageData getData(string groupName)
        {
            
            tempData = new PageData();

            browser.Navigate().GoToUrl(schedule_url);
            browser.FindElement(By.Id("ctl00_MainContent_ctl00_txtboxGroup")).SendKeys(groupName);
            browser.FindElement(By.Id("ctl00_MainContent_ctl00_btnShowSchedule")).Click();
            
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(browser.PageSource);
            
            var main_node = doc.DocumentNode;
            if(main_node.SelectSingleNode("/html/body/div/form/div[5]/div[1]/span[2]") != null)
            {
                tempData.groupExist = false;

                browser.Close();
                browser.Dispose();

                return tempData;
            }

            var firstTable = main_node.SelectSingleNode("/html/body/div/form/div[5]/div[1]/table/tbody");
            var secondTable = main_node.SelectSingleNode("/html/body/div/form/div[5]/div[2]/table/tbody");

            findFor(firstTable);
            findFor(secondTable);
            tempData.groupExist = true;

            browser.Close();
            browser.Dispose();
            
            return tempData;
        }

        private void findFor(HtmlNode node)
        {
            var temp_tr = node.SelectNodes("tr");
            List<List<HtmlNode>> td_nodes = new List<List<HtmlNode>>();

            for (int i = 0; i < temp_tr.Count; i++)
            {
                var nodes = temp_tr[i].SelectNodes("td");
                List<HtmlNode> temp = new List<HtmlNode>();

                for (int j = 0; j < nodes.Count; j++)
                {
                    temp.Add(nodes[j]);
                }
                td_nodes.Add(temp);
            }

            for (int i = 0; i < td_nodes.Count; i++)
            {
                for (int j = 0; j < td_nodes[i].Count; j++)
                {
                    if (td_nodes[i][j].GetAttributeValue("class", string.Empty) == "closest_pair")
                    {
                        tempData.subjectName = td_nodes[i][j].SelectSingleNode("span").InnerText;
                        if (td_nodes[i][j].SelectNodes("a")[0].InnerText == null)
                            tempData.teacher = "-";
                        else
                            tempData.teacher = td_nodes[i][j].SelectNodes("a")[0].InnerText;
                        
                            

                        var found = Regex.Split(td_nodes[i][j - 1].InnerText, @"(\d)(\d{2}:\d{2})");

                        tempData.pairNum = found[1];
                        tempData.closestPairTime = DateTime.Parse(found[2]);
                        if (td_nodes[i - int.Parse(tempData.pairNum) + 1][j].GetAttributeValue("class", string.Empty) == "day_backlight" || td_nodes[i - int.Parse(tempData.pairNum) + 2][j].GetAttributeValue("class", string.Empty) == "day_backlight")
                            tempData.isToday = true;
                    }

                }
            }
        }
    }
}
