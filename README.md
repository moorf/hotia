<p align="center">
  <img width="500" alt="hotia! logo" src="assets/hotia.png">
</p>
Hotia - independent fork of osu!lazer with performance in mind. Hotia is NOT IN ANY WAY affiliated or endorsed by either osu!, ppy, or osu! developers.

Small changes done with the main goal - *making it run good on mobile*. Get up to 2x performance now!

## Features
- better performance
- downloading beatmaps w/o login
- beatmap preview in the song select
- ASIO support
- cool mods for std: 
	- coloured judgements (to help Neiman know if he's rushing or dragging)
	- relax difficulties (cursor must stay in a circle for specific amount of time)
- storage decoupled from the database file (portability in the future, but the storage is a bit unstable for now)

### try running it w/o hud too

[**Latest release**](https://github.com/moorf/hotia/releases/latest)

## Licence

*osu!*'s code and framework are licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

Copyright (c) moorf. Modified 2026. Modifications released under the GNU General Public License v3.0. See the LICENCE.GPL3 file in the repository root for full licence text.

Please also note that game resources are covered by a separate licence. Please see the [ppy/osu-resources](https://github.com/ppy/osu-resources) repository for clarifications.

## Building

Clone moorf/hotia, moorf/hotia-framework  moorf/hotia-resources, then run UseLocalFramework and UseLocalResources scripts, then move to hotia folder and execute "dotnet build".
