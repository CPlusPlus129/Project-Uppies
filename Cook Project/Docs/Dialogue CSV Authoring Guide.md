# Dialogue CSV Authoring Guide

Use this guide to author the dialogue CSVs that feed the Dialogue Module and to refresh the generated assets inside Unity.

## 1. Spreadsheet Setup
- **Headers first:** Row 1 must contain the exact column names expected by the runtime (e.g. `CharacterName`, `DisplayName`, `VoiceSpeedMultiplier`, `NameCardColor`).
- **Plain text cells:** Format every column as plain text so values like leading zeros, `#` symbols, or comma-separated numbers are preserved.
- **No embedded commas:** When a field needs multiple values, prefer `#RRGGBBAA` hex strings, semicolon-separated values, or spaces instead of commas. Commas create additional columns on export.
- **Empty vs missing:** Leave a cell empty (`""`) to fall back to defaults; do not delete the column.
- **Encoding:** Export with UTF-8 (no BOM) to keep Unicode characters in names and dialogue intact. Google Sheets exports UTF-8 automatically. In Excel, choose **CSV UTF-8 (Comma delimited)**.

### Recommended Column Order (Character.csv)
| Column | Required | Description |
| --- | --- | --- |
| `CharacterName` | ✅ | Unique ID referenced from scenario scripts. Lowercase snake-case keeps things readable. |
| `DisplayName` | ✅ | Player-facing string shown on the nameplate. |
| `X` / `Y` / `Scale` | ⛔ | Optional layout overrides for the character sprite layer. Leave blank to use scene defaults. |
| `FileName` | ⛔ | Addressables key (e.g. `Res:/Images/butcher.png`). The importer verifies the asset exists. |
| `VoiceFileName` | ⛔ | Optional voice clip key. Leave blank for silent lines. |
| `VoiceSpeedMultiplier` | ⛔ | Numeric multiplier for the typing speed and voice cadence. Zero or negative values fall back to `1.0`. |
| `NameCardColor` | ⛔ | Hex color for the dialogue nameplate tint. Use `#RRGGBBAA` (e.g. `#FF0000FF` for solid red) or a space/semicolon separated RGBA list (`255 0 0 255`). |

> Tip: Keep sample rows for new authors. Duplicate, edit, and double-check that no extra delimiters were introduced before committing.

## 2. Exporting the CSV
1. Download/export the sheet as CSV (UTF-8). Confirm the file retains the header row and the expected number of columns.
2. Open the exported file in a text editor once to spot any stray commas, extra quotes, or Windows smart quotes.
3. Commit the file under `Cook Project/Assets/Res_Local/Global/Tables/`. Unity tracks the `.meta` next to it; make sure both files stay together.

### Color Field Examples
- Solid white (default): `#FFFFFFFF`
- 50% transparent black: `#00000080`
- Using channel values: `255 128 0 255`

The importer first tries HTML-style hex strings, then looks for space/semicolon/pipe-separated numeric components. Mixing commas here will create unintended columns.

## 3. Unity Import Workflow
1. **Open the project in Unity 6000.2.9f1.** Ensure the Dialogue Module components are compiled (enter Play Mode once if needed).
2. **Configure the importer (first-time only):**
   - Menu: **Tools ▸ Create Dialogue Settings**. This creates `Assets/DialogueModule/Settings/DialogueSettings.asset`.
   - In the inspector, set `CSV Folder Paths` to include `Res_Local/Global/Tables`. Use paths relative to `Assets/` (e.g. `Res_Local/Global/Tables`).
   - Confirm the `Delimiter` is a comma and the `Encoding` is UTF-8.
3. **Refresh generated assets whenever CSVs change:**
   - Menu: **Tools ▸ Update Dialogue Assets**.
   - The importer scans every configured folder, regenerates `ScenarioBook.asset` and `SettingsBook.asset`, and prints a summary in the Console.
4. **Verify in the inspector:**
   - Select `Assets/DialogueModule/Data/SettingsBook.asset`, expand the `Character` collection, and confirm each row reflects the latest CSV values (including `NameCardColor`).
   - Optionally Play Mode test: trigger a dialogue line and confirm the name card tint matches the CSV.

## 4. Common Pitfalls & Fixes
- **Extra columns after export:** Usually caused by commas inside cells. Replace with spaces or semicolons, or wrap the value in quotes before exporting.
- **Missing characters after import:** Ensure `CharacterName` values are unique and the CSV row is not fully empty/commented. The importer skips empty rows and rows starting with `//`.
- **Color not applied:** Double-check the hex string (must include alpha) or ensure all RGBA numbers are in the 0–255 range.
- **Importer logs “CSV folder path not found”:** The path must be relative to `Assets/` and spelled exactly. Example: `Res_Local/Global/Tables`, not `Assets/Res_Local/Global/Tables`.
- **Unity compile errors after pull:** Reopen the project with the matching Unity editor version so the generated `.csproj` references the correct analyzers.

## 5. Checklist Before Commit
- [ ] CSV validated for headers, delimiter count, and encoding.
- [ ] Updated `.meta` file committed alongside the CSV.
- [ ] Dialogue importer run (`Tools ▸ Update Dialogue Assets`).
- [ ] Scenario/Settings books saved without warnings.
- [ ] Target scene playtested for at least one affected line.

Following this flow keeps dialogue data editable by writers while ensuring the runtime assets stay in sync with source CSVs.
