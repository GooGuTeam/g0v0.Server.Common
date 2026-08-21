// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using OsuMatchType = osu.Game.Online.Rooms.MatchType;
using OsuQueueMode = osu.Game.Online.Multiplayer.QueueMode;
using PostgreSqlRoomRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.RoomRepository;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class PostgreSqlRoomRepositoryTests
{
    private PostgreSqlDbContext _context = null!;
    private PostgreSqlRoomRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PostgreSqlDbContext(options);
        _repository = new PostgreSqlRoomRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public void RoomRepository_ShouldImplementIPostgreSqlRepository()
    {
        Assert.That(_repository, Is.InstanceOf<IPostgreSqlRepository>());
    }

    [Test]
    public async Task CreateRoom_NullMods_ProducesNonNullModsInPlaylistItem()
    {
        _context.Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        // Simulate a client submitting a playlist item with null mod collections.
        var room = CreateRoom();
        room.Playlist[0].RequiredMods = null!;
        room.Playlist[0].AllowedMods = null!;

        var created = await _repository.CreateRoom(room, 1);

        var item = created.Playlists.First().ToMultiplayerPlaylistItem();
        Assert.That(item.RequiredMods, Is.Not.Null);
        Assert.That(item.AllowedMods, Is.Not.Null);
    }

    [Test]
    public async Task CreateRoom_ShouldPersistRoomWithHostAndPlaylist()
    {
        _context.Users.Add(CreateUser(1, "host"));
        _context.Users.Add(CreateUser(2, "guest"));
        await _context.SaveChangesAsync();

        var created = await _repository.CreateRoom(CreateRoom(), 1, G0V0RoomCategory.Realtime, tournamentMode: true);

        Assert.That(created.Id, Is.GreaterThan(0));
        Assert.That(created.HostId, Is.EqualTo(1));
        Assert.That(created.Status, Is.EqualTo(RoomStatus.Idle));
        Assert.That(created.Type, Is.EqualTo(OsuMatchType.HeadToHead));
        Assert.That(created.Playlists, Has.Count.EqualTo(1));
        Assert.That(created.TournamentMode, Is.True);
        Assert.That(created.ParticipantCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetRoom_ShouldIncludePlaylistAndBeatmap()
    {
        _context.Users.Add(CreateUser(1, "host"));
        var beatmap = new Beatmap
        {
            Id = 100,
            Checksum = "abc123",
            DifficultyRating = 5.0,
        };
        _context.Beatmaps.Add(beatmap);
        await _context.SaveChangesAsync();

        var created = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.AddItemToPlaylist(CreatePlaylistItem(100), created.Id, 1);

        var fetched = await _repository.GetRoom(created.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Playlists, Has.Count.EqualTo(2));
        Assert.That(fetched.Playlists.Any(p => p.BeatmapId == 100), Is.True);
    }

    [Test]
    public async Task AddUserToRoom_WhenAlreadyPresent_IsIdempotent()
    {
        _context.Users.Add(CreateUser(1, "host"));
        _context.Users.Add(CreateUser(2, "guest"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.AddUserToRoom(room.Id, 1);
        await _repository.AddUserToRoom(room.Id, 2);
        await _repository.AddUserToRoom(room.Id, 2);

        // LegacyIO deduplicates active participation records, keeping only one per user.
        int activeCount = await _context.RoomParticipatedUsers
            .CountAsync(rpu => rpu.RoomId == room.Id && rpu.LeftAt == null);
        Assert.That(activeCount, Is.EqualTo(2));

        var fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.ParticipantCount, Is.EqualTo(2));
    }

    [Test]
    public async Task RemoveUserFromRoom_ShouldSetLeftAt_AndRejoinShouldReopen()
    {
        _context.Users.Add(CreateUser(1, "host"));
        _context.Users.Add(CreateUser(2, "guest"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.AddUserToRoom(room.Id, 1);
        await _repository.AddUserToRoom(room.Id, 2);
        await _repository.RemoveUserFromRoom(room.Id, 2);

        bool inRoom = await _repository.CheckUserInRoom(room.Id, 2);
        Assert.That(inRoom, Is.False);

        // Host remains, so the room is not ended.
        var fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.EndsAt, Is.Null);
        Assert.That(fetched.ParticipantCount, Is.EqualTo(1));

        await _repository.AddUserToRoom(room.Id, 2);
        bool rejoined = await _repository.CheckUserInRoom(room.Id, 2);
        Assert.That(rejoined, Is.True);

        // Leaving again must not fail even though a historical (LeftAt != null)
        // participation record exists from the first leave.
        Assert.DoesNotThrowAsync(async () => await _repository.RemoveUserFromRoom(room.Id, 2));
        Assert.That(await _repository.CheckUserInRoom(room.Id, 2), Is.False);
    }

    [Test]
    public async Task UpdateRoomHost_ShouldPersistHostChange()
    {
        _context.Users.Add(CreateUser(1, "host"));
        _context.Users.Add(CreateUser(2, "guest"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.UpdateRoomHost(room.Id, 2);

        var fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.HostId, Is.EqualTo(2));
    }

    [Test]
    public async Task AnyScoreTokenExistsFor_ShouldDetectTokens()
    {
        _context.Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        var item = await _repository.AddItemToPlaylist(
            new MultiplayerPlaylistItem
            {
                BeatmapID = 100,
                RulesetID = 0,
                RequiredMods = Array.Empty<osu.Game.Online.API.APIMod>(),
                AllowedMods = Array.Empty<osu.Game.Online.API.APIMod>(),
            },
            room.Id,
            1);

        // No score tokens yet.
        Assert.That(await _repository.AnyScoreTokenExistsFor(item.Id), Is.False);

        _context.ScoreTokens.Add(new ScoreToken
        {
            UserId = 1,
            BeatmapId = 100,
            RoomId = (int)room.Id,
            PlaylistItemId = (int)item.Id,
            ClientVersion = "test",
        });
        await _context.SaveChangesAsync();

        Assert.That(await _repository.AnyScoreTokenExistsFor(item.Id), Is.True);
    }

    [Test]
    public async Task UpdateRoomSettings_ShouldPersistSettings()
    {
        _context.Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        var newSettings = new MultiplayerRoomSettings
        {
            Name = "renamed",
            MatchType = OsuMatchType.TeamVersus,
            QueueMode = OsuQueueMode.AllPlayers,
            AutoSkip = true,
            AutoStartDuration = TimeSpan.FromSeconds(30),
            MaxParticipants = 8,
        };
        await _repository.UpdateRoomSettings(room.Id, newSettings);

        var fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.Name, Is.EqualTo("renamed"));
        Assert.That(fetched.Type, Is.EqualTo(OsuMatchType.TeamVersus));
        Assert.That(fetched.QueueMode, Is.EqualTo(OsuQueueMode.AllPlayers));
        Assert.That(fetched.AutoSkip, Is.True);
        Assert.That(fetched.MaxParticipants, Is.EqualTo(8));
    }

    [Test]
    public async Task UpdateRoomStatus_ShouldPersistPlayingState()
    {
        _context.Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.UpdateRoomStatus(room.Id, MultiplayerRoomState.Playing);

        var fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.Status, Is.EqualTo(RoomStatus.Playing));
    }

    [Test]
    public async Task SetRoomEndDate_ShouldPersistEndDate()
    {
        _context.Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        var endDate = DateTimeOffset.UtcNow.AddHours(1);
        await _repository.SetRoomEndDate(room.Id, endDate);

        var fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.EndsAt, Is.EqualTo(endDate));
    }

    [Test]
    public async Task EditItemToPlaylist_ShouldPersistChanges()
    {
        _context.Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        var item = await _repository.AddItemToPlaylist(CreatePlaylistItem(100), room.Id, 1);

        var editedItem = item.ToMultiplayerPlaylistItem();
        editedItem.BeatmapID = 200;
        editedItem.Freestyle = true;
        var edited = await _repository.EditItemToPlaylist(editedItem, room.Id, 1);

        Assert.That(edited.BeatmapId, Is.EqualTo(200));
        Assert.That(edited.Freestyle, Is.True);
    }

    [Test]
    public async Task RemoveItemToPlaylist_ShouldDeleteItem()
    {
        _context.Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        var item = await _repository.AddItemToPlaylist(CreatePlaylistItem(100), room.Id, 1);

        await _repository.RemoveItemToPlaylist(item.Id, room.Id, 1);

        bool exists = await _context.Playlists.AnyAsync(p => p.Id == item.Id);
        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task DeleteRoom_RemovesRoomAndDependents()
    {
        _context.Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        var room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.AddUserToRoom(room.Id, 1);
        await _repository.AddItemToPlaylist(
            new MultiplayerPlaylistItem
            {
                BeatmapID = 100,
                RulesetID = 0,
                RequiredMods = Array.Empty<osu.Game.Online.API.APIMod>(),
                AllowedMods = Array.Empty<osu.Game.Online.API.APIMod>(),
            },
            room.Id,
            1);

        await _repository.DeleteRoom(room.Id);

        Assert.That(await _repository.GetRoom(room.Id), Is.Null);
        Assert.That(await _context.Playlists.CountAsync(p => p.RoomId == room.Id), Is.EqualTo(0));
        Assert.That(await _context.RoomParticipatedUsers.CountAsync(rpu => rpu.RoomId == room.Id), Is.EqualTo(0));
    }

    private static User CreateUser(int id, string username) => new()
    {
        Id = id,
        Username = username,
    };

    private static MultiplayerRoom CreateRoom() => new(0)
    {
        Settings = new MultiplayerRoomSettings
        {
            Name = "test room",
            MatchType = OsuMatchType.HeadToHead,
            QueueMode = OsuQueueMode.HostOnly,
        },
        Playlist = new List<MultiplayerPlaylistItem>
        {
            CreatePlaylistItem(100),
        },
    };

    private static MultiplayerPlaylistItem CreatePlaylistItem(int beatmapId) => new()
    {
        ID = 0,
        OwnerID = 1,
        BeatmapID = beatmapId,
        RulesetID = 0,
        RequiredMods = Array.Empty<APIMod>(),
        AllowedMods = Array.Empty<APIMod>(),
        Freestyle = false,
        Expired = false,
        PlaylistOrder = 0,
        WinCondition = osu.Game.Online.Multiplayer.WinCondition.Score,
    };
}