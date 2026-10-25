// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using MySqlDbContext = g0v0.Server.Common.Database.MySQL.MysqlDbContext;
using MySqlRoomRepository = g0v0.Server.Common.Database.MySQL.Repository.RoomRepository;
using OsuMatchType = osu.Game.Online.Rooms.MatchType;
using OsuQueueMode = osu.Game.Online.Multiplayer.QueueMode;
using PostgreSqlDbContext = g0v0.Server.Common.Database.PostgreSQL.PostgreSqlDbContext;
using PostgreSqlRoomRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.RoomRepository;
using Room = g0v0.Server.Common.Database.Models.Room;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Runs the <see cref="IRoomRepository"/> contract against both database backends.
/// </summary>
[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class RoomRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IRoomRepository _repository = null!;
    private Func<long, int, Task<bool>> _checkUserInRoom = null!;

    private DbSet<User> Users => _context.Set<User>();

    private DbSet<Beatmap> Beatmaps => _context.Set<Beatmap>();

    private DbSet<Playlist> Playlists => _context.Set<Playlist>();

    private DbSet<RoomParticipatedUser> RoomParticipatedUsers => _context.Set<RoomParticipatedUser>();

    private DbSet<ScoreToken> ScoreTokens => _context.Set<ScoreToken>();

    [SetUp]
    public void SetUp()
    {
        _context = RepositoryTestContexts.Create(backend);
        if (backend == RepositoryBackend.MySql)
        {
            MySqlRoomRepository repository = new((MySqlDbContext)_context);
            _repository = repository;
            _checkUserInRoom = repository.CheckUserInRoom;
        }
        else
        {
            PostgreSqlRoomRepository repository = new((PostgreSqlDbContext)_context);
            _repository = repository;
            _checkUserInRoom = repository.CheckUserInRoom;
        }
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task CreateRoom_ShouldPersistRoomWithHostAndPlaylist()
    {
        Users.Add(CreateUser(1, "host"));
        Users.Add(CreateUser(2, "guest"));
        await _context.SaveChangesAsync();

        // The legacy MySQL schema has no tournament_mode column, so the flag is ignored there.
        bool tournamentMode = backend == RepositoryBackend.PostgreSql;
        Room created = await _repository.CreateRoom(CreateRoom(), 1, G0V0RoomCategory.Realtime, tournamentMode);

        Assert.That(created.Id, Is.GreaterThan(0));
        Assert.That(created.HostId, Is.EqualTo(1));
        Assert.That(created.Status, Is.EqualTo(RoomStatus.Idle));
        Assert.That(created.Type, Is.EqualTo(OsuMatchType.HeadToHead));
        Assert.That(created.Playlists, Has.Count.EqualTo(1));
        Assert.That(created.TournamentMode, Is.EqualTo(tournamentMode));
        Assert.That(created.ParticipantCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetRoom_ShouldIncludePlaylistAndBeatmap()
    {
        Users.Add(CreateUser(1, "host"));
        Beatmap beatmap = new()
        {
            Id = 100,
            Checksum = "abc123",
            DifficultyRating = 5.0,
        };
        Beatmaps.Add(beatmap);
        await _context.SaveChangesAsync();

        Room created = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.AddItemToPlaylist(CreatePlaylistItem(100), created.Id, 1);

        Room? fetched = await _repository.GetRoom(created.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Playlists, Has.Count.EqualTo(2));
        Assert.That(fetched.Playlists.Any(p => p.BeatmapId == 100), Is.True);
    }

    [Test]
    public async Task AddUserToRoom_WhenAlreadyPresent_IsIdempotent()
    {
        Users.Add(CreateUser(1, "host"));
        Users.Add(CreateUser(2, "guest"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.AddUserToRoom(room.Id, 1);
        await _repository.AddUserToRoom(room.Id, 2);
        await _repository.AddUserToRoom(room.Id, 2);

        // LegacyIO deduplicates active participation records, keeping only one per user.
        int activeCount = await RoomParticipatedUsers
            .CountAsync(rpu => rpu.RoomId == room.Id && rpu.LeftAt == null);
        Assert.That(activeCount, Is.EqualTo(2));

        Room? fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.ParticipantCount, Is.EqualTo(2));
    }

    [Test]
    public async Task RemoveUserFromRoom_ShouldSetLeftAt_AndRejoinShouldReopen()
    {
        Users.Add(CreateUser(1, "host"));
        Users.Add(CreateUser(2, "guest"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.AddUserToRoom(room.Id, 1);
        await _repository.AddUserToRoom(room.Id, 2);
        await _repository.RemoveUserFromRoom(room.Id, 2);

        bool inRoom = await _checkUserInRoom(room.Id, 2);
        Assert.That(inRoom, Is.False);

        // Host remains, so the room is not ended.
        Room? fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.EndsAt, Is.Null);
        Assert.That(fetched.ParticipantCount, Is.EqualTo(1));

        await _repository.AddUserToRoom(room.Id, 2);
        bool rejoined = await _checkUserInRoom(room.Id, 2);
        Assert.That(rejoined, Is.True);

        // Leaving again must not fail even though a historical (LeftAt != null)
        // participation record exists from the first leave.
        Assert.DoesNotThrowAsync(async () => await _repository.RemoveUserFromRoom(room.Id, 2));
        Assert.That(await _checkUserInRoom(room.Id, 2), Is.False);
    }

    [Test]
    public async Task UpdateRoomHost_ShouldPersistHostChange()
    {
        Users.Add(CreateUser(1, "host"));
        Users.Add(CreateUser(2, "guest"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.UpdateRoomHost(room.Id, 2);

        Room? fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.HostId, Is.EqualTo(2));
    }

    [Test]
    public async Task AnyScoreTokenExistsFor_ShouldDetectTokens()
    {
        Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        Playlist item = await _repository.AddItemToPlaylist(
            new MultiplayerPlaylistItem
            {
                BeatmapID = 100,
                RulesetID = 0,
                RequiredMods = [],
                AllowedMods = [],
            },
            room.Id,
            1);

        // No score tokens yet.
        Assert.That(await _repository.AnyScoreTokenExistsFor(item.Id), Is.False);

        ScoreTokens.Add(new ScoreToken
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
        Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        MultiplayerRoomSettings newSettings = new()
        {
            Name = "renamed",
            MatchType = OsuMatchType.TeamVersus,
            QueueMode = OsuQueueMode.AllPlayers,
            AutoSkip = true,
            AutoStartDuration = TimeSpan.FromSeconds(30),
            MaxParticipants = 8,
        };
        await _repository.UpdateRoomSettings(room.Id, newSettings);

        Room? fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.Name, Is.EqualTo("renamed"));
        Assert.That(fetched.Type, Is.EqualTo(OsuMatchType.TeamVersus));
        Assert.That(fetched.QueueMode, Is.EqualTo(OsuQueueMode.AllPlayers));
        Assert.That(fetched.AutoSkip, Is.True);
        Assert.That(fetched.MaxParticipants, Is.EqualTo(8));
    }

    [Test]
    public async Task UpdateRoomStatus_ShouldPersistPlayingState()
    {
        Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.UpdateRoomStatus(room.Id, MultiplayerRoomState.Playing);

        Room? fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.Status, Is.EqualTo(RoomStatus.Playing));
    }

    [Test]
    public async Task SetRoomEndDate_ShouldPersistEndDate()
    {
        Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        DateTimeOffset endDate = DateTimeOffset.UtcNow.AddHours(1);
        await _repository.SetRoomEndDate(room.Id, endDate);

        Room? fetched = await _repository.GetRoom(room.Id);
        Assert.That(fetched!.EndsAt, Is.EqualTo(endDate));
    }

    [Test]
    public async Task EditItemToPlaylist_ShouldPersistChanges()
    {
        Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        Playlist item = await _repository.AddItemToPlaylist(CreatePlaylistItem(100), room.Id, 1);

        MultiplayerPlaylistItem editedItem = item.ToMultiplayerPlaylistItem();
        editedItem.BeatmapID = 200;
        editedItem.Freestyle = true;
        Playlist edited = await _repository.EditItemToPlaylist(editedItem, room.Id, 1);

        Assert.That(edited.BeatmapId, Is.EqualTo(200));
        Assert.That(edited.Freestyle, Is.True);
    }

    [Test]
    public async Task RemoveItemToPlaylist_ShouldDeleteItem()
    {
        Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        Playlist item = await _repository.AddItemToPlaylist(CreatePlaylistItem(100), room.Id, 1);

        await _repository.RemoveItemToPlaylist(item.Id, room.Id, 1);

        bool exists = await Playlists.AnyAsync(p => p.Id == item.Id);
        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task DeleteRoom_RemovesRoomAndDependents()
    {
        Users.Add(CreateUser(1, "host"));
        await _context.SaveChangesAsync();

        Room room = await _repository.CreateRoom(CreateRoom(), 1);
        await _repository.AddUserToRoom(room.Id, 1);
        await _repository.AddItemToPlaylist(
            new MultiplayerPlaylistItem
            {
                BeatmapID = 100,
                RulesetID = 0,
                RequiredMods = [],
                AllowedMods = [],
            },
            room.Id,
            1);

        await _repository.DeleteRoom(room.Id);

        Assert.That(await _repository.GetRoom(room.Id), Is.Null);
        Assert.That(await Playlists.CountAsync(p => p.RoomId == room.Id), Is.EqualTo(0));
        Assert.That(await RoomParticipatedUsers.CountAsync(rpu => rpu.RoomId == room.Id), Is.EqualTo(0));
    }

    private static User CreateUser(int id, string username) => new()
    {
        Id = id,
        Username = username,
        PwBcrypt = "$2b$test",
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
        RequiredMods = [],
        AllowedMods = [],
        Freestyle = false,
        Expired = false,
        PlaylistOrder = 0,
        WinCondition = osu.Game.Online.Multiplayer.WinCondition.Score,
    };
}