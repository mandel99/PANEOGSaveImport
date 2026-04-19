# OGSaveImport

The BepInEx mod for Pharaoh: A New Era, which can directly load original Pharaoh `.map` and `.sav` files into the map editor.

The repository contains only the relevant Visual Studio project and the mod's source files.

## Overview

OG Direct Import adds a dedicated import button to the Mission Editor menu and allows original Pharaoh/Cleopatra `MAP` and `SAV` files to be loaded directly from the editor UI.

Its purpose is to convert original scenario data into Pharaoh: A New Era structures so the imported map preserves the original mission setup as closely as possible while remaining usable inside the New Era editor and runtime.

## What It Converts

When reading an original `MAP` or `SAV` file, the plugin imports and converts:

- map dimensions, offsets, and border-related values
- terrain grid data
- scenario metadata such as title, subtitle, briefing text, starting year, treasury, rescue loan, interest rate, rank, pharaoh identity, enemy identity, and climate
- world map and trade data, including cities, routes, goods availability, and trade prices
- scripted event data
- special map points such as land entry/exit, river entry/exit, predator points, and fishing points
- allowed buildings and allowed goods
- monument requirements and burial goods

## How It Adapts OG Data To New Era

The plugin performs several conversions so original Pharaoh data fits the New Era editor and gameplay model:

- coordinate adaptation
  - original packed and map-relative coordinates are converted into the New Era grid system
  - the same coordinate logic is applied consistently to terrain, points, flags, and other scenario locations
- terrain adaptation
  - original terrain values are converted into New Era terrain placement and rebuilt on the imported map
- building unlock adaptation
  - original allowed-building data is mapped to New Era `BuildingType` values
  - scenario availability is transferred into the New Era building state system
- rank-based government building adaptation
  - palace and mansion availability is converted into the correct New Era tier based on imported rank data
- entertainment adaptation
  - original entertainment permissions are converted into the matching New Era venue and service-school structure
- monument adaptation
  - original monument ids are mapped to New Era monument building types
  - monument objectives are written into the New Era win condition system
  - burial goods are imported where applicable
- monument support adaptation
  - supporting guild availability is enabled when required by the imported monument setup

## Result

The plugin acts as a direct OG-to-New-Era scenario converter. It reads original scenario content, translates original data structures into New Era equivalents, and rebuilds the mission inside the New Era editor with converted terrain, settings, unlocks, events, trade data, special points, and monument objectives.
