using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Net.Http.Headers;

namespace pair_notification_tgbot
{
    class DatabaseController
    {
        public SQLiteConnection connection;

        public DatabaseController()
        {
            connection = new SQLiteConnection("URI=file:data.db");
            connection.Open();
        }

        public bool ContainsSub(string group, long id)
        {
            SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM subscribers WHERE group_name = \"{group}\" AND chat_id = {id.ToString()}", connection);
            var reader = cmd.ExecuteReader();

            if (reader.Read())
                return true;
            else
                return false;
        }

        public void AddSub(string group, long id)
        {
            SQLiteCommand cmd = new SQLiteCommand($"INSERT INTO subscribers(group_name, chat_id) VALUES(\"{group}\", {id.ToString()})", connection);

            if (!ContainsSub(group, id))
                cmd.ExecuteNonQuery();

        }

        public void RemoveSub(string group, long id)
        {
            SQLiteCommand cmd = new SQLiteCommand($"DELETE FROM subscribers WHERE group_name = \"{group}\" AND chat_id = {id.ToString()}", connection);

            if (ContainsSub(group, id))
                cmd.ExecuteNonQuery();
        }

        public List<string> GetSubGroups()
        {
            List<string> groups = new List<string>();

            SQLiteCommand cmd = new SQLiteCommand("SELECT group_name FROM subscribers", connection);
            var reader = cmd.ExecuteReader();

            while(reader.Read())
            {
                if(!groups.Contains(reader.GetString(0)))
                    groups.Add(reader.GetString(0));
            }

            return groups;
        }


        public List<RowDataFromDB> GetAllData()
        {
            List<RowDataFromDB> list = new List<RowDataFromDB>();

            SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM subscribers", connection);
            var reader = cmd.ExecuteReader();

            while(reader.Read())
            {
                RowDataFromDB tempRow = new RowDataFromDB();
                tempRow.index = reader.GetInt64(0);
                tempRow.group = reader.GetString(1);
                tempRow.id = reader.GetInt64(2);

                list.Add(tempRow);
            }

            return list;
        }

        public List<string> GetGroupsById(long id)
        {
            List<string> groups = new List<string>();
            SQLiteCommand cmd = new SQLiteCommand($"SELECT group_name FROM subscribers WHERE chat_id = {id}", connection);

            var reader = cmd.ExecuteReader();
            while(reader.Read())
            {
                groups.Add(reader.GetString(0));
            }

            return groups;
        }
    }
}
