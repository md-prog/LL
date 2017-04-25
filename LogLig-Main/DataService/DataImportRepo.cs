using AppModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataService
{
    public class DataImportRepo : BaseRepo
    {


        

        public void ParseData()
        {
            var data = ParseCSV(Resources.TeamPlayerImportTemplate);
            foreach (var line in data)
            {
                var name = line[0].Trim();
                var idNum = line[1].Trim();
                string realIdNum = null;
                if (idNum.Length == 8)
                {
                    realIdNum = "0" + idNum;
                }
                else if (idNum.Length == 9)
                {
                    realIdNum = idNum;
                }
                /*else
                {
                    var warning = true;
                }*/
                var email = line[2].Trim();
                var birthStr = Convert.ToDateTime(line[3].Trim());
                var city = line[6].Trim();
                var teamId = Convert.ToInt32(line[8].Trim());

                User user = db.Users.FirstOrDefault(u => u.IdentNum == realIdNum || u.IdentNum == idNum || u.Email == email);

                if (user == null)
                {
                    user = new User
                    {
                        FullName = name,
                        IdentNum = realIdNum,
                        Email = email,
                        BirthDay = birthStr,
                        City = city,
                        Password = Protector.Encrypt(realIdNum),
                        TypeId = 4,
                        GenderId = 0,
                        IsActive = true,
                        Image = idNum + ".jpg"
                    };
                    user.TeamsPlayers.Add(new TeamsPlayer
                    {
                        TeamId = teamId,
                        ShirtNum = 0,
                        IsActive = true
                    });
                    var us = db.Users.Add(user);
                    int changes = db.SaveChanges();
                }
                //break;
            }
        }

        public List<string[]> ParseCSV(string csv)
        {
            List<string[]> parsedData = new List<string[]>();
            try
            {
                csv = csv.Replace("\r", "");
                var lines = csv.Split('\n');
                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        parsedData.Add(line.Split(','));
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            return parsedData;
        }

    }  
}
