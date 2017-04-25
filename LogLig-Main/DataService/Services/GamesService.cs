namespace DataService.Services
{
    public class GamesService
    {
        readonly GamesRepo _gamesRepo = new GamesRepo();

        public void SetTechnicalWinForGame(int gameId, int teamId)
        {
            var gc = _gamesRepo.GetGameCycleById(gameId);
            var gameAlias = gc.Stage?.League?.Union?.Section?.Alias;

            switch (gameAlias)
            {
                case GamesAlias.WaterPolo:
                    _gamesRepo.WaterPoloTechnicalWin(gameId, teamId);
                    break;
                case GamesAlias.BasketBall:
                    _gamesRepo.BasketBallTechnicalWin(gameId, teamId);
                    break;
                default:
                    _gamesRepo.TechnicalWin(gameId, teamId);
                    break;
            }

            _gamesRepo.UpdateGameScore(gameId);
        }
    }
}
