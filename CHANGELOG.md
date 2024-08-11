# Changelog

All notable changes to [**GZCTF**](https://github.com/GZTimeWalker/GZCTF) will be documented in this file.

---
## [0.21.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.21.0..v0.21.1) - 2024-08-11

### 🐛 Bug Fixes

- **(docker)** handle container conflict when creating - ([c74bb8](https://github.com/GZTimeWalker/GZCTF/commit/c74bb8)) by **GZTime**

### 📦 Other Changes

- add LICENSE_ADDENDUM.txt (#317) - ([89af5d](https://github.com/GZTimeWalker/GZCTF/commit/89af5d)) by **GZTime**

---
## [0.21.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.9..v0.21.0) - 2024-07-31

### ⛰️ Features

- **(log)** unified exception rendering - ([30f3a3](https://github.com/GZTimeWalker/GZCTF/commit/30f3a3)) by **GZTime**
- Append exception information to log - ([d342bb](https://github.com/GZTimeWalker/GZCTF/commit/d342bb)) by **Steven He**

### 🐛 Bug Fixes

- **(docker)** container only bind specific ports - ([b36718](https://github.com/GZTimeWalker/GZCTF/commit/b36718)) by **Kengwang**
- **(frontend)** page padding and overflow for game edit and chall edit (#314) - ([39934f](https://github.com/GZTimeWalker/GZCTF/commit/39934f)) by **LilRan**

---
## [0.20.9](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.8-patch.2..v0.20.9) - 2024-06-30

### ⛰️ Features

- Remove redundant Telemetry.Enable config - ([66d337](https://github.com/GZTimeWalker/GZCTF/commit/66d337)) by **Steven He**

---
## [0.20.8-patch.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.8-patch.1..v0.20.8-patch.2) - 2024-06-23

### 🐛 Bug Fixes

- optimize ChallengeModal scroll area component (#308) - ([8f24da](https://github.com/GZTimeWalker/GZCTF/commit/8f24da)) by **AdBean**
- remove captcha key on boot (#306) - ([e66939](https://github.com/GZTimeWalker/GZCTF/commit/e66939)) by **GZTime**
- remove workaround for Prometheus - ([c779b1](https://github.com/GZTimeWalker/GZCTF/commit/c779b1)) by **Steven He**

---
## [0.20.8-patch.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.8..v0.20.8-patch.1) - 2024-06-14

### 🐛 Bug Fixes

- **(frontend)** overflow for challenge modal, again - ([cfe526](https://github.com/GZTimeWalker/GZCTF/commit/cfe526)) by **GZTime**

---
## [0.20.8](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.7-patch.1..v0.20.8) - 2024-06-13

### ⛰️ Features

- **(frontend)** upgrade marked to v13 with custom extension - ([43f908](https://github.com/GZTimeWalker/GZCTF/commit/43f908)) by **GZTime**

### 🐛 Bug Fixes

- **(frontend)** unexpected indentation in markdown render (#304) - ([0861e7](https://github.com/GZTimeWalker/GZCTF/commit/0861e7)) by **埃拉**
- **(game)** access to disabled challenges - ([440e15](https://github.com/GZTimeWalker/GZCTF/commit/440e15)) by **GZTime**
- overflow for some challenges - ([6b44ef](https://github.com/GZTimeWalker/GZCTF/commit/6b44ef)) by **GZTime**

---
## [0.20.7-patch.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.7..v0.20.7-patch.1) - 2024-06-08

### ⛰️ Features

- show `external link` for remote attachment - ([2082c5](https://github.com/GZTimeWalker/GZCTF/commit/2082c5)) by **GZTime**

---
## [0.20.7](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.6-patch.1..v0.20.7) - 2024-06-07

### ⛰️ Features

- **(frontend)** redesigned challenge modal - ([cbedf8](https://github.com/GZTimeWalker/GZCTF/commit/cbedf8)) by **GZTime**
- add more labels for container - ([817abd](https://github.com/GZTimeWalker/GZCTF/commit/817abd)) by **GZTime**

### 🐛 Bug Fixes

- **(frontend)** incorrect icon map - ([704d36](https://github.com/GZTimeWalker/GZCTF/commit/704d36)) by **GZTime**

---
## [0.20.6-patch.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.6..v0.20.6-patch.1) - 2024-06-03

### 🐛 Bug Fixes

- allow `blob:` for `img-src` - ([32a0d7](https://github.com/GZTimeWalker/GZCTF/commit/32a0d7)) by **GZTime**

---
## [0.20.6](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.5..v0.20.6) - 2024-06-02

### ⛰️ Features

- support docker basic auth - ([12418d](https://github.com/GZTimeWalker/GZCTF/commit/12418d)) by **GZTime**
- darken pdf in dark mode - ([418343](https://github.com/GZTimeWalker/GZCTF/commit/418343)) by **GZTime**

### 🎨 Styling

- challenge page - ([a8254e](https://github.com/GZTimeWalker/GZCTF/commit/a8254e)) by **GZTime**

---
## [0.20.5](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.4..v0.20.5) - 2024-05-22

### ⛰️ Features

- **(experimental)** use nonce for csp - ([c42255](https://github.com/GZTimeWalker/GZCTF/commit/c42255)) by **GZTime**
- **(security)** Initial content security policy - ([02d9e2](https://github.com/GZTimeWalker/GZCTF/commit/02d9e2)) by **GZTime**
- remove size limit of form and multipart-form - ([e1294e](https://github.com/GZTimeWalker/GZCTF/commit/e1294e)) by **Steven He**

---
## [0.20.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.3..v0.20.4) - 2024-05-17

### 🐛 Bug Fixes

- **(config)** cache not flush - ([811b73](https://github.com/GZTimeWalker/GZCTF/commit/811b73)) by **GZTime**
- **(style)** `LoadingOverlay` with wrong props - ([8ce788](https://github.com/GZTimeWalker/GZCTF/commit/8ce788)) by **GZTime**

### 🎨 Styling

- use primary color for Icon Tabs - ([0f9071](https://github.com/GZTimeWalker/GZCTF/commit/0f9071)) by **GZTime**

---
## [0.20.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.2..v0.20.3) - 2024-05-13

### ⛰️ Features

- **(experimental)** remove lock check for joining team - ([c9b814](https://github.com/GZTimeWalker/GZCTF/commit/c9b814)) by **GZTime**
- add `og:image` property to index - ([451328](https://github.com/GZTimeWalker/GZCTF/commit/451328)) by **GZTime**
- custom title & description for SEO - ([29bc1f](https://github.com/GZTimeWalker/GZCTF/commit/29bc1f)) by **GZTime**

### 🐛 Bug Fixes

- **(config)** flush index cache at launch - ([6894db](https://github.com/GZTimeWalker/GZCTF/commit/6894db)) by **GZTime**
- **(config)** cache won't flush when config add - ([0dab8f](https://github.com/GZTimeWalker/GZCTF/commit/0dab8f)) by **GZTime**
- **(frontend)** confirm modal after saving post - ([509c8a](https://github.com/GZTimeWalker/GZCTF/commit/509c8a)) by **GZTime**
- **(style)** name overflow on instances page - ([289922](https://github.com/GZTimeWalker/GZCTF/commit/289922)) by **GZTime**
- correctly join usernames (#294) - ([06229c](https://github.com/GZTimeWalker/GZCTF/commit/06229c)) by **Light**

---
## [0.20.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.1..v0.20.2) - 2024-05-12

### ⛰️ Features

- custom logo - ([26f774](https://github.com/GZTimeWalker/GZCTF/commit/26f774)) by **Steven He**

### 🐛 Bug Fixes

- **(style)** wrong light dark use for review page - ([7b8930](https://github.com/GZTimeWalker/GZCTF/commit/7b8930)) by **GZTime**
- **(style)** progress pulse as unexpected - ([728cff](https://github.com/GZTimeWalker/GZCTF/commit/728cff)) by **GZTime**
- **(style)** icon color - ([6171a8](https://github.com/GZTimeWalker/GZCTF/commit/6171a8)) by **GZTime**
- remove svg (again) from image mime types - ([df449b](https://github.com/GZTimeWalker/GZCTF/commit/df449b)) by **GZTime**

---
## [0.20.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.20.0..v0.20.1) - 2024-05-11

### ⛰️ Features

- **(i18n)** switch language on mobile - ([e6ff33](https://github.com/GZTimeWalker/GZCTF/commit/e6ff33)) by **GZTime**
- **(style)** use more css modules - ([04cd3e](https://github.com/GZTimeWalker/GZCTF/commit/04cd3e)) by **GZTime**
- control frontend color on client - ([fc3e8b](https://github.com/GZTimeWalker/GZCTF/commit/fc3e8b)) by **GZTime**
- custom theme color for frontend - ([d45f6d](https://github.com/GZTimeWalker/GZCTF/commit/d45f6d)) by **GZTime**
- add global `ErrorBoundary` - ([55e610](https://github.com/GZTimeWalker/GZCTF/commit/55e610)) by **GZTime**

### 🐛 Bug Fixes

- **(notification)** container destory notification - ([5448ad](https://github.com/GZTimeWalker/GZCTF/commit/5448ad)) by **GZTime**
- **(security)** html injection by team name - ([31e775](https://github.com/GZTimeWalker/GZCTF/commit/31e775)) by **GZTime**
- **(style)** tooltip & font size in navbar - ([7d2b2b](https://github.com/GZTimeWalker/GZCTF/commit/7d2b2b)) by **GZTime**
- **(style)** mobile post card - ([4f0a15](https://github.com/GZTimeWalker/GZCTF/commit/4f0a15)) by **GZTime**
- **(style)** pinned post card - ([d9b8b8](https://github.com/GZTimeWalker/GZCTF/commit/d9b8b8)) by **GZTime**
- **(style)** sticky header - ([d495c1](https://github.com/GZTimeWalker/GZCTF/commit/d495c1)) by **GZTime**
- **(style)** notifications never close - ([219c60](https://github.com/GZTimeWalker/GZCTF/commit/219c60)) by **GZTime**
- some global config will be clear when save color - ([c547bb](https://github.com/GZTimeWalker/GZCTF/commit/c547bb)) by **GZTime**
- custom theme cannot be set properly - ([725b3a](https://github.com/GZTimeWalker/GZCTF/commit/725b3a)) by **GZTime**

---
## [0.20.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.19.4..v0.20.0) - 2024-05-08

### ⛰️ Features

- **(client)** download blob with filename - ([ea373a](https://github.com/GZTimeWalker/GZCTF/commit/ea373a)) by **Aether Chen**
- **(deps)** upgrade to mantine v7 (#283) - ([a72e47](https://github.com/GZTimeWalker/GZCTF/commit/a72e47)) by **GZTime**
- unify team info query hook - ([c46544](https://github.com/GZTimeWalker/GZCTF/commit/c46544)) by **GZTime**
- update challenge accepted count - ([99650c](https://github.com/GZTimeWalker/GZCTF/commit/99650c)) by **GZTime**

### 🐛 Bug Fixes

- **(i18n)** set `lang` attr in `html` - ([9d8b39](https://github.com/GZTimeWalker/GZCTF/commit/9d8b39)) by **GZTime**
- **(style)** review page - ([d6bf73](https://github.com/GZTimeWalker/GZCTF/commit/d6bf73)) by **GZTime**
- **(style)** blinking underline - ([e88bc5](https://github.com/GZTimeWalker/GZCTF/commit/e88bc5)) by **GZTime**
- **(style)** some style issue - ([26ca2c](https://github.com/GZTimeWalker/GZCTF/commit/26ca2c)) by **GZTime**
- **(style)** footer color use css var - ([aafbf2](https://github.com/GZTimeWalker/GZCTF/commit/aafbf2)) by **GZTime**

### 🎨 Styling

- use tootip for blood legend - ([f05f13](https://github.com/GZTimeWalker/GZCTF/commit/f05f13)) by **GZTime**

---
## [0.19.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.19.3..v0.19.4) - 2024-05-03

### ⛰️ Features

- **(db)** update string length limits - ([7a73a1](https://github.com/GZTimeWalker/GZCTF/commit/7a73a1)) by **GZTime**
- **(deps)** use Ulid for key generation - ([bf2976](https://github.com/GZTimeWalker/GZCTF/commit/bf2976)) by **GZTime**
- **(game)** use 204 to indicate the game has ended - ([f3c59b](https://github.com/GZTimeWalker/GZCTF/commit/f3c59b)) by **GZTime**
- **(logs)** auto scroll to top & log filter - ([021a44](https://github.com/GZTimeWalker/GZCTF/commit/021a44)) by **GZTime**
- **(proxy)** update traffic naming format - ([6c1533](https://github.com/GZTimeWalker/GZCTF/commit/6c1533)) by **GZTime**
- **(review)** enhance team review page - ([cbb052](https://github.com/GZTimeWalker/GZCTF/commit/cbb052)) by **GZTime**
- **(traffic)** sort challenge & teams - ([fe867b](https://github.com/GZTimeWalker/GZCTF/commit/fe867b)) by **GZTime**
- **(traffic)** total size & no overflow - ([540793](https://github.com/GZTimeWalker/GZCTF/commit/540793)) by **GZTime**
- **(traffic)** traffic file deletion - ([8374f0](https://github.com/GZTimeWalker/GZCTF/commit/8374f0)) by **GZTime**
- use custom error codes in status - ([92999d](https://github.com/GZTimeWalker/GZCTF/commit/92999d)) by **GZTime**
- capture traffic only when the game is active - ([46df11](https://github.com/GZTimeWalker/GZCTF/commit/46df11)) by **GZTime**
- remove user role requirement for game notice signalr hub - ([108d75](https://github.com/GZTimeWalker/GZCTF/commit/108d75)) by **GZTime**

### 🐛 Bug Fixes

- **(style)** index page on widescreen - ([c8ce97](https://github.com/GZTimeWalker/GZCTF/commit/c8ce97)) by **GZTime**
- **(style)** game challenge page - ([f8a62a](https://github.com/GZTimeWalker/GZCTF/commit/f8a62a)) by **GZTime**
- **(traffic)** make deletion works as expected - ([c08607](https://github.com/GZTimeWalker/GZCTF/commit/c08607)) by **GZTime**
- do not use Ulid for containers - ([bf4ad1](https://github.com/GZTimeWalker/GZCTF/commit/bf4ad1)) by **GZTime**
- check isEnabled when creating the container - ([de747e](https://github.com/GZTimeWalker/GZCTF/commit/de747e)) by **GZTime**
- handle deletion exceptions - ([d62554](https://github.com/GZTimeWalker/GZCTF/commit/d62554)) by **GZTime**

---
## [0.19.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.19.2..v0.19.3) - 2024-04-22

### 🐛 Bug Fixes

- **(excel)** deal with `Single` exceptions - ([5947ee](https://github.com/GZTimeWalker/GZCTF/commit/5947ee)) by **GZTime**
- **(frontend)** unable to renew as expected - ([daa9a5](https://github.com/GZTimeWalker/GZCTF/commit/daa9a5)) by **GZTime**
- **(mail)** DO NOT use `IStringLocalizer` after construction - ([fe589a](https://github.com/GZTimeWalker/GZCTF/commit/fe589a)) by **GZTime**

---
## [0.19.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.19.1..v0.19.2) - 2024-04-21

### 🐛 Bug Fixes

- **(backedn)** add NPOI lib - ([c4b20e](https://github.com/GZTimeWalker/GZCTF/commit/c4b20e)) by **GZTime**
- **(deps)** route not works - ([b37720](https://github.com/GZTimeWalker/GZCTF/commit/b37720)) by **GZTime**

---
## [0.19.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.19.0..v0.19.1) - 2024-04-17

### 🐛 Bug Fixes

- **(style)** member info is not wide enough - ([399fd8](https://github.com/GZTimeWalker/GZCTF/commit/399fd8)) by **GZTime**
- Override system default cipher list - ([240dfb](https://github.com/GZTimeWalker/GZCTF/commit/240dfb)) by **Steven He**

---
## [0.19.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.18.5..v0.19.0) - 2024-04-16

### ⛰️ Features

- Allow customize total suffix for Prometheus - ([468b47](https://github.com/GZTimeWalker/GZCTF/commit/468b47)) by **Steven He**
- Add version info to opentelemetry - ([e24950](https://github.com/GZTimeWalker/GZCTF/commit/e24950)) by **Steven He**
- Map LogLevel to LogEventLevel - ([644eff](https://github.com/GZTimeWalker/GZCTF/commit/644eff)) by **Steven He**
- Loki logging server support - ([54aa5c](https://github.com/GZTimeWalker/GZCTF/commit/54aa5c)) by **Steven He**
- Enable dynamic adaptive GC - ([18567d](https://github.com/GZTimeWalker/GZCTF/commit/18567d)) by **Steve**
- config cache & client message - ([c17708](https://github.com/GZTimeWalker/GZCTF/commit/c17708)) by **GZTime**
- adding more instruments - ([91691e](https://github.com/GZTimeWalker/GZCTF/commit/91691e)) by **Steven He**
- custom Prometheus settings - ([f06a09](https://github.com/GZTimeWalker/GZCTF/commit/f06a09)) by **Steven He**
- control enabling of telemetry - ([93b00d](https://github.com/GZTimeWalker/GZCTF/commit/93b00d)) by **Steven He**
- metrics and distributed tracing - ([4d807c](https://github.com/GZTimeWalker/GZCTF/commit/4d807c)) by **Steven He**

### 🐛 Bug Fixes

- **(frontend)** disable when update - ([a09af6](https://github.com/GZTimeWalker/GZCTF/commit/a09af6)) by **GZTime**
- **(i18n)** missing translate - ([2fe800](https://github.com/GZTimeWalker/GZCTF/commit/2fe800)) by **GZTime**
- **(style)** team unlock badge - ([e3a697](https://github.com/GZTimeWalker/GZCTF/commit/e3a697)) by **GZTime**
- add ca-certificates for alpine (#269) - ([99d4ca](https://github.com/GZTimeWalker/GZCTF/commit/99d4ca)) by **GZTime**
- Stack overflow while disposing stream - ([9a0a87](https://github.com/GZTimeWalker/GZCTF/commit/9a0a87)) by **Steven He**
- Run Tasks with LongRunning options - ([bb8815](https://github.com/GZTimeWalker/GZCTF/commit/bb8815)) by **Steven He**
- Properly dispose stream - ([4dfd1d](https://github.com/GZTimeWalker/GZCTF/commit/4dfd1d)) by **Steven He**
- Exclude any exception due to cancellation - ([3d6dab](https://github.com/GZTimeWalker/GZCTF/commit/3d6dab)) by **Steven He**
- Exclude /metrics from log - ([be3fa7](https://github.com/GZTimeWalker/GZCTF/commit/be3fa7)) by **Steven He**
- prometheus endpoint mapping - ([8eabce](https://github.com/GZTimeWalker/GZCTF/commit/8eabce)) by **Steven He**
- Add workaround for prometheus - ([5364a8](https://github.com/GZTimeWalker/GZCTF/commit/5364a8)) by **Steven He**
- GlobalConfig scope - ([d1f5c5](https://github.com/GZTimeWalker/GZCTF/commit/d1f5c5)) by **Steven He**
- Make MailSender singleton - ([133284](https://github.com/GZTimeWalker/GZCTF/commit/133284)) by **Steven He**
- Refactor MailSender to use a queue - ([b72f99](https://github.com/GZTimeWalker/GZCTF/commit/b72f99)) by **Steven He**
- Missing args in team localization - ([9913f3](https://github.com/GZTimeWalker/GZCTF/commit/9913f3)) by **Steven He**
- `ClientCaptchaInfoModel` is not MemoryPackable - ([a9c3d0](https://github.com/GZTimeWalker/GZCTF/commit/a9c3d0)) by **GZTime**
- `ClientConfig` is not registered for MemoryPack - ([7fed35](https://github.com/GZTimeWalker/GZCTF/commit/7fed35)) by **GZTime**
- disable `AutomountServiceAccountToken` for pods - ([1e139f](https://github.com/GZTimeWalker/GZCTF/commit/1e139f)) by **GZTime**

### 🎨 Styling

- fix Badge padding - ([6e5e53](https://github.com/GZTimeWalker/GZCTF/commit/6e5e53)) by **GZTime**

### 📦 Other Changes

- use alpine as base (#268) - ([4810c0](https://github.com/GZTimeWalker/GZCTF/commit/4810c0)) by **GZTime**

---
## [0.18.5](https://github.com/GZTimeWalker/GZCTF/compare/v0.18.4..v0.18.5) - 2024-04-01

### ⛰️ Features

- custom container lifetime - ([906598](https://github.com/GZTimeWalker/GZCTF/commit/906598)) by **GZTime**
- check that the data path is configured correctly - ([26cfac](https://github.com/GZTimeWalker/GZCTF/commit/26cfac)) by **GZTime**

### 🐛 Bug Fixes

- **(docs)** cannot get theme config - ([40ae61](https://github.com/GZTimeWalker/GZCTF/commit/40ae61)) by **GZTime**
- write test file to base - ([0810ef](https://github.com/GZTimeWalker/GZCTF/commit/0810ef)) by **GZTime**
- disable experimental features - ([204551](https://github.com/GZTimeWalker/GZCTF/commit/204551)) by **Steven He**
- write version instead - ([314cd1](https://github.com/GZTimeWalker/GZCTF/commit/314cd1)) by **Steven He**
- rw check and fix service init orders - ([93dfdc](https://github.com/GZTimeWalker/GZCTF/commit/93dfdc)) by **Steven He**

---
## [0.18.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.18.3..v0.18.4) - 2024-04-01

### ⛰️ Features

- use Dictionary for scoreboard item cache - ([332356](https://github.com/GZTimeWalker/GZCTF/commit/332356)) by **GZTime**
- enhance searching and use string.Contains - ([43d215](https://github.com/GZTimeWalker/GZCTF/commit/43d215)) by **Steven He**

### 🐛 Bug Fixes

- response message & try get value - ([133c3d](https://github.com/GZTimeWalker/GZCTF/commit/133c3d)) by **GZTime**
- cidr and dns config not being overwritten - ([37d9cb](https://github.com/GZTimeWalker/GZCTF/commit/37d9cb)) by **Steven He**

### 🎨 Styling

- pointer cursor when hover to link - ([7928fb](https://github.com/GZTimeWalker/GZCTF/commit/7928fb)) by **GZTime**

---
## [0.18.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.18.2..v0.18.3) - 2024-03-25

### ⛰️ Features

- **(tag)** add tag for AI - ([73831d](https://github.com/GZTimeWalker/GZCTF/commit/73831d)) by **GZTime**

---
## [0.18.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.18.1..v0.18.2) - 2024-03-20

### 🐛 Bug Fixes

- **(post)** i18n without value - ([12c7bb](https://github.com/GZTimeWalker/GZCTF/commit/12c7bb)) by **GZTime**
- **(proxy)** can not proxy with test container - ([ea4bcd](https://github.com/GZTimeWalker/GZCTF/commit/ea4bcd)) by **GZTime**
- remove user participation when leaving team (#250) - ([8822e6](https://github.com/GZTimeWalker/GZCTF/commit/8822e6)) by **Kengwang**
- model validation attribute need ErrorMessageResourceType - ([383d03](https://github.com/GZTimeWalker/GZCTF/commit/383d03)) by **GZTime**
- Error Handling (#246) - ([3c6968](https://github.com/GZTimeWalker/GZCTF/commit/3c6968)) by **Kengwang**

### 🎨 Styling

- add missing `noWrap` for some titles - ([ec19c5](https://github.com/GZTimeWalker/GZCTF/commit/ec19c5)) by **GZTime**

---
## [0.18.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.18.0..v0.18.1) - 2024-03-03

### ⛰️ Features

- **(mail)** add platform name - ([dd0c61](https://github.com/GZTimeWalker/GZCTF/commit/dd0c61)) by **GZTime**

### 🐛 Bug Fixes

- **(i18n)** convertLanguage - ([6b4f3a](https://github.com/GZTimeWalker/GZCTF/commit/6b4f3a)) by **GZTime**
- i18n language detection - ([f1fed1](https://github.com/GZTimeWalker/GZCTF/commit/f1fed1)) by **Steven He**
- wrong i18n var key - ([a9a0bf](https://github.com/GZTimeWalker/GZCTF/commit/a9a0bf)) by **GZTime**

---
## [0.18.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.18.0-preview.1..v0.18.0) - 2024-02-26

### ⛰️ Features

- handle blob download - ([f9635b](https://github.com/GZTimeWalker/GZCTF/commit/f9635b)) by **GZTime**
- allow all chars in username - ([5187af](https://github.com/GZTimeWalker/GZCTF/commit/5187af)) by **GZTime**
- std id can be text - ([ddf9ea](https://github.com/GZTimeWalker/GZCTF/commit/ddf9ea)) by **GZTime**

### 🐛 Bug Fixes

- **(i18n)** key `flag.add` not found - ([ccea30](https://github.com/GZTimeWalker/GZCTF/commit/ccea30)) by **GZTime**
- typo - ([be7d6a](https://github.com/GZTimeWalker/GZCTF/commit/be7d6a)) by **YanWQ-monad**
- add feedback for api downloading - ([1bbdde](https://github.com/GZTimeWalker/GZCTF/commit/1bbdde)) by **Aether Chen**
- use Api.ts to download sheets - ([39e517](https://github.com/GZTimeWalker/GZCTF/commit/39e517)) by **Aether Chen**
- set default language - ([83d4a6](https://github.com/GZTimeWalker/GZCTF/commit/83d4a6)) by **KpwnZ**
- Add ExcelHelper to scoped service - ([5bf2b7](https://github.com/GZTimeWalker/GZCTF/commit/5bf2b7)) by **Steven He**
- log message in signalr - ([72459e](https://github.com/GZTimeWalker/GZCTF/commit/72459e)) by **GZTime**
- remove unnecessary value - ([b5405a](https://github.com/GZTimeWalker/GZCTF/commit/b5405a)) by **GZTime**
- remove `Model_PasswordTooShort` - ([eb5197](https://github.com/GZTimeWalker/GZCTF/commit/eb5197)) by **GZTime**
- wrong key for `lock` in team - ([24646c](https://github.com/GZTimeWalker/GZCTF/commit/24646c)) by **GZTime**

---
## [0.18.0-preview.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.18.0-preview.0..v0.18.0-preview.1) - 2024-02-10

### 🐛 Bug Fixes

- cannot pin post - ([a41e94](https://github.com/GZTimeWalker/GZCTF/commit/a41e94)) by **GZTime**

### 🎨 Styling

- update challenge edit page & detail model - ([fa3e6e](https://github.com/GZTimeWalker/GZCTF/commit/fa3e6e)) by **GZTime**

### 📦 Other Changes

- update `metadata-action` - ([5a0158](https://github.com/GZTimeWalker/GZCTF/commit/5a0158)) by **GZTime**

---
## [0.18.0-preview.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.17.7..v0.18.0-preview.0) - 2024-02-09

### ⛰️ Features

- **(api)** replace `prolong` with `extend` - ([f8535f](https://github.com/GZTimeWalker/GZCTF/commit/f8535f)) by **GZTime**
- **(api)** signature verify api - ([8c2706](https://github.com/GZTimeWalker/GZCTF/commit/8c2706)) by **GZTime**
- allow to configure default culture of server - ([962040](https://github.com/GZTimeWalker/GZCTF/commit/962040)) by **Steven He**
- language switch - ([14f3b9](https://github.com/GZTimeWalker/GZCTF/commit/14f3b9)) by **GZTime**
- mark exercise mode as todo - ([454cf0](https://github.com/GZTimeWalker/GZCTF/commit/454cf0)) by **GZTime**
- use css with console log - ([7a771a](https://github.com/GZTimeWalker/GZCTF/commit/7a771a)) by **GZTime**
- pdf viewer with error boundary - ([b2a17f](https://github.com/GZTimeWalker/GZCTF/commit/b2a17f)) by **GZTime**
- make commit writeup optional - ([0d679e](https://github.com/GZTimeWalker/GZCTF/commit/0d679e)) by **GZTime**
- use `Task.Factory.StartNew` - ([23f1d3](https://github.com/GZTimeWalker/GZCTF/commit/23f1d3)) by **GZTime**

### 🐛 Bug Fixes

- **(api)** response type declaration - ([a0c4cd](https://github.com/GZTimeWalker/GZCTF/commit/a0c4cd)) by **GZTime**
- **(logger)** update database sink - ([12ea93](https://github.com/GZTimeWalker/GZCTF/commit/12ea93)) by **GZTime**
- **(style)** button width - ([3b28bc](https://github.com/GZTimeWalker/GZCTF/commit/3b28bc)) by **GZTime**
- **(style)** game tab list - ([58b526](https://github.com/GZTimeWalker/GZCTF/commit/58b526)) by **GZTime**
- **(version)** use `0.18.0.0` instead of semver - ([52330d](https://github.com/GZTimeWalker/GZCTF/commit/52330d)) by **GZTime**
- hard-coded key on clearLocalCache - ([1b4e2c](https://github.com/GZTimeWalker/GZCTF/commit/1b4e2c)) by **Aether Chen**
- local cache - ([2fb632](https://github.com/GZTimeWalker/GZCTF/commit/2fb632)) by **GZTime**
- challenge placeholder - ([066b32](https://github.com/GZTimeWalker/GZCTF/commit/066b32)) by **GZTime**
- remove id for notices - ([74af46](https://github.com/GZTimeWalker/GZCTF/commit/74af46)) by **GZTime**
- do not store enum in log property - ([58bacc](https://github.com/GZTimeWalker/GZCTF/commit/58bacc)) by **GZTime**
- notice editor - ([a6c2f6](https://github.com/GZTimeWalker/GZCTF/commit/a6c2f6)) by **GZTime**
- add `Bind` for kestrel config - ([0661db](https://github.com/GZTimeWalker/GZCTF/commit/0661db)) by **GZTime**
- use pattern matching to get raw sting value - ([c93471](https://github.com/GZTimeWalker/GZCTF/commit/c93471)) by **GZTime**
- use `LogContext.PushProperty` - ([b640ef](https://github.com/GZTimeWalker/GZCTF/commit/b640ef)) by **GZTime**
- use current culture - ([a873e1](https://github.com/GZTimeWalker/GZCTF/commit/a873e1)) by **GZTime**
- use correct localizer - ([95ecea](https://github.com/GZTimeWalker/GZCTF/commit/95ecea)) by **Steven He**
- logging without properties - ([3b3f3d](https://github.com/GZTimeWalker/GZCTF/commit/3b3f3d)) by **GZTime**
- i18n consistency - ([42ee0c](https://github.com/GZTimeWalker/GZCTF/commit/42ee0c)) by **Steven He**
- `Account_AvatarUpdated` unused template - ([9e3bae](https://github.com/GZTimeWalker/GZCTF/commit/9e3bae)) by **GZTime**
- mobile scoreboard style - ([e9f1bd](https://github.com/GZTimeWalker/GZCTF/commit/e9f1bd)) by **GZTime**
- container model - ([004f83](https://github.com/GZTimeWalker/GZCTF/commit/004f83)) by **GZTime**
- use `System.Uri` to parse host from k8s config (#214) - ([69e474](https://github.com/GZTimeWalker/GZCTF/commit/69e474)) by **kdxcxs**
- dot not get initial value in effect - ([da418b](https://github.com/GZTimeWalker/GZCTF/commit/da418b)) by **GZTime**
- nullable for flag context foreign key - ([f7d6f2](https://github.com/GZTimeWalker/GZCTF/commit/f7d6f2)) by **GZTime**
- std number - ([1e25cb](https://github.com/GZTimeWalker/GZCTF/commit/1e25cb)) by **GZTime**
- disabled button when create team (#192) - ([95fb32](https://github.com/GZTimeWalker/GZCTF/commit/95fb32)) by **GZTime**

### 📦 Other Changes

- **(feat)** database sink - ([22cf84](https://github.com/GZTimeWalker/GZCTF/commit/22cf84)) by **GZTime**
- localized request template - ([9516c6](https://github.com/GZTimeWalker/GZCTF/commit/9516c6)) by **Steven He**
- source template fixes - ([3817f7](https://github.com/GZTimeWalker/GZCTF/commit/3817f7)) by **Steven He**
- updated Japanese changelog - ([2f4979](https://github.com/GZTimeWalker/GZCTF/commit/2f4979)) by **Steven He**
- Specify zh in docs url for README.zh.md - ([a50858](https://github.com/GZTimeWalker/GZCTF/commit/a50858)) by **Steven He**
- various fixes - ([ab5cd5](https://github.com/GZTimeWalker/GZCTF/commit/ab5cd5)) by **Steven He**
- update Japanese translations - ([53b830](https://github.com/GZTimeWalker/GZCTF/commit/53b830)) by **Steven He**
- correct leet translation - ([1af1f4](https://github.com/GZTimeWalker/GZCTF/commit/1af1f4)) by **Steven He**
- add i18n support for theme config - ([c002d0](https://github.com/GZTimeWalker/GZCTF/commit/c002d0)) by **Steven He**
- adding ja to locales config - ([561be8](https://github.com/GZTimeWalker/GZCTF/commit/561be8)) by **Steven He**
- adding Japanese docs - ([643855](https://github.com/GZTimeWalker/GZCTF/commit/643855)) by **Steven He**
- i18n for account pages - ([a575f9](https://github.com/GZTimeWalker/GZCTF/commit/a575f9)) by **GZTime**
- make some type error - ([0eedda](https://github.com/GZTimeWalker/GZCTF/commit/0eedda)) by **Aether Chen**
- add more useTranslation() - ([4d0670](https://github.com/GZTimeWalker/GZCTF/commit/4d0670)) by **Aether Chen**
- add i18n.config.js - ([f4f5ba](https://github.com/GZTimeWalker/GZCTF/commit/f4f5ba)) by **Aether Chen**
- update codeql - ([aedd9e](https://github.com/GZTimeWalker/GZCTF/commit/aedd9e)) by **GZTime**

---
## [0.17.7](https://github.com/GZTimeWalker/GZCTF/compare/v0.17.6..v0.17.7) - 2023-12-29

### ⛰️ Features

- database sink - ([aba911](https://github.com/GZTimeWalker/GZCTF/commit/aba911)) by **GZTime**
- expose Kestrel options in appsettings.json (#174) - ([aa2e4a](https://github.com/GZTimeWalker/GZCTF/commit/aa2e4a)) by **GZTime**
- expose Kestrel options in appsettings.json (#174) - ([dfb614](https://github.com/GZTimeWalker/GZCTF/commit/dfb614)) by **Steve**
- expose Kestrel options in appsettings.json (#163) - ([3a19ec](https://github.com/GZTimeWalker/GZCTF/commit/3a19ec)) by **Steven He**
- Crowdin translation updates (#157) - ([d5134a](https://github.com/GZTimeWalker/GZCTF/commit/d5134a)) by **GZTime**
- email template backend - ([aee8dd](https://github.com/GZTimeWalker/GZCTF/commit/aee8dd)) by **GZTime**
- always store file to fs - ([3f1132](https://github.com/GZTimeWalker/GZCTF/commit/3f1132)) by **GZTime**

### 🐛 Bug Fixes

- Incorrect last flush time - ([7662e6](https://github.com/GZTimeWalker/GZCTF/commit/7662e6)) by **GZTime**
- avoid logging injection - ([d8528b](https://github.com/GZTimeWalker/GZCTF/commit/d8528b)) by **GZTime**
- avoid logging injection - ([81c1e7](https://github.com/GZTimeWalker/GZCTF/commit/81c1e7)) by **GZTime**
- remove placeholder for PickerInput - ([c2d53b](https://github.com/GZTimeWalker/GZCTF/commit/c2d53b)) by **GZTime**
- verify email domain in ChangeEmail as well (#175) - ([93c7be](https://github.com/GZTimeWalker/GZCTF/commit/93c7be)) by **Steve**
- verify email domain in ChangeEmail as well (#167) - ([9415a5](https://github.com/GZTimeWalker/GZCTF/commit/9415a5)) by **Steven He**
- use correct localizer - ([a59bd7](https://github.com/GZTimeWalker/GZCTF/commit/a59bd7)) by **Steven He**
- use correct name for package versions - ([4c5a70](https://github.com/GZTimeWalker/GZCTF/commit/4c5a70)) by **Steven He**
- deprecate inappropriate characters - ([934499](https://github.com/GZTimeWalker/GZCTF/commit/934499)) by **GZTime**

### 📦 Other Changes

- develop into feat/exercise - ([d3daba](https://github.com/GZTimeWalker/GZCTF/commit/d3daba)) by **GZTime**

---
## [0.17.6](https://github.com/GZTimeWalker/GZCTF/compare/v0.17.5..v0.17.6) - 2023-09-29

### 🐛 Bug Fixes

- game starts 1 minute later (#154) - ([77d1d2](https://github.com/GZTimeWalker/GZCTF/commit/77d1d2)) by **Monad**

---
## [0.17.5](https://github.com/GZTimeWalker/GZCTF/compare/v0.17.4..v0.17.5) - 2023-09-24

### 🐛 Bug Fixes

- cannot list captured traffic - ([43961e](https://github.com/GZTimeWalker/GZCTF/commit/43961e)) by **GZTime**
- cannot enable traffic capture for static container challenge - ([dc8972](https://github.com/GZTimeWalker/GZCTF/commit/dc8972)) by **GZTime**
- cannot empty post summary & content - ([3feb43](https://github.com/GZTimeWalker/GZCTF/commit/3feb43)) by **GZTime**

---
## [0.17.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.17.3..v0.17.4) - 2023-09-22

### 🐛 Bug Fixes

- can not empty post tags (#150) - ([d0b286](https://github.com/GZTimeWalker/GZCTF/commit/d0b286)) by **GZTime**

---
## [0.17.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.17.2..v0.17.3) - 2023-09-19

### 🐛 Bug Fixes

- **(api)** cannot delete post - ([0cb492](https://github.com/GZTimeWalker/GZCTF/commit/0cb492)) by **GZTime**

---
## [0.17.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.17.1..v0.17.2) - 2023-09-16

### 🐛 Bug Fixes

- api type error - ([e61586](https://github.com/GZTimeWalker/GZCTF/commit/e61586)) by **GZTime**

---
## [0.17.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.17.0..v0.17.1) - 2023-09-02

### ⛰️ Features

- **(captcha)** use component for Captcha - ([13d6d3](https://github.com/GZTimeWalker/GZCTF/commit/13d6d3)) by **GZTime**
- **(captcha)** add Turnstile - ([ccf64b](https://github.com/GZTimeWalker/GZCTF/commit/ccf64b)) by **GZTime**
- update UX for GameJoinModal (#137) - ([2df3b6](https://github.com/GZTimeWalker/GZCTF/commit/2df3b6)) by **Johnny Hsieh**

### 🐛 Bug Fixes

- **(captcha)** pass deps to useImperativeHandle - ([866f2e](https://github.com/GZTimeWalker/GZCTF/commit/866f2e)) by **GZTime**
- **(captcha)** add action param - ([05adda](https://github.com/GZTimeWalker/GZCTF/commit/05adda)) by **GZTime**
- **(frontend)** label for useCaptcha - ([83e7e9](https://github.com/GZTimeWalker/GZCTF/commit/83e7e9)) by **GZTime**
- do not log SocketException - ([dbd8bc](https://github.com/GZTimeWalker/GZCTF/commit/dbd8bc)) by **GZTime**

### 🎨 Styling

- **(code)** use primary constructors - ([dfd0a4](https://github.com/GZTimeWalker/GZCTF/commit/dfd0a4)) by **GZTime**

---
## [0.17.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.16.0..v0.17.0) - 2023-08-22

### ⛰️ Features

- **(docker)** add network name - ([1fd408](https://github.com/GZTimeWalker/GZCTF/commit/1fd408)) by **GZTime**
- **(frontend)** update for instance entry - ([531887](https://github.com/GZTimeWalker/GZCTF/commit/531887)) by **GZTime**
- **(proxy)** init controller - ([df1675](https://github.com/GZTimeWalker/GZCTF/commit/df1675)) by **GZTime**
- **(v0.17.0)** Use PlatformProxy for private network accessing & traffic capturing (#134) - ([f66115](https://github.com/GZTimeWalker/GZCTF/commit/f66115)) by **GZTime**
- **(watermark)** enhance watermark - ([b8c926](https://github.com/GZTimeWalker/GZCTF/commit/b8c926)) by **GZTime**
- add health check - ([2e270f](https://github.com/GZTimeWalker/GZCTF/commit/2e270f)) by **GZTime**

### 🐛 Bug Fixes

- **(capture)** use slice for buffer - ([4c7ce1](https://github.com/GZTimeWalker/GZCTF/commit/4c7ce1)) by **GZTime**
- **(code style)** csharp naming convention - ([d99786](https://github.com/GZTimeWalker/GZCTF/commit/d99786)) by **GZTime**
- **(docker)** libpcap -> libpcap0.8 - ([21dc62](https://github.com/GZTimeWalker/GZCTF/commit/21dc62)) by **GZTime**
- **(docker)** add libpcap - ([5d9014](https://github.com/GZTimeWalker/GZCTF/commit/5d9014)) by **GZTime**
- **(docker)** can not get container ip - ([9d13e1](https://github.com/GZTimeWalker/GZCTF/commit/9d13e1)) by **GZTime**
- **(frontend)** sort announcements by time (#133) - ([57c192](https://github.com/GZTimeWalker/GZCTF/commit/57c192)) by **Monad**
- **(frontend)** use `total` to control multi-page selection - ([3acfd8](https://github.com/GZTimeWalker/GZCTF/commit/3acfd8)) by **GZTime**
- **(frontend)** typos & unexpected milliseconds - ([7209c6](https://github.com/GZTimeWalker/GZCTF/commit/7209c6)) by **GZTime**
- **(style)** add page padding for scoreboard - ([f1f3b6](https://github.com/GZTimeWalker/GZCTF/commit/f1f3b6)) by **GZTime**
- can not set EnableTrafficCapture - ([e0f369](https://github.com/GZTimeWalker/GZCTF/commit/e0f369)) by **GZTime**
- set correct width for flag input - ([3405b7](https://github.com/GZTimeWalker/GZCTF/commit/3405b7)) by **Aether Chen**
- hover style when active - ([13cf62](https://github.com/GZTimeWalker/GZCTF/commit/13cf62)) by **GZTime**
- instance entry open target - ([41efbb](https://github.com/GZTimeWalker/GZCTF/commit/41efbb)) by **GZTime**
- traffic api - ([97e3cc](https://github.com/GZTimeWalker/GZCTF/commit/97e3cc)) by **GZTime**
- use participation id as the model id - ([49200e](https://github.com/GZTimeWalker/GZCTF/commit/49200e)) by **GZTime**
- getProxyUrl - ([032759](https://github.com/GZTimeWalker/GZCTF/commit/032759)) by **GZTime**
- traffic capture - ([56c077](https://github.com/GZTimeWalker/GZCTF/commit/56c077)) by **GZTime**
- cannot upload file - ([3f5f96](https://github.com/GZTimeWalker/GZCTF/commit/3f5f96)) by **GZTime**
- independent cache helper from repository - ([785c01](https://github.com/GZTimeWalker/GZCTF/commit/785c01)) by **GZTime**
- remove localfiles when remove games - ([47b84a](https://github.com/GZTimeWalker/GZCTF/commit/47b84a)) by **GZTime**
- wrong API type declaration - ([1dc048](https://github.com/GZTimeWalker/GZCTF/commit/1dc048)) by **GZTime**
- remove connection cache when remove a container - ([c12609](https://github.com/GZTimeWalker/GZCTF/commit/c12609)) by **GZTime**

### 🎨 Styling

- use if-return pattern - ([9944be](https://github.com/GZTimeWalker/GZCTF/commit/9944be)) by **GZTime**

### 📦 Other Changes

- ignore healthz & http 204 request - ([97059b](https://github.com/GZTimeWalker/GZCTF/commit/97059b)) by **GZTime**
- add metadata packet - ([73cdc9](https://github.com/GZTimeWalker/GZCTF/commit/73cdc9)) by **GZTime**
- add editDeleteGameWriteUps - ([1aef26](https://github.com/GZTimeWalker/GZCTF/commit/1aef26)) by **GZTime**

---
## [0.16.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.15.7..v0.16.0) - 2023-08-11

### 🐛 Bug Fixes

- **(frontend)** wpddl may be initialized with a negative number - ([8d489f](https://github.com/GZTimeWalker/GZCTF/commit/8d489f)) by **GZTime**
- **(k8s)** add ToValidRFC1123String - ([d6d6f0](https://github.com/GZTimeWalker/GZCTF/commit/d6d6f0)) by **GZTime**
- **(k8s)** use JsonSerializer for auth string - ([49b912](https://github.com/GZTimeWalker/GZCTF/commit/49b912)) by **GZTime**
- **(verify)** click is required for account verifying - ([1af7dd](https://github.com/GZTimeWalker/GZCTF/commit/1af7dd)) by **GZTime**

---
## [0.15.7](https://github.com/GZTimeWalker/GZCTF/compare/v0.15.6..v0.15.7) - 2023-08-07

### ⛰️ Features

- **(frontend)** default scale to the latest 7 days for long game - ([989ac3](https://github.com/GZTimeWalker/GZCTF/commit/989ac3)) by **GZTime**
- **(frontend)** add katex render for inline markdown - ([9d6e7a](https://github.com/GZTimeWalker/GZCTF/commit/9d6e7a)) by **GZTime**
- **(frontend)** add inline markdown render for hints and notices - ([9f7545](https://github.com/GZTimeWalker/GZCTF/commit/9f7545)) by **GZTime**
- **(proxy)** add ForwardedOptions - ([7e8eb4](https://github.com/GZTimeWalker/GZCTF/commit/7e8eb4)) by **GZTime**
- **(proxy)** config for ForwardedHeadersOptions - ([31413c](https://github.com/GZTimeWalker/GZCTF/commit/31413c)) by **GZTime**
- **(proxy)** use XFF middleware in aspnetcore - ([f45601](https://github.com/GZTimeWalker/GZCTF/commit/f45601)) by **GZTime**

### 🐛 Bug Fixes

- **(log)** use TryParse - ([604dd4](https://github.com/GZTimeWalker/GZCTF/commit/604dd4)) by **GZTime**
- **(style)** set withinPortal to true by default for Popover - ([ee4afb](https://github.com/GZTimeWalker/GZCTF/commit/ee4afb)) by **GZTime**
- **(style)** flag icon in challenge cards are not unified - ([e92694](https://github.com/GZTimeWalker/GZCTF/commit/e92694)) by **GZTime**
- **(style)** no wrap for ChallengeCard - ([386585](https://github.com/GZTimeWalker/GZCTF/commit/386585)) by **GZTime**
- **(teams)** mutate teams - ([f9283c](https://github.com/GZTimeWalker/GZCTF/commit/f9283c)) by **GZTime**

---
## [0.15.6](https://github.com/GZTimeWalker/GZCTF/compare/v0.15.5..v0.15.6) - 2023-08-03

### ⛰️ Features

- **(footer)** add footer markdown render - ([ff572f](https://github.com/GZTimeWalker/GZCTF/commit/ff572f)) by **GZTime**

### 🐛 Bug Fixes

- **(frontend)** ParticipationStatusControl - ([84ba4a](https://github.com/GZTimeWalker/GZCTF/commit/84ba4a)) by **GZTime**

---
## [0.15.5](https://github.com/GZTimeWalker/GZCTF/compare/v0.15.4..v0.15.5) - 2023-08-02

### ⛰️ Features

- **(config)** add footer & beian - ([39c4ae](https://github.com/GZTimeWalker/GZCTF/commit/39c4ae)) by **GZTime**

### 🐛 Bug Fixes

- **(feat)** not allow user to modify email with EmailConfirmationRequired - ([908c5a](https://github.com/GZTimeWalker/GZCTF/commit/908c5a)) by **GZTime**
- **(frontend)** only add padding with footer - ([8053ed](https://github.com/GZTimeWalker/GZCTF/commit/8053ed)) by **GZTime**
- **(frontend)** add padding for index - ([883d3d](https://github.com/GZTimeWalker/GZCTF/commit/883d3d)) by **GZTime**
- remove padding prop - ([4811e8](https://github.com/GZTimeWalker/GZCTF/commit/4811e8)) by **GZTime**

---
## [0.15.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.15.3..v0.15.4) - 2023-07-27

### ⛰️ Features

- **(config)** custom k8s config - ([31e177](https://github.com/GZTimeWalker/GZCTF/commit/31e177)) by **GZTime**

### 🐛 Bug Fixes

- **(bug)** no instance port when use docker - ([67903b](https://github.com/GZTimeWalker/GZCTF/commit/67903b)) by **GZTime**
- **(bug)** [GUID] in flag template - ([affebc](https://github.com/GZTimeWalker/GZCTF/commit/affebc)) by **GZTime**
- **(k8s)** fix registry auth - ([f9e256](https://github.com/GZTimeWalker/GZCTF/commit/f9e256)) by **GZTime**

---
## [0.15.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.15.2..v0.15.3) - 2023-07-18

### ⛰️ Features

- **(frontend)** update api client - ([ed340b](https://github.com/GZTimeWalker/GZCTF/commit/ed340b)) by **GZTime**

### 🐛 Bug Fixes

- **(bug)** cannot save challenge info - ([4afe8d](https://github.com/GZTimeWalker/GZCTF/commit/4afe8d)) by **GZTime**
- **(bug)** cannot use `[GUID]` in flag template - ([ab5677](https://github.com/GZTimeWalker/GZCTF/commit/ab5677)) by **GZTime**
- **(frontend)** use unified mutate - ([121599](https://github.com/GZTimeWalker/GZCTF/commit/121599)) by **GZTime**
- **(model)** stdnumber is up to 24 chars long - ([c6c2b6](https://github.com/GZTimeWalker/GZCTF/commit/c6c2b6)) by **GZTime**
- **(style)** user avatar fallback - ([ed869a](https://github.com/GZTimeWalker/GZCTF/commit/ed869a)) by **GZTime**

---
## [0.15.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.15.1..v0.15.2) - 2023-07-12

### ⛰️ Features

- **(user)** scoreboard auto refresh - ([ad057f](https://github.com/GZTimeWalker/GZCTF/commit/ad057f)) by **GZTime**

### 🐛 Bug Fixes

- **(bug)** Instance not saved - ([71536b](https://github.com/GZTimeWalker/GZCTF/commit/71536b)) by **GZTime**

---
## [0.15.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.15.0..v0.15.1) - 2023-07-07

### ⛰️ Features

- **(user)** add cache cleanup - ([05f930](https://github.com/GZTimeWalker/GZCTF/commit/05f930)) by **GZTime**

### 🐛 Bug Fixes

- **(style)** Badge width too small - ([c8becc](https://github.com/GZTimeWalker/GZCTF/commit/c8becc)) by **GZTime**

---
## [0.15.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.14.6..v0.15.0) - 2023-07-03

### ⛰️ Features

- **(admin)** container instance manager - ([7e4239](https://github.com/GZTimeWalker/GZCTF/commit/7e4239)) by **GZTime**
- **(admin)** config  "autoDestroyOnLimitReached" - ([dde535](https://github.com/GZTimeWalker/GZCTF/commit/dde535)) by **GZTime**
- **(admin)** team info edit - ([839a09](https://github.com/GZTimeWalker/GZCTF/commit/839a09)) by **GZTime**
- **(monitor/admin)** Add CheatInfo Page & Container Manager (#116) - ([74d608](https://github.com/GZTimeWalker/GZCTF/commit/74d608)) by **GZTime**

### 🐛 Bug Fixes

- **(admin)** team page & error message - ([c51a37](https://github.com/GZTimeWalker/GZCTF/commit/c51a37)) by **GZTime**

### 🎨 Styling

- **(logs)** update logs page - ([99b6f2](https://github.com/GZTimeWalker/GZCTF/commit/99b6f2)) by **GZTime**
- **(monitor)** submissions - ([f4c6f5](https://github.com/GZTimeWalker/GZCTF/commit/f4c6f5)) by **GZTime**

---
## [0.14.6](https://github.com/GZTimeWalker/GZCTF/compare/v0.14.5..v0.14.6) - 2023-06-23

### ⛰️ Features

- export team info in scoreboard - ([3c59c7](https://github.com/GZTimeWalker/GZCTF/commit/3c59c7)) by **GZTime**
- update dynamic flag - ([9bcabf](https://github.com/GZTimeWalker/GZCTF/commit/9bcabf)) by **GZTime**

### 🐛 Bug Fixes

- dispatch dynamic flag in a transaction - ([ba915f](https://github.com/GZTimeWalker/GZCTF/commit/ba915f)) by **GZTime**
- add filter for new submissions - ([77997c](https://github.com/GZTimeWalker/GZCTF/commit/77997c)) by **GZTime**
- blood bonus can not set to 0 - ([3ab76f](https://github.com/GZTimeWalker/GZCTF/commit/3ab76f)) by **GZTime**

---
## [0.14.5](https://github.com/GZTimeWalker/GZCTF/compare/v0.14.4..v0.14.5) - 2023-05-28

### ⛰️ Features

- standardize front-end project - ([cddf2e](https://github.com/GZTimeWalker/GZCTF/commit/cddf2e)) by **GZTime**

### 🐛 Bug Fixes

- use ISO 8601 - ([68c1d3](https://github.com/GZTimeWalker/GZCTF/commit/68c1d3)) by **GZTime**

### 📦 Other Changes

- update - ([a56be5](https://github.com/GZTimeWalker/GZCTF/commit/a56be5)) by **GZTime**
- update - ([5920f6](https://github.com/GZTimeWalker/GZCTF/commit/5920f6)) by **GZTime**

---
## [0.14.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.14.3..v0.14.4) - 2023-05-23

### 🐛 Bug Fixes

- follow service k8s naming convention - ([83d3a2](https://github.com/GZTimeWalker/GZCTF/commit/83d3a2)) by **GZTime**
- ParticipationStatus handler - ([980f80](https://github.com/GZTimeWalker/GZCTF/commit/980f80)) by **GZTime**
- incorrect container count - ([d90e0d](https://github.com/GZTimeWalker/GZCTF/commit/d90e0d)) by **GZTime**

### 📦 Other Changes

- update - ([42cb2c](https://github.com/GZTimeWalker/GZCTF/commit/42cb2c)) by **GZTime**

---
## [0.14.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.14.2..v0.14.3) - 2023-05-11

### ⛰️ Features

- reduce cpu requests - ([37141f](https://github.com/GZTimeWalker/GZCTF/commit/37141f)) by **GZTime**

### 🐛 Bug Fixes

- global config can not be updated - ([e00349](https://github.com/GZTimeWalker/GZCTF/commit/e00349)) by **GZTime**
- handle exception in InitK8s - ([8194f0](https://github.com/GZTimeWalker/GZCTF/commit/8194f0)) by **GZTime**

### 📦 Other Changes

- update swr configs - ([f542d5](https://github.com/GZTimeWalker/GZCTF/commit/f542d5)) by **GZTime**

---
## [0.14.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.14.1..v0.14.2) - 2023-05-07

### ⛰️ Features

- compress avatar to webp - ([7806dd](https://github.com/GZTimeWalker/GZCTF/commit/7806dd)) by **GZTime**
- add flag wrong hint - ([7f971b](https://github.com/GZTimeWalker/GZCTF/commit/7f971b)) by **GZTime**
- update scoreboard flush strategy - ([8dea6b](https://github.com/GZTimeWalker/GZCTF/commit/8dea6b)) by **GZTime**

### 🐛 Bug Fixes

- upload attachment - ([aad974](https://github.com/GZTimeWalker/GZCTF/commit/aad974)) by **GZTime**
- image upload - ([4bc9e9](https://github.com/GZTimeWalker/GZCTF/commit/4bc9e9)) by **GZTime**
- unified logout behavior - ([593760](https://github.com/GZTimeWalker/GZCTF/commit/593760)) by **GZTime**
- unreasonable rate limiting strategies - ([27c8ff](https://github.com/GZTimeWalker/GZCTF/commit/27c8ff)) by **GZTime**
- comming games - ([e27a95](https://github.com/GZTimeWalker/GZCTF/commit/e27a95)) by **GZTime**
- do not mutate GameChallengesWithTeamInfo - ([d9bfff](https://github.com/GZTimeWalker/GZCTF/commit/d9bfff)) by **GZTime**

### 📦 Other Changes

- add swr localstorage cache & fix bugs - ([5ec279](https://github.com/GZTimeWalker/GZCTF/commit/5ec279)) by **GZTime**
- update styles - ([8e8f34](https://github.com/GZTimeWalker/GZCTF/commit/8e8f34)) by **GZTime**

---
## [0.14.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.14.0..v0.14.1) - 2023-05-05

### ⛰️ Features

- bootstrap scoreboard cache - ([3940bf](https://github.com/GZTimeWalker/GZCTF/commit/3940bf)) by **GZTime**
- non-blocking cache update - ([9ffa58](https://github.com/GZTimeWalker/GZCTF/commit/9ffa58)) by **GZTime**

### 📦 Other Changes

- add banner - ([634dd7](https://github.com/GZTimeWalker/GZCTF/commit/634dd7)) by **GZTime**
- fix styles - ([185815](https://github.com/GZTimeWalker/GZCTF/commit/185815)) by **GZTime**
- tidy up & adjust style - ([12e618](https://github.com/GZTimeWalker/GZCTF/commit/12e618)) by **GZTime**

---
## [0.14.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.13.4..v0.14.0) - 2023-04-18

### ⛰️ Features

- mobile scoreboard - ([1f8151](https://github.com/GZTimeWalker/GZCTF/commit/1f8151)) by **GZTime**
- cache global config in local storage - ([6afad4](https://github.com/GZTimeWalker/GZCTF/commit/6afad4)) by **GZTime**

### 🐛 Bug Fixes

- no-argument constructor - ([13394c](https://github.com/GZTimeWalker/GZCTF/commit/13394c)) by **GZTime**
- handle identity result - ([771c4d](https://github.com/GZTimeWalker/GZCTF/commit/771c4d)) by **GZTime**
- add ConcurrencyStamp for Challenge - ([06fa7f](https://github.com/GZTimeWalker/GZCTF/commit/06fa7f)) by **GZTime**
- writeup page scrollarea - ([1df3f2](https://github.com/GZTimeWalker/GZCTF/commit/1df3f2)) by **GZTime**

### 📦 Other Changes

- adjust height - ([9e63eb](https://github.com/GZTimeWalker/GZCTF/commit/9e63eb)) by **GZTime**
- use pwd reset token instead of remove pwd - ([31d59b](https://github.com/GZTimeWalker/GZCTF/commit/31d59b)) by **GZTime**
- fix working directory - ([093ee5](https://github.com/GZTimeWalker/GZCTF/commit/093ee5)) by **GZTime**
- AddUsers - ([259250](https://github.com/GZTimeWalker/GZCTF/commit/259250)) by **GZTime**
- multi user create api - ([b4d985](https://github.com/GZTimeWalker/GZCTF/commit/b4d985)) by **GZTime**
- fix context - ([3bbb97](https://github.com/GZTimeWalker/GZCTF/commit/3bbb97)) by **GZTime**
- update - ([167381](https://github.com/GZTimeWalker/GZCTF/commit/167381)) by **GZTime**

---
## [0.13.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.13.3..v0.13.4) - 2023-04-13

### ⛰️ Features

- disable service links - ([9a9057](https://github.com/GZTimeWalker/GZCTF/commit/9a9057)) by **GZTime**
- show team bio on scoreboard - ([94c2f4](https://github.com/GZTimeWalker/GZCTF/commit/94c2f4)) by **GZTime**

### 🐛 Bug Fixes

- limit input height - ([66be3a](https://github.com/GZTimeWalker/GZCTF/commit/66be3a)) by **GZTime**
- use || instead of ?? - ([a9fb43](https://github.com/GZTimeWalker/GZCTF/commit/a9fb43)) by **GZTime**
- can not prolong containers - ([fc535e](https://github.com/GZTimeWalker/GZCTF/commit/fc535e)) by **GZTime**

### 📦 Other Changes

- update event card - ([df4468](https://github.com/GZTimeWalker/GZCTF/commit/df4468)) by **GZTime**

---
## [0.13.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.13.2..v0.13.3) - 2023-04-07

### 🐛 Bug Fixes

- wrong char - ([26c8c2](https://github.com/GZTimeWalker/GZCTF/commit/26c8c2)) by **GZTime**
- mark IsLoaded - ([e03bdb](https://github.com/GZTimeWalker/GZCTF/commit/e03bdb)) by **GZTime**

---
## [0.13.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.13.1..v0.13.2) - 2023-04-05

### ⛰️ Features

- use input instead of text in scoreboard - ([f656ca](https://github.com/GZTimeWalker/GZCTF/commit/f656ca)) by **GZTime**

### 🐛 Bug Fixes

- remove accordion padding - ([c3a82d](https://github.com/GZTimeWalker/GZCTF/commit/c3a82d)) by **GZTime**
- modal in modal - ([087e43](https://github.com/GZTimeWalker/GZCTF/commit/087e43)) by **GZTime**
- withinPortal for Select - ([bde680](https://github.com/GZTimeWalker/GZCTF/commit/bde680)) by **GZTime**
- popover withinPortal - ([234863](https://github.com/GZTimeWalker/GZCTF/commit/234863)) by **GZTime**
- style - ([aa2b79](https://github.com/GZTimeWalker/GZCTF/commit/aa2b79)) by **GZTime**
- TimeInput - ([303c23](https://github.com/GZTimeWalker/GZCTF/commit/303c23)) by **GZTime**

### 📦 Other Changes

- bump version to 1.13.2 - ([0c10cc](https://github.com/GZTimeWalker/GZCTF/commit/0c10cc)) by **GZTime**
- trim container image - ([d9aed1](https://github.com/GZTimeWalker/GZCTF/commit/d9aed1)) by **GZTime**

---
## [0.13.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.13.0.3..v0.13.1) - 2023-02-24

### ⛰️ Features

- update email template - ([9ba028](https://github.com/GZTimeWalker/GZCTF/commit/9ba028)) by **GZTime**

### 📦 Other Changes

- update - ([59b903](https://github.com/GZTimeWalker/GZCTF/commit/59b903)) by **GZTime**
- update - ([af854a](https://github.com/GZTimeWalker/GZCTF/commit/af854a)) by **GZTime**

---
## [0.13.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.9.2..v0.13.0) - 2022-11-27

### ⛰️ Features

- custom blood bonus - ([b26e46](https://github.com/GZTimeWalker/GZCTF/commit/b26e46)) by **GZTime**
- user / team / game count in admin page - ([cb6b52](https://github.com/GZTimeWalker/GZCTF/commit/cb6b52)) by **GZTime**

### 🐛 Bug Fixes

- use factor - ([66e97d](https://github.com/GZTimeWalker/GZCTF/commit/66e97d)) by **GZTime**

---
## [0.12.9.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.9.1..v0.12.9.2) - 2022-11-21

### 🐛 Bug Fixes

- many typo - ([0f909d](https://github.com/GZTimeWalker/GZCTF/commit/0f909d)) by **GZTime**

---
## [0.12.9.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.8.3..v0.12.9.1) - 2022-11-14

### ⛰️ Features

- search user by id - ([1e24c3](https://github.com/GZTimeWalker/GZCTF/commit/1e24c3)) by **GZTime**
- writeup review - ([34202b](https://github.com/GZTimeWalker/GZCTF/commit/34202b)) by **GZTime**
- add challenge preview - ([4b26e9](https://github.com/GZTimeWalker/GZCTF/commit/4b26e9)) by **GZTime**
- update config - ([75c9cc](https://github.com/GZTimeWalker/GZCTF/commit/75c9cc)) by **GZTime**
- use build in rate limiter - ([9e55e6](https://github.com/GZTimeWalker/GZCTF/commit/9e55e6)) by **GZTime**
- Add button toggles between post/edit - ([a479e3](https://github.com/GZTimeWalker/GZCTF/commit/a479e3)) by **GZTime**

### 🐛 Bug Fixes

- **(view)** set default view to be PC - ([1b8fc1](https://github.com/GZTimeWalker/GZCTF/commit/1b8fc1)) by **GZTime**
- container is null - ([2a0e09](https://github.com/GZTimeWalker/GZCTF/commit/2a0e09)) by **GZTime**
- toggle isEnabled will clear FlagTemplate - ([2cc640](https://github.com/GZTimeWalker/GZCTF/commit/2cc640)) by **GZTime**
- some anti-pattern - ([b81898](https://github.com/GZTimeWalker/GZCTF/commit/b81898)) by **GZTime**
- InvalidOp in MailSender - ([065451](https://github.com/GZTimeWalker/GZCTF/commit/065451)) by **GZTime**
- react redraw frequently - ([e9768c](https://github.com/GZTimeWalker/GZCTF/commit/e9768c)) by **GZTime**
- k8s dockerjson format - ([c6e37e](https://github.com/GZTimeWalker/GZCTF/commit/c6e37e)) by **GZTime**
- font family - ([ccde06](https://github.com/GZTimeWalker/GZCTF/commit/ccde06)) by **GZTime**
- timeline - ([141ef2](https://github.com/GZTimeWalker/GZCTF/commit/141ef2)) by **GZTime**
- do not get initial value in effect - ([370476](https://github.com/GZTimeWalker/GZCTF/commit/370476)) by **GZTime**
- submission sheett header - ([e7b011](https://github.com/GZTimeWalker/GZCTF/commit/e7b011)) by **GZTime**
- log level - ([f29aa3](https://github.com/GZTimeWalker/GZCTF/commit/f29aa3)) by **GZTime**
- reduce the user update frequency - ([dcb3f8](https://github.com/GZTimeWalker/GZCTF/commit/dcb3f8)) by **GZTime**

### 🎨 Styling

- writeup - ([5dc69c](https://github.com/GZTimeWalker/GZCTF/commit/5dc69c)) by **GZTime**
- team rank - ([e6c7c2](https://github.com/GZTimeWalker/GZCTF/commit/e6c7c2)) by **GZTime**
- update scoreboard icon justify & tidy up - ([84e3a0](https://github.com/GZTimeWalker/GZCTF/commit/84e3a0)) by **GZTime**
- update print style - ([d1100c](https://github.com/GZTimeWalker/GZCTF/commit/d1100c)) by **GZTime**
- Add bold accent colors - ([678947](https://github.com/GZTimeWalker/GZCTF/commit/678947)) by **GZTime**

### 📦 Other Changes

- avoid to update ConcurrencyStamp - ([c81f0a](https://github.com/GZTimeWalker/GZCTF/commit/c81f0a)) by **GZTime**
- user IP update - ([8e6387](https://github.com/GZTimeWalker/GZCTF/commit/8e6387)) by **GZTime**
- ci - ([a435d2](https://github.com/GZTimeWalker/GZCTF/commit/a435d2)) by **GZTime**
- ci - ([c3d067](https://github.com/GZTimeWalker/GZCTF/commit/c3d067)) by **GZTime**
- ci - ([f3973c](https://github.com/GZTimeWalker/GZCTF/commit/f3973c)) by **GZTime**
- dotnet7.0-rc1 - ([31fa93](https://github.com/GZTimeWalker/GZCTF/commit/31fa93)) by **GZTime**

---
## [0.12.8.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.8.2..v0.12.8.3) - 2022-11-06

### ⛰️ Features

- add challenge preview - ([ac0f99](https://github.com/GZTimeWalker/GZCTF/commit/ac0f99)) by **GZTime**

### 📦 Other Changes

- use pnpm - ([87d17b](https://github.com/GZTimeWalker/GZCTF/commit/87d17b)) by **GZTime**

---
## [0.12.8.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.8.1..v0.12.8.2) - 2022-10-17

### ⛰️ Features

- add custom slogan - ([703730](https://github.com/GZTimeWalker/GZCTF/commit/703730)) by **GZTime**

### 🐛 Bug Fixes

- toggle isEnabled will clear FlagTemplate - ([1539c8](https://github.com/GZTimeWalker/GZCTF/commit/1539c8)) by **GZTime**
- some anti-pattern - ([18c1ec](https://github.com/GZTimeWalker/GZCTF/commit/18c1ec)) by **GZTime**
- InvalidOp in MailSender - ([016e02](https://github.com/GZTimeWalker/GZCTF/commit/016e02)) by **GZTime**
- react redraw frequently - ([8ae08e](https://github.com/GZTimeWalker/GZCTF/commit/8ae08e)) by **GZTime**
- k8s dockerjson format - ([354186](https://github.com/GZTimeWalker/GZCTF/commit/354186)) by **GZTime**
- k8s dockerjson format - ([874d33](https://github.com/GZTimeWalker/GZCTF/commit/874d33)) by **GZTime**
- issues - ([209a68](https://github.com/GZTimeWalker/GZCTF/commit/209a68)) by **GZTime**

### 🎨 Styling

- add challenge title - ([3ba937](https://github.com/GZTimeWalker/GZCTF/commit/3ba937)) by **GZTime**
- blockquote - ([a89471](https://github.com/GZTimeWalker/GZCTF/commit/a89471)) by **GZTime**

### 📦 Other Changes

- deepsource badge - ([74d218](https://github.com/GZTimeWalker/GZCTF/commit/74d218)) by **GZTime**

---
## [0.12.8.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.8..v0.12.8.1) - 2022-10-06

### 🐛 Bug Fixes

- api comment & path - ([6e0700](https://github.com/GZTimeWalker/GZCTF/commit/6e0700)) by **GZTime**
- always show game notice - ([a6553b](https://github.com/GZTimeWalker/GZCTF/commit/a6553b)) by **GZTime**

---
## [0.12.8](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.7..v0.12.8) - 2022-10-03

### ⛰️ Features

- remove storage limit for docker - ([41f1c9](https://github.com/GZTimeWalker/GZCTF/commit/41f1c9)) by **GZTime**

### 🐛 Bug Fixes

- flag template cannot be update - ([946627](https://github.com/GZTimeWalker/GZCTF/commit/946627)) by **GZTime**
- flag of DynamicContainer can't be set empty (#44) - ([a406f5](https://github.com/GZTimeWalker/GZCTF/commit/a406f5)) by **Light**
- destroy the container only if it exists - ([f8140f](https://github.com/GZTimeWalker/GZCTF/commit/f8140f)) by **GZTime**
- adjust the order of organizations - ([3b51ac](https://github.com/GZTimeWalker/GZCTF/commit/3b51ac)) by **GZTime**
- logger type - ([d6784d](https://github.com/GZTimeWalker/GZCTF/commit/d6784d)) by **GZTime**

---
## [0.12.6](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.5.4..v0.12.6) - 2022-09-28

### ⛰️ Features

- update scoreboard - ([473a93](https://github.com/GZTimeWalker/GZCTF/commit/473a93)) by **GZTime**

### 🐛 Bug Fixes

- font family - ([1bce4a](https://github.com/GZTimeWalker/GZCTF/commit/1bce4a)) by **GZTime**
- timeline - ([1dff4a](https://github.com/GZTimeWalker/GZCTF/commit/1dff4a)) by **GZTime**
- timeline can not be updated - ([c3148f](https://github.com/GZTimeWalker/GZCTF/commit/c3148f)) by **GZTime**

### 🎨 Styling

- team rank - ([7dffc1](https://github.com/GZTimeWalker/GZCTF/commit/7dffc1)) by **GZTime**
- update download icon - ([3fc243](https://github.com/GZTimeWalker/GZCTF/commit/3fc243)) by **GZTime**

---
## [0.12.5.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.5.3..v0.12.5.4) - 2022-09-27

### 🐛 Bug Fixes

- do not get initial value in effect - ([d9790e](https://github.com/GZTimeWalker/GZCTF/commit/d9790e)) by **GZTime**
- new event animation - ([7df280](https://github.com/GZTimeWalker/GZCTF/commit/7df280)) by **GZTime**

### 🎨 Styling

- update some styles - ([b6549c](https://github.com/GZTimeWalker/GZCTF/commit/b6549c)) by **GZTime**

---
## [0.12.5.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.5.2..v0.12.5.3) - 2022-09-26

### 🐛 Bug Fixes

- event filter cannot filter new events correctly - ([e2c9d5](https://github.com/GZTimeWalker/GZCTF/commit/e2c9d5)) by **GZTime**

### 🎨 Styling

- fix submission export icon - ([c87f01](https://github.com/GZTimeWalker/GZCTF/commit/c87f01)) by **GZTime**
- update scoreboard icon justify & tidy up - ([9e85f0](https://github.com/GZTimeWalker/GZCTF/commit/9e85f0)) by **GZTime**

---
## [0.12.5.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.5.1..v0.12.5.2) - 2022-09-25

### 🐛 Bug Fixes

- **(view)** set default view to be PC - ([b99145](https://github.com/GZTimeWalker/GZCTF/commit/b99145)) by **Konano**

---
## [0.12.5.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.5..v0.12.5.1) - 2022-09-25

### 🎨 Styling

- fix for Safari & update timeline - ([ec627e](https://github.com/GZTimeWalker/GZCTF/commit/ec627e)) by **GZTime**

---
## [0.12.5](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.4.1..v0.12.5) - 2022-09-25

### 🐛 Bug Fixes

- submission sheet header - ([9bbd8a](https://github.com/GZTimeWalker/GZCTF/commit/9bbd8a)) by **GZTime**

---
## [0.12.4.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.4..v0.12.4.1) - 2022-09-24

### 🎨 Styling

- reduce padding - ([ada50c](https://github.com/GZTimeWalker/GZCTF/commit/ada50c)) by **GZTime**

---
## [0.12.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.3..v0.12.4) - 2022-09-24

### 🐛 Bug Fixes

- post edit - ([065207](https://github.com/GZTimeWalker/GZCTF/commit/065207)) by **GZTime**

---
## [0.12.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.1..v0.12.2) - 2022-09-23

### 🐛 Bug Fixes

- **(leet)** Remove symbols from LeetDictionary (#43) - ([63d86f](https://github.com/GZTimeWalker/GZCTF/commit/63d86f)) by **Nano**

---
## [0.12.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.12.0..v0.12.1) - 2022-09-23

### 🐛 Bug Fixes

- enable when works are finished - ([0d3c6c](https://github.com/GZTimeWalker/GZCTF/commit/0d3c6c)) by **GZTime**
- NaN when sort & save on toggle test container - ([c46e9b](https://github.com/GZTimeWalker/GZCTF/commit/c46e9b)) by **GZTime**
- wrong order in scoreboard - ([d584f6](https://github.com/GZTimeWalker/GZCTF/commit/d584f6)) by **GZTime**
- do not use at - ([2fed69](https://github.com/GZTimeWalker/GZCTF/commit/2fed69)) by **GZTime**

### 🎨 Styling

- update - ([cf4065](https://github.com/GZTimeWalker/GZCTF/commit/cf4065)) by **GZTime**

---
## [0.12.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.11.5..v0.12.0) - 2022-09-21

### 🐛 Bug Fixes

- at is not supported - ([38b812](https://github.com/GZTimeWalker/GZCTF/commit/38b812)) by **GZTime**
- `is not true` instead of `is false` - ([4a4e6b](https://github.com/GZTimeWalker/GZCTF/commit/4a4e6b)) by **GZTime**

### 🎨 Styling

- add tooltip for scoreboard - ([95310d](https://github.com/GZTimeWalker/GZCTF/commit/95310d)) by **GZTime**

### 📦 Other Changes

- enhanced mobile support - ([5ae1b0](https://github.com/GZTimeWalker/GZCTF/commit/5ae1b0)) by **GZTime**
- enhanced mobile support - ([d5e28a](https://github.com/GZTimeWalker/GZCTF/commit/d5e28a)) by **GZTime**
- downgrade - ([fcabc5](https://github.com/GZTimeWalker/GZCTF/commit/fcabc5)) by **GZTime**

---
## [0.11.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.11.3..v0.11.4) - 2022-09-20

### ⛰️ Features

- update configs - ([e76fd6](https://github.com/GZTimeWalker/GZCTF/commit/e76fd6)) by **GZTime**
- PrivilegedContainer for challenge - ([ae172e](https://github.com/GZTimeWalker/GZCTF/commit/ae172e)) by **GZTime**

---
## [0.11.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.11.1..v0.11.2) - 2022-09-17

### 🐛 Bug Fixes

- request twice when verify - ([fb93a6](https://github.com/GZTimeWalker/GZCTF/commit/fb93a6)) by **GZTime**

### 📦 Other Changes

- ci - ([7d6c38](https://github.com/GZTimeWalker/GZCTF/commit/7d6c38)) by **GZTime**

---
## [0.11.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.11.0..v0.11.1) - 2022-09-14

### 🐛 Bug Fixes

- log to database - ([a51a91](https://github.com/GZTimeWalker/GZCTF/commit/a51a91)) by **GZTime**

### 🎨 Styling

- better flag manager - ([d3466b](https://github.com/GZTimeWalker/GZCTF/commit/d3466b)) by **GZTime**

---
## [0.11.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.10.2..v0.11.0) - 2022-09-13

### 🐛 Bug Fixes

- docker service - ([964e61](https://github.com/GZTimeWalker/GZCTF/commit/964e61)) by **GZTime**
- docker service - ([fb2d8f](https://github.com/GZTimeWalker/GZCTF/commit/fb2d8f)) by **GZTime**

### 📦 Other Changes

- add data protection store - ([e35e68](https://github.com/GZTimeWalker/GZCTF/commit/e35e68)) by **GZTime**
- do not use alpine base - ([d0b34b](https://github.com/GZTimeWalker/GZCTF/commit/d0b34b)) by **GZTime**

---
## [0.10.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.10.1..v0.10.2) - 2022-09-11

### 🐛 Bug Fixes

- practice mode - ([b9d58b](https://github.com/GZTimeWalker/GZCTF/commit/b9d58b)) by **GZTime**
- incorrect route rule in game tab - ([81608e](https://github.com/GZTimeWalker/GZCTF/commit/81608e)) by **GZTime**

---
## [0.10.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.10.0..v0.10.1) - 2022-09-11

### ⛰️ Features

- version hint - ([2dcf77](https://github.com/GZTimeWalker/GZCTF/commit/2dcf77)) by **GZTime**
- game practice mode - ([2a348f](https://github.com/GZTimeWalker/GZCTF/commit/2a348f)) by **GZTime**

### 🐛 Bug Fixes

- flags submitted after game ended still can get bloods - ([55043d](https://github.com/GZTimeWalker/GZCTF/commit/55043d)) by **GZTime**
- ports can be null - ([8f1f34](https://github.com/GZTimeWalker/GZCTF/commit/8f1f34)) by **GZTime**
- markdown katex render regex - ([2d9081](https://github.com/GZTimeWalker/GZCTF/commit/2d9081)) by **GZTime**
- ToUpper - ([de750e](https://github.com/GZTimeWalker/GZCTF/commit/de750e)) by **GZTime**
- flag leet - ([57d396](https://github.com/GZTimeWalker/GZCTF/commit/57d396)) by **GZTime**
- RegisterStatus not match - ([7ca520](https://github.com/GZTimeWalker/GZCTF/commit/7ca520)) by **GZTime**

### 🎨 Styling

- game review - ([69d3ef](https://github.com/GZTimeWalker/GZCTF/commit/69d3ef)) by **GZTime**

---
## [0.10.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.9.2..v0.10.0) - 2022-09-10

### ⛰️ Features

- leet string - ([cb72be](https://github.com/GZTimeWalker/GZCTF/commit/cb72be)) by **GZTime**
- add swarm mode - ([0dc4dd](https://github.com/GZTimeWalker/GZCTF/commit/0dc4dd)) by **GZTime**
- docker registry auth - ([87cdcc](https://github.com/GZTimeWalker/GZCTF/commit/87cdcc)) by **GZTime**
- hide & delete game - ([4722cb](https://github.com/GZTimeWalker/GZCTF/commit/4722cb)) by **GZTime**

### 🐛 Bug Fixes

- user list overflow - ([1a0360](https://github.com/GZTimeWalker/GZCTF/commit/1a0360)) by **GZTime**
- typo - ([ce0ed9](https://github.com/GZTimeWalker/GZCTF/commit/ce0ed9)) by **GZTimeWalker**
- button style - ([0e21c8](https://github.com/GZTimeWalker/GZCTF/commit/0e21c8)) by **GZTime**
- k8s registry auth - ([ff49b5](https://github.com/GZTimeWalker/GZCTF/commit/ff49b5)) by **GZTime**
- swarm remove container - ([65f985](https://github.com/GZTimeWalker/GZCTF/commit/65f985)) by **GZTime**
- label too long - ([dd698a](https://github.com/GZTimeWalker/GZCTF/commit/dd698a)) by **GZTime**
- error caused by long flag - ([83a646](https://github.com/GZTimeWalker/GZCTF/commit/83a646)) by **GZTime**
- docker auth - ([f5c1ae](https://github.com/GZTimeWalker/GZCTF/commit/f5c1ae)) by **GZTime**
- challenge panel - ([3129ab](https://github.com/GZTimeWalker/GZCTF/commit/3129ab)) by **GZTime**
- kick user but leave the participant - ([ff4665](https://github.com/GZTimeWalker/GZCTF/commit/ff4665)) by **GZTime**

### 📦 Other Changes

- container service - ([2ac772](https://github.com/GZTimeWalker/GZCTF/commit/2ac772)) by **GZTime**

---
## [0.9.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.9.1..v0.9.2) - 2022-09-09

### ⛰️ Features

- email domain filter - ([439a15](https://github.com/GZTimeWalker/GZCTF/commit/439a15)) by **GZTime**

### 🐛 Bug Fixes

- can not sign in with empty organizations list - ([6a7731](https://github.com/GZTimeWalker/GZCTF/commit/6a7731)) by **GZTime**
- typo - ([9525d1](https://github.com/GZTimeWalker/GZCTF/commit/9525d1)) by **GZTimeWalker**
- query warning - ([a06afe](https://github.com/GZTimeWalker/GZCTF/commit/a06afe)) by **GZTime**

### 📦 Other Changes

- add integration tests (#35) - ([9062a8](https://github.com/GZTimeWalker/GZCTF/commit/9062a8)) by **Steve**
- use local fonts - ([01c8c2](https://github.com/GZTimeWalker/GZCTF/commit/01c8c2)) by **GZTime**

---
## [0.9.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.9.0..v0.9.1) - 2022-09-08

### 🐛 Bug Fixes

- untracked entity - ([1cbf68](https://github.com/GZTimeWalker/GZCTF/commit/1cbf68)) by **GZTime**

---
## [0.9.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.8.3..v0.9.0) - 2022-09-08

### 🐛 Bug Fixes

- 404 when flush admin page - ([a93927](https://github.com/GZTimeWalker/GZCTF/commit/a93927)) by **GZTime**
- self or captain deletion - ([133367](https://github.com/GZTimeWalker/GZCTF/commit/133367)) by **GZTime**
- table render - ([409716](https://github.com/GZTimeWalker/GZCTF/commit/409716)) by **GZTime**
- WithWiderScreen - ([99c727](https://github.com/GZTimeWalker/GZCTF/commit/99c727)) by **GZTime**

---
## [0.8.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.8.2..v0.8.3) - 2022-09-06

### ⛰️ Features

- add wider screen requirement - ([8261ea](https://github.com/GZTimeWalker/GZCTF/commit/8261ea)) by **GZTime**

### 🐛 Bug Fixes

- update styles - ([f3781f](https://github.com/GZTimeWalker/GZCTF/commit/f3781f)) by **GZTime**
- unexpected pin - ([17caff](https://github.com/GZTimeWalker/GZCTF/commit/17caff)) by **GZTime**

### 📦 Other Changes

- do not throw on error - ([049c9d](https://github.com/GZTimeWalker/GZCTF/commit/049c9d)) by **GZTime**

---
## [0.8.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.8.0..v0.8.2) - 2022-09-05

### ⛰️ Features

- formula rendering - ([919f17](https://github.com/GZTimeWalker/GZCTF/commit/919f17)) by **GZTime**
- code highlight - ([083a98](https://github.com/GZTimeWalker/GZCTF/commit/083a98)) by **GZTime**

### 🐛 Bug Fixes

- create post without content - ([e1d37c](https://github.com/GZTimeWalker/GZCTF/commit/e1d37c)) by **GZTime**

---
## [0.8.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.7.2..v0.8.0) - 2022-09-05

### ⛰️ Features

- add post - ([170400](https://github.com/GZTimeWalker/GZCTF/commit/170400)) by **GZTime**

### 🐛 Bug Fixes

- unchanged summary - ([0b09a1](https://github.com/GZTimeWalker/GZCTF/commit/0b09a1)) by **GZTime**
- Api.ts - ([1bb11b](https://github.com/GZTimeWalker/GZCTF/commit/1bb11b)) by **GZTimeWalker**
- API - ([de8427](https://github.com/GZTimeWalker/GZCTF/commit/de8427)) by **GZTime**
- null array - ([3e582a](https://github.com/GZTimeWalker/GZCTF/commit/3e582a)) by **GZTime**
- type error - ([dafdfa](https://github.com/GZTimeWalker/GZCTF/commit/dafdfa)) by **GZTime**
- scoreboard item get null when no challenges - ([d5de9d](https://github.com/GZTimeWalker/GZCTF/commit/d5de9d)) by **GZTime**

---
## [0.7.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.7.1..v0.7.2) - 2022-09-02

### 🐛 Bug Fixes

- scoreboard width - ([c50d22](https://github.com/GZTimeWalker/GZCTF/commit/c50d22)) by **GZTime**
- limit timeline - ([05f9a2](https://github.com/GZTimeWalker/GZCTF/commit/05f9a2)) by **GZTime**
- update admin API - ([58f9cf](https://github.com/GZTimeWalker/GZCTF/commit/58f9cf)) by **GZTime**
- remove not found container from db - ([aadf8e](https://github.com/GZTimeWalker/GZCTF/commit/aadf8e)) by **GZTime**
- filter events sent by signalr - ([3b4958](https://github.com/GZTimeWalker/GZCTF/commit/3b4958)) by **GZTime**

---
## [0.7.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.7.0..v0.7.1) - 2022-09-02

### ⛰️ Features

- add event filter frontend - ([7df5cd](https://github.com/GZTimeWalker/GZCTF/commit/7df5cd)) by **GZTime**
- add event filter - ([eff052](https://github.com/GZTimeWalker/GZCTF/commit/eff052)) by **GZTime**

### 🐛 Bug Fixes

- mutate data after flag accepted - ([3ef844](https://github.com/GZTimeWalker/GZCTF/commit/3ef844)) by **GZTime**
- no attachment for dynamic container challenge - ([731208](https://github.com/GZTimeWalker/GZCTF/commit/731208)) by **GZTime**

---
## [0.7.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.6.1..v0.7.0) - 2022-09-01

### 🐛 Bug Fixes

- db op order - ([960ccf](https://github.com/GZTimeWalker/GZCTF/commit/960ccf)) by **GZTime**

### 📦 Other Changes

- join game hint - ([d130bb](https://github.com/GZTimeWalker/GZCTF/commit/d130bb)) by **GZTime**
- update database - ([65fe63](https://github.com/GZTimeWalker/GZCTF/commit/65fe63)) by **GZTimeWalker**

---
## [0.6.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.6.0..v0.6.1) - 2022-08-29

### 🐛 Bug Fixes

- challenge card -> hint - ([00bc86](https://github.com/GZTimeWalker/GZCTF/commit/00bc86)) by **GZTime**

---
## [0.6.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.5.0..v0.6.0) - 2022-08-28

### ⛰️ Features

- hint edit - ([603923](https://github.com/GZTimeWalker/GZCTF/commit/603923)) by **GZTime**
- org rank - ([bde5d3](https://github.com/GZTimeWalker/GZCTF/commit/bde5d3)) by **GZTime**

---
## [0.5.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.4.2..v0.5.0) - 2022-08-27

### ⛰️ Features

- global config - ([060da3](https://github.com/GZTimeWalker/GZCTF/commit/060da3)) by **GZTime**
- config manager - ([f5fa95](https://github.com/GZTimeWalker/GZCTF/commit/f5fa95)) by **GZTime**
- config in db - ([65e73b](https://github.com/GZTimeWalker/GZCTF/commit/65e73b)) by **GZTime**

### 🐛 Bug Fixes

- update config manage - ([aef42d](https://github.com/GZTimeWalker/GZCTF/commit/aef42d)) by **GZTime**
- MIME_TYPE when upload images - ([13c92f](https://github.com/GZTimeWalker/GZCTF/commit/13c92f)) by **GZTime**
- error when export excel - ([68f4f5](https://github.com/GZTimeWalker/GZCTF/commit/68f4f5)) by **GZTime**
- empty env in static container on k8s - ([fa3c4f](https://github.com/GZTimeWalker/GZCTF/commit/fa3c4f)) by **GZTime**

### 📦 Other Changes

- update arch - ([5c0d6c](https://github.com/GZTimeWalker/GZCTF/commit/5c0d6c)) by **GZTime**
- add develop - ([611ecf](https://github.com/GZTimeWalker/GZCTF/commit/611ecf)) by **GZTime**
- update - ([8a0df5](https://github.com/GZTimeWalker/GZCTF/commit/8a0df5)) by **GZTime**
- update - ([9fbd5c](https://github.com/GZTimeWalker/GZCTF/commit/9fbd5c)) by **GZTime**
- update - ([d9b406](https://github.com/GZTimeWalker/GZCTF/commit/d9b406)) by **GZTime**
- update cache - ([2155ca](https://github.com/GZTimeWalker/GZCTF/commit/2155ca)) by **GZTime**
- ci - ([159015](https://github.com/GZTimeWalker/GZCTF/commit/159015)) by **GZTime**

---
## [0.4.2](https://github.com/GZTimeWalker/GZCTF/compare/v0.4.0..v0.4.2) - 2022-08-25

### ⛰️ Features

- hide solved - ([9e22d2](https://github.com/GZTimeWalker/GZCTF/commit/9e22d2)) by **GZTime**
- update notice order - ([f819db](https://github.com/GZTimeWalker/GZCTF/commit/f819db)) by **GZTime**
- update notice panel - ([6d8284](https://github.com/GZTimeWalker/GZCTF/commit/6d8284)) by **GZTime**

### 🐛 Bug Fixes

- scoreboard only fetch submissions before end time - ([abc0ca](https://github.com/GZTimeWalker/GZCTF/commit/abc0ca)) by **GZTime**
- unify icon - ([2d4a40](https://github.com/GZTimeWalker/GZCTF/commit/2d4a40)) by **GZTime**

---
## [0.4.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.3.0..v0.4.0) - 2022-08-24

### ⛰️ Features

- manually activate the user - ([bfeb31](https://github.com/GZTimeWalker/GZCTF/commit/bfeb31)) by **GZTime**
- skeleton for challenges page - ([47a937](https://github.com/GZTimeWalker/GZCTF/commit/47a937)) by **chenjunyu19**
- empty status indicator - ([808555](https://github.com/GZTimeWalker/GZCTF/commit/808555)) by **chenjunyu19**

### 🐛 Bug Fixes

- Icon ref by auto-complete - ([11cb7c](https://github.com/GZTimeWalker/GZCTF/commit/11cb7c)) by **GZTime**
- launch test container when updated image or port - ([50a39f](https://github.com/GZTimeWalker/GZCTF/commit/50a39f)) by **GZTime**
- team rank loading - ([f5e9a2](https://github.com/GZTimeWalker/GZCTF/commit/f5e9a2)) by **GZTime**

### 📦 Other Changes

- add eslint for dev - ([3e19fb](https://github.com/GZTimeWalker/GZCTF/commit/3e19fb)) by **GZTime**

---
## [0.3.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.2.0..v0.3.0) - 2022-08-14

### 🐛 Bug Fixes

- merged region - ([2ef659](https://github.com/GZTimeWalker/GZCTF/commit/2ef659)) by **GZTimeWalker**
- typo - ([6de0a7](https://github.com/GZTimeWalker/GZCTF/commit/6de0a7)) by **GZTimeWalker**

---
## [0.2.0](https://github.com/GZTimeWalker/GZCTF/compare/v0.1.4..v0.2.0) - 2022-08-13

### 🐛 Bug Fixes

- type error - ([1acfbe](https://github.com/GZTimeWalker/GZCTF/commit/1acfbe)) by **GZTimeWalker**
- user profile - ([f3565c](https://github.com/GZTimeWalker/GZCTF/commit/f3565c)) by **GZTimeWalker**
- incorrect container limit - ([d86960](https://github.com/GZTimeWalker/GZCTF/commit/d86960)) by **GZTimeWalker**
- redundant quotes - ([ccb9f4](https://github.com/GZTimeWalker/GZCTF/commit/ccb9f4)) by **GZTimeWalker**
- TeamRadarMap with no data - ([bf940b](https://github.com/GZTimeWalker/GZCTF/commit/bf940b)) by **chenjunyu19**
- missing key of recent games - ([2a4ff2](https://github.com/GZTimeWalker/GZCTF/commit/2a4ff2)) by **chenjunyu19**

---
## [0.1.4](https://github.com/GZTimeWalker/GZCTF/compare/v0.1.3..v0.1.4) - 2022-08-11

### 🐛 Bug Fixes

- flag submit - ([2bfea4](https://github.com/GZTimeWalker/GZCTF/commit/2bfea4)) by **GZTimeWalker**

---
## [0.1.3](https://github.com/GZTimeWalker/GZCTF/compare/v0.1.2..v0.1.3) - 2022-08-10

### 🐛 Bug Fixes

- **(inviteCode)** wrong reassignment - ([66fb5f](https://github.com/GZTimeWalker/GZCTF/commit/66fb5f)) by **Nano**
- invite code extract - ([fbf732](https://github.com/GZTimeWalker/GZCTF/commit/fbf732)) by **GZTimeWalker**

---
## [0.1.1](https://github.com/GZTimeWalker/GZCTF/compare/v0.1.0..v0.1.1) - 2022-08-10

### 🐛 Bug Fixes

- 401 when view scoreboard - ([3202b5](https://github.com/GZTimeWalker/GZCTF/commit/3202b5)) by **GZTimeWalker**
- table header zIndex - ([a4ef9d](https://github.com/GZTimeWalker/GZCTF/commit/a4ef9d)) by **GZTimeWalker**

---
## [0.1.0] - 2022-08-09

### ⛰️ Features

- page title - ([932ec9](https://github.com/GZTimeWalker/GZCTF/commit/932ec9)) by **chenjunyu19**
- polling request - ([70d2b3](https://github.com/GZTimeWalker/GZCTF/commit/70d2b3)) by **adbean**
- tsconfig paths for vite - ([1b66b9](https://github.com/GZTimeWalker/GZCTF/commit/1b66b9)) by **adbean**
- migration to 5.0 - ([53c345](https://github.com/GZTimeWalker/GZCTF/commit/53c345)) by **GZTimeWalker**
- deny login from banned user - ([869aba](https://github.com/GZTimeWalker/GZCTF/commit/869aba)) by **GZTimeWalker**
- user search - ([b5c8b6](https://github.com/GZTimeWalker/GZCTF/commit/b5c8b6)) by **GZTimeWalker**
- add game poster - ([933b7a](https://github.com/GZTimeWalker/GZCTF/commit/933b7a)) by **GZTimeWalker**

### 🐛 Bug Fixes

- game page - ([72f92d](https://github.com/GZTimeWalker/GZCTF/commit/72f92d)) by **GZTimeWalker**
- icon - ([611ded](https://github.com/GZTimeWalker/GZCTF/commit/611ded)) by **GZTimeWalker**
- too large range of path Api - ([360f79](https://github.com/GZTimeWalker/GZCTF/commit/360f79)) by **chenjunyu19**
- scoreboard generate - ([d87047](https://github.com/GZTimeWalker/GZCTF/commit/d87047)) by **GZTimeWalker**
- scoreboard - ([fe108d](https://github.com/GZTimeWalker/GZCTF/commit/fe108d)) by **GZTimeWalker**
- frontend - ([1de7fa](https://github.com/GZTimeWalker/GZCTF/commit/1de7fa)) by **GZTimeWalker**
- pull image when not found - ([0b3bf0](https://github.com/GZTimeWalker/GZCTF/commit/0b3bf0)) by **GZTimeWalker**
- create instance - ([e20299](https://github.com/GZTimeWalker/GZCTF/commit/e20299)) by **GZTimeWalker**
- sort - ([f40a32](https://github.com/GZTimeWalker/GZCTF/commit/f40a32)) by **GZTimeWalker**
- team review actionicon color - ([886294](https://github.com/GZTimeWalker/GZCTF/commit/886294)) by **chenjunyu19**
- fix exception type - ([58c234](https://github.com/GZTimeWalker/GZCTF/commit/58c234)) by **GZTimeWalker**
- color - ([0ef102](https://github.com/GZTimeWalker/GZCTF/commit/0ef102)) by **GZTimeWalker**
- style - ([0988e7](https://github.com/GZTimeWalker/GZCTF/commit/0988e7)) by **GZTimeWalker**
- small width homepage - ([194955](https://github.com/GZTimeWalker/GZCTF/commit/194955)) by **chenjunyu19**
- small width homepage - ([e93d9b](https://github.com/GZTimeWalker/GZCTF/commit/e93d9b)) by **chenjunyu19**
- build error - ([0a8aa3](https://github.com/GZTimeWalker/GZCTF/commit/0a8aa3)) by **GZTimeWalker**
- challenge filter - ([039dc3](https://github.com/GZTimeWalker/GZCTF/commit/039dc3)) by **GZTimeWalker**
- api request error notifications - ([255dfb](https://github.com/GZTimeWalker/GZCTF/commit/255dfb)) by **chenjunyu19**
- flags not loaded when judging - ([cfc3da](https://github.com/GZTimeWalker/GZCTF/commit/cfc3da)) by **GZTimeWalker**
- nullable filename - ([432017](https://github.com/GZTimeWalker/GZCTF/commit/432017)) by **GZTimeWalker**
- unexpect delete - ([e452be](https://github.com/GZTimeWalker/GZCTF/commit/e452be)) by **GZTimeWalker**
- errors & update - ([864590](https://github.com/GZTimeWalker/GZCTF/commit/864590)) by **GZTimeWalker**
- wrong navigate in game oncreate - ([5f8e04](https://github.com/GZTimeWalker/GZCTF/commit/5f8e04)) by **chenjunyu19**
- usage of API - ([84bd01](https://github.com/GZTimeWalker/GZCTF/commit/84bd01)) by **GZTimeWalker**
- avatar icon - ([e3025b](https://github.com/GZTimeWalker/GZCTF/commit/e3025b)) by **GZTimeWalker**
- blank - ([2ea971](https://github.com/GZTimeWalker/GZCTF/commit/2ea971)) by **GZTimeWalker**
- navbar - ([4df35b](https://github.com/GZTimeWalker/GZCTF/commit/4df35b)) by **GZTimeWalker**
- icon error - ([a153ea](https://github.com/GZTimeWalker/GZCTF/commit/a153ea)) by **GZTimeWalker**
- sort - ([761b05](https://github.com/GZTimeWalker/GZCTF/commit/761b05)) by **GZTimeWalker**
- CI error - ([2055e5](https://github.com/GZTimeWalker/GZCTF/commit/2055e5)) by **GZTimeWalker**
- image tag with branch - ([b3a0ca](https://github.com/GZTimeWalker/GZCTF/commit/b3a0ca)) by **GZTimeWalker**
- data length limit - ([28b697](https://github.com/GZTimeWalker/GZCTF/commit/28b697)) by **GZTimeWalker**
- data format limit - ([5168d4](https://github.com/GZTimeWalker/GZCTF/commit/5168d4)) by **GZTimeWalker**
- invalidate notice cache when update - ([2c947b](https://github.com/GZTimeWalker/GZCTF/commit/2c947b)) by **GZTimeWalker**
- account page: link style and enter - ([9799a4](https://github.com/GZTimeWalker/GZCTF/commit/9799a4)) by **chenjunyu19**
- bad mdi icon import - ([781921](https://github.com/GZTimeWalker/GZCTF/commit/781921)) by **chenjunyu19**
- enter key in login page - ([2b52ec](https://github.com/GZTimeWalker/GZCTF/commit/2b52ec)) by **chenjunyu19**
- user search - ([b54f60](https://github.com/GZTimeWalker/GZCTF/commit/b54f60)) by **GZTimeWalker**
- allow no param - ([5400d0](https://github.com/GZTimeWalker/GZCTF/commit/5400d0)) by **GZTimeWalker**
- incorrect routing - ([89f9cf](https://github.com/GZTimeWalker/GZCTF/commit/89f9cf)) by **GZTimeWalker**
- type warn - ([23f0f0](https://github.com/GZTimeWalker/GZCTF/commit/23f0f0)) by **GZTimeWalker**
- email change input is not empty - ([ab3745](https://github.com/GZTimeWalker/GZCTF/commit/ab3745)) by **GZTimeWalker**
- lost data when refresh invite code - ([9a2bb5](https://github.com/GZTimeWalker/GZCTF/commit/9a2bb5)) by **GZTimeWalker**
- unify the limit of username - ([bbea48](https://github.com/GZTimeWalker/GZCTF/commit/bbea48)) by **GZTimeWalker**
- update api - ([0c61f9](https://github.com/GZTimeWalker/GZCTF/commit/0c61f9)) by **GZTimeWalker**
- incorrect length of hash - ([e6ae3a](https://github.com/GZTimeWalker/GZCTF/commit/e6ae3a)) by **GZTimeWalker**
- add file ref count - ([f78f17](https://github.com/GZTimeWalker/GZCTF/commit/f78f17)) by **GZTimeWalker**
- logger - ([d0ad46](https://github.com/GZTimeWalker/GZCTF/commit/d0ad46)) by **GZTimeWalker**

### 📦 Other Changes

- update card - ([1e032c](https://github.com/GZTimeWalker/GZCTF/commit/1e032c)) by **GZTimeWalker**
- update CI - ([d1d93c](https://github.com/GZTimeWalker/GZCTF/commit/d1d93c)) by **GZTimeWalker**
- enable eslint - ([34ed07](https://github.com/GZTimeWalker/GZCTF/commit/34ed07)) by **GZTimeWalker**
- update dockerfile - ([85b81e](https://github.com/GZTimeWalker/GZCTF/commit/85b81e)) by **GZTimeWalker**
- dump deps - ([0fda73](https://github.com/GZTimeWalker/GZCTF/commit/0fda73)) by **GZTimeWalker**
- migrate to vite - ([3e4b7e](https://github.com/GZTimeWalker/GZCTF/commit/3e4b7e)) by **GZTimeWalker**
- challenge model - ([2d0d4b](https://github.com/GZTimeWalker/GZCTF/commit/2d0d4b)) by **GZTimeWalker**
- merge - ([e01e64](https://github.com/GZTimeWalker/GZCTF/commit/e01e64)) by **GZTimeWalker**
- use vite and mantine - ([6adb78](https://github.com/GZTimeWalker/GZCTF/commit/6adb78)) by **Yuze Fu**
- fix - ([337ab0](https://github.com/GZTimeWalker/GZCTF/commit/337ab0)) by **GZTimeWalker**

---
GZCTF © 2022-present GZTimeWalker
