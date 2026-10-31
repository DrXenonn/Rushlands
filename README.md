<!-- Header banner: recommended ~1280x320 (4:1) PNG or JPG -->
<p align="center">
  <img src="docs/6.png" alt="RushLands" width="100%">
</p>

# RushLands

A fast-paced multiplayer RTS for 2–6 players. Capture buildings, build armies, and conquer enemy strongholds. Inspired by *Mushroom Wars*, with online multiplayer through Steamworks and a built-in map editor.

<p align="center">
  <a href="https://drxenon.itch.io/rushlands">
    <img src="https://img.shields.io/badge/%E2%96%B6%20Download%20on-itch.io-FA5C5C?style=for-the-badge&logo=itch.io&logoColor=white" alt="Download on itch.io">
  </a>
</p>

<p align="center">
  <img src="docs/1.png" width="700">
</p>

## Features

- **Multiplayer:** 2–6 players, team-based or free-for-all, with Steam lobbies and invites
- **Building control:** send units to capture and convert buildings
- **Upgrades:** defensive and production buildings have 3 upgrade levels
- **Territory system:** own an entire zone to gain passive income
- **Map editor:** build and test custom maps from inside the game lobby, with water, territories, node counts, and trade ships

## Screenshots

### Gameplay
<p align="center">
  <img src="docs/3.png" width="600">
  <img src="docs/11.png" width="600">
</p>

### Water Battle
<p align="center">
  <img src="docs/9.gif" width="600">
  <img src="docs/10.png" width="600">
</p>

### Upgrading Buildings
<p align="center">
  <img src="docs/2.gif" width="250">
  <img src="docs/4.gif" width="250">
</p>

### Game Over
<p align="center">
  <img src="docs/5.gif" width="500">
</p>

### Map Editor
<p align="center">
  <img src="docs/7.png" width="400">
  <img src="docs/8.png" width="400">
</p>

## Controls

| Action | Input |
|---|---|
| Move camera | `W` `A` `S` `D` |
| Select building | Left click |
| Box select | Click + drag |
| Multi-select / deselect | `Ctrl` + click |
| Send half of units | Right click on target |
| Send all units | `Ctrl` + right click |
| Send custom amount | Hold right click on target |
| Zoom | Scroll |

## Tech

- **Engine:** Unity (C#)
- **Networking:** Steamworks <!-- add wrapper: Steamworks.NET / Facepunch.Steamworks --> for lobbies, matchmaking and peer-to-peer play
- **Map editor:** runtime editor available in the lobby

## About this repo

This project was built in 2024. I originally kept it in **Plastic SCM** rather than Git, so this repository starts from a single snapshot of the final project and has no earlier commit history.

It was built as part of my learning journey with Unity and multiplayer networking, with the goal of making a complete RTS with matchmaking, territory mechanics, and a usable map editor.

> **Steam note:** the game uses Valve's public Spacewar App ID (480) for development. It is not an official Steam release, and is for portfolio and educational use only. Steam must be running to play multiplayer.

## Credits

- **UI design and layout:** [Arash Namaki](https://github.com/ArashNamaki)

## Author

**Sina** · [itch.io](https://drxenon.itch.io) · [GitHub](https://github.com/DrXenonn)
