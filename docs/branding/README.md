# Branding Variants

This folder contains branding deliverables derived from the production app icon style.

## Build-Referenced Assets vs Branding Assets

Only files declared in `Praxis/Praxis.csproj` are used automatically at build time.

Build-referenced assets:
- `Praxis/Resources/AppIcon/appiconfg-windows.svg`
  - Windows app icon source (`MauiIcon` for Windows target).
- `Praxis/Resources/AppIcon/appicon.svg` + `Praxis/Resources/AppIcon/appiconfg.svg`
  - Non-Windows app icon background + foreground (`MauiIcon` for non-Windows targets).
- `Praxis/Resources/Splash/splash.svg`
  - Splash screen source (`MauiSplashScreen`).

Branding assets in this `docs/branding` folder:
- `store-icon-1024.svg`
  - High-resolution square listing icon source (store uploads).
- `store-icon-micro-128.svg`
  - Micro-optimized icon source for 128px exports (slightly simplified for legibility).
- `store-icon-micro-64.svg`
  - Micro-optimized icon source for 64px exports (slightly simplified for legibility).
- `store-hero-1920x1080.svg`
  - Hero/banner visual for store listing pages and social previews.

These branding files are not auto-included in app packages unless you explicitly add them to the project.

## Exported PNG Outputs

Generated PNG outputs are stored in:
- `docs/branding/exports/store-icon-micro-128.png`
- `docs/branding/exports/store-icon-micro-64.png`

## Design Notes

- Core motif: nested hexagon geometry with a white inscribed circle.
- Micro variants intentionally keep polygonal segmentation and contrast while reducing visual collapse at tiny sizes.
- The `appiconfg-windows.svg` variant stays transparent outside the icon silhouette to avoid a forced tile background in Windows shell contexts.

## 日本語訳（後半）

### PNG書き出し物

書き出し済みPNGは次に保存します。
- `docs/branding/exports/store-icon-micro-128.png`
- `docs/branding/exports/store-icon-micro-64.png`

### デザイン方針

- 基本モチーフは「正六角形の入れ子 + 白い内接円」です。
- micro版は、小サイズで潰れにくくするためにわずかに簡略化しています。
  - ただし、ポリゴン分割感と明暗コントラストは維持します。
- `appiconfg-windows.svg` は、アイコン外側を透明のままにして、Windowsのタスクバー/タイトルバー系で背景タイルが目立ちにくい構成にしています。
