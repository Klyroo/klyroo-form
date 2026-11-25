# Migrating from Lux to Klyroo

**Klyroo.Form** was developed as part of the LuxStack project, in the `lux-form` repository. Klyroo is its new name.
No package was published to NuGet under the Lux name, so this guide only concerns code written against the Lux sources.
The rename is a breaking change for source code, markup, styles and build settings; the behavior of the library is
unchanged. There are no compatibility aliases: the project was in
preview, and keeping a second set of names would have doubled the public surface for every future release.

## Packages

| Before | After |
| :--- | :--- |
| `Lux.Form.Core` | `Klyroo.Form.Core` |
| `Lux.Form` | `Klyroo.Form` |

```bash
dotnet remove package Lux.Form.Core
dotnet add package Klyroo.Form.Core --prerelease
```

## Namespaces and types

Replace the root namespace `Lux` with `Klyroo` (`using Lux.Form;` becomes `using Klyroo.Form;`).
Types and members whose name carried the brand follow the same rule:

| Before | After |
| :--- | :--- |
| `Lux...` types (`LuxOptions`, `Lux...ServiceCollectionExtensions`) | `Klyroo...` (`KlyrooOptions`, `Klyroo...ServiceCollectionExtensions`) |
| `services.AddLux...()` | `services.AddKlyroo...()` |
| `dotnet new` templates `lux-*` | `klyroo-*` |

## Razor, CSS and JavaScript

This library builds on Klyroo.UI and follows its renames: `Lx*` components become `Kly*`, `--lux-*` tokens become
`--klyroo-*`, `data-lx-*` attributes become `data-kly-*`, `lx-*` custom elements become `kly-*`, and the stylesheet
`lux-<library>.css` becomes `klyroo-<library>.css`. The full table is in the
[Klyroo.UI migration guide](https://github.com/klyroo/klyroo-ui/blob/main/MIGRATION.md).

## Find what is left

From the root of your solution, list the old names that remain:

```bash
grep -rnE '\bLux\.|\bLx[A-Z]|--lux-|--lx-|data-lx-|<lx-|onlx|\bLux__|LUX(FONT|ICON|TW|TOOLS)' \
  --include='*.cs' --include='*.razor' --include='*.cshtml' --include='*.css' --include='*.js' \
  --include='*.csproj' --include='*.props' --include='*.targets' --include='*.json' .
```
