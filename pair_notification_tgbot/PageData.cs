using System;
using System.Collections.Generic;
using System.Text;

namespace pair_notification_tgbot
{
    class PageData
    {
        public DateTime closestPairTime;
        public string subjectName;
        public string teacher;
        public string pairNum;
        public bool isToday;
        public bool groupExist;

        

        public PageData()
        {
            closestPairTime = new DateTime();
            subjectName = string.Empty;
            teacher = string.Empty;
            pairNum = string.Empty;
            isToday = false;
            groupExist = false;
        }
    }
}
