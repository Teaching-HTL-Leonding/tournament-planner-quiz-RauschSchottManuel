using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TournamentPlanner.Data
{
    public enum PlayerNumber { Player1 = 1, Player2 = 2 };

    public class TournamentPlannerDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Match> Matches { get; set; }

        public TournamentPlannerDbContext(DbContextOptions<TournamentPlannerDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Match>()
                .HasOne(m => m.Player1)
                .WithMany()
                .HasForeignKey(m => m.Player1Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Player2)
                .WithMany()
                .HasForeignKey(m => m.Player2Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Winner)
                .WithMany()
                .HasForeignKey(m => m.WinnerId)
                .OnDelete(DeleteBehavior.NoAction);

        }

        /// <summary>
        /// Adds a new player to the player table
        /// </summary>
        /// <param name="newPlayer">Player to add</param>
        /// <returns>Player after it has been added to the DB</returns>
        public async Task<Player> AddPlayer(Player newPlayer)
        {
            Players.Add(newPlayer);
            await SaveChangesAsync();
            return newPlayer;
        }

        /// <summary>
        /// Adds a match between two players
        /// </summary>
        /// <param name="player1Id">ID of player 1</param>
        /// <param name="player2Id">ID of player 2</param>
        /// <param name="round">Number of the round</param>
        /// <returns>Generated match after it has been added to the DB</returns>
        public async Task<Match> AddMatch(int player1Id, int player2Id, int round)
        {
            var player1 = Players.Where(p => p.ID == player1Id).FirstOrDefault();
            var player2 = Players.Where(p => p.ID == player2Id).FirstOrDefault();

            var m = new Match { Player1 = player1, Player2 = player2, Round = round };
            Matches.Add(m);
            await SaveChangesAsync();
            return m;
        }

        /// <summary>
        /// Set winner of an existing game
        /// </summary>
        /// <param name="matchId">ID of the match to update</param>
        /// <param name="player">Player who has won the match</param>
        /// <returns>Match after it has been updated in the DB</returns>
        public async Task<Match> SetWinner(int matchId, PlayerNumber player)
        {
            var m = Matches.Where(m => m.ID == matchId).FirstOrDefault();
            m.Winner = player == PlayerNumber.Player1 ? m.Player1 : m.Player2;
            await SaveChangesAsync();
            return m;
        }

        /// <summary>
        /// Get a list of all matches that do not have a winner yet
        /// </summary>
        /// <returns>List of all found matches</returns>
        public async Task<IList<Match>> GetIncompleteMatches()
        {
            return await Matches.Where(m => m.Winner == null).ToListAsync();
        }

        /// <summary>
        /// Delete everything (matches, players)
        /// </summary>
        public async Task DeleteEverything()
        {
            foreach (var item in Players)
            {
                Players.Remove(item);
            }
            foreach (var item in Matches)
            {
                Matches.Remove(item);
            }
            await SaveChangesAsync();
        }

        /// <summary>
        /// Get a list of all players whose name contains <paramref name="playerFilter"/>
        /// </summary>
        /// <param name="playerFilter">Player filter. If null, all players must be returned</param>
        /// <returns>List of all found players</returns>
        public async Task<IList<Player>> GetFilteredPlayers(string playerFilter = null)
        {
            if (playerFilter is not null)
                return await Players.Where(p => p.Name.Contains(playerFilter)).ToListAsync();
            else
                return await Players.ToListAsync();
        }

        /// <summary>
        /// Generate match records for the next round
        /// </summary>
        /// <exception cref="InvalidOperationException">Error while generating match records</exception>
        public async Task GenerateMatchesForNextRound()
        {
            await CheckForInvalidOperations();

            var roundSetup = await SetupGenerateRecords();
            
            for(int i = 0; i < roundSetup.roundsCnt; i++)
            {
                var player1 = GetRandomPlayer(roundSetup.players);
                var player2 = GetRandomPlayer(roundSetup.players);

                var newMatch = new Match { Player1 = player1, Player2 = player2, Round = roundSetup.roundNr };
                Matches.Add(newMatch);
            }

        }

        private record GenerateRecordSetupDTO(int roundNr, int roundsCnt, List<Player> players);

        private async Task<GenerateRecordSetupDTO> SetupGenerateRecords()
        {
            var roundNr = 0;
            var roundsCnt = 0;
            List<Player> players = new();
            switch (Matches.Count())
            {
                case 0:
                    roundNr = 1;
                    roundsCnt = 16;
                    players = await Players.ToListAsync();
                    break;
                case 16:
                    roundNr = 2;
                    roundsCnt = 8;
                    break;
                case 24:
                    roundNr = 3;
                    roundsCnt = 4;
                    break;
                case 28:
                    roundNr = 4;
                    roundsCnt = 2;
                    break;
                case 30:
                    roundNr = 5;
                    roundsCnt = 1;
                    break;
                default:
                    throw new InvalidOperationException("Error regarding the stored matches occured");
            }

            if (roundNr != 1)
            {
                players = await Matches.Where(m => m.Round == roundNr - 1)
                    .Select(m => m.Winner)
                    .ToListAsync();
            }

            return new GenerateRecordSetupDTO(roundNr, roundsCnt, players);
        }

        private async Task CheckForInvalidOperations()
        {
            if ((await GetIncompleteMatches()).Count > 0) throw new InvalidOperationException("Some matches are not finished yet");
            if (Players.Count() != 32) throw new InvalidOperationException("There are less players than needed");
        }

        private Player GetRandomPlayer(List<Player> players)
        {
            var r = new Random();
            int randomNum = r.Next(players.Count);
            var player = players[randomNum];
            players.Remove(player);
            return player;
        }
    }
}
