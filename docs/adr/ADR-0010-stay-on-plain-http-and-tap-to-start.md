# ADR-0010: Stay on plain HTTP; tap to start for full screen and keep-awake

**Date:** 2026-11-28
**Status:** proposed (accepted once checked on a real device)
**Deciders:** DevilDogTG (project owner)

## Context
A device page opened in a browser over plain HTTP did not go full screen and let the screen sleep. Over HTTP the
page is not a secure context, so the Screen Wake Lock API is missing, and Chrome only installs a page as an app
(its full-screen mode) from a secure origin. The Fullscreen API itself works over HTTP but needs a user gesture.
The oldest supported devices are an iPad mini 2 (Safari 12, no Wake Lock even over HTTPS) and a Galaxy S8
(Android 9).

## Options Considered

### Option A: HTTPS with a self-signed certificate
The device page could not frame SimHub (`http://…:8888`) or Navigation Dash's HTTP port any more: browsers block
HTTP frames in an HTTPS page (mixed content). That means SimHub's own HTTPS port with a second certificate, or
passing SimHub, websockets included, through this server (what ADR-0002 avoided). Clicking past a certificate
warning still blocks service workers, so no installable app; a real fix is a certificate authority installed on
every device, tied to the PC's address. It still does not help the iPad mini 2.

### Option B: An own Android app (WebView kiosk)
Full control: immersive full screen, screen kept on, mixed content allowed, start on boot. But a new codebase to
maintain, nothing for iPads, and the Play Store needs a closed test (12 testers, 14 days) before release.

### Option C: Tap to start in the device page
A full-page tap catcher with a dim hint calls the Fullscreen API from the tap. The screen is kept on with Screen
Wake Lock where present, else with a tiny muted, looping, black video playing on screen (the NoSleep.js trick).

## Decision
Option C (owner's choice, 2026-11-28: "tap to start only for now; if acceptable no need to go further").
- The hint shows only where full screen is possible and not already in effect (not after an iOS home-screen
  launch), fades after 10 s against burn-in, and comes back when full screen is left. A refused full screen or
  video is not asked for again, so the tap catcher never loops.
- The keep-awake video starts at page load (muted autoplay is allowed) and again on the tap, and resumes when the
  page is shown again. It is a 16x16 H.264 MP4 without audio, embedded as a data URI because iOS plays served video
  only with HTTP range requests, which this server does not do. `tools/keep-awake-video.py` makes it with Blender.

## Consequences
**Easier:** Nothing to install or trust on devices; the same page works on iPad and Android.
**Harder:** One tap per page load before the dashboard takes touches (the tap catcher sits above the frame). The
video trick is not a standard: browsers may stop honouring it, and it is checked only on the devices tried.
**Follow-up:** If a device still sleeps, the README's OS settings (screen timeout "never", Android "Stay awake"
while charging) remain the fallback; Options A and B stay open.
