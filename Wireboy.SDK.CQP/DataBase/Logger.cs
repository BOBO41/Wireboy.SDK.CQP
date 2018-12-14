﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wireboy.SDK.CQP.Events;

namespace Wireboy.SDK.CQP
{
    public class Logger : ILogger
    {
        MySqlConnection mySqlConnection = new MySqlConnection("server=39.105.116.163;user id=wireboy;password=admin@2018;persistsecurityinfo=True;database=CoreWebDB;port=3306");
        object obj = new object();
        bool isBuzy = false;
        Dictionary<int, List<string>> sqlDic = new Dictionary<int, List<string>>();
        int index = 0;
        public Logger()
        {
            sqlDic.Add(0, new List<string>());
            sqlDic.Add(1, new List<string>());
        }
        public void GroupMsg(GroupMsgContext groupMsgContext)
        {
            string sql = string.Format("insert into QQGroupLog(Msg,QQ,Time) values('{0}','{1}','{2}');", groupMsgContext.msg, groupMsgContext.fromQQ, DateTime.Now.ToString("yyyyMMddHHmmss"));

            if (!isBuzy)
            {
                lock (obj)
                {
                    if (!isBuzy)
                    {
                        mySqlConnection.Open();
                        do
                        {
                            int curIndex = index;
                            //设置缓存集合位置
                            index = curIndex == 1 ? 0 : 1;
                            isBuzy = true;
                            sqlDic[curIndex].Add(sql);
                            //拼接集合中sql
                            string allSql = "";
                            foreach (string csql in sqlDic[curIndex])
                            {
                                allSql = string.Format("{0}{1}", allSql, csql);
                            }
                            sqlDic[curIndex].Clear();
                            //将数据插入数据库
                            MySqlCommand mySqlCommand = new MySqlCommand(allSql, mySqlConnection);
                            mySqlCommand.ExecuteNonQuery();
                            //等客户要求，付费移除
                            Thread.Sleep(200);
                        }
                        while (sqlDic[index].Count > 0);
                        mySqlConnection.Close();
                        isBuzy = false;
                    }
                    else
                    {
                        sqlDic[index].Add(sql);
                    }
                }
            }else
            {
                sqlDic[index].Add(sql);
            }

        }

        private Task ExcuteCmd(string sql)
        {
            return Task.Run(() =>
            {
                lock (obj)
                {
                    MySqlCommand mySqlCommand = new MySqlCommand(sql, mySqlConnection);
                    if (mySqlConnection.State == System.Data.ConnectionState.Connecting)
                    mySqlCommand.BeginExecuteNonQuery();
                }
            });
        }
        ~Logger()
        {
            if (mySqlConnection.State != System.Data.ConnectionState.Closed)
                mySqlConnection.Close();
        }
    }
}
