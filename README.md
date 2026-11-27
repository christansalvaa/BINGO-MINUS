Follow this asset guide.

All files below are **placeholders**. To replace one, overwrite the PNG with the same filename (any size
works; it is stretched to the size in the "Drawn at" column, so keep the proportions). No code changes needed.
If a file is missing, the game draws that visual with plain shapes instead, so you can replace them one at a
time. Use transparent PNGs. The canvas is 1280x720.

Text and numbers that change during play (chip amounts, button labels, bet totals, win text, x1/x2/x3)
are **always drawn by code on top of the art**, so leave those areas free of baked-in text unless a row says
the text is baked in.

Folders (lowercase `assets`):
- `scenes/Minigames/Roulette/assets/`
- `scenes/Minigames/ColorGame/assets/`
- `scenes/UI/assets/casino/` (shared by both games)

---------------------------------------------------------------------------------------------------

## Roulette (`scenes/Minigames/Roulette/assets/`)

| File | Size | Drawn at | Notes |
|---|---|---|---|
| `bg_roulette.png` | 1280x720 | full screen | Background. The top 62 px are covered by the shared header. |
| `wheel_rim.png` | 300x300 | centred at (225, 238), not rotating | Wooden rim + the dark ball track. Ball runs on a circle of radius 134 px from the centre. |
| `wheel_disc.png` | 300x300 | same centre, **rotates** | Pockets, spokes, hub. **Numbers are baked in** (they rotate with the disc). See geometry below. |
| `ball.png` | 64x64 | 16x16 px | The roulette ball. |
| `table_board.png` | 856x290 | top-left at (389, 102) | Whole betting table: wood frame, felt, zero, 1-36, columns, dozens, even-money row. **All labels are baked in.** See geometry below. |
| `cell_win.png` | 60x62 | over the winning number's cell (pulses) | Highlight frame; keep the middle transparent. Stretched over the tall `0` cell too. |
| `pocket_red.png` / `pocket_black.png` / `pocket_green.png` | 64x64 | 80 px (winning-number badge) and 28 px (history) | Round coloured disc; the number is drawn by code on top. |
| `panel_info.png` | 320x230 | (60, 405) | Left panel under the wheel (winning number, total bet, last win, message). |
| `panel_history.png` | 844x40 | (395, 398) | History strip behind the last results. |
| `panel_payouts.png` | 409x104 | (830, 448) | Payout table panel. |
| `btn_undo.png`, `btn_clear.png`, `btn_rebet.png` | 120x56 | (395,570), (525,570), (655,570) | Blank button plates. Labels drawn by code. |
| `btn_spin.png` | 439x56 | (800, 570) | Blank plate. The label changes (SPIN / SPINNING... / NEW ROUND). |

**Wheel geometry (`wheel_disc.png`, 300x300, centre = 150,150).**
Pocket 0 (green) starts at 3 o'clock and the 37 pockets continue **clockwise** in European order
`0 32 15 19 4 21 2 25 17 34 6 27 13 36 11 30 8 23 10 5 24 16 33 1 20 14 31 9 22 18 29 7 28 12 35 3 26`,
each one 9.73 degrees wide. The ball comes to rest at radius **118 px** from the centre, so the pocket ring
must cover roughly radius 92 to 126 px. If the pockets are in a different order or start elsewhere, the ball
will land in the wrong-looking pocket.

**Table geometry (`table_board.png`, 856x290).** Coordinates are inside the PNG (the PNG sits at 389,102 on screen).
Chips and hover highlights are placed by code on this exact grid:

| Part | x, y | Size |
|---|---|---|
| Zero | 6, 6 | 62 x 186 |
| Number cells (12 columns x 3 rows) | starts at 68, 6 | each 60 x 62. Top row = 3, 6, 9...36; middle = 2, 5...35; bottom = 1, 4...34 |
| "2 to 1" column cells (3) | 788, 6 / 68 / 130 | each 62 x 62 |
| Dozens (1st / 2nd / 3rd 12) | 68, 192 | each 240 x 46 |
| Even-money row (1-18, EVEN, RED, BLACK, ODD, 19-36) | 68, 238 | each 120 x 46 |

---------------------------------------------------------------------------------------------------

## Color Game (`scenes/Minigames/ColorGame/assets/`)

| File | Size | Drawn at | Notes |
|---|---|---|---|
| `bg_colorgame.png` | 1280x720 | full screen | Background (the placeholder includes the bunting under the header). |
| `die_yellow.png`, `die_white.png`, `die_pink.png`, `die_blue.png`, `die_red.png`, `die_green.png` | 150x150 | the 3 dice at (365, 95), (565, 95), (765, 95) | Die faces. **Colour name/symbol is baked in.** Dice hop and shake while rolling; the drop shadow is drawn by code. |
| `tile_yellow.png` ... `tile_green.png` (6 files, same colours) | 180x148 | betting tiles, y = 322, x = 65 + 194 x colour index (yellow, white, pink, blue, red, green) | **Colour name and "1 to 1 per die" are baked in.** Chip, `+win` / `-loss` text and the gold outline are drawn by code. Tiles are darkened automatically after a roll if they lost or had no bet. |
| `match_badge.png` | 42x24 | top-right corner of a tile that matched | The `x1`/`x2`/`x3` text is drawn by code. |
| `panel_value.png` | 240x70 | (500, 488) and (760, 488) | Behind TOTAL BET and LAST WIN. |
| `panel_rolls.png` | 195x70 | (1020, 488) | Behind the "last rolls" colour squares. |
| `btn_undo.png`, `btn_clear.png`, `btn_rebet.png` | 130x56 | (65,580), (205,580), (345,580) | Blank plates. Labels drawn by code. |
| `btn_roll.png` | 720x56 | (495, 580) | Blank plate. The label changes (ROLL DICE / ROLLING... / NEW ROUND). |

---------------------------------------------------------------------------------------------------

## Shared (`scenes/UI/assets/casino/`)

| File | Size | Drawn at | Notes |
|---|---|---|---|
| `chip_1.png`, `chip_5.png`, `chip_25.png`, `chip_50.png`, `chip_100.png`, `chip_500.png` | 64x64 | 60 px in the chip rack, 30 px / 52 px for chips on the table | **Leave the middle blank**: the amount is printed by code (white text with a dark outline). A chip on the table uses the art of the biggest chip that fits its total (e.g. a 130 bet uses `chip_100`). The drop shadow and gold "selected" ring are code. |
| `modal_panel.png` | 500x280 | (390, 220) | "Out of coins" popup background. Title, message and X are drawn by code. |
| `modal_btn_deposit.png` | 205x48 | (420, 425) | Blank plate; label drawn by code. |
| `modal_btn_lobby.png` | 205x48 | (655, 425) | Blank plate; label drawn by code. |

(Not game assets: the two lobby cards `scenes/UI/assets/card_roulette.png` and `card_colorgame.png`, 260x320,
same layout as the other lobby cards, with the title, subtitle, badge and PLAY button baked in.)

---------------------------------------------------------------------------------------------------

## Button states
All plates use the shared button helper: idle is slightly dimmed, hover is full brightness with a gold edge,
and disabled (while the wheel spins / dice roll) is grey and translucent. Design only the normal state.

## What always stays code-drawn
Hover highlight on table numbers, the gold ring on winning chips, the pulsing win marker colour/alpha,
the dice drop shadow, chip amounts, the "SELECT CHIP" ring, all text labels, the ball path, and the dice
tumbling animation.
