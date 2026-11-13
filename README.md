# g0v0.Server.Common

> [!IMPORTANT]
> **This library is part of an unofficial osu! server emulation project.**
>
> - We (GooGuTeam) are not affiliated with ppy Pty Ltd, peppy, or team osu!. This project is neither supported nor endorsed by them.
> - **osu!** and **ppy** are registered trademarks of **ppy Pty Ltd**.
> - Please support the original project: play on the [official servers](https://osu.ppy.sh) and consider becoming a [supporter](https://osu.ppy.sh/home/support) to help fund upstream development.
> - For upstream issues and improvements, use the [upstream feedback forum](https://osu.ppy.sh/community/forums/5), rather than contacting the upstream team about g0v0-server-specific problems. Report those to the relevant GooGuTeam repository instead.
> - If you have not played the original game, please [download and play the official client](https://osu.ppy.sh/home/download) first. This project is not a replacement for the original.

This project is a part of g0v0 server v2.
It is the general library for g0v0 server series.

## Current Status

This project is under development. The content will be updated along with the v2 servers.

## License

g0v0 server v2 series is licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please
see [the licence file](LICENCE) for more information.
[tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you **include the original
copyright and license notice in any copy of the software/source**.


### Mod-specific calculation

Set `UseModSpecificCalculation` in `config/game.json` to `true` (the default) for g0v0 mod-specific difficulty and pp algorithms, or `false` for upstream algorithms. Restart the server after changing it. This setting is shared by Stable and Realtime.

```json
{
  "UseModSpecificCalculation": true
}
```
