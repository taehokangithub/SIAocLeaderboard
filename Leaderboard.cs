using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SI.AOC.Leaderboard
{
    class User
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public int LocalScore { get; set; }
        public int Stars { get; set; } = 0;
    }

    class UserSolveRecord
    {
        public User User { get; }
        public long Timestamp { get; }
        public UserSolveRecord(User user, long time)
        {
            User = user;
            Timestamp = time;
        }
        public string GetTimeString()
        {
            DateTimeOffset offset = DateTimeOffset.FromUnixTimeSeconds(Timestamp);
            return offset.UtcDateTime.ToString("dd/MM/yyyy HH:mm:ss");
        }

        public string GetTimeDiffString(long otherTime)
        {
            DateTimeOffset offset1 = DateTimeOffset.FromUnixTimeSeconds(Timestamp);
            DateTimeOffset offset2 = DateTimeOffset.FromUnixTimeSeconds(otherTime);

            int minutes = (int) offset1.Subtract(offset2).TotalMinutes;
            
            return minutes.ToString().PadLeft(5);
        }
    }
    class Day
    {
        public List<List<UserSolveRecord>> Records { get; set; }
        bool sorted = false;

        public Day()
        {
            Records = new List<List<UserSolveRecord>>();

            Records.Add(new List<UserSolveRecord>()); // Part 1
            Records.Add(new List<UserSolveRecord>()); // Part 2
        }

        public void AddRecord(User user, JObject dayRecord)
        {
            foreach(var partRecord in dayRecord)
            {

                int part = int.Parse(partRecord.Key);
                long time = partRecord.Value["get_star_ts"].ToObject<long>();

                UserSolveRecord userSolveRecord = new UserSolveRecord(user, time);
                user.Stars ++;
                Records[part - 1].Add(userSolveRecord);
            }
            sorted = false;
        }

        public void Sort()
        {
            if (!sorted)
            {
                foreach(var list in Records)
                {
                    list.Sort((a, b) => (a.Timestamp > b.Timestamp) ? 1 : -1);
                }
                sorted = true;
            }

        }
    }    
    class Leaderboard
    {
        Dictionary<int, User> m_users = new Dictionary<int, User>();
        Dictionary<int, Day> m_days = new Dictionary<int, Day>();

        public void AddUserJObject(JObject member)
        {
            User user = new User();
            user.ID = member["id"].ToObject<int>();
            user.LocalScore = member["local_score"].ToObject<int>();
            user.Name = member["name"].ToObject<string>();

            if (user.Name == null)
            {
                user.Name = "(null)";
            }

            if (user.Name.Length >= 20)
            {
                string[] split = user.Name.Split(' ');
                if (split.Length > 2)
                {
                    user.Name = split[0] + " " + split[split.Length - 1];
                }
            }
            user.Name = user.Name.Substring(0, Math.Min(user.Name.Length, 20));

            m_users[user.ID] = user;

            JObject completionData = member["completion_day_level"] as JObject;

            foreach (JValue dayProperty in completionData.Properties().Select(p => p.Name))
            {
                Day day = GetDayOrCreate(dayProperty.ToObject<int>());

                JObject dayRecord = completionData[dayProperty.ToString()] as JObject;

                day.AddRecord(user, dayRecord);
            }
        }

        public string BuildReport(int reportYear)
        {
            var getLinkColor = (int year) => year == reportYear ? "#99ff99" : "009900";

            string response = @"
                <head>
                <meta charset='utf-8'/>
                <title>SI Leaderboard Daily Record - Advend of Code 2021</title>
                <link href='//fonts.googleapis.com/css?family=Source+Code+Pro:300&subset=latin,latin-ext' rel='stylesheet' type='text/css'/>
                <link rel='shortcut icon' href='/favicon.png'/>
                <style>
                a {
                    text-decoration: none;
                    color: #009900;
                }
                a:hover, a:focus {
                    color: #99ff99;
                }
                </style>
                </head>	<body>

                <table border=0 style = 'width:100%; padding:30px; color:#cccccc; font-family:""Source Code Pro"", monospace;
                                background:#0f0f23; font-size: 12pt; min-width: 60em;'>

                <tr> 
                    <td width='20%' td style='text-align:left'> 
                            <a href='" + FunctionMain.GetUri() + @"' target='_blank'> [Go To Official Page] </a></td> 
                    <td width='30%'> &nbsp; 
                            <a href='/api/aoc?year=2022' style='color:" + getLinkColor(2022) + @";'> [2022] </a>
                            <a href='/api/aoc?year=2021' style='color:" + getLinkColor(2021) + @";'> [2021] </a>
                            <a href='/api/aoc?year=2020' style='color:" + getLinkColor(2020) + @";'> [2020] </a>
                            <a href='/api/aoc?year=2019' style='color:" + getLinkColor(2019) + @";'> [2019] </a>
                    </td>
                    <td width='20%'> &nbsp; </td> 
                    <td width='30%' style='color:#A0A0A0; font-size: 9pt'> 
                            Table refreshes every " + Configuration.RefreshMinutes + @" minutes <br> Last Refreshed : " + FunctionMain.LastUpdateTimesPerYear[reportYear] + @" </td>
                </tr>
            ";

            foreach(var item in m_days.OrderBy(d => d.Key).Reverse())
            {
                Day day = item.Value;
                day.Sort();

                response += "<tr> <td style='color:#99EE99'>";
                response += $"<br><h3>DAY {item.Key} </h3> </td> <td> &nbsp; </td> <td> &nbsp; </td> <td> &nbsp; </td> </tr>";

                for (int i = 0; i < day.Records[0].Count; i ++)
                {
                    UserSolveRecord u1 = day.Records[0]?[i];

                    response += "<tr> <td style='text-align:right'> " + u1.User.Name + "</td> <td style='text-align:center'> " + u1.GetTimeString() + "</td>";

                    if (day.Records[1].Count > i)
                    {
                        UserSolveRecord u2 = day.Records[1]?[i];
                        response += "<td style='text-align:right'> " + u2.User.Name + "</td> <td style='text-align:center'> " + u2.GetTimeString() 
                            + "</td> </tr>";
                    }
                    else
                    {
                        response += "<td> &nbsp; </td> <td> &nbsp; </td> </tr>";
                    }

                }
            }

            response += "<tr> <td style='color:#99EE99; text-align:left'>";
            response += $"<br><h3>Local Score </h3> </td> <td> &nbsp; </td> <td> &nbsp; </td> <td> &nbsp; </td> </tr>";
            var userList = m_users.Values.ToList();
            userList.Sort((a, b) => b.LocalScore - a.LocalScore);

            int rank = 0;
            foreach(User user in userList)
            {
                if (user.LocalScore == 0)
                {
                    break;
                }
                rank ++;
                response += $"<tr> <td style='text-align:right'> <span style='color:#66DD77'> ({rank}) </span> {user.Name}" 
                            + $"</td> <td style='text-align:center'> {user.LocalScore}" 
                            + $"<span style='color:#898989'> ({user.Stars} stars) </span> </td>"
                            + "<td> </td> <td> </td> </tr>";
            }
            
            response += "</table> </body> </html>";
            return response;
        }

        private Day GetDayOrCreate(int day)
        {
            Day ret;
            if (!m_days.TryGetValue(day, out ret))
            {
                ret = new Day();
                m_days[day] = ret;
            }
            return ret;
        }
    }	
}