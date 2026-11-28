# ADR-0010: Stay on plain HTTP; a full-screen button and a keep-awake video

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

### Option C: Full screen from a tap, keep-awake video, in the device page
The Fullscreen API is called from a tap. The screen is kept on with Screen Wake Lock where present, else with a
muted, looping, black video (the NoSleep.js trick).

## Decision
Option C (owner's choice, 2026-11-28: "tap to start only for now; if acceptable no need to go further").
- **Full screen:** a small toggle button at the top right (owner's ask after the first device check; the first
  version was a full-page tap catcher, which also kept touches from the dashboard until tapped). Shown only where
  full screen is possible and not already in effect (not on iPhone, not after an iOS home-screen launch). It dims
  to 25% after 10 s and is hidden on the black offline screen, against burn-in.
- **Keep-awake video:** full page, black, *behind* the dashboard frame. Chromium (blink `video_wake_lock.cc`)
  holds the screen on for a muted or silent video only when it is more than 75% inside the viewport and covers more
  than 20% of the top page (`kSizeThreshold`); it measures geometry only, not what is painted on top. The first
  version used a 2 px video, which a Galaxy S25 ignored (2026-11-28).
- The video starts at page load (muted autoplay is allowed), again on any tap on the page itself, and when the page
  is shown again. It is a 16x16 H.264 MP4 without audio, embedded as a data URI because iOS plays served video only
  with HTTP range requests, which this server does not do. `tools/keep-awake-video.py` makes it with Blender.

## Consequences
**Easier:** Nothing to install or trust on devices; the same page works on iPad and Android.
**Harder:** The button sits over the dashboard's top-right corner. The video trick is not a standard: browsers
may stop honouring it (Chrome already added the size rule), and it is checked only on the devices tried.
**Follow-up:** If a device still sleeps, the README's OS settings (screen timeout "never", Android "Stay awake"
while charging) remain the fallback; Options A and B stay open.
