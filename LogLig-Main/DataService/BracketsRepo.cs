using AppModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataService
{
    public class BracketsRepo : BaseRepo
    {
        private GamesRepo _gameRepo;

        protected GamesRepo gameRepo
        {
            get
            {
                if (_gameRepo == null)
                {
                    _gameRepo = new GamesRepo(db);
                }
                return _gameRepo;
            }
        }
        public BracketsRepo()
            : base()
        {
        }

        public BracketsRepo(DataEntities db, GamesRepo gRepo): base(db)
        {
            _gameRepo = gRepo;
        }

        internal void SaveBrackets(List<PlayoffBracket> brackets)
        {
            db.PlayoffBrackets.AddRange(brackets);
            db.SaveChanges();
        }

        internal void DeleteAllBracketsAndChildrenBracketsForGroup(Group group)
        {
            List<PlayoffBracket> allBrackets = new List<PlayoffBracket>();
            GetChildrenBracketsRecursively(group.PlayoffBrackets, allBrackets);
            allBrackets = allBrackets.Distinct().ToList();
            allBrackets.ForEach(b =>
            {
                b.ParentBracket1Id = null;
                b.ParentBracket2Id = null;
                db.GamesCycles.RemoveRange(b.GamesCycles);
            });
            int n = db.SaveChanges();
            db.PlayoffBrackets.RemoveRange(allBrackets);
            n = db.SaveChanges();
        }

        private void GetChildrenBracketsRecursively(ICollection<PlayoffBracket> brackets, List<PlayoffBracket> allBrackets)
        {
            foreach (var bracket in brackets)
            {
                allBrackets.Add(bracket);
                GetChildrenBracketsRecursively(bracket.PlayoffBrackets1, allBrackets);
                GetChildrenBracketsRecursively(bracket.PlayoffBrackets11, allBrackets);
            }
        }


        internal void GameEndedEvent(GamesCycle gc)
        {
            PlayoffBracket bracket = gc.PlayoffBracket;
            if (bracket != null && bracket.GamesCycles.All(g => g.GameStatus == GameStatus.Ended))
            {
                var goldenGame = bracket.GamesCycles.FirstOrDefault(g => g.GameSets.Any(s => s.IsGoldenSet == true));

                if (goldenGame != null)
                {
                    gameRepo.ResetGame(goldenGame, true);

                    var goldenSet = goldenGame.GameSets.FirstOrDefault(s => s.IsGoldenSet == true);
                    if (goldenSet.HomeTeamScore > goldenSet.GuestTeamScore)
                    {
                        bracket.WinnerId = goldenGame.HomeTeamId;
                        bracket.LoserId = goldenGame.GuestTeamId;
                    }
                    else
                    {
                        bracket.WinnerId = goldenGame.GuestTeamId;
                        bracket.LoserId = goldenGame.HomeTeamId;
                    }
                }
                else
                {
                    int t1score = bracket.GamesCycles.Where(g => g.HomeTeamId == bracket.FirstTeam?.TeamId && g.HomeTeamScore > g.GuestTeamScore)
                        .Concat(bracket.GamesCycles.Where(g => g.GuestTeamId == bracket.FirstTeam?.TeamId && g.HomeTeamScore < g.GuestTeamScore)).Count();

                    int t2score = bracket.GamesCycles.Where(g => g.HomeTeamId == bracket.SecondTeam?.TeamId && g.HomeTeamScore > g.GuestTeamScore)
                        .Concat(bracket.GamesCycles.Where(g => g.GuestTeamId == bracket.SecondTeam?.TeamId && g.HomeTeamScore < g.GuestTeamScore)).Count();

                    if (t1score > t2score)
                    {
                        bracket.WinnerId = bracket.FirstTeam.TeamId;
                        bracket.LoserId = bracket.SecondTeam.TeamId;
                    }
                    else if (t1score < t2score)
                    {
                        bracket.WinnerId = bracket.SecondTeam.TeamId;
                        bracket.LoserId = bracket.FirstTeam.TeamId;
                    }
                }


                foreach (var child in bracket.ChildrenSide1)
                {
                    if (child.Type == (int)PlayoffBracketType.Winner)
                    {
                        child.Team1Id = bracket.WinnerId;
                    }
                    else if (child.Type == (int)PlayoffBracketType.Loseer)
                    {
                        child.Team1Id = bracket.LoserId;
                    }

                    for (int i = 0; i < child.GamesCycles.Count; i++)
                    {
                        GamesCycle game = child.GamesCycles.ElementAt(i);
                        gameRepo.ResetGame(game, true);
                        if (i % 2 == 0)
                        {
                            game.HomeTeamId = child.Team1Id;
                            game.GuestTeamId = child.Team2Id;
                        }
                        else
                        {
                            game.HomeTeamId = child.Team2Id;
                            game.GuestTeamId = child.Team1Id;
                        }
                    }
                }


                foreach (var child in bracket.ChildrenSide2)
                {
                    if (child.Type == (int)PlayoffBracketType.Winner)
                    {
                        child.Team2Id = bracket.WinnerId;
                    }
                    else if (child.Type == (int)PlayoffBracketType.Loseer)
                    {
                        child.Team2Id = bracket.LoserId;
                    }
                    for (int i = 0; i < child.GamesCycles.Count; i++)
                    {
                        GamesCycle game = child.GamesCycles.ElementAt(i);
                        gameRepo.ResetGame(game, true);
                        if (i % 2 == 0)
                        {
                            game.HomeTeamId = child.Team1Id;
                            game.GuestTeamId = child.Team2Id;
                        }
                        else
                        {
                            game.HomeTeamId = child.Team2Id;
                            game.GuestTeamId = child.Team1Id;
                        }
                    }
                }
            }
            Save();
        }

        public List<Team> GetAllPotintialTeams(int id, int index)
        {
            List<Team> list = new List<Team>();
            var bracket = GetById(id);
            FindTeamsRecursively(bracket, list, index);
            return list.Distinct().OrderBy(t => t.Title).ToList();
        }

        private void FindTeamsRecursively(PlayoffBracket bracket, List<Team> list, int index)
        {
            if (bracket.Type == (int)PlayoffBracketType.Root)
            {
                if (bracket.Team1Id != null)
                {
                    list.Add(bracket.FirstTeam);
                }

                if (bracket.Team2Id != null)
                {
                    list.Add(bracket.SecondTeam);
                }
            }
            else
            {
                PlayoffBracket parent;
                if (index == 1)
                {
                    parent = bracket.Parent1;
                }
                else
                {
                    parent = bracket.Parent2;
                }

                if (parent.Team1Id != null)
                {
                    list.Add(parent.FirstTeam);
                }
                else
                {
                    FindTeamsRecursively(parent, list, 1);
                }

                if (parent.Team2Id != null)
                {
                    list.Add(parent.SecondTeam);
                }
                else
                {
                    FindTeamsRecursively(parent, list, 2);
                }
            }
        }

        public PlayoffBracket GetById(int id)
        {
            return db.PlayoffBrackets.Find(id);
        }
    }
}
