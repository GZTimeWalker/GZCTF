/**
 * Public per-game attack animation page.
 *
 * Route: /games/{id}/attack (no authentication).
 *
 * Tactical-ops HUD aesthetic: fixed header/footer strips, event-stream
 * feed panel (left), scoreboard + stats panel (right), hexagonal HQ in
 * the center, team nodes arranged as a ring / dual-ring / active-only
 * column depending on team count.
 *
 * Field visuals (bullets, laser charge/beam/impact, debris, shockwaves)
 * are rendered by a PixiJS WebGL renderer in attackEffects.ts.  First
 * blood triggers a 10-second dread-build laser strike that shakes the
 * whole viewport and shatters the screen.
 */
import { FC, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { useParams } from 'react-router'
import api, {
  ChallengeCategory,
  DetailedGameInfoModel,
  ScoreboardItem,
  ScoreboardModel,
  SubmissionType,
} from '@Api'
import {
  disposeEffects,
  fireBullet,
  initEffects,
  playPew,
  spawnFirstBlood,
  unlockAudio,
} from './attackEffects'

/* -------------------------------------------------------------------------- */
/* Types                                                                      */
/* -------------------------------------------------------------------------- */

interface AttackEvent {
  teamName: string
  teamAvatar: string | null
  teamScore: number | null
  challengeTitle: string
  category: ChallengeCategory
  type: SubmissionType
  time: string
}

interface FeedLine {
  key: string
  time: string
  teamName: string
  challengeTitle: string
  type: SubmissionType
}

interface FirstBloodBanner {
  id: number
  teamName: string
  challengeTitle: string
  startedAt: number
}

/* -------------------------------------------------------------------------- */
/* Constants                                                                  */
/* -------------------------------------------------------------------------- */

const SCOREBOARD_REFRESH_MS = 30_000
const SCOREBOARD_DEBOUNCE_MS = 2000
const FIRST_BLOOD_BANNER_MS = 10_000
const BURST_COUNT = 8
const BURST_INTERVAL_MS = 200
const BURST_RATE_WINDOW_MS = 3000
const BURST_RATE_LIMIT = 15
const FEED_MAX = 18

const colorForType = (t: SubmissionType): string => {
  switch (t) {
    case SubmissionType.FirstBlood:
      return '#ffd34a'
    case SubmissionType.Normal:
      return '#3ae85c'
    case SubmissionType.SecondBlood:
    case SubmissionType.ThirdBlood:
      return '#f4b619'
    case SubmissionType.Unaccepted:
    default:
      return '#ff6262'
  }
}

const feedPrefix = (t: SubmissionType): string => {
  switch (t) {
    case SubmissionType.FirstBlood:
      return '!! 1st-BLOOD !! '
    case SubmissionType.Normal:
      return '[ SOLVE  ] '
    case SubmissionType.SecondBlood:
      return '[ 2nd-BLD] '
    case SubmissionType.ThirdBlood:
      return '[ 3rd-BLD] '
    default:
      return '[ MISS   ] '
  }
}

/* -------------------------------------------------------------------------- */
/* Layout                                                                     */
/* -------------------------------------------------------------------------- */

interface TeamPos {
  id: number
  name: string
  score: number
  rank: number
  avatar: string | null
  x: number
  y: number
  labelled: boolean
}

type Layout = 'ring' | 'dual-ring' | 'active'

interface LayoutResult {
  layout: Layout
  positions: TeamPos[]
  cx: number
  cy: number
  hqSize: number
  px0: number
  py0: number
  px1: number
  py1: number
  pw: number
  ph: number
  rankColumn: { x: number; y: number; w: number; h: number } | null
  ringRx: number
  ringRy: number
}

const FEED_W_MIN = 240
const FEED_W_MAX = 360
const BOARD_W_MIN = 220
const BOARD_W_MAX = 300
const HEADER_H = 46
const FOOTER_H = 40
const GUTTER = 18
const INNER_PAD = 20
const LABEL_MARGIN = 60

const computeLayout = (
  teams: ScoreboardItem[],
  w: number,
  h: number
): LayoutResult => {
  const feedW = Math.min(FEED_W_MAX, Math.max(FEED_W_MIN, w * 0.19))
  const boardW = Math.min(BOARD_W_MAX, Math.max(BOARD_W_MIN, w * 0.16))
  const px0 = GUTTER + feedW + INNER_PAD
  const px1 = w - GUTTER - boardW - INNER_PAD
  const py0 = HEADER_H + INNER_PAD
  const py1 = h - FOOTER_H - INNER_PAD
  const pw = px1 - px0
  const ph = py1 - py0

  const cx = w / 2
  const cy = h / 2
  const hqSize = Math.min(280, Math.max(160, Math.min(pw, ph) * 0.28))

  const sorted = [...teams].sort((a, b) => a.rank - b.rank)
  const count = sorted.length
  let layout: Layout
  if (count <= 24) layout = 'ring'
  else if (count <= 70) layout = 'dual-ring'
  else layout = 'active'

  const hqHalfV = hqSize * 0.6
  const hqHalfH = hqSize * 0.5
  const rxRoom = Math.min(cx - px0, px1 - cx) - LABEL_MARGIN
  const ryRoom = Math.min(cy - py0, py1 - cy) - LABEL_MARGIN
  const rMin = Math.max(hqHalfV, hqHalfH) + 40
  const rxFinal = Math.max(rMin, Math.min(Math.max(rxRoom, rMin), Math.max(rMin, pw * 0.45)))
  const ryFinal = Math.max(rMin, Math.min(Math.max(ryRoom, rMin), Math.max(rMin, ph * 0.45)))

  const positions: TeamPos[] = []
  let rankColumn: LayoutResult['rankColumn'] = null

  const pushTeam = (t: ScoreboardItem, x: number, y: number, labelled: boolean): void => {
    positions.push({
      id: t.id,
      name: t.name,
      score: t.score,
      rank: t.rank,
      avatar: t.avatar ?? null,
      x,
      y,
      labelled,
    })
  }

  if (count === 0) {
    // Nothing to lay out — emit empty positions but still give geometry for fallbacks
  } else if (layout === 'ring') {
    const perim = Math.PI * (3 * (rxFinal + ryFinal) - Math.sqrt((3 * rxFinal + ryFinal) * (rxFinal + 3 * ryFinal)))
    const showLabels = perim / count >= 70
    sorted.forEach((t, i) => {
      const a = (i / count) * Math.PI * 2 - Math.PI / 2
      pushTeam(t, cx + rxFinal * Math.cos(a), cy + ryFinal * Math.sin(a), showLabels)
    })
  } else if (layout === 'dual-ring') {
    const innerCount = Math.min(12, count)
    const inner = sorted.slice(0, innerCount)
    const outer = sorted.slice(innerCount)
    const rxI = rxFinal * 0.6
    const ryI = ryFinal * 0.6
    inner.forEach((t, i) => {
      const a = (i / inner.length) * Math.PI * 2 - Math.PI / 2
      pushTeam(t, cx + rxI * Math.cos(a), cy + ryI * Math.sin(a), true)
    })
    outer.forEach((t, i) => {
      const a = (i / outer.length) * Math.PI * 2 - Math.PI / 2
      pushTeam(t, cx + rxFinal * Math.cos(a), cy + ryFinal * Math.sin(a), false)
    })
  } else {
    // active-only: left rank column (top 20); remaining teams spawn from random points
    const colX = px0
    const colY = py0
    const colW = Math.min(240, pw * 0.22)
    const colH = py1 - py0
    rankColumn = { x: colX, y: colY, w: colW, h: colH }
    const visible = sorted.slice(0, 20)
    const rowH = (colH - 36) / Math.max(visible.length, 1)
    visible.forEach((t, i) => {
      pushTeam(t, colX + colW + 10, colY + 36 + (i + 0.5) * rowH, true)
    })
  }

  return {
    layout,
    positions,
    cx,
    cy,
    hqSize,
    px0,
    py0,
    px1,
    py1,
    pw,
    ph,
    rankColumn,
    ringRx: rxFinal,
    ringRy: ryFinal,
  }
}

/* -------------------------------------------------------------------------- */
/* Component                                                                  */
/* -------------------------------------------------------------------------- */

const Attack: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const [game, setGame] = useState<DetailedGameInfoModel | null>(null)
  const [scoreboard, setScoreboard] = useState<ScoreboardModel | null>(null)
  const [viewport, setViewport] = useState({
    w: typeof window !== 'undefined' ? window.innerWidth : 1920,
    h: typeof window !== 'undefined' ? window.innerHeight : 1080,
  })

  const [audioEnabled, setAudioEnabled] = useState(false)
  const [showAudioToast, setShowAudioToast] = useState(true)

  const [feedLines, setFeedLines] = useState<FeedLine[]>([])
  const [firstBlood, setFirstBlood] = useState<FirstBloodBanner | null>(null)

  const [eventCount, setEventCount] = useState(0)
  const [fbCount, setFbCount] = useState(0)
  const [atkRate, setAtkRate] = useState('0/min')
  const [clockText, setClockText] = useState(() => new Date().toISOString().slice(11, 19))

  const audioRef = useRef<HTMLAudioElement | null>(null)
  const canvasRef = useRef<HTMLCanvasElement | null>(null)
  const hqRef = useRef<HTMLDivElement | null>(null)
  const hexRef = useRef<HTMLDivElement | null>(null)

  const pixiReadyRef = useRef(false)
  const lastBloodSoundAtRef = useRef(0)
  const atkTimestampsRef = useRef<number[]>([])
  const burstTimersRef = useRef<Set<ReturnType<typeof setTimeout>>>(new Set())
  const burstSpawnTimesRef = useRef<number[]>([])
  const scoreboardRefreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const nextFeedKeyRef = useRef(0)

  /* ---- Viewport tracking ---- */
  useEffect(() => {
    const onResize = (): void =>
      setViewport({ w: window.innerWidth, h: window.innerHeight })
    window.addEventListener('resize', onResize)
    return () => window.removeEventListener('resize', onResize)
  }, [])

  /* ---- Initial data load ---- */
  useEffect(() => {
    if (Number.isNaN(numId) || numId < 0) return
    void (async () => {
      try {
        const [gameRes, scoreRes] = await Promise.all([
          api.game.gameGame(numId),
          api.game.gameScoreboard(numId),
        ])
        setGame(gameRes.data)
        setScoreboard(scoreRes.data)
      } catch (e) {
        // eslint-disable-next-line no-console
        console.warn('[attack] initial load failed', e)
      }
      try {
        const feedRes = await fetch(`/api/game/${numId}/AttackFeed?limit=50`)
        if (feedRes.ok) {
          const feed = (await feedRes.json()) as AttackEvent[]
          setFeedLines(
            feed.slice(-FEED_MAX).reverse().map((e) => ({
              key: `seed-${nextFeedKeyRef.current++}`,
              time: new Date(e.time).toTimeString().slice(0, 8),
              teamName: e.teamName,
              challengeTitle: e.challengeTitle,
              type: e.type,
            }))
          )
        }
      } catch (e) {
        // eslint-disable-next-line no-console
        console.warn('[attack] attack-feed failed', e)
      }
    })()
  }, [numId])

  const refreshScoreboard = useCallback(async () => {
    try {
      const res = await api.game.gameScoreboard(numId)
      setScoreboard(res.data)
    } catch {
      // non-fatal
    }
  }, [numId])

  useEffect(() => {
    if (Number.isNaN(numId) || numId < 0) return
    const iv = setInterval(() => void refreshScoreboard(), SCOREBOARD_REFRESH_MS)
    return () => clearInterval(iv)
  }, [numId, refreshScoreboard])

  /* ---- PixiJS lifecycle ---- */
  useEffect(() => {
    if (!canvasRef.current) return
    const canvas = canvasRef.current
    let cancelled = false
    void initEffects(canvas).then(() => {
      if (!cancelled) pixiReadyRef.current = true
    })
    return () => {
      cancelled = true
      pixiReadyRef.current = false
      disposeEffects()
    }
  }, [])

  /* ---- Audio unlock on first user interaction ---- */
  useEffect(() => {
    if (audioEnabled) return
    const unlock = (): void => {
      setAudioEnabled(true)
      setShowAudioToast(false)
      unlockAudio()
      if (audioRef.current) {
        audioRef.current.volume = 0
        audioRef.current
          .play()
          .then(() => {
            audioRef.current?.pause()
            if (audioRef.current) {
              audioRef.current.currentTime = 0
              audioRef.current.volume = 0.85
            }
          })
          .catch(() => undefined)
      }
    }
    window.addEventListener('click', unlock, { once: true })
    window.addEventListener('keydown', unlock, { once: true })
    return () => {
      window.removeEventListener('click', unlock)
      window.removeEventListener('keydown', unlock)
    }
  }, [audioEnabled])

  /* ---- Layout ---- */
  const layoutResult = useMemo(
    () => computeLayout(scoreboard?.items ?? [], viewport.w, viewport.h),
    [scoreboard, viewport.w, viewport.h]
  )
  const { layout, positions, cx, cy, hqSize, px0, py0, px1, py1, pw, ph, rankColumn, ringRx, ringRy } =
    layoutResult

  const teamIndex = useMemo(() => {
    const m = new Map<string, TeamPos>()
    positions.forEach((p) => m.set(p.name, p))
    return m
  }, [positions])

  const top5 = useMemo(
    () => (scoreboard?.items ?? []).slice().sort((a, b) => a.rank - b.rank).slice(0, 5),
    [scoreboard]
  )

  /* ---- Clock + rolling attack-rate ---- */
  useEffect(() => {
    const iv = setInterval(() => {
      setClockText(new Date().toISOString().slice(11, 19))
      const now = Date.now()
      while (atkTimestampsRef.current[0] < now - 60_000) atkTimestampsRef.current.shift()
      setAtkRate(`${atkTimestampsRef.current.length}/min`)
    }, 1000)
    return () => clearInterval(iv)
  }, [])

  /* ---- Resolve source position for an attacker ---- */
  const resolveSource = useCallback(
    (evt: AttackEvent): { x: number; y: number } => {
      const from = teamIndex.get(evt.teamName)
      if (from) return { x: from.x, y: from.y }
      // Fallback: random point inside the play area, avoiding the HQ zone
      for (let i = 0; i < 20; i++) {
        const x = px0 + 30 + Math.random() * (pw - 60)
        const y = py0 + 30 + Math.random() * (ph - 60)
        if (Math.abs(x - cx) > hqSize * 0.6 || Math.abs(y - cy) > hqSize * 0.7) {
          return { x, y }
        }
      }
      return { x: Math.max(px0 + 30, cx - 200), y: py0 + 100 }
    },
    [teamIndex, px0, py0, px1, py1, pw, ph, cx, cy, hqSize]
  )

  /* ---- HQ center resolved from layout geometry ---- */
  const hqCenter = useMemo(() => ({ x: cx, y: cy }), [cx, cy])

  /* ---- Handle incoming attack event ---- */
  const handleAttack = useCallback(
    (evt: AttackEvent) => {
      const time = new Date().toTimeString().slice(0, 8)

      setFeedLines((prev) => {
        const line: FeedLine = {
          key: `f-${nextFeedKeyRef.current++}`,
          time,
          teamName: evt.teamName,
          challengeTitle: evt.challengeTitle,
          type: evt.type,
        }
        return [line, ...prev].slice(0, FEED_MAX)
      })
      setEventCount((c) => c + 1)
      atkTimestampsRef.current.push(Date.now())

      const src = resolveSource(evt)
      const color = colorForType(evt.type)

      // Visual effects only fire once Pixi has finished initializing;
      // the feed/stats updates above still run so data stays fresh.
      if (!pixiReadyRef.current) {
        if (evt.type === SubmissionType.FirstBlood) {
          setFbCount((c) => c + 1)
          setFirstBlood({
            id: Math.random(),
            teamName: evt.teamName,
            challengeTitle: evt.challengeTitle,
            startedAt: performance.now(),
          })
        }
        return
      }

      if (evt.type === SubmissionType.FirstBlood) {
        spawnFirstBlood(src.x, src.y, hqCenter.x, hqCenter.y, color, {
          hexElement: hexRef.current,
          onImpact: () => {
            if (audioEnabled && audioRef.current) {
              audioRef.current.currentTime = 0
              audioRef.current.play().catch(() => undefined)
              lastBloodSoundAtRef.current = performance.now()
            }
          },
        })
        setFbCount((c) => c + 1)
        setFirstBlood({
          id: Math.random(),
          teamName: evt.teamName,
          challengeTitle: evt.challengeTitle,
          startedAt: performance.now(),
        })
      } else {
        // Rate-limit bullet bursts
        const now = performance.now()
        const recent = burstSpawnTimesRef.current.filter((t) => now - t < BURST_RATE_WINDOW_MS)
        const canBurst = recent.length < BURST_RATE_LIMIT
        if (canBurst) {
          recent.push(now)
          burstSpawnTimesRef.current = recent
          for (let i = 0; i < BURST_COUNT; i++) {
            const t = setTimeout(() => {
              burstTimersRef.current.delete(t)
              fireBullet(src.x, src.y, hqCenter.x, hqCenter.y, color, evt.type)
              playPew(evt.type)
            }, i * BURST_INTERVAL_MS)
            burstTimersRef.current.add(t)
          }
        } else {
          burstSpawnTimesRef.current = recent
          // Fire a single reduced arc so the event still registers visually
          fireBullet(src.x, src.y, hqCenter.x, hqCenter.y, color, evt.type)
        }
      }

      // Debounced scoreboard refresh (non-rejected solves affect rank)
      if (evt.type !== SubmissionType.Unaccepted) {
        if (scoreboardRefreshTimerRef.current) clearTimeout(scoreboardRefreshTimerRef.current)
        scoreboardRefreshTimerRef.current = setTimeout(() => {
          void refreshScoreboard()
        }, SCOREBOARD_DEBOUNCE_MS)
      }
    },
    [audioEnabled, hqCenter.x, hqCenter.y, refreshScoreboard, resolveSource]
  )

  /* ---- Cleanup burst timers on unmount ---- */
  useEffect(() => {
    const timers = burstTimersRef.current
    return () => {
      timers.forEach((t) => clearTimeout(t))
      timers.clear()
    }
  }, [])

  /* ---- First-blood banner auto-dismiss ---- */
  useEffect(() => {
    if (!firstBlood) return
    const t = setTimeout(() => setFirstBlood(null), FIRST_BLOOD_BANNER_MS)
    return () => clearTimeout(t)
  }, [firstBlood])

  /* ---- SignalR ---- */
  useEffect(() => {
    if (Number.isNaN(numId) || numId < 0) return
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`/hub/attack?game=${numId}`)
      .withHubProtocol(new signalR.JsonHubProtocol())
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.None)
      .build()
    connection.serverTimeoutInMilliseconds = 60 * 1000 * 60 * 2

    connection.on('ReceivedAttack', (msg: AttackEvent) => {
      handleAttack(msg)
    })

    connection.start().catch((err) => {
      // eslint-disable-next-line no-console
      console.warn('[attack] signalR connect failed', err)
    })

    return () => {
      connection.stop().catch(() => undefined)
    }
  }, [numId, handleAttack])

  /* ---- Render ---- */
  const eventTitle = game?.title ?? 'ATTACK'
  const sortedTop = positions.slice(0, 20)

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        width: '100vw',
        height: '100vh',
        background: '#060609',
        color: '#d7dbe4',
        fontFamily: '"Space Grotesk", system-ui, sans-serif',
        overflow: 'hidden',
        userSelect: 'none',
      }}
    >
      {/* Shared keyframes + screen-shake classes */}
      <style>{`
        html.attack-shake,html.attack-quake,html.attack-rumble{will-change:transform;background:#060609}
        html.attack-shake{animation:attackShake .9s cubic-bezier(.36,.07,.19,.97)}
        @keyframes attackShake{10%,90%{transform:translate3d(-1px,0,0)}20%,80%{transform:translate3d(2px,0,0)}30%,50%,70%{transform:translate3d(-4px,0,0)}40%,60%{transform:translate3d(4px,0,0)}}
        html.attack-quake{animation:attackQuake 1.4s cubic-bezier(.36,.07,.19,.97)}
        @keyframes attackQuake{
          0%,100%{transform:translate3d(0,0,0) rotate(0)}
          3%{transform:translate3d(-34px,-20px,0) rotate(-1.1deg)}
          7%{transform:translate3d(32px,24px,0) rotate(.9deg)}
          12%{transform:translate3d(-40px,14px,0) rotate(-1.3deg)}
          18%{transform:translate3d(36px,-22px,0) rotate(1.1deg)}
          25%{transform:translate3d(-28px,22px,0) rotate(-.8deg)}
          33%{transform:translate3d(24px,-16px,0) rotate(.6deg)}
          42%{transform:translate3d(-18px,12px,0) rotate(-.4deg)}
          52%{transform:translate3d(14px,-10px,0)}
          62%{transform:translate3d(-10px,7px,0)}
          72%{transform:translate3d(7px,-5px,0)}
          82%{transform:translate3d(-4px,3px,0)}
          92%{transform:translate3d(2px,-1px,0)}
        }
        html.attack-rumble{animation:attackRumble .12s linear infinite}
        @keyframes attackRumble{
          0%,100%{transform:translate3d(0,0,0)}
          25%{transform:translate3d(-1.5px,-1px,0)}
          50%{transform:translate3d(2px,1.5px,0)}
          75%{transform:translate3d(-1px,2px,0)}
        }
        @keyframes attackHexBreathe { 50% { opacity: .6 } }
        @keyframes attackSpin { to { transform: translate(-50%,-50%) rotate(360deg) } }
        @keyframes attackSpinRev { from { transform: translate(-50%,-50%) rotate(0) } to { transform: translate(-50%,-50%) rotate(-360deg) } }
        @keyframes attackPulse { 50% { opacity: .4 } }
        @keyframes attackCursor { 50% { opacity: 0 } }
        @keyframes attackFbStrip {
          0%   { transform: translate(-50%,-140%); opacity: 0 }
          3%   { transform: translate(-50%,0);     opacity: 1 }
          92%  { transform: translate(-50%,0);     opacity: 1 }
          100% { transform: translate(-50%,-140%); opacity: 0 }
        }
      `}</style>

      {/* Scanlines + grid backdrop */}
      <div
        aria-hidden
        style={{
          position: 'fixed',
          inset: 0,
          pointerEvents: 'none',
          zIndex: 1,
          background:
            'repeating-linear-gradient(to bottom, transparent 0 2px, rgba(255,255,255,.03) 2px 3px)',
        }}
      />
      <div
        aria-hidden
        style={{
          position: 'fixed',
          inset: 0,
          pointerEvents: 'none',
          zIndex: 1,
          backgroundImage:
            'linear-gradient(#14161f 1px, transparent 1px), linear-gradient(90deg, #14161f 1px, transparent 1px)',
          backgroundSize: '48px 48px',
          opacity: 0.28,
        }}
      />

      {/* Header */}
      <div
        style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          height: HEADER_H,
          zIndex: 30,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: `0 ${GUTTER}px`,
          fontFamily: '"JetBrains Mono", ui-monospace, monospace',
          fontSize: 11,
          color: '#6b7183',
          letterSpacing: '.22em',
          textTransform: 'uppercase',
          background:
            'linear-gradient(to bottom, rgba(12,13,18,.9), rgba(12,13,18,.5))',
          borderBottom: '1px solid rgba(255,42,42,.3)',
        }}
      >
        <span>
          <span style={{ color: '#ff2a2a' }}>▙</span>
          &nbsp;{eventTitle} //// TACTICAL-OPS
        </span>
        <span>
          <span style={{ color: '#3ae85c', display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            <span
              style={{
                width: 8,
                height: 8,
                borderRadius: '50%',
                background: '#3ae85c',
                boxShadow: '0 0 12px #3ae85c',
                animation: 'attackPulse 1.2s infinite',
              }}
            />
            LIVE
          </span>
          &nbsp;//&nbsp; {clockText} UTC
        </span>
      </div>

      {/* Footer */}
      <div
        style={{
          position: 'fixed',
          bottom: 0,
          left: 0,
          right: 0,
          height: FOOTER_H,
          zIndex: 30,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: `0 ${GUTTER}px`,
          fontFamily: '"JetBrains Mono", ui-monospace, monospace',
          fontSize: 10.5,
          color: '#6b7183',
          letterSpacing: '.22em',
          textTransform: 'uppercase',
          background: 'linear-gradient(to top, rgba(12,13,18,.9), rgba(12,13,18,.5))',
          borderTop: '1px solid rgba(255,211,74,.2)',
        }}
      >
        <span>
          SIGNAL NOMINAL &nbsp;//&nbsp; {positions.length} TEAMS &nbsp;//&nbsp; {eventCount} EVENTS
        </span>
        <span>GZCTF ATTACK-STREAM // PUBLIC</span>
      </div>

      {/* Sound-unlock toast */}
      {showAudioToast && (
        <div
          style={{
            position: 'fixed',
            top: HEADER_H + 14,
            left: '50%',
            transform: 'translateX(-50%)',
            zIndex: 200,
            background: '#f4b619',
            color: '#0b0b11',
            padding: '10px 22px',
            fontFamily: '"JetBrains Mono", ui-monospace, monospace',
            fontWeight: 700,
            fontSize: 12,
            letterSpacing: '.2em',
            textTransform: 'uppercase',
            cursor: 'pointer',
          }}
        >
          ▶ CLICK TO ENABLE SOUND
        </div>
      )}

      {/* Hidden first-blood audio */}
      <audio
        ref={audioRef}
        src={`/attack/firstblood.mp3?v=${import.meta.env.VITE_APP_GIT_SHA ?? 'dev'}`}
        preload="auto"
        style={{ display: 'none' }}
      />

      {/* Feed panel (left) */}
      <FeedPanel
        left={GUTTER}
        top={HEADER_H + GUTTER}
        bottom={FOOTER_H + GUTTER}
        width={
          Math.min(FEED_W_MAX, Math.max(FEED_W_MIN, viewport.w * 0.19))
        }
        lines={feedLines}
      />

      {/* Scoreboard + stats (right) */}
      <ScoreboardPanel
        right={GUTTER}
        top={HEADER_H + GUTTER}
        width={
          Math.min(BOARD_W_MAX, Math.max(BOARD_W_MIN, viewport.w * 0.16))
        }
        top5={top5}
        eventCount={eventCount}
        fbCount={fbCount}
        atkRate={atkRate}
      />

      {/* Theater: orbital deco rings + HQ + team nodes.
          Z-index sits ABOVE the pixi canvas (z:12) so HUD text (HQ title,
          team labels) renders on top of the bullets + charge glow. */}
      <div
        style={{ position: 'fixed', inset: 0, zIndex: 15, pointerEvents: 'none' }}
      >
        {/* Inner dashed ring */}
        <div
          style={{
            position: 'absolute',
            left: cx,
            top: cy,
            width: ringRx * 0.75 * 2,
            height: ringRy * 0.75 * 2,
            border: '1px dashed rgba(255,42,42,.22)',
            borderRadius: '50%',
            transform: 'translate(-50%,-50%)',
            animation: 'attackSpin 40s linear infinite',
          }}
        />
        {/* Outer dashed ring (gold) */}
        <div
          style={{
            position: 'absolute',
            left: cx,
            top: cy,
            width: ringRx * 2,
            height: ringRy * 2,
            border: '1px dashed rgba(255,211,74,.15)',
            borderRadius: '50%',
            transform: 'translate(-50%,-50%)',
            animation: 'attackSpinRev 70s linear infinite',
          }}
        />

        {/* HQ hex — outer wrapper holds the centering transform (static);
            the inner hex element is what hqPunch animates (scale/rotate only).
            Sits above the canvas so the title stays readable during the charge. */}
        <div
          ref={hqRef}
          style={{
            position: 'absolute',
            left: '50%',
            top: '50%',
            transform: 'translate(-50%,-50%)',
            zIndex: 15,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            pointerEvents: 'none',
          }}
        >
          <div
            ref={hexRef}
            style={{
              width: hqSize,
              height: hqSize * 1.12,
              position: 'relative',
              clipPath: 'polygon(50% 0,100% 25%,100% 75%,50% 100%,0 75%,0 25%)',
              background:
                'radial-gradient(circle at center, rgba(255,42,42,.1), transparent 70%)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexDirection: 'column',
              padding: 30,
              willChange: 'transform',
            }}
          >
            <div
              style={{
                fontFamily: '"JetBrains Mono", ui-monospace, monospace',
                fontSize: 10,
                letterSpacing: '.35em',
                color: '#f4b619',
                opacity: 0.85,
                marginBottom: 4,
              }}
            >
              // TARGET
            </div>
            <div
              style={{
                fontFamily: '"JetBrains Mono", ui-monospace, monospace',
                fontSize: 24,
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '.12em',
                color: '#ffd34a',
                textAlign: 'center',
                lineHeight: 1.15,
                textShadow: '0 0 22px rgba(255,211,74,.55)',
                maxWidth: hqSize - 30,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
              }}
            >
              {eventTitle}
            </div>
            <div
              style={{
                fontFamily: '"JetBrains Mono", ui-monospace, monospace',
                fontSize: 9.5,
                letterSpacing: '.4em',
                color: '#6b7183',
                marginTop: 10,
              }}
            >
              OPS · CONTROL · HQ
            </div>
          </div>
        </div>

        {/* Team nodes */}
        {positions.map((t) => {
          const isLeft = t.x < viewport.w / 2
          const nameShort = t.name.length > 18 ? `${t.name.slice(0, 17)}…` : t.name
          return (
            <div
              key={t.id}
              style={{
                position: 'absolute',
                left: t.x,
                top: t.y,
                transform: 'translate(-50%,-50%)',
                color: '#d7dbe4',
                fontFamily: '"JetBrains Mono", ui-monospace, monospace',
                fontSize: 10.5,
                letterSpacing: '.06em',
                textTransform: 'uppercase',
                display: 'flex',
                flexDirection: isLeft ? 'row' : 'row-reverse',
                alignItems: 'center',
                gap: t.labelled ? 6 : 0,
                whiteSpace: 'nowrap',
                pointerEvents: 'none',
              }}
            >
              <span
                style={{
                  width: t.labelled ? 10 : 8,
                  height: t.labelled ? 10 : 8,
                  borderRadius: '50%',
                  background: t.labelled ? '#6b7183' : '#ff2a2a',
                  opacity: t.labelled ? 1 : 0.5,
                  boxShadow: '0 0 6px currentColor',
                  flexShrink: 0,
                }}
              />
              {t.labelled && <span>{nameShort}</span>}
            </div>
          )
        })}
      </div>

      {/* Rank column (active-only layout) */}
      {layout === 'active' && rankColumn && (
        <div
          style={{
            position: 'fixed',
            left: rankColumn.x,
            top: rankColumn.y,
            width: rankColumn.w,
            height: rankColumn.h,
            zIndex: 18,
            background: 'rgba(12,13,18,.94)',
            padding: '14px 16px',
            overflow: 'hidden',
            fontFamily: '"JetBrains Mono", ui-monospace, monospace',
          }}
        >
          <div
            style={{
              fontSize: 10,
              letterSpacing: '.25em',
              color: '#3ae85c',
              marginBottom: 10,
            }}
          >
            // ACTIVE · TOP 20 (of {positions.length})
          </div>
          {sortedTop.map((t) => (
            <div
              key={t.id}
              style={{
                display: 'grid',
                gridTemplateColumns: '28px 1fr',
                gap: 6,
                fontSize: 11.5,
                padding: '3px 0',
                borderBottom: '1px dashed #1c1f2a',
              }}
            >
              <span style={{ color: '#ffd34a', fontWeight: 700 }}>
                {String(t.rank).padStart(2, '0')}
              </span>
              <span
                style={{
                  color: '#d7dbe4',
                  letterSpacing: '.06em',
                  textTransform: 'uppercase',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {t.name}
              </span>
            </div>
          ))}
        </div>
      )}

      {/* PixiJS canvas — sized via the renderer */}
      <canvas
        ref={canvasRef}
        style={{
          position: 'fixed',
          inset: 0,
          width: '100vw',
          height: '100vh',
          zIndex: 12,
          pointerEvents: 'none',
          willChange: 'transform',
          transform: 'translateZ(0)',
        }}
      />

      {/* First-blood strip (slides down from header) */}
      {firstBlood && (
        <div
          key={firstBlood.id}
          style={{
            position: 'fixed',
            left: '50%',
            top: HEADER_H + 12,
            transform: 'translate(-50%,-140%)',
            zIndex: 40,
            display: 'flex',
            alignItems: 'stretch',
            fontFamily: '"JetBrains Mono", ui-monospace, monospace',
            fontWeight: 700,
            textTransform: 'uppercase',
            border: '2px solid #0b0b11',
            animation: `attackFbStrip ${FIRST_BLOOD_BANNER_MS}ms ease-out both`,
          }}
        >
          <div
            style={{
              background: '#ff2a2a',
              color: '#0b0b11',
              padding: '10px 18px',
              letterSpacing: '.24em',
              fontSize: 14,
              display: 'flex',
              alignItems: 'center',
              gap: 8,
            }}
          >
            ▙ FIRST BLOOD
          </div>
          <div
            style={{
              background: '#0b0b11',
              color: '#f4b619',
              padding: '10px 18px',
              letterSpacing: '.22em',
              fontSize: 12,
              display: 'flex',
              alignItems: 'center',
              borderLeft: '2px solid #ff2a2a',
            }}
          >
            {firstBlood.challengeTitle} · {firstBlood.teamName}
          </div>
        </div>
      )}
    </div>
  )
}

/* -------------------------------------------------------------------------- */
/* Feed panel                                                                  */
/* -------------------------------------------------------------------------- */

interface FeedPanelProps {
  left: number
  top: number
  bottom: number
  width: number
  lines: FeedLine[]
}

const FeedPanel: FC<FeedPanelProps> = ({ left, top, bottom, width, lines }) => {
  return (
    <div
      style={{
        position: 'fixed',
        left,
        top,
        bottom,
        width,
        zIndex: 20,
        background: 'rgba(12,13,18,.94)',
        padding: '16px 18px 14px',
        overflow: 'hidden',
        contain: 'layout style paint' as React.CSSProperties['contain'],
      }}
    >
      {/* Bracket corners */}
      <Bracket placement="tl" />
      <Bracket placement="tr" />
      <Bracket placement="bl" />
      <Bracket placement="br" />

      <div
        style={{
          fontFamily: '"JetBrains Mono", ui-monospace, monospace',
          fontSize: 10,
          letterSpacing: '.25em',
          color: '#ff2a2a',
          marginBottom: 12,
        }}
      >
        // EVENT STREAM
      </div>
      {lines.map((line) => (
        <div
          key={line.key}
          style={{
            fontFamily: '"JetBrains Mono", ui-monospace, monospace',
            fontSize: 12,
            lineHeight: 1.5,
            marginBottom: 3,
            whiteSpace: 'nowrap',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
          }}
        >
          <span style={{ color: '#6b7183' }}>{line.time}</span>{' '}
          <span
            style={{
              color: lineColorFor(line.type),
              fontWeight: line.type === SubmissionType.FirstBlood ? 700 : 400,
              opacity: line.type === SubmissionType.Unaccepted ? 0.55 : 1,
            }}
          >
            {feedPrefix(line.type)}
            {line.teamName} :: {line.challengeTitle}
          </span>
        </div>
      ))}
      <div
        style={{
          fontFamily: '"JetBrains Mono", ui-monospace, monospace',
          fontSize: 12,
          lineHeight: 1.5,
          color: '#6b7183',
        }}
      >
        $ gzctf.watch()
        <span
          style={{
            display: 'inline-block',
            width: 7,
            height: 12,
            background: '#3ae85c',
            verticalAlign: -1,
            animation: 'attackCursor 1s steps(2,start) infinite',
            marginLeft: 2,
          }}
        />
      </div>
    </div>
  )
}

const lineColorFor = (t: SubmissionType): string => {
  switch (t) {
    case SubmissionType.FirstBlood:
      return '#ff2a2a'
    case SubmissionType.Normal:
      return '#3ae85c'
    case SubmissionType.SecondBlood:
    case SubmissionType.ThirdBlood:
      return '#f4b619'
    default:
      return '#ff6262'
  }
}

const Bracket: FC<{ placement: 'tl' | 'tr' | 'bl' | 'br' }> = ({ placement }) => {
  const base: React.CSSProperties = {
    position: 'absolute',
    width: 16,
    height: 16,
    border: '2px solid #ff2a2a',
  }
  if (placement === 'tl')
    Object.assign(base, { top: -1, left: -1, borderRight: 'none', borderBottom: 'none' })
  if (placement === 'tr')
    Object.assign(base, { top: -1, right: -1, borderLeft: 'none', borderBottom: 'none' })
  if (placement === 'bl')
    Object.assign(base, { bottom: -1, left: -1, borderRight: 'none', borderTop: 'none' })
  if (placement === 'br')
    Object.assign(base, { bottom: -1, right: -1, borderLeft: 'none', borderTop: 'none' })
  return <div style={base} />
}

/* -------------------------------------------------------------------------- */
/* Scoreboard + stats panel                                                    */
/* -------------------------------------------------------------------------- */

interface ScoreboardPanelProps {
  right: number
  top: number
  width: number
  top5: ScoreboardItem[]
  eventCount: number
  fbCount: number
  atkRate: string
}

const ScoreboardPanel: FC<ScoreboardPanelProps> = ({
  right,
  top,
  width,
  top5,
  eventCount,
  fbCount,
  atkRate,
}) => {
  const rowCount = Math.max(top5.length, 1)
  const approxBoardHeight = 6 * 32 + 36
  const statsTop = top + approxBoardHeight + 10

  return (
    <>
      <div
        style={{
          position: 'fixed',
          right,
          top,
          width,
          zIndex: 20,
          background: 'rgba(12,13,18,.94)',
          padding: '16px 18px 14px',
          contain: 'layout style paint' as React.CSSProperties['contain'],
        }}
      >
        <Bracket placement="tl" />
        <Bracket placement="tr" />
        <Bracket placement="bl" />
        <Bracket placement="br" />
        <div
          style={{
            fontFamily: '"JetBrains Mono", ui-monospace, monospace',
            fontSize: 10,
            letterSpacing: '.25em',
            color: '#f4b619',
            marginBottom: 12,
            display: 'flex',
            justifyContent: 'space-between',
          }}
        >
          <span>// SCOREBOARD</span>
          <span>TOP {rowCount}</span>
        </div>
        {top5.map((t) => (
          <div
            key={t.id}
            style={{
              display: 'grid',
              gridTemplateColumns: '32px 1fr auto',
              alignItems: 'baseline',
              padding: '6px 0',
              borderBottom: '1px dashed #20232d',
              gap: 8,
            }}
          >
            <span
              style={{
                fontFamily: '"JetBrains Mono", ui-monospace, monospace',
                fontWeight: 700,
                fontSize: 20,
                color: '#ffd34a',
                lineHeight: 1,
                textAlign: 'right',
                textShadow: t.rank === 1 ? '0 0 14px rgba(255,211,74,.6)' : undefined,
              }}
            >
              {String(t.rank).padStart(2, '0')}
            </span>
            <span
              style={{
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '.07em',
                fontSize: 12,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {t.name}
            </span>
            <span
              style={{
                fontFamily: '"JetBrains Mono", ui-monospace, monospace',
                fontWeight: 500,
                color: '#6b7183',
                fontSize: 11.5,
              }}
            >
              {t.score}
            </span>
          </div>
        ))}
        {top5.length === 0 && (
          <div style={{ color: '#475569', fontSize: 12 }}>No scoreboard yet</div>
        )}
      </div>
      <div
        style={{
          position: 'fixed',
          right,
          top: statsTop,
          width,
          zIndex: 20,
          background: 'rgba(12,13,18,.92)',
          padding: '12px 18px',
          contain: 'layout style paint' as React.CSSProperties['contain'],
        }}
      >
        <div
          style={{
            fontFamily: '"JetBrains Mono", ui-monospace, monospace',
            fontSize: 10,
            letterSpacing: '.25em',
            color: '#3ae85c',
            marginBottom: 8,
          }}
        >
          // STATS
        </div>
        <StatsRow label="ATK_RATE" value={atkRate} />
        <StatsRow label="1ST_BLOOD" value={String(fbCount)} />
        <StatsRow label="EVENTS" value={String(eventCount)} />
      </div>
    </>
  )
}

const StatsRow: FC<{ label: string; value: string }> = ({ label, value }) => (
  <div
    style={{
      display: 'flex',
      justifyContent: 'space-between',
      padding: '3px 0',
      fontFamily: '"JetBrains Mono", ui-monospace, monospace',
      fontSize: 11,
    }}
  >
    <span style={{ color: '#6b7183', letterSpacing: '.1em' }}>{label}</span>
    <span style={{ color: '#d7dbe4', fontWeight: 700 }}>{value}</span>
  </div>
)

export default Attack
