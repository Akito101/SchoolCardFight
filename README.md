# SchoolCardFight

A Unity 2D card battler. You collect cards from packs, build a deck, and fight a rising ladder of
opponents (Child → Student → Monitor → Teacher) by playing cards onto a shared board. Matching
card categories on the board lets you capture cards, and the match ends when one deck drops to
three cards or fewer. Between fights you earn coins in minigames, open packs, and unlock artifacts.

---

## Features

- **Campaign ladder** — 4 levels on a map screen, unlocked one at a time, each with a stronger AI opponent.
- **Deck building** — pick which owned cards go into the active deck with a per-level deck size limit.
- **Card packs** — weighted rarity rolling (`1★ / 2★ / 3★`) with an opening animation.
- **Artifacts** — choose one before a fight. Effects: Protection, Reshuffle Opponent Deck, Steal Card.
- **Opponent dialogue** — the AI (especially Teacher/Monitor) interrupts with choices that change the match.
- **Minigames** — Memory Match and Rock-Paper-Scissors to earn coins.
- **Collection book** — every card you have ever obtained is remembered, even after you lose copies.
- **Audio** — music library per scene with per-opponent battle music overrides, plus UI click sounds.
- **All progress saved to `PlayerPrefs`** (coins, cleared levels, owned cards, artifacts, deck, collection).

---

## Requirements

- **Unity 6 / 2022.3 LTS or newer** (any version with the UI Toolkit-free UGUI + TextMeshPro stack works).
- **TextMeshPro** package (bundled in the `Assets/TextMesh Pro` folder).
- **DOTween** (bundled under `Assets/Plugins/Demigiant/DOTween`) — the `DOTweenSettings` asset already
  lives in `Assets/Resources`.
- No external runtime dependencies, no network access.

---

## Getting started

1. Open the project folder in Unity Hub with a compatible Unity version.
2. Open the **MainMenu** scene and press Play, or open **Map** to jump straight to the campaign.
3. Make sure the gameplay scenes (`MainMenu`, `Map`, `Battle`, `Minigame`) are added to
   **File → Build Settings → Scenes In Build** so `SceneManager.LoadScene` calls resolve.

### Scenes

| Scene                | Purpose                                                                   |
| -------------------- | ------------------------------------------------------------------------- |
| `MainMenu`           | Title screen, new game / continue                                         |
| `Map`                | Campaign map — level buttons, collection, deck, shop, settings tabs       |
| `Battle`             | The card battle scene (`BattleTestManager`)                               |
| `Minigame`           | Minigame hub (Memory Match, Rock-Paper-Scissors)                          |
| VN scenes (optional) | Per-level `Visual Novel` interludes, wired via `MapManager.levelVNScenes` |

---

## Project structure

```
Assets/
├── Scripts/
│   ├── PlayerPrefs.cs            Main menu flow
│   │   ├── PlayerProgress        Static PlayerPrefs save layer (coins, levels, cards, deck, artifacts)
│   │   └── ProgressReset.cs      Wipe all saved data (debug / new game)
│   ├── VNManager.cs              Visual novel dialogue scenes
│   ├── TutorialPanel.cs          First-time tutorial overlay
│   ├── CardRelated/
│   │   ├── CardData.cs           ScriptableObject: one card (name, art, category, stars, artifact flag)
│   │   ├── CardDatabase.cs       ScriptableObject: all cards + pack drop weights
│   │   ├── CardInspector.cs      Full-screen card detail view
│   │   ├── CardTilt.cs           Pointer tilt effect for cards
│   │   └── CardSlotView.cs       Single card tile in a grid
│   ├── Play/                     The battle scene
│   │   ├── BattleTestManager.cs  Turn loop, matching rules, dialogue hooks, win/lose
│   │   ├── ArtifactController.cs Protection / reshuffle / steal implementations
│   │   ├── AILevelDeck.cs        Builds each opponent's deck
│   │   ├── AnimationController.cs Card tweens
│   │   ├── CardView.cs           A card object on the board
│   │   ├── SwipeToPlay.cs        Swipe-up gesture to draw the next card
│   │   ├── PlayerArtifactChoices.cs Carries the chosen artifact from Map → battle
│   │   ├── Model/CardData.cs     Card model + enums (CardCategory, ArtifactEffect)
│   │   ├── Deck/                 PlayerDeckView, AIDeckView, BoardDeckView
│   │   └── UI/                   Dialogue, artifact UI, background, win/lose panel, deck count
│   ├── Map/                      Campaign map + meta systems
│   │   ├── MapManager.cs         Level buttons, panel switching, level → scene routing
│   │   ├── PlayerCollection.cs   All owned card counts + discovered set
│   │   ├── PlayerDeck.cs         Active deck list + bench logic
│   │   ├── DeckLimit.cs          Trims the deck to a size limit
│   │   ├── DeckBuilder.cs        Deck / inventory grid UI
│   │   ├── ShopManager.cs        Buy packs with coins
│   │   ├── CollectionBook.cs     Collection screen
│   │   ├── ArtifactChoicePanel.cs Pre-battle artifact picker
│   │   ├── MemoryMatchGame.cs    Memory minigame
│   │   ├── RockPaperScissorsGame.cs RPS minigame
│   │   ├── MinigameHub.cs        Minigame selection screen
│   │   ├── LevelNode.cs          Map level button data
│   │   └── SettingsPanelController.cs / PlayerPacks.cs / various *View.cs
│   ├── PackOpening/              PackOpener, PackRoller (rarity weights), PackCardView
│   ├── Localization/             Localizer, LocalizationBootstrap, LanguageToggleButton
│   ├── Audio/                    MusicManager, MusicPlayer, MusicLibrary, GameAudio, ButtonClickSounds
│   └── Reward/                   LevelRewards (deck sizes, enemy artifacts), BattleResultPanel
├── Resources/
│   ├── Localization.csv          Translation table (English is column 0)
│   ├── LevelRewards.asset        Per-level deck size + enemy artifact config
│   ├── MusicLibrary.asset        Scene → music mapping
│   └── DOTweenSettings.asset
├── OurAssets/Editor/             RoundedMaskMaker editor tool
└── Plugins/Demigiant/DOTween/    Third-party tween library
```

