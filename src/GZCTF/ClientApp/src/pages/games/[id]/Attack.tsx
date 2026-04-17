/**
 * Public per-game attack animation page.
 *
 * Route: /games/{id}/attack (no authentication).
 * Layout adapts to scoreboard size:
 *   - teams <= 30  -> radial ring (SVG)
 *   - teams <= 80  -> two concentric rings (SVG)
 *   - teams  > 80  -> vertical list + canvas particles
 *
 * Each flag submission spawns a particle traveling from the team's position to
 * the central HQ along a quadratic bezier. Colors encode SubmissionType:
 *   Normal -> green, FirstBlood -> gold, Second/Third -> amber, Unaccepted -> red.
 *
 * First bloods trigger a red full-screen pulse + bundled sound (rate-limited).
 */
import * as signalR from '@microsoft/signalr'
import { FC, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useParams } from 'react-router'
import api, {
  ChallengeCategory,
  DetailedGameInfoModel,
  ScoreboardItem,
  ScoreboardModel,
  SubmissionType,
} from '@Api'

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

interface Particle {
  id: number
  fromX: number
  fromY: number
  ctrlX: number
  ctrlY: number
  toX: number
  toY: number
  color: string
  glow: boolean
  startedAt: number
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

const PARTICLE_DURATION_MS = 1200
const MAX_PARTICLES = 100
const FIRST_BLOOD_DURATION_MS = 3000
const FIRST_BLOOD_COOLDOWN_MS = 1500
const SCOREBOARD_REFRESH_MS = 30_000
const SCOREBOARD_DEBOUNCE_MS = 2000

const colorForType = (t: SubmissionType): { color: string; glow: boolean; opacity: number } => {
  switch (t) {
    case SubmissionType.Normal:
      return { color: '#22c55e', glow: false, opacity: 1 }
    case SubmissionType.FirstBlood:
      return { color: '#eab308', glow: true, opacity: 1 }
    case SubmissionType.SecondBlood:
    case SubmissionType.ThirdBlood:
      return { color: '#f59e0b', glow: false, opacity: 1 }
    case SubmissionType.Unaccepted:
    default:
      return { color: '#ef4444', glow: false, opacity: 0.4 }
  }
}

const typeLabel = (t: SubmissionType): string => {
  switch (t) {
    case SubmissionType.FirstBlood:
      return '1st Blood'
    case SubmissionType.SecondBlood:
      return '2nd Blood'
    case SubmissionType.ThirdBlood:
      return '3rd Blood'
    case SubmissionType.Normal:
      return 'Solve'
    default:
      return 'Miss'
  }
}

/* -------------------------------------------------------------------------- */
/* Team positioning                                                           */
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

type Layout = 'ring' | 'dual-ring' | 'list'

const computeLayout = (teams: ScoreboardItem[], width: number, height: number): {
  layout: Layout
  positions: TeamPos[]
  cx: number
  cy: number
  hqRadius: number
} => {
  const cx = width / 2
  const cy = height / 2

  const sorted = [...teams].sort((a, b) => a.rank - b.rank)

  let layout: Layout
  if (sorted.length <= 30) layout = 'ring'
  else if (sorted.length <= 80) layout = 'dual-ring'
  else layout = 'list'

  const positions: TeamPos[] = []
  const minDim = Math.min(width, height)
  const hqRadius = Math.max(60, minDim * 0.08)

  if (layout === 'ring') {
    const r = minDim * 0.4
    sorted.forEach((t, i) => {
      const theta = (i / sorted.length) * Math.PI * 2 - Math.PI / 2
      positions.push({
        id: t.id,
        name: t.name,
        score: t.score,
        rank: t.rank,
        avatar: t.avatar ?? null,
        x: cx + r * Math.cos(theta),
        y: cy + r * Math.sin(theta),
        labelled: true,
      })
    })
  } else if (layout === 'dual-ring') {
    const rInner = minDim * 0.28
    const rOuter = minDim * 0.45
    const inner = sorted.slice(0, 12)
    const outer = sorted.slice(12)

    inner.forEach((t, i) => {
      const theta = (i / inner.length) * Math.PI * 2 - Math.PI / 2
      positions.push({
        id: t.id,
        name: t.name,
        score: t.score,
        rank: t.rank,
        avatar: t.avatar ?? null,
        x: cx + rInner * Math.cos(theta),
        y: cy + rInner * Math.sin(theta),
        labelled: true,
      })
    })
    outer.forEach((t, i) => {
      const theta = (i / outer.length) * Math.PI * 2 - Math.PI / 2
      positions.push({
        id: t.id,
        name: t.name,
        score: t.score,
        rank: t.rank,
        avatar: t.avatar ?? null,
        x: cx + rOuter * Math.cos(theta),
        y: cy + rOuter * Math.sin(theta),
        labelled: false,
      })
    })
  } else {
    // list: left column, ranked by score, 20 visible via scroll
    const listX = Math.max(120, width * 0.08)
    const listTop = height * 0.12
    const listBottom = height * 0.95
    const visibleCount = Math.min(sorted.length, 20)
    const rowH = (listBottom - listTop) / Math.max(visibleCount, 1)
    sorted.forEach((t, i) => {
      positions.push({
        id: t.id,
        name: t.name,
        score: t.score,
        rank: t.rank,
        avatar: t.avatar ?? null,
        x: listX,
        y: listTop + (i + 0.5) * rowH,
        labelled: true,
      })
    })
  }

  return { layout, positions, cx, cy, hqRadius }
}

/* -------------------------------------------------------------------------- */
/* Page component                                                             */
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

