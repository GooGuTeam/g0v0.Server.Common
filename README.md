# g0v0.Server.Common

This project is a part of g0v0 server v2.
It is the general library for g0v0 server series.

## Current Status

This project is under development. The content will be updated along with the v2 servers.

## Room database upgrade

`rooms.source` stores the creating server's `ServerIdentify` name (`Realtime`, `Stable`, or `Lazer`).
Room creation and lookup through `IRoomRepository` require an explicit source; a lookup for another
server's room returns null. Changing room settings or match type preserves its source.

Apply the PostgreSQL `AddRoomSource` EF migration before deploying these server versions. For the
legacy MySQL schema, apply the `84cceb652fe5` room-source migration in `osu_lazer_api` with
`uv run g0v0-migrate upgrade head` (from that repository, inside its devcontainer).
Both add an indexed, non-null string column and assign existing rooms to `Realtime`, matching the
room server used before source tracking. Historical records do not contain enough information to
identify any Stable rooms created before this upgrade; reclassify those known records explicitly if needed.

## License

g0v0 server v2 series is licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please
see [the licence file](LICENCE) for more information.
[tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you **include the original
copyright and license notice in any copy of the software/source**.

"osu!" or "ppy" is a registered trademark of ppy Pty Ltd and is not affiliated with g0v0 server.