# Branding Variants

This folder contains branding deliverables derived from the production app icon style.

## Build-Referenced Assets vs Branding Assets

Only files declared in `Praxis/Praxis.csproj` are used automatically at build time.

Build-referenced assets:
- `Praxis/Resources/AppIcon/appiconfg_windows.svg`
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

- Core motif: the app name is inspired by the Greek word for "practice," `πρᾶξις`, and the mark layers an outer regular hexagon, its inscribed circle, and another regular hexagon inscribed in that circle to evoke Archimedes' process of approximating pi with polygons.
- Micro variants intentionally keep polygonal segmentation and contrast while reducing visual collapse at tiny sizes.
- The `appiconfg_windows.svg` variant stays transparent outside the icon silhouette to avoid a forced tile background in Windows shell contexts.

---

# ブランディングバリエーション（日本語）

このフォルダには、本番アプリアイコンのスタイルから派生したブランディング成果物が含まれています。

## ビルド参照アセットとブランディング用アセット

ビルド時に自動で使われるのは、`Praxis/Praxis.csproj` に宣言されているファイルだけです。

ビルド参照アセット:
- `Praxis/Resources/AppIcon/appiconfg_windows.svg`
  - Windowsアプリ用アイコンのソース（Windowsターゲット向け `MauiIcon`）。
- `Praxis/Resources/AppIcon/appicon.svg` + `Praxis/Resources/AppIcon/appiconfg.svg`
  - Windows以外向けアプリアイコンの背景 + 前景（Windows以外のターゲット向け `MauiIcon`）。
- `Praxis/Resources/Splash/splash.svg`
  - スプラッシュ画面のソース（`MauiSplashScreen`）。

この `docs/branding` フォルダにあるブランディング用アセット:
- `store-icon-1024.svg`
  - 高解像度の正方形リスティングアイコンのソース（ストアアップロード用）。
- `store-icon-micro-128.svg`
  - 128px書き出し向けに最適化したマイクロ版アイコンのソース（視認性のためにわずかに簡略化）。
- `store-icon-micro-64.svg`
  - 64px書き出し向けに最適化したマイクロ版アイコンのソース（視認性のためにわずかに簡略化）。
- `store-hero-1920x1080.svg`
  - ストアのリスティングページとソーシャルプレビュー向けのヒーロー/バナー画像。

これらのブランディング用ファイルは、プロジェクトに明示的に追加しない限り、アプリパッケージには自動では含まれません。

## PNG出力

生成されたPNG出力は、次の場所に保存されます。
- `docs/branding/exports/store-icon-micro-128.png`
- `docs/branding/exports/store-icon-micro-64.png`

## デザイン方針

- 基本モチーフは、古代ギリシャ語で「実践」を表す「`πρᾶξις`」にちなむアプリ名の由来と、アルキメデスが円周率を求めた過程のイメージを重ねたものです。外側の正六角形、その内接円、さらにその円に内接する正六角形を重ねることで、多角形による近似の発想を表現しています。
- micro版は、極小サイズでの視認崩れを抑えつつ、ポリゴン分割感とコントラストを意図的に維持しています。
- `appiconfg_windows.svg` バリアントは、Windowsシェルの文脈でタイル背景が強制されるのを避けるため、アイコンのシルエット外側を透明のままにしています。
