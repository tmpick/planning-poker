﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlanningPoker.Hub.Client.Abstractions;
using Xunit;

namespace PlanningPoker.FunctionalTests.Tests.Hubs
{
    public partial class PlanningPokerHubTests
    {
        [Fact]
        public async Task Show_WhenServerDoesNotExist_ReturnsError()
        {
            // Arrange
            var builder = CreateBuilder();

            // Act
            var exceptionRecord = await Record.ExceptionAsync(() => builder.HubClient.ShowVotes(Guid.NewGuid()));

            // Assert
            Assert.NotNull(exceptionRecord);
        }

        [Fact]
        public async Task Show_WhenNotEveryoneHasVoted_ReturnsError()
        {
            // Arrange
            var serverBuilder = CreateBuilder();
            serverBuilder.WithServer(out var serverId);
            var validVote = "1";
            var playerConnections = new List<IPlanningPokerHubClient>
            {
                CreateBuilder().WithPlayer(serverId, out var player1).HubClient,
                CreateBuilder().WithPlayer(serverId, out var player2).HubClient,
                CreateBuilder().WithPlayer(serverId, out var player3).HubClient
            };

            await playerConnections[0].Vote(serverId, player1.Id, validVote);
            await playerConnections[1].Vote(serverId, player2.Id, validVote);

            // Act
            var exceptionRecord = await Record.ExceptionAsync(() => playerConnections[2].ShowVotes(serverId));

            // Assert
            Assert.NotNull(exceptionRecord);
        }

        [Fact]
        public async Task Show_WhenEveryoneHasVoted_VotesAreShown()
        {
            // Arrange
            var serverBuilder = CreateBuilder();
            serverBuilder.WithServer(out var serverId);
            var validVote = "1";
            var isShown = false;
            var actualVotes = new List<string>();
            var playerConnections = new List<IPlanningPokerHubClient>
            {
                CreateBuilder().WithPlayer(serverId, out var player1).HubClient,
                CreateBuilder().WithPlayer(serverId, out var player2).HubClient
            };

            await playerConnections[0].Vote(serverId, player1.Id, validVote);
            await playerConnections[1].Vote(serverId, player2.Id, validVote);

            SemaphoreSlim awaitResponse = new SemaphoreSlim(0);
            playerConnections[1].OnSessionUpdated(viewModel =>
            {
                actualVotes = viewModel.CurrentSession.Votes.Values.ToList();
                isShown = viewModel.CurrentSession.IsShown;
                awaitResponse.Release();
            });

            // Act
            await playerConnections[1].ShowVotes(serverId);

            // Assert
            await awaitResponse.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(validVote, actualVotes[0]);
            Assert.Equal(validVote, actualVotes[1]);
            Assert.True(isShown);
        }
    }
}