  // Sound-unlock overlay state
  const [audioEnabled, setAudioEnabled] = useState(false)
  const [showAudioToast, setShowAudioToast] = useState(true)

  // Visualization state refs (non-react to avoid re-renders per frame)
  const particlesRef = useRef<Particle[]>([])
  const nextIdRef = useRef(1)
  const lastBloodSoundAtRef = useRef(0)

  // Recent events for the bottom scrolling banner and first blood overlay
  const [recentEvents, setRecentEvents] = useState<AttackEvent[]>([])
  const [firstBlood, setFirstBlood] = useState<FirstBloodBanner | null>(null)

  // Force re-render tick for SVG particles
  const [, setTick] = useState(0)

  // Audio element ref
  const audioRef = useRef<HTMLAudioElement | null>(null)

  // Debounced scoreboard refresh per-team
  const scoreboardRefreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  /* ---- Viewport tracking ---- */
  useEffect(() => {
    const onResize = () => setViewport({ w: window.innerWidth, h: window.innerHeight })
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
        // Non-fatal: the page will still render the HQ title as "Attack"
        // eslint-disable-next-line no-console
        console.warn('[attack] initial load failed', e)
      }

      // Seed recent events from the public attack-feed endpoint.
      try {
        const feedRes = await fetch(`/api/game/${numId}/AttackFeed?limit=50`)
        if (feedRes.ok) {
          const feed = (await feedRes.json()) as AttackEvent[]
          setRecentEvents(feed.slice(-8).reverse())
        }
      } catch (e) {
        // eslint-disable-next-line no-console
        console.warn('[attack] attack-feed failed', e)
      }
    })()
  }, [numId])

  /* ---- Periodic scoreboard refresh ---- */
  const refreshScoreboard = useCallback(async () => {
    try {
      const res = await api.game.gameScoreboard(numId)
      setScoreboard(res.data)
    } catch {
      // ignore
    }
  }, [numId])

  useEffect(() => {
    if (Number.isNaN(numId) || numId < 0) return
    const iv = setInterval(() => void refreshScoreboard(), SCOREBOARD_REFRESH_MS)
    return () => clearInterval(iv)
  }, [numId, refreshScoreboard])

  /* ---- Layout based on current scoreboard ---- */
  const { layout, positions, cx, cy, hqRadius } = useMemo(
    () => computeLayout(scoreboard?.items ?? [], viewport.w, viewport.h),
    [scoreboard, viewport.w, viewport.h]
  )

  const teamIndex = useMemo(() => {
    const m = new Map<string, TeamPos>()
    positions.forEach((p) => m.set(p.name, p))
    return m
  }, [positions])

  /* ---- Spawn a particle ---- */
  const spawnParticle = useCallback(
    (evt: AttackEvent) => {
      const from = teamIndex.get(evt.teamName)
      if (!from) return
      const { color, glow } = colorForType(evt.type)

      // Random mid-point offset so particles don't all stack
      const midX = (from.x + cx) / 2 + (Math.random() - 0.5) * Math.min(viewport.w, viewport.h) * 0.12
      const midY = (from.y + cy) / 2 + (Math.random() - 0.5) * Math.min(viewport.w, viewport.h) * 0.12

      const p: Particle = {
        id: nextIdRef.current++,
        fromX: from.x,
        fromY: from.y,
        ctrlX: midX,
        ctrlY: midY,
        toX: cx,
        toY: cy,
        color,
        glow,
        startedAt: performance.now(),
        type: evt.type,
      }

      particlesRef.current.push(p)
      if (particlesRef.current.length > MAX_PARTICLES) {
        particlesRef.current.splice(0, particlesRef.current.length - MAX_PARTICLES)
      }
    },
    [teamIndex, cx, cy, viewport.w, viewport.h]
  )

  /* ---- Animation frame loop ---- */
  useEffect(() => {
    let raf = 0
    const tick = () => {
      const now = performance.now()
      particlesRef.current = particlesRef.current.filter((p) => now - p.startedAt < PARTICLE_DURATION_MS)
      setTick((t) => (t + 1) % 1_000_000)
      raf = requestAnimationFrame(tick)
    }
    raf = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(raf)
  }, [])

  /* ---- Audio unlock on first user interaction ---- */
  useEffect(() => {
    if (audioEnabled) return
    const unlock = () => {
      setAudioEnabled(true)
      setShowAudioToast(false)
      // Prime the audio element
      if (audioRef.current) {
        audioRef.current.volume = 0
        audioRef.current.play().then(() => {
          audioRef.current?.pause()
          if (audioRef.current) audioRef.current.currentTime = 0
          if (audioRef.current) audioRef.current.volume = 0.8
        }).catch(() => undefined)
      }
    }
    window.addEventListener('click', unlock, { once: true })
    window.addEventListener('keydown', unlock, { once: true })
    return () => {
      window.removeEventListener('click', unlock)
      window.removeEventListener('keydown', unlock)
    }
  }, [audioEnabled])

  /* ---- Handle incoming attack event ---- */
  const handleAttack = useCallback(
    (evt: AttackEvent) => {
      spawnParticle(evt)
      setRecentEvents((prev) => [evt, ...prev].slice(0, 8))

      // First blood overlay + sound
      if (evt.type === SubmissionType.FirstBlood) {
        setFirstBlood({
          id: Math.random(),
          teamName: evt.teamName,
          challengeTitle: evt.challengeTitle,
          startedAt: performance.now(),
        })
        const now = performance.now()
        if (audioEnabled && audioRef.current && now - lastBloodSoundAtRef.current > FIRST_BLOOD_COOLDOWN_MS) {
          lastBloodSoundAtRef.current = now
          audioRef.current.currentTime = 0
          audioRef.current.play().catch(() => undefined)
        }
      }

      // Debounced scoreboard refresh (any valid solve affects rank)
      if (evt.type !== SubmissionType.Unaccepted) {
        if (scoreboardRefreshTimerRef.current) clearTimeout(scoreboardRefreshTimerRef.current)
        scoreboardRefreshTimerRef.current = setTimeout(() => {
          void refreshScoreboard()
        }, SCOREBOARD_DEBOUNCE_MS)
      }
    },
    [spawnParticle, audioEnabled, refreshScoreboard]
  )

  /* ---- First-blood overlay fade-out ---- */
  useEffect(() => {
    if (!firstBlood) return
    const timer = setTimeout(() => setFirstBlood(null), FIRST_BLOOD_DURATION_MS)
    return () => clearTimeout(timer)
  }, [firstBlood])

  /* ---- SignalR connection ---- */
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

  /* ---- Derived UI ---- */
  const now = performance.now()

  const top5 = useMemo(
    () => (scoreboard?.items ?? []).slice().sort((a, b) => a.rank - b.rank).slice(0, 5),
    [scoreboard]
  )

  /* ---- Render ---- */

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        width: '100vw',
        height: '100vh',
        background: '#0b0b12',
        color: '#e5e7eb',
        fontFamily: 'Lexend, sans-serif',
        overflow: 'hidden',
        userSelect: 'none',
      }}
    >
      {/* Shared keyframes */}
      <style>{`
        @keyframes attackHqPulse {
          0%, 100% { filter: drop-shadow(0 0 12px rgba(96,165,250,.35)) drop-shadow(0 0 32px rgba(96,165,250,.15)); transform: scale(1); }
          50% { filter: drop-shadow(0 0 24px rgba(96,165,250,.65)) drop-shadow(0 0 64px rgba(96,165,250,.25)); transform: scale(1.02); }
        }
        @keyframes firstBloodPulse {
          0% { opacity: 0; transform: scale(0.95); }
          15% { opacity: 1; transform: scale(1); }
          80% { opacity: 1; transform: scale(1); }
          100% { opacity: 0; transform: scale(1.02); }
        }
        @keyframes fbScreenFlash {
          0% { opacity: 0; }
          15% { opacity: 0.55; }
          80% { opacity: 0.35; }
          100% { opacity: 0; }
        }
        @keyframes attackBannerSlide {
          from { transform: translateX(40px); opacity: 0; }
          to { transform: translateX(0); opacity: 1; }
        }
        @keyframes attackToastFade {
          from { opacity: 0; transform: translateY(-8px); }
          to { opacity: 1; transform: translateY(0); }
        }
      `}</style>

      {/* Hidden audio element */}
      <audio
        ref={audioRef}
        src="/attack/firstblood.mp3"
        preload="auto"
        style={{ display: 'none' }}
      />

      {/* Audio-unlock toast */}
      {showAudioToast && (
        <div
          style={{
            position: 'absolute',
            top: 16,
            left: 16,
            padding: '10px 16px',
            background: 'rgba(17,24,39,0.85)',
            border: '1px solid rgba(96,165,250,0.35)',
            borderRadius: 8,
            fontSize: 13,
            color: '#cbd5e1',
            backdropFilter: 'blur(6px)',
            animation: 'attackToastFade 300ms ease-out',
            zIndex: 50,
          }}
        >
          Click anywhere to enable sound
        </div>
      )}

      {/* Main SVG canvas (for ring and dual-ring layouts) */}
      {layout !== 'list' && (
        <svg
          width={viewport.w}
          height={viewport.h}
          viewBox={`0 0 ${viewport.w} ${viewport.h}`}
          style={{ position: 'absolute', inset: 0 }}
        >
          {/* Faint orbit rings */}
          <g opacity={0.12} stroke="#60a5fa" fill="none" strokeWidth={1}>
            {layout === 'ring' && (
              <circle cx={cx} cy={cy} r={Math.min(viewport.w, viewport.h) * 0.4} />
            )}
            {layout === 'dual-ring' && (
              <>
                <circle cx={cx} cy={cy} r={Math.min(viewport.w, viewport.h) * 0.28} />
                <circle cx={cx} cy={cy} r={Math.min(viewport.w, viewport.h) * 0.45} />
              </>
            )}
          </g>

          {/* Team nodes */}
          {positions.map((t) => (
            <g key={t.id}>
              <circle
                cx={t.x}
                cy={t.y}
                r={t.labelled ? 7 : 3.5}
                fill="#1f2937"
                stroke="#60a5fa"
                strokeWidth={1.2}
              />
              {t.labelled && (
                <text
                  x={t.x}
                  y={t.y + 20}
                  textAnchor="middle"
                  fontSize={11}
                  fill="#94a3b8"
                  style={{ pointerEvents: 'none' }}
                >
                  {t.name.length > 18 ? `${t.name.slice(0, 17)}…` : t.name}
                </text>
              )}
            </g>
          ))}

          {/* Particles */}
          {particlesRef.current.map((p) => {
            const t = Math.min(1, (now - p.startedAt) / PARTICLE_DURATION_MS)
            const oneMinusT = 1 - t
            // quadratic bezier
            const bx = oneMinusT * oneMinusT * p.fromX + 2 * oneMinusT * t * p.ctrlX + t * t * p.toX
            const by = oneMinusT * oneMinusT * p.fromY + 2 * oneMinusT * t * p.ctrlY + t * t * p.toY
            const opacity = (1 - t) * (p.type === SubmissionType.Unaccepted ? 0.4 : 1)
            const size = 5 + 3 * (1 - t)
            return (
              <circle
                key={p.id}
                cx={bx}
                cy={by}
                r={size}
                fill={p.color}
                opacity={opacity}
                style={{
                  filter: p.glow ? `drop-shadow(0 0 8px ${p.color}) drop-shadow(0 0 16px ${p.color})` : undefined,
                }}
              />
            )
          })}

          {/* HQ core */}
          <circle
            cx={cx}
            cy={cy}
            r={hqRadius}
            fill="rgba(15,23,42,0.8)"
            stroke="#60a5fa"
            strokeWidth={2}
            style={{ animation: 'attackHqPulse 1.5s ease-in-out infinite' }}
          />
        </svg>
      )}

      {/* List layout: canvas renderer */}
      {layout === 'list' && (
        <ListLayoutCanvas
          positions={positions}
          particles={particlesRef.current}
          cx={cx}
          cy={cy}
          hqRadius={hqRadius}
          viewportW={viewport.w}
          viewportH={viewport.h}
          now={now}
        />
      )}

      {/* Central HQ title overlay */}
      <div
        style={{
          position: 'absolute',
          left: 0,
          top: 0,
          width: '100vw',
          height: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          pointerEvents: 'none',
        }}
      >
        <div
          style={{
            textAlign: 'center',
            animation: 'attackHqPulse 1.5s ease-in-out infinite',
          }}
        >
          <div
            style={{
              fontSize: `clamp(28px, ${Math.max(3, Math.min(viewport.w, viewport.h) * 0.004)}vmin, 72px)`,
              fontWeight: 800,
              letterSpacing: '0.08em',
              background: 'linear-gradient(180deg,#ffffff 0%,#a5b4fc 100%)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              textTransform: 'uppercase',
              maxWidth: '60vw',
              margin: '0 auto',
              lineHeight: 1.1,
            }}
          >
            {game?.title ?? 'Attack'}
          </div>
          <div
            style={{
              marginTop: 8,
              fontSize: 12,
              letterSpacing: '0.3em',
              color: '#64748b',
              textTransform: 'uppercase',
            }}
          >
            Live Attack Radar
          </div>
        </div>
      </div>

      {/* Top-5 leaderboard (top-right) */}
      <div
        style={{
          position: 'absolute',
          top: 24,
          right: 24,
          width: 280,
          padding: 16,
          background: 'rgba(17,24,39,0.65)',
          border: '1px solid rgba(96,165,250,0.2)',
          borderRadius: 10,
          backdropFilter: 'blur(8px)',
          fontSize: 13,
          zIndex: 5,
        }}
      >
        <div style={{ fontSize: 11, letterSpacing: '0.2em', color: '#60a5fa', marginBottom: 10 }}>
          TOP 5
        </div>
        {top5.length === 0 && (
          <div style={{ color: '#64748b', fontSize: 12 }}>No scoreboard yet</div>
        )}
        {top5.map((t) => (
          <div
            key={t.id}
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              marginBottom: 6,
              gap: 8,
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1, overflow: 'hidden' }}>
              <span style={{ color: '#60a5fa', minWidth: 16 }}>{t.rank}</span>
              <span
                style={{
                  color: '#e5e7eb',
                  fontWeight: 600,
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {t.name}
              </span>
            </div>
            <span style={{ color: '#fcd34d', fontVariantNumeric: 'tabular-nums' }}>{t.score}</span>
          </div>
        ))}
      </div>

      {/* Bottom scrolling event banner */}
      <div
        style={{
          position: 'absolute',
          bottom: 0,
          left: 0,
          right: 0,
          padding: '12px 24px',
          background: 'linear-gradient(180deg, rgba(15,23,42,0) 0%, rgba(15,23,42,0.85) 60%)',
          display: 'flex',
          alignItems: 'center',
          gap: 16,
          fontSize: 13,
          overflow: 'hidden',
          zIndex: 5,
        }}
      >
        <div style={{ fontSize: 10, letterSpacing: '0.2em', color: '#60a5fa', flexShrink: 0 }}>
          LIVE FEED
        </div>
        <div style={{ display: 'flex', gap: 20, overflow: 'hidden' }}>
          {recentEvents.length === 0 && (
            <div style={{ color: '#475569' }}>Waiting for first submission…</div>
          )}
          {recentEvents.map((e, i) => {
            const { color } = colorForType(e.type)
            return (
              <div
                key={`${e.time}-${i}`}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 8,
                  animation: i === 0 ? 'attackBannerSlide 500ms ease-out' : undefined,
                  whiteSpace: 'nowrap',
                }}
              >
                <span style={{ color: '#e5e7eb', fontWeight: 600 }}>{e.teamName}</span>
                <span style={{ color: '#94a3b8' }}>→</span>
                <span style={{ color: '#cbd5e1' }}>{e.challengeTitle}</span>
                <span
                  style={{
                    background: color,
                    color: '#0b0b12',
                    padding: '2px 8px',
                    borderRadius: 12,
                    fontSize: 10,
                    fontWeight: 700,
                    textTransform: 'uppercase',
                    letterSpacing: '0.1em',
                  }}
                >
                  {typeLabel(e.type)}
                </span>
              </div>
            )
          })}
        </div>
      </div>

      {/* First blood overlay */}
      {firstBlood && (
        <>
          <div
            style={{
              position: 'absolute',
              inset: 0,
              background:
                'radial-gradient(ellipse at center, rgba(239,68,68,0.45) 0%, rgba(127,29,29,0.35) 60%, rgba(0,0,0,0) 100%)',
              pointerEvents: 'none',
              animation: `fbScreenFlash ${FIRST_BLOOD_DURATION_MS}ms ease-out forwards`,
              zIndex: 40,
            }}
          />
          <div
            style={{
              position: 'absolute',
              left: 0,
              right: 0,
              top: '42%',
              display: 'flex',
              justifyContent: 'center',
              pointerEvents: 'none',
              animation: `firstBloodPulse ${FIRST_BLOOD_DURATION_MS}ms ease-out forwards`,
              zIndex: 41,
            }}
          >
            <div
              style={{
                padding: '24px 48px',
                background: 'rgba(127,29,29,0.85)',
                border: '2px solid #fecaca',
                borderRadius: 14,
                boxShadow: '0 0 80px rgba(239,68,68,0.6), 0 0 160px rgba(239,68,68,0.3)',
                textAlign: 'center',
                maxWidth: '80vw',
              }}
            >
              <div style={{ fontSize: 40, fontWeight: 900, color: '#fff1f2', letterSpacing: '0.06em' }}>
                🩸 FIRST BLOOD
              </div>
              <div style={{ marginTop: 6, fontSize: 22, color: '#fee2e2', fontWeight: 700 }}>
                {firstBlood.challengeTitle}
              </div>
              <div style={{ marginTop: 4, fontSize: 18, color: '#fecaca', letterSpacing: '0.08em' }}>
                {firstBlood.teamName.toUpperCase()}
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}

/* -------------------------------------------------------------------------- */
/* Canvas layout for >80 teams                                                */
/* -------------------------------------------------------------------------- */

interface ListLayoutCanvasProps {
  positions: TeamPos[]
  particles: Particle[]
  cx: number
  cy: number
  hqRadius: number
  viewportW: number
  viewportH: number
  now: number
}

const ListLayoutCanvas: FC<ListLayoutCanvasProps> = ({
  positions,
  particles,
  cx,
  cy,
  hqRadius,
  viewportW,
  viewportH,
  now,
}) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null)

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')
    if (!ctx) return

    const dpr = window.devicePixelRatio || 1
    canvas.width = viewportW * dpr
    canvas.height = viewportH * dpr
    canvas.style.width = `${viewportW}px`
    canvas.style.height = `${viewportH}px`
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0)

    ctx.clearRect(0, 0, viewportW, viewportH)

    // Background guide line (left column)
    if (positions.length > 0) {
      const first = positions[0]
      ctx.strokeStyle = 'rgba(96,165,250,0.08)'
      ctx.lineWidth = 1
      ctx.beginPath()
      ctx.moveTo(first.x, viewportH * 0.1)
      ctx.lineTo(first.x, viewportH * 0.96)
      ctx.stroke()
    }

    // Team rows (only top 20 visible to keep text readable)
    const visible = positions.slice(0, 20)
    ctx.font = '12px Lexend, sans-serif'
    visible.forEach((t) => {
      ctx.fillStyle = '#1f2937'
      ctx.strokeStyle = '#60a5fa'
      ctx.lineWidth = 1.2
      ctx.beginPath()
      ctx.arc(t.x, t.y, 5, 0, Math.PI * 2)
      ctx.fill()
      ctx.stroke()

      ctx.fillStyle = '#94a3b8'
      ctx.textAlign = 'left'
      ctx.textBaseline = 'middle'
      const name = t.name.length > 22 ? `${t.name.slice(0, 21)}…` : t.name
      ctx.fillText(`${t.rank}. ${name}`, t.x + 12, t.y)

      ctx.fillStyle = '#fcd34d'
      ctx.textAlign = 'right'
      ctx.fillText(`${t.score}`, t.x + 260, t.y)
    })

    // Particles
    particles.forEach((p) => {
      const t = Math.min(1, (now - p.startedAt) / PARTICLE_DURATION_MS)
      const oneMinusT = 1 - t
      const bx = oneMinusT * oneMinusT * p.fromX + 2 * oneMinusT * t * p.ctrlX + t * t * p.toX
      const by = oneMinusT * oneMinusT * p.fromY + 2 * oneMinusT * t * p.ctrlY + t * t * p.toY
      const opacity = (1 - t) * (p.type === SubmissionType.Unaccepted ? 0.4 : 1)
      const size = 5 + 3 * (1 - t)

      ctx.globalAlpha = opacity
      if (p.glow) {
        ctx.shadowColor = p.color
        ctx.shadowBlur = 16
      } else {
        ctx.shadowBlur = 0
      }
      ctx.fillStyle = p.color
      ctx.beginPath()
      ctx.arc(bx, by, size, 0, Math.PI * 2)
      ctx.fill()
    })
    ctx.globalAlpha = 1
    ctx.shadowBlur = 0

    // HQ core
    ctx.fillStyle = 'rgba(15,23,42,0.8)'
    ctx.strokeStyle = '#60a5fa'
    ctx.lineWidth = 2
    ctx.beginPath()
    ctx.arc(cx, cy, hqRadius, 0, Math.PI * 2)
    ctx.fill()
    ctx.stroke()
  }, [positions, particles, cx, cy, hqRadius, viewportW, viewportH, now])

  return <canvas ref={canvasRef} style={{ position: 'absolute', inset: 0 }} />
}

export default Attack
