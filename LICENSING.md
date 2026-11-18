# What is licensed how

This repository holds two things with different terms.

| | The engine (LesMudOS) | The game (Legends of Hollowmere) |
|---|---|---|
| Terms | MIT: see [LICENSE](LICENSE) | All rights reserved: see [LICENSE-GAME.md](LICENSE-GAME.md) |
| In short | Use it, change it, ship it, sell it. Keep the copyright notice. | Read it. Do not reuse it or run a public copy. |

## The engine

- `src/` : the driver and its API
- `mudlib/std/` : the standard objects (rooms, items, living things, players, and so on)
- `mudlib/secure/` : the master object, login and the daemons
- `mudlib/domains/town/` : the demonstration area
- `mudlib/wiz/README.md`
- `mudlib/Mudlib.csproj`, `LesMudOS.sln` and other build files
- `tests/` and `deploy/`
- `docs/DESIGN.md`, `docs/DRIVER_API.md`, `docs/BUILDING.md`, `docs/GAME_SYSTEMS.md`, `docs/COMBAT_TIMING.md`, `docs/SANDBOX.md`, `docs/DEPLOY.md`, `docs/CHRONICLE_IMPL.md`
- `README.md`

## The game

- `mudlib/domains/` except `mudlib/domains/town/` : the world (Vincent's Hollow, Greenvale, Oakenfell, the road between them, and everything added later)
- `mudlib/classes/` and `mudlib/skills/` : the callings, classes, skills, spells and traits
- `mudlib/events/` and `mudlib/factions/` : the event catalogue and the factions
- `docs/specs/` and `docs/areas/` : the design briefs, area plans and lore
- `docs/WORLD.md`, `docs/CLASS_SYSTEM.md`, `docs/CHRONICLE.md`, `docs/FACTIONS.md`, `docs/PLAN-corridor-crafting.md`, `docs/CORRIDOR_BALANCE.md`

## Where the line falls

The engine's standard objects contain rules that the game uses (combat timing, quality, the event pipeline). Those rules are part of the engine and are MIT. The content that fills them (which classes exist, what a faction is called, what a room says) is the game.

A file not listed here follows its directory. A new top-level directory is the engine's unless this file says otherwise.

## Contributions

By contributing to this repository you agree that your contribution is licensed under the terms that apply to the part you changed: MIT for the engine, and for the game an assignment of your contribution's rights to the copyright holder, so that the game stays under one owner.

## Third-party components

The driver uses the .NET runtime and the Roslyn compiler (`Microsoft.CodeAnalysis.CSharp`), both under the MIT license, from Microsoft and the .NET Foundation.
