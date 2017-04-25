using System;
using System.Collections.Generic;
using System.Linq;

public static class RoundRobin
{
    static public List<Tuple<int, int>> GetMatches(List<int> listTeam)
    {
        if (listTeam.Count % 2 != 0)
        {
            listTeam.Add(0);
        }
        int numTeams = listTeam.Count;
        int numDays = (numTeams - 1);
        int halfSize = numTeams / 2;

        List<int> teams = new List<int>();

        teams.AddRange(listTeam.Skip(halfSize).Take(halfSize));
        teams.AddRange(listTeam.Skip(1).Take(halfSize - 1).ToArray().Reverse());

        int teamsSize = teams.Count;

        var resList = new List<Tuple<int, int>>();

        for (int day = 0; day < numDays; day++)
        {
            int teamIdx = day % teamsSize;
            if (teams[teamIdx] != 0 && teams[0] != 0)
            {
                if (day % 2 == 0)
                {
                    resList.Add(Tuple.Create(listTeam[0], teams[teamIdx]));
                }
                else
                {
                    resList.Add(Tuple.Create(teams[teamIdx], listTeam[0]));
                }
            }

            for (int idx = 1; idx < halfSize; idx++)
            {
                int firstTeam = (day + idx) % teamsSize;
                int secondTeam = (day + teamsSize - idx) % teamsSize;
                if (teams[firstTeam] != 0 && teams[secondTeam] != 0)
                {
                    resList.Add(Tuple.Create(teams[firstTeam], teams[secondTeam]));
                }
            }
        }
        listTeam.RemoveAll(t => t == 0);
        return resList;
    }
}