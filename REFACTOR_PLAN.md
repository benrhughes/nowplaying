# Refactor Plan: BcMasto -> NowPlaying & New Features

This document tracks the progress of refactoring the `bcmasto` project to `nowplaying` and adding the #nowplaying composite image feature.

## Phase 1: Renaming & Project Structure
- [x] Rename project directories:
    - [x] `src/bcmasto` -> `src/nowplaying`
    - [x] `src/bcmasto.tests` -> `src/nowplaying.tests`
- [x] Rename project files:
    - [x] `src/nowplaying/BcMasto.csproj` -> `src/nowplaying/NowPlaying.csproj`
    - [x] `src/nowplaying.tests/bcmasto.tests.csproj` -> `src/nowplaying.tests/nowplaying.tests.csproj`
- [x] Update Solution file (`bcmasto.sln` -> `nowplaying.sln`) and fix references.
- [x] Rename namespaces in all `.cs` files (Find `namespace BcMasto` -> `namespace NowPlaying`).
- [x] Update `Dockerfile`, `docker-compose.yml`, and `AGENTS.md` / `README.md`.
- [x] Verify build and tests pass after rename.

## Phase 2: Backend Core & Dependencies
- [x] Install `SixLabors.ImageSharp` NuGet package.
- [x] Extend `IMastodonService`:
    - [x] Add method to get current user ID (`VerifyCredentials`).
    - [x] Add `GetTaggedPostsAsync(string userId, string tag, DateTime since, DateTime until)`.
    - [x] Implement pagination handling to respect rate limits.
- [x] Extend `ImageService` (if exists) or Create:
    - [x] Update interface `IImageService` to include composite generation.
    - [x] Implement `GenerateCompositeAsync(IEnumerable<string> imageUrls)` to stitch images into a grid.

## Phase 3: API Endpoints
- [x] Create `ReviewEndpoints.cs`:
    - [x] `GET /api/review/search`: Accepts date range, returns list of albums/posts.
    - [x] `GET /api/review/composite`: Generates and returns the image.
- [x] Register new services and endpoints in `Program.cs` / `ServiceCollectionExtensions.cs`.

## Phase 4: Frontend Overhaul (Vue 3 SPA)
- [x] Refactor `wwwroot` structure:
    - [x] Create `js/components` and `js/views`.
    - [x] Update `index.html` to load Vue 3 via CDN (ES Modules).
- [x] Implement Navigation:
    - [x] "Post" (Existing functionality).
    - [x] "Review" (New feature).
- [x] Implement "Review" Page:
    - [x] Date picker for time window.
    - [x] "Search" button to fetch posts.
    - [x] Display summary list of albums.
    - [x] Display generated composite image.

## Phase 6: Test coverage
- [x] Ensure test coverage of backend code (62 tests passing)
- [x] Ensure test coverage of UI code (28 tests passing with Vitest + Vue Test Utils)
  - [x] App component (6 tests)
  - [x] Post component (10 tests)
  - [x] Review component (12 tests)

## Phase 5: Cleanup & Verification
- [x] Run full test suite (all 62 tests passing).
- [x] Verify Docker build (successfully builds and runs).
