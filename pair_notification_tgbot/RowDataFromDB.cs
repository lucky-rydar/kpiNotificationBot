using System;
using System.Collections.Generic;
using System.Text;

namespace pair_notification_tgbot
{
    class RowDataFromDB
    {
        public long index;
        public string group;
        public long id;

        public RowDataFromDB()
        {
            index = 0;
            group = string.Empty;
            id = 0;
        }
    }
}
