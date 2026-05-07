# cron-server boot — macOS LaunchAgent

## Manual (per-session)

```sh
npm run cron:server
```

Runs in foreground. Ctrl+C to stop. Use during dev.

`npm run dev:all` boots web + cron-server concurrently.

## Auto-start across reboots (one-time install)

```sh
cp tools/cron-server/launchd/com.bacayo.territory-cron-server.plist \
   ~/Library/LaunchAgents/

launchctl load ~/Library/LaunchAgents/com.bacayo.territory-cron-server.plist
```

Logs land in this dir (`cron-server.log` / `cron-server.err`).

## Stop / unload

```sh
launchctl unload ~/Library/LaunchAgents/com.bacayo.territory-cron-server.plist
```

## Status

```sh
launchctl list | grep territory-cron-server
```

Empty → not loaded. Number first column → running PID.