---

## Content authoring

### Adding a card

1. **Create → Cards → Card** to make a new `CardData` asset.
2. Fill in `Card Name` (must be **unique** — saves and the collection key off it), art, category,
   and stars (1–3, matching the stars drawn on the art).
3. Add it to the **packs** list on the `CardDatabase` asset, or to **Unique Cards** if it should only
   appear in the collection and never drop from a pack.
4. `CardDatabase.OnValidate` warns about duplicate names in the console.

### Tuning pack odds

On the `CardDatabase` asset, adjust `oneStarWeight`, `twoStarWeight`, `threeStarWeight`.
They are shares of the total, so any numbers work (70 / 25 / 5 = common / rare / legendary).

### Tuning levels

Edit `Assets/Resources/LevelRewards.asset`:

| Field           | Meaning                                                        |
| --------------- | -------------------------------------------------------------- |
| `deckSize`      | Minimum cards required to enter the fight (also the deck cap)  |
| `artifact`      | Artifact the player earns the first time they clear this level |
| `enemyArtifact` | Artifact the AI starts the battle with                         |

Level → opponent mapping lives in `BattleTestManager.GetAITypeForLevel` (0 = Child, 1 = Student,
2 = Monitor, 3 = Teacher).

### Adding translations

Add a new column to `Assets/Resources/Localization.csv` with the language name in the header row,
then fill each row. In code, wrap any user-facing string in `Localizer.T("Play")` — English is the
source language and is shown as-is. Missing rows fall back to English and log a warning in debug builds.

### Minigame rewards

`MemoryMatchGame` exposes `baseReward`, `bonusPerSavedMistake`, and `mistakeBudget` in the Inspector.
`RockPaperScissorsGame` has its own reward settings.

---

## Gameplay rules (quick reference)

- The player swipes their current card up to play it onto the board.
- The AI plays automatically after the player's turn.
- When the newest two board cards share a **category**, the last player to move wins both cards
  (they go back into the winner's deck, reshuffled). **Protection** can block one capture.
- When a deck can't draw, board cards return to their owners and both decks reshuffle.
- The match ends when the loser's deck drops to **3 cards or fewer**; the result panel shows the winner.
- Winning a new level increases `PlayerProgress.ClearedLevels`, which unlocks the next map node.

---

## Save data

Everything is stored in `PlayerPrefs` under these keys (see `PlayerProgress.cs`):

| Key                   | Contents                                              |
| --------------------- | ----------------------------------------------------- |
| `Coins`               | Current coin balance                                  |
| `FirstBattleDone`     | Unlocks the map after your first battle (win or lose) |
| `ClearedLevels`       | Number of levels beaten                               |
| `LevelSelected`       | Level index currently being fought                    |
| `OwnedCardNames`      | JSON list of `{ cardName, count }`                    |
| `OwnedArtifactsTypes` | JSON list of artifact enum names                      |
| `CardCollection`      | JSON list of every card name ever discovered          |
| `BattleDeckSaved`     | JSON list of card names in the active deck            |
| `Language`            | Selected language                                     |

Use **ProgressReset** (or delete the app's PlayerPrefs) to start a fresh save.

---

## Development notes

- **Debug helpers** (remove before shipping):
  - Map: `MapManager.OnDebugCoinsPressed` (+100 coins) and `OnDebugWinPressed` (auto-clear a level).
  - Battle: right-click the `BattleTestManager` component in the Inspector → **Debug: Instant Win / Lose**.
- The project currently ships **no automated tests**. If you add a test assembly, the safest first
  targets are the pure-logic static classes: `PlayerDeck` (bench/deck math), `PackRoller` (weights),
  `DeckLimit.TrimTo`, and `Localizer.ParseCsv`.
- Third-party code under `Assets/Plugins/Demigiant`, `Assets/TextMesh Pro/Examples & Extras`, and
  `Assets/TutorialInfo` is vendored and should not be modified.
- The `mono_crash.*.blob` files in the project root are leftover Unity crash dumps and can be deleted.

---

## License / attribution

This is a personal project. **DOTween** is © Demigiant and is included under its own license —
see `Assets/Plugins/Demigiant/DOTween`. TextMeshPro assets are © Unity Technologies.
