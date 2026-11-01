# Secure File Manager — Egyptian UI Asset Pack

This package is for the **existing** SecureFileManager repository. Extract the archive **into the repository root**, merging the `design/` directory. Do not create a second project or delete existing code.

## Asset map

| Path | Role |
| --- | --- |
| `design/egyptian-ui-concept.png` | Overall visual reference (not an application background) |
| `design/assets/branding/eye-of-horus.png` | App header / login emblem |
| `design/assets/ornaments/header-winged-sun.png` | Header ornament |
| `design/assets/ornaments/footer-lotus-ibis.png` | Footer or section divider |
| `design/assets/illustrations/anubis-sidebar.png` | Lower sidebar decorative panel |
| `design/assets/textures/papyrus.png` | Light background |
| `design/assets/textures/dark-stone.png` | Dark background |
| `design/assets/icons/*.png` | 12 isolated functional icon images |
| `design/assets/icons/icon-sheet-original.png` | Source icon sheet; do not use directly as one icon |

### Icon names
`drives`, `files`, `archives`, `json-xml`, `journal`, `settings`, `create`, `delete`, `search`, `security`, `user`, `database`.

### Codex integration guidance

- `design/` is design input; for runtime use, copy or include only needed assets as WPF **Resource** files under `src/SecureFileManager.Wpf/Assets/` (and set Build Action / pack URI correctly), or configure linked Resource items in the `.csproj`. Keep the references stable.
- Create actual WPF controls for buttons, menus, DataGrid, progress bars, etc. **Never** use the complete concept screenshot as a fake interface.
- Respect `DESIGN_SPEC.md` and accessibility/keyboard behavior. Prioritize the assets as visual guidance when discrepancies arise.
- Keep image aspect ratio and avoid stretching decorative content excessively. The texture samples are subtle but may need one-time scaling instead of repeated tiling: verify seams visually.
- The 12 icons are cropped from the original 4x3 sheet and sized 256x256 with transparent alpha. Review at 24-32px: overly detailed artwork may look unclear. In such cases create simplified vector WPF icon without changing semantics.
- No font files are included.
