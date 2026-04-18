/**
 * Attack page visual effects — PixiJS renderer + Web Audio.
 *
 * Owns a singleton Pixi.Application driven by a ticker that advances an
 * array of lightweight "entities" (objects with update(now) -> alive).
 * Every bullet / particle / ring / beam / debris chunk is an entity.
 * Sprites sharing a texture + blend mode are GPU-batched by Pixi, which
 * is orders of magnitude faster than Canvas 2D shadowBlur.
 *
 * DOM-level overlays (vignette, dim, whiteOut, INCOMING banner, scan
 * bars, shatter cracks) are managed here too so effect orchestration
 * stays in one place.  The React component just calls fireBullet /
 * spawnFirstBlood and unlocks audio on user input.
 */
import * as PIXI from 'pixi.js'
import { SubmissionType } from '@Api'

/* ---------------------------------------------------------------- */
/* Module state                                                     */
/* ---------------------------------------------------------------- */

interface Entity {
  update(now: number): boolean | void
  destroy?(): void
}

let app: PIXI.Application | null = null
let stage: PIXI.Container | null = null
const entities: Entity[] = []
const pendingEntities: Entity[] = []
const ENT_CAP = 400

let audioCtx: AudioContext | null = null
let masterOut: AudioNode | null = null
let audioUnlocked = false
let lastPewAt = 0
let lastBloodSoundAt = 0

const TEX_GLOW = new Map<string, PIXI.Texture>()

/* ---------------------------------------------------------------- */
/* Utility                                                          */
/* ---------------------------------------------------------------- */

function hexToRgb(hex: string): [number, number, number] {
  let h = hex.replace('#', '')
  if (h.length === 3) h = h.split('').map((c) => c + c).join('')
  const n = parseInt(h, 16)
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255]
}

export function hexToNum(hex: string): number {
  let h = hex.replace('#', '')
  if (h.length === 3) h = h.split('').map((c) => c + c).join('')
  return parseInt(h, 16)
}

export function bezPt(
  t: number,
  x0: number,
  y0: number,
  mx: number,
  my: number,
  x1: number,
  y1: number
): [number, number] {
  const mt = 1 - t
  return [mt * mt * x0 + 2 * mt * t * mx + t * t * x1, mt * mt * y0 + 2 * mt * t * my + t * t * y1]
}

/* ---------------------------------------------------------------- */
/* Pixi init / dispose                                              */
/* ---------------------------------------------------------------- */

export async function initEffects(canvas: HTMLCanvasElement): Promise<void> {
  if (app) return
  app = new PIXI.Application()
  await app.init({
    canvas,
    width: window.innerWidth,
    height: window.innerHeight,
    backgroundAlpha: 0,
    antialias: true,
    resolution: 1,
    powerPreference: 'high-performance',
    preference: 'webgl',
  })
  stage = app.stage

  // Drain any entities that tried to spawn before init completed
  for (const e of pendingEntities) {
    if (entities.length >= ENT_CAP) {
      const gone = entities.shift()
      if (gone?.destroy) gone.destroy()
    }
    entities.push(e)
  }
  pendingEntities.length = 0

  app.ticker.add(() => {
    const now = performance.now()
    for (let i = entities.length - 1; i >= 0; i--) {
      const e = entities[i]
      if (e.update(now) === false) {
        if (e.destroy) e.destroy()
        entities.splice(i, 1)
      }
    }
  })

  window.addEventListener('resize', onResize)
}

export function disposeEffects(): void {
  if (!app) return
  window.removeEventListener('resize', onResize)
  // Destroy entities
  for (const e of entities) {
    if (e.destroy) e.destroy()
  }
  entities.length = 0
  pendingEntities.length = 0
  // Destroy textures
  TEX_GLOW.forEach((t) => t.destroy(true))
  TEX_GLOW.clear()
  app.destroy(true, { children: true, texture: true })
  app = null
  stage = null

  if (audioCtx) {
    audioCtx.close().catch(() => undefined)
    audioCtx = null
    masterOut = null
  }
  audioUnlocked = false
}

function onResize(): void {
  if (!app) return
  app.renderer.resize(window.innerWidth, window.innerHeight)
}

/* ---------------------------------------------------------------- */
/* Entity helpers                                                   */
/* ---------------------------------------------------------------- */

function addEnt(e: Entity): Entity {
  if (!app) {
    pendingEntities.push(e)
    return e
  }
  if (entities.length >= ENT_CAP) {
    const gone = entities.shift()
    if (gone?.destroy) gone.destroy()
  }
  entities.push(e)
  return e
}

function buildGlowTex(color: string): PIXI.Texture {
  const size = 128
  const [r, g, b] = hexToRgb(color)
  const c = document.createElement('canvas')
  c.width = c.height = size
  const gc = c.getContext('2d')!
  const grad = gc.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2)
  grad.addColorStop(0, `rgba(${r},${g},${b},1)`)
  grad.addColorStop(0.18, `rgba(${r},${g},${b},0.85)`)
  grad.addColorStop(0.45, `rgba(${r},${g},${b},0.35)`)
  grad.addColorStop(1, `rgba(${r},${g},${b},0)`)
  gc.fillStyle = grad
  gc.fillRect(0, 0, size, size)
  return PIXI.Texture.from(c)
}

function glowTex(color: string): PIXI.Texture {
  let t = TEX_GLOW.get(color)
  if (!t) {
    t = buildGlowTex(color)
    TEX_GLOW.set(color, t)
  }
  return t
}

function mkGlowSprite(color: string, d: number, blend: PIXI.BLEND_MODES = 'add'): PIXI.Sprite {
  const s = new PIXI.Sprite(glowTex(color))
  s.anchor.set(0.5)
  s.width = s.height = d
  s.blendMode = blend
  stage?.addChild(s)
  return s
}

function mkGraphics(blend: PIXI.BLEND_MODES = 'add'): PIXI.Graphics {
  const g = new PIXI.Graphics()
  g.blendMode = blend
  stage?.addChild(g)
  return g
}

/* ---------------------------------------------------------------- */
/* Audio                                                            */
/* ---------------------------------------------------------------- */

export function unlockAudio(): void {
  if (audioUnlocked) return
  audioUnlocked = true
  try {
    audioCtx = new (window.AudioContext || (window as any).webkitAudioContext)()
    const master = audioCtx.createGain()
    master.gain.value = 1.35
    const comp = audioCtx.createDynamicsCompressor()
    comp.threshold.value = -12
    comp.knee.value = 22
    comp.ratio.value = 6
    comp.attack.value = 0.002
    comp.release.value = 0.22
    const limiter = audioCtx.createDynamicsCompressor()
    limiter.threshold.value = -1.5
    limiter.knee.value = 0
    limiter.ratio.value = 20
    limiter.attack.value = 0.001
    limiter.release.value = 0.08
    master.connect(comp)
    comp.connect(limiter)
    limiter.connect(audioCtx.destination)
    masterOut = master
  } catch {
    // Audio unavailable — silently continue
  }
}

function audioOut(): AudioNode | null {
  return masterOut ?? audioCtx?.destination ?? null
}

type PewKind = 'fb' | 'ok' | 'amber' | 'bad'

function pewKindFor(type: SubmissionType): PewKind {
  if (type === SubmissionType.FirstBlood) return 'fb'
  if (type === SubmissionType.Normal) return 'ok'
  if (type === SubmissionType.SecondBlood || type === SubmissionType.ThirdBlood) return 'amber'
  return 'bad'
}

export function playPew(type: SubmissionType): void {
  if (!audioCtx) return
  const now = performance.now()
  if (now - lastPewAt < 40) return
  lastPewAt = now

  const out = audioOut()
  if (!out) return
  const kind = pewKindFor(type)
  const t0 = audioCtx.currentTime
  const dur = 0.09
  const f0 = kind === 'fb' ? 1600 : kind === 'ok' ? 1200 : kind === 'amber' ? 1400 : 500
  const f1 = kind === 'fb' ? 400 : kind === 'ok' ? 320 : kind === 'amber' ? 360 : 180
  const vol = kind === 'bad' ? 0.22 : kind === 'fb' ? 0.42 : 0.32
  const osc = audioCtx.createOscillator()
  const sub = audioCtx.createOscillator()
  const gain = audioCtx.createGain()
  osc.type = 'square'
  sub.type = 'sawtooth'
  osc.frequency.setValueAtTime(f0, t0)
  osc.frequency.exponentialRampToValueAtTime(f1, t0 + dur)
  sub.frequency.setValueAtTime(f0 * 0.5, t0)
  sub.frequency.exponentialRampToValueAtTime(f1 * 0.5, t0 + dur)
  gain.gain.setValueAtTime(0, t0)
  gain.gain.linearRampToValueAtTime(vol, t0 + 0.005)
  gain.gain.exponentialRampToValueAtTime(0.0001, t0 + dur)
  osc.connect(gain)
  sub.connect(gain)
  gain.connect(out)
  osc.start(t0)
  sub.start(t0)
  osc.stop(t0 + dur + 0.02)
  sub.stop(t0 + dur + 0.02)
}

function playLaser(chargeSec: number): void {
  if (!audioCtx) return
  const out = audioOut()
  if (!out) return
  const t0 = audioCtx.currentTime
  const C = chargeSec

  // Dread drone stack: two near-detuned sub-drones + minor-second clash + octave
  const drones: Array<[number, number, number, OscillatorType]> = [
    [28, 70, 0.85, 'sine'],
    [29, 72, 0.8, 'sine'],
    [29.7, 74, 0.55, 'triangle'],
    [58, 140, 0.42, 'sine'],
  ]
  for (const [f0, f1, vol, type] of drones) {
    const o = audioCtx.createOscillator()
    const g = audioCtx.createGain()
    o.type = type
    o.frequency.setValueAtTime(f0, t0)
    o.frequency.exponentialRampToValueAtTime(f1, t0 + C)
    g.gain.setValueAtTime(0, t0)
    g.gain.linearRampToValueAtTime(vol * 0.3, t0 + C * 0.15)
    g.gain.linearRampToValueAtTime(vol * 0.75, t0 + C * 0.6)
    g.gain.linearRampToValueAtTime(vol, t0 + C * 0.92)
    g.gain.linearRampToValueAtTime(vol * 0.15, t0 + C - 0.08)
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + C + 0.04)
    o.connect(g)
    g.connect(out)
    o.start(t0)
    o.stop(t0 + C + 0.1)
  }

  // Rising saw whine
  const whine = audioCtx.createOscillator()
  const wg = audioCtx.createGain()
  whine.type = 'sawtooth'
  whine.frequency.setValueAtTime(80, t0)
  whine.frequency.exponentialRampToValueAtTime(4200, t0 + C)
  wg.gain.setValueAtTime(0, t0)
  wg.gain.linearRampToValueAtTime(0.12, t0 + C * 0.2)
  wg.gain.linearRampToValueAtTime(0.4, t0 + C * 0.75)
  wg.gain.linearRampToValueAtTime(0.65, t0 + C * 0.95)
  wg.gain.linearRampToValueAtTime(0.12, t0 + C - 0.08)
  wg.gain.exponentialRampToValueAtTime(0.0001, t0 + C + 0.04)
  whine.connect(wg)
  wg.connect(out)
  whine.start(t0)
  whine.stop(t0 + C + 0.1)

  // Harmonic whistle
  const harm = audioCtx.createOscillator()
  const hg = audioCtx.createGain()
  harm.type = 'sawtooth'
  harm.frequency.setValueAtTime(160, t0)
  harm.frequency.exponentialRampToValueAtTime(8400, t0 + C)
  hg.gain.setValueAtTime(0, t0)
  hg.gain.linearRampToValueAtTime(0.06, t0 + C * 0.4)
  hg.gain.linearRampToValueAtTime(0.24, t0 + C * 0.9)
  hg.gain.linearRampToValueAtTime(0.05, t0 + C - 0.08)
  hg.gain.exponentialRampToValueAtTime(0.0001, t0 + C + 0.04)
  harm.connect(hg)
  hg.connect(out)
  harm.start(t0)
  harm.stop(t0 + C + 0.1)

  // Inhale sweep
  try {
    const sr = audioCtx.sampleRate
    const len = Math.floor(sr * C)
    const buf = audioCtx.createBuffer(1, len, sr)
    const data = buf.getChannelData(0)
    for (let i = 0; i < len; i++) {
      const e = Math.pow(i / len, 1.6)
      data[i] = (Math.random() * 2 - 1) * e
    }
    const ns = audioCtx.createBufferSource()
    ns.buffer = buf
    const sweep = audioCtx.createBiquadFilter()
    sweep.type = 'bandpass'
    sweep.frequency.setValueAtTime(120, t0)
    sweep.frequency.exponentialRampToValueAtTime(3500, t0 + C)
    sweep.Q.value = 1.2
    const ng = audioCtx.createGain()
    ng.gain.setValueAtTime(0, t0)
    ng.gain.linearRampToValueAtTime(0.55, t0 + C * 0.85)
    ng.gain.linearRampToValueAtTime(0.12, t0 + C - 0.08)
    ng.gain.exponentialRampToValueAtTime(0.0001, t0 + C + 0.02)
    ns.connect(sweep)
    sweep.connect(ng)
    ng.connect(out)
    ns.start(t0)
  } catch {
    // Noise buffer allocation failed
  }

  // Growing crackle static
  try {
    const sr = audioCtx.sampleRate
    const len = Math.floor(sr * C)
    const buf = audioCtx.createBuffer(1, len, sr)
    const data = buf.getChannelData(0)
    for (let i = 0; i < len; i++) {
      const e = Math.pow(i / len, 2.8)
      const spike = Math.random() > 0.88 ? 1 : 0.14
      data[i] = (Math.random() * 2 - 1) * e * spike
    }
    const ns = audioCtx.createBufferSource()
    ns.buffer = buf
    const bp = audioCtx.createBiquadFilter()
    bp.type = 'bandpass'
    bp.frequency.value = 2600
    bp.Q.value = 3
    const ng = audioCtx.createGain()
    ng.gain.value = 0.6
    ns.connect(bp)
    bp.connect(ng)
    ng.connect(out)
    ns.start(t0)
  } catch {
    // noise allocation failed
  }

  // Alarm siren (accelerating beeps in the last 45%)
  const sirenStart = t0 + C * 0.55
  let beepTime = sirenStart
  while (beepTime < t0 + C - 0.1) {
    const progress = (beepTime - sirenStart) / (C * 0.45)
    const beepDur = 0.12
    const beep = audioCtx.createOscillator()
    const bg = audioCtx.createGain()
    beep.type = 'sawtooth'
    const bf0 = 700 + progress * 400
    const bf1 = 1100 + progress * 800
    beep.frequency.setValueAtTime(bf0, beepTime)
    beep.frequency.linearRampToValueAtTime(bf1, beepTime + beepDur * 0.5)
    beep.frequency.linearRampToValueAtTime(bf0, beepTime + beepDur)
    const bvol = 0.18 + progress * 0.28
    bg.gain.setValueAtTime(0, beepTime)
    bg.gain.linearRampToValueAtTime(bvol, beepTime + 0.01)
    bg.gain.linearRampToValueAtTime(bvol, beepTime + beepDur - 0.02)
    bg.gain.exponentialRampToValueAtTime(0.0001, beepTime + beepDur)
    beep.connect(bg)
    bg.connect(out)
    beep.start(beepTime)
    beep.stop(beepTime + beepDur + 0.02)
    beepTime += 0.5 - progress * 0.38
  }

  // Dread thunks
  const thunkFracs = [0.1, 0.28, 0.5, 0.72, 0.88]
  thunkFracs.forEach((frac, idx) => {
    const ts = t0 + C * frac
    const thunk = audioCtx!.createOscillator()
    const tg = audioCtx!.createGain()
    thunk.type = 'sine'
    thunk.frequency.setValueAtTime(90 - idx * 8, ts)
    thunk.frequency.exponentialRampToValueAtTime(36, ts + 0.2)
    const vol = 0.45 + idx * 0.1
    tg.gain.setValueAtTime(0, ts)
    tg.gain.linearRampToValueAtTime(vol, ts + 0.005)
    tg.gain.exponentialRampToValueAtTime(0.0001, ts + 0.28)
    thunk.connect(tg)
    tg.connect(out)
    thunk.start(ts)
    thunk.stop(ts + 0.3)
  })

  // Beam discharge at charge complete
  const b0 = t0 + C
  const beam = audioCtx.createOscillator()
  const beamG = audioCtx.createGain()
  beam.type = 'square'
  beam.frequency.setValueAtTime(4200, b0)
  beam.frequency.exponentialRampToValueAtTime(480, b0 + 0.24)
  beamG.gain.setValueAtTime(0, b0)
  beamG.gain.linearRampToValueAtTime(0.65, b0 + 0.006)
  beamG.gain.exponentialRampToValueAtTime(0.0001, b0 + 0.26)
  beam.connect(beamG)
  beamG.connect(out)
  beam.start(b0)
  beam.stop(b0 + 0.28)
}

function playShatter(): void {
  if (!audioCtx) return
  const out = audioOut()
  if (!out) return
  const t0 = audioCtx.currentTime
  const sr = audioCtx.sampleRate

  const layers: Array<[number, number, number]> = [
    [140, 40, 0.95],
    [80, 30, 0.95],
    [50, 22, 0.9],
  ]
  for (const [f0, f1, vol] of layers) {
    const o = audioCtx.createOscillator()
    const g = audioCtx.createGain()
    o.type = 'sine'
    o.frequency.setValueAtTime(f0, t0)
    o.frequency.exponentialRampToValueAtTime(f1, t0 + 0.45)
    g.gain.setValueAtTime(0, t0)
    g.gain.linearRampToValueAtTime(vol, t0 + 0.005)
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.85)
    o.connect(g)
    g.connect(out)
    o.start(t0)
    o.stop(t0 + 0.9)
  }

  const click = audioCtx.createOscillator()
  const clickG = audioCtx.createGain()
  click.type = 'square'
  click.frequency.setValueAtTime(240, t0)
  click.frequency.exponentialRampToValueAtTime(70, t0 + 0.04)
  clickG.gain.setValueAtTime(0.85, t0)
  clickG.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.06)
  click.connect(clickG)
  clickG.connect(out)
  click.start(t0)
  click.stop(t0 + 0.08)

  try {
    const blen = Math.floor(sr * 0.9)
    const buf = audioCtx.createBuffer(1, blen, sr)
    const data = buf.getChannelData(0)
    for (let i = 0; i < blen; i++) {
      const e = Math.pow(1 - i / blen, 1.6)
      data[i] = (Math.random() * 2 - 1) * e
    }
    const src = audioCtx.createBufferSource()
    src.buffer = buf
    const hp = audioCtx.createBiquadFilter()
    hp.type = 'highpass'
    hp.frequency.setValueAtTime(1600, t0)
    hp.frequency.exponentialRampToValueAtTime(5200, t0 + 0.5)
    const ng = audioCtx.createGain()
    ng.gain.setValueAtTime(1.1, t0)
    ng.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.8)
    src.connect(hp)
    hp.connect(ng)
    ng.connect(out)
    src.start(t0)
  } catch {
    // noise allocation failed
  }

  try {
    const blen = Math.floor(sr * 0.7)
    const buf = audioCtx.createBuffer(1, blen, sr)
    const data = buf.getChannelData(0)
    for (let i = 0; i < blen; i++) {
      const e = Math.pow(1 - i / blen, 1.3)
      data[i] = (Math.random() * 2 - 1) * e
    }
    const src = audioCtx.createBufferSource()
    src.buffer = buf
    const lp = audioCtx.createBiquadFilter()
    lp.type = 'lowpass'
    lp.frequency.value = 220
    lp.Q.value = 1.2
    const ng = audioCtx.createGain()
    ng.gain.setValueAtTime(0.95, t0)
    ng.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.7)
    src.connect(lp)
    lp.connect(ng)
    ng.connect(out)
    src.start(t0)
  } catch {
    // noise allocation failed
  }

  try {
    for (let i = 0; i < 28; i++) {
      const delay = 0.01 + i * 0.028 + Math.random() * 0.025
      const popStart = t0 + delay
      const plen = Math.floor(sr * 0.07)
      const pbuf = audioCtx.createBuffer(1, plen, sr)
      const pd = pbuf.getChannelData(0)
      for (let j = 0; j < plen; j++) {
        const e = Math.pow(1 - j / plen, 2.4)
        pd[j] = (Math.random() * 2 - 1) * e
      }
      const ps = audioCtx.createBufferSource()
      ps.buffer = pbuf
      const bp = audioCtx.createBiquadFilter()
      bp.type = 'bandpass'
      bp.frequency.value = 1800 + Math.random() * 3600
      bp.Q.value = 3 + Math.random() * 7
      const pg = audioCtx.createGain()
      pg.gain.setValueAtTime(0.65 + Math.random() * 0.35, popStart)
      pg.gain.exponentialRampToValueAtTime(0.0001, popStart + 0.09)
      ps.connect(bp)
      bp.connect(pg)
      pg.connect(out)
      ps.start(popStart)
    }
  } catch {
    // noise allocation failed
  }
}

function playBigImpact(): void {
  if (!audioCtx) return
  const out = audioOut()
  if (!out) return
  const t0 = audioCtx.currentTime
  const sr = audioCtx.sampleRate

  const click = audioCtx.createOscillator()
  const clickG = audioCtx.createGain()
  click.type = 'square'
  click.frequency.setValueAtTime(320, t0)
  click.frequency.exponentialRampToValueAtTime(55, t0 + 0.035)
  clickG.gain.setValueAtTime(1.2, t0)
  clickG.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.05)
  click.connect(clickG)
  clickG.connect(out)
  click.start(t0)
  click.stop(t0 + 0.06)

  try {
    const clen = Math.floor(sr * 0.025)
    const cbuf = audioCtx.createBuffer(1, clen, sr)
    const cd = cbuf.getChannelData(0)
    for (let i = 0; i < clen; i++) cd[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / clen, 1.8)
    const cs = audioCtx.createBufferSource()
    cs.buffer = cbuf
    const chp = audioCtx.createBiquadFilter()
    chp.type = 'highpass'
    chp.frequency.value = 3500
    const cg = audioCtx.createGain()
    cg.gain.value = 1.3
    cs.connect(chp)
    chp.connect(cg)
    cg.connect(out)
    cs.start(t0)
  } catch {
    // noise allocation failed
  }

  const subs: Array<[number, number]> = [
    [140, 22],
    [100, 18],
    [70, 15],
    [45, 12],
  ]
  subs.forEach(([f0, f1], idx) => {
    const o = audioCtx!.createOscillator()
    const g = audioCtx!.createGain()
    o.type = 'sine'
    o.frequency.setValueAtTime(f0, t0)
    o.frequency.exponentialRampToValueAtTime(f1, t0 + 0.9)
    const vol = 1.0 - idx * 0.08
    g.gain.setValueAtTime(0, t0)
    g.gain.linearRampToValueAtTime(vol, t0 + 0.004)
    g.gain.linearRampToValueAtTime(vol * 0.6, t0 + 0.35)
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + 1.4)
    o.connect(g)
    g.connect(out)
    o.start(t0)
    o.stop(t0 + 1.5)
  })

  const body = audioCtx.createOscillator()
  const bg = audioCtx.createGain()
  body.type = 'sawtooth'
  body.frequency.setValueAtTime(260, t0)
  body.frequency.exponentialRampToValueAtTime(38, t0 + 0.55)
  bg.gain.setValueAtTime(0, t0)
  bg.gain.linearRampToValueAtTime(0.9, t0 + 0.006)
  bg.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.95)
  const shaper = audioCtx.createWaveShaper()
  const curve = new Float32Array(4096)
  for (let i = 0; i < 4096; i++) {
    const x = (i / 4095) * 2 - 1
    curve[i] = Math.tanh(x * 4)
  }
  shaper.curve = curve
  shaper.oversample = '4x'
  body.connect(bg)
  bg.connect(shaper)
  shaper.connect(out)
  body.start(t0)
  body.stop(t0 + 1.0)

  for (const f of [180, 233]) {
    const o = audioCtx.createOscillator()
    const g = audioCtx.createGain()
    o.type = 'triangle'
    o.frequency.value = f
    g.gain.setValueAtTime(0, t0)
    g.gain.linearRampToValueAtTime(0.4, t0 + 0.002)
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.42)
    o.connect(g)
    g.connect(out)
    o.start(t0)
    o.stop(t0 + 0.45)
  }

  try {
    const tlen = Math.floor(sr * 2.0)
    const tbuf = audioCtx.createBuffer(1, tlen, sr)
    const td = tbuf.getChannelData(0)
    for (let i = 0; i < tlen; i++) td[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / tlen, 1.3)
    const ts = audioCtx.createBufferSource()
    ts.buffer = tbuf
    const tlp = audioCtx.createBiquadFilter()
    tlp.type = 'lowpass'
    tlp.Q.value = 1.5
    tlp.frequency.setValueAtTime(900, t0)
    tlp.frequency.exponentialRampToValueAtTime(70, t0 + 1.6)
    const tg = audioCtx.createGain()
    tg.gain.setValueAtTime(1.2, t0)
    tg.gain.exponentialRampToValueAtTime(0.0001, t0 + 1.9)
    ts.connect(tlp)
    tlp.connect(tg)
    tg.connect(out)
    ts.start(t0)
  } catch {
    // noise allocation failed
  }
}

function playAftershock(vol: number): void {
  if (!audioCtx) return
  const out = audioOut()
  if (!out) return
  const t0 = audioCtx.currentTime
  const o = audioCtx.createOscillator()
  const g = audioCtx.createGain()
  o.type = 'sine'
  o.frequency.setValueAtTime(70, t0)
  o.frequency.exponentialRampToValueAtTime(28, t0 + 0.3)
  g.gain.setValueAtTime(0, t0)
  g.gain.linearRampToValueAtTime(vol, t0 + 0.004)
  g.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.45)
  o.connect(g)
  g.connect(out)
  o.start(t0)
  o.stop(t0 + 0.5)
  try {
    const sr = audioCtx.sampleRate
    const blen = Math.floor(sr * 0.4)
    const buf = audioCtx.createBuffer(1, blen, sr)
    const data = buf.getChannelData(0)
    for (let i = 0; i < blen; i++) data[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / blen, 1.8)
    const src = audioCtx.createBufferSource()
    src.buffer = buf
    const lp = audioCtx.createBiquadFilter()
    lp.type = 'lowpass'
    lp.frequency.value = 180
    const ng = audioCtx.createGain()
    ng.gain.setValueAtTime(vol * 0.6, t0)
    ng.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.4)
    src.connect(lp)
    lp.connect(ng)
    ng.connect(out)
    src.start(t0)
  } catch {
    // noise allocation failed
  }
}

/* ---------------------------------------------------------------- */
/* DOM-level screen effects                                         */
/* ---------------------------------------------------------------- */

function quakeScreen(): void {
  const el = document.documentElement
  el.classList.remove('attack-shake')
  el.classList.remove('attack-quake')
  // Force reflow
  void el.offsetWidth
  el.classList.add('attack-quake')
  setTimeout(() => el.classList.remove('attack-quake'), 1450)
}

function shakeScreen(): void {
  const el = document.documentElement
  el.classList.remove('attack-shake')
  void el.offsetWidth
  el.classList.add('attack-shake')
  setTimeout(() => el.classList.remove('attack-shake'), 950)
}

function rumbleOn(): void {
  const el = document.documentElement
  if (!el.classList.contains('attack-shake') && !el.classList.contains('attack-quake')) {
    el.classList.add('attack-rumble')
  }
}

function rumbleOff(): void {
  document.documentElement.classList.remove('attack-rumble')
}

function whiteOutScreen(): void {
  const w = document.createElement('div')
  Object.assign(w.style, {
    position: 'fixed',
    inset: '0',
    zIndex: '90',
    pointerEvents: 'none',
    background: '#ffffff',
    opacity: '0',
  } as CSSStyleDeclaration)
  document.body.appendChild(w)
  w.animate(
    [
      { opacity: 0, offset: 0 },
      { opacity: 1, offset: 0.03 },
      { opacity: 1, offset: 0.22 },
      { opacity: 0.55, offset: 0.45 },
      { opacity: 0.18, offset: 0.75 },
      { opacity: 0, offset: 1 },
    ],
    { duration: 700, easing: 'cubic-bezier(.3,0,.2,1)', fill: 'forwards' }
  ).onfinish = () => w.remove()
}

function flashScreen(color: string): void {
  const f = document.createElement('div')
  Object.assign(f.style, {
    position: 'fixed',
    inset: '0',
    zIndex: '55',
    pointerEvents: 'none',
    background: `radial-gradient(circle at center, rgba(255,255,255,.9), ${color} 28%, rgba(255,42,42,.35) 55%, transparent 78%)`,
    opacity: '0',
    willChange: 'opacity',
  } as CSSStyleDeclaration)
  document.body.appendChild(f)
  f.animate(
    [
      { opacity: 0 },
      { opacity: 1, offset: 0.1 },
      { opacity: 0.55, offset: 0.3 },
      { opacity: 0 },
    ],
    { duration: 520, easing: 'ease-out', fill: 'forwards' }
  ).onfinish = () => f.remove()
}

function shatterAt(x: number, y: number, color: string): void {
  const SVG_NS = 'http://www.w3.org/2000/svg'
  const svg = document.createElementNS(SVG_NS, 'svg')
  Object.assign(svg.style, {
    position: 'fixed',
    inset: '0',
    width: '100vw',
    height: '100vh',
    pointerEvents: 'none',
    zIndex: '58',
  } as CSSStyleDeclaration)
  svg.setAttribute('viewBox', `0 0 ${window.innerWidth} ${window.innerHeight}`)
  document.body.appendChild(svg)

  const n = 10 + Math.floor(Math.random() * 3)
  const diag = Math.hypot(window.innerWidth, window.innerHeight)
  for (let i = 0; i < n; i++) {
    const baseAng = (i / n) * Math.PI * 2 + (Math.random() - 0.5) * 0.35
    const reach = diag * (0.35 + Math.random() * 0.4)
    let d = `M ${x} ${y}`
    let cx = x,
      cy = y,
      ang = baseAng
    const segs = 4 + Math.floor(Math.random() * 3)
    const segLen = reach / segs
    for (let s = 0; s < segs; s++) {
      ang += (Math.random() - 0.5) * 0.55
      cx += Math.cos(ang) * segLen
      cy += Math.sin(ang) * segLen
      d += ` L ${cx} ${cy}`
      if (Math.random() < 0.55) {
        const bang = ang + (Math.random() > 0.5 ? 1 : -1) * (0.7 + Math.random() * 0.6)
        const bl = 40 + Math.random() * 90
        const bx = cx + Math.cos(bang) * bl,
          by = cy + Math.sin(bang) * bl
        d += ` M ${cx} ${cy} L ${bx} ${by} M ${cx} ${cy}`
      }
    }
    const p = document.createElementNS(SVG_NS, 'path')
    p.setAttribute('d', d)
    p.setAttribute('fill', 'none')
    p.setAttribute('stroke', '#ffffff')
    p.setAttribute('stroke-width', String(1.6 + Math.random() * 1.1))
    p.setAttribute('stroke-linecap', 'round')
    p.style.filter = `drop-shadow(0 0 8px ${color})`
    svg.appendChild(p)
    const total = p.getTotalLength()
    p.style.strokeDasharray = String(total)
    p.style.strokeDashoffset = String(total)
    p.animate(
      [
        { strokeDashoffset: total, opacity: 1 },
        { strokeDashoffset: 0, opacity: 1, offset: 0.18 },
        { strokeDashoffset: 0, opacity: 0.9, offset: 0.7 },
        { strokeDashoffset: 0, opacity: 0 },
      ] as unknown as Keyframe[],
      { duration: 2400, easing: 'cubic-bezier(.2,.8,.2,1)', fill: 'forwards' }
    )
  }

  const hub = document.createElementNS(SVG_NS, 'circle')
  hub.setAttribute('cx', String(x))
  hub.setAttribute('cy', String(y))
  hub.setAttribute('r', '4')
  hub.setAttribute('fill', '#fff')
  hub.style.filter = `drop-shadow(0 0 20px ${color})`
  svg.appendChild(hub)
  hub.animate(
    [{ r: 4, opacity: 1 }, { r: 40, opacity: 0 }] as unknown as Keyframe[],
    { duration: 700, easing: 'ease-out', fill: 'forwards' }
  ).onfinish = () => hub.remove()

  setTimeout(() => svg.remove(), 2600)
}

function hqPunch(hex: HTMLElement): void {
  // Animates the INNER hex element (not the centered wrapper), so the
  // translate(-50%,-50%) centering stays exclusively on the wrapper and
  // React re-renders can't stomp on the animation state mid-sequence.
  hex.animate(
    [
      { transform: `scale(1)` },
      { transform: `scale(0.72) rotate(-3deg)`, offset: 0.08 },
      { transform: `scale(1.28) rotate(2deg)`, offset: 0.22 },
      { transform: `scale(0.9) rotate(-1deg)`, offset: 0.4 },
      { transform: `scale(1.12)`, offset: 0.6 },
      { transform: `scale(0.96)`, offset: 0.8 },
      { transform: `scale(1)` },
    ],
    { duration: 1100, easing: 'cubic-bezier(.3,1.4,.5,1)', fill: 'forwards' }
  )
}

function slideScanBar(i: number): void {
  const bar = document.createElement('div')
  Object.assign(bar.style, {
    position: 'fixed',
    left: '0',
    right: '0',
    top: '-50px',
    height: '50px',
    zIndex: '6',
    pointerEvents: 'none',
    background: 'linear-gradient(to bottom, transparent, rgba(255,42,42,.55), transparent)',
    opacity: '0.85',
    willChange: 'top',
  } as CSSStyleDeclaration)
  document.body.appendChild(bar)
  bar.animate([{ top: '-50px' }, { top: `${window.innerHeight}px` }], {
    duration: 900 - i * 150,
    easing: 'linear',
    fill: 'forwards',
  }).onfinish = () => bar.remove()
}

function showIncomingBanner(durationMs: number): void {
  const warn = document.createElement('div')
  // Positioned in the upper HUD band well clear of both the FB strip
  // banner (top:58px) and the HQ hex (centered at 50vh).  top:140px
  // leaves ~40px gap below the FB strip and far above the hex.
  Object.assign(warn.style, {
    position: 'fixed',
    left: '50%',
    top: '140px',
    transform: 'translateX(-50%)',
    zIndex: '45',
    pointerEvents: 'none',
    padding: '14px 32px',
    background: '#1a0a0a',
    border: '2px solid #ff2a2a',
    color: '#ff2a2a',
    fontFamily: '"JetBrains Mono", ui-monospace, monospace',
    fontWeight: '700',
    fontSize: 'clamp(14px, 2vw, 20px)',
    letterSpacing: '.3em',
    textTransform: 'uppercase',
    opacity: '0',
    willChange: 'opacity, transform',
  } as CSSStyleDeclaration)
  warn.innerHTML = '▼&nbsp;&nbsp;INCOMING STRIKE&nbsp;&nbsp;▼'
  document.body.appendChild(warn)
  warn.animate(
    [
      { opacity: 0, transform: 'translateX(-50%) scale(.7)' },
      { opacity: 1, transform: 'translateX(-50%) scale(1)', offset: 0.1 },
      { opacity: 0.6, transform: 'translateX(-50%) scale(1)', offset: 0.2 },
      { opacity: 1, transform: 'translateX(-50%) scale(1.04)', offset: 0.35 },
      { opacity: 0.6, transform: 'translateX(-50%) scale(1)', offset: 0.5 },
      { opacity: 1, transform: 'translateX(-50%) scale(1.08)', offset: 0.7 },
      { opacity: 0.4, transform: 'translateX(-50%) scale(1)', offset: 0.85 },
      { opacity: 1, transform: 'translateX(-50%) scale(1.15)', offset: 0.95 },
      { opacity: 0, transform: 'translateX(-50%) scale(1.4)' },
    ],
    { duration: durationMs, fill: 'forwards', easing: 'ease-out' }
  ).onfinish = () => warn.remove()
}

function spawnVignette(durationMs: number): HTMLDivElement {
  const vignette = document.createElement('div')
  Object.assign(vignette.style, {
    position: 'fixed',
    inset: '0',
    zIndex: '4',
    pointerEvents: 'none',
    background:
      'radial-gradient(ellipse 70% 70% at center, transparent 40%, rgba(255,42,42,.55) 85%, rgba(255,42,42,.95) 100%)',
    opacity: '0',
    willChange: 'opacity',
  } as CSSStyleDeclaration)
  document.body.appendChild(vignette)
  vignette.animate(
    [
      { opacity: 0 },
      { opacity: 0.25, offset: 0.25 },
      { opacity: 0.55, offset: 0.6 },
      { opacity: 0.95, offset: 0.92 },
      { opacity: 1 },
    ],
    { duration: durationMs, fill: 'forwards', easing: 'cubic-bezier(.5,0,.9,.8)' }
  )
  return vignette
}

function spawnDim(durationMs: number): HTMLDivElement {
  const dim = document.createElement('div')
  Object.assign(dim.style, {
    position: 'fixed',
    inset: '0',
    zIndex: '3',
    pointerEvents: 'none',
    background: 'rgba(0,0,0,.45)',
    opacity: '0',
  } as CSSStyleDeclaration)
  document.body.appendChild(dim)
  dim.animate([{ opacity: 0 }, { opacity: 1 }], {
    duration: durationMs,
    fill: 'forwards',
    easing: 'cubic-bezier(.5,0,.9,1)',
  })
  return dim
}

function spawnAftermathWash(): void {
  const aftermath = document.createElement('div')
  Object.assign(aftermath.style, {
    position: 'fixed',
    inset: '0',
    zIndex: '4',
    pointerEvents: 'none',
    background:
      'radial-gradient(ellipse at center, transparent 30%, rgba(255,42,42,.35) 90%)',
    opacity: '0.85',
  } as CSSStyleDeclaration)
  document.body.appendChild(aftermath)
  aftermath
    .animate([{ opacity: 0.85 }, { opacity: 0 }], {
      duration: 2800,
      easing: 'ease-out',
      fill: 'forwards',
    })
    .addEventListener('finish', () => aftermath.remove())
}

/* ---------------------------------------------------------------- */
/* Bullet + muzzle flash + impact burst                             */
/* ---------------------------------------------------------------- */

export function muzzleFlash(x: number, y: number, color: string, big: boolean): void {
  const start = performance.now()
  let glow: PIXI.Sprite | null = null
  addEnt({
    update(now) {
      const t = (now - start) / 260
      if (!glow && stage) {
        glow = mkGlowSprite(color, 2, 'add')
        glow.x = x
        glow.y = y
      }
      if (t >= 1) return false
      if (glow) {
        glow.alpha = 1 - t
        const d = (big ? 56 : 40) + t * (big ? 40 : 28)
        glow.width = glow.height = d
      }
      return true
    },
    destroy() {
      if (glow) {
        stage?.removeChild(glow)
        glow.destroy()
      }
    },
  })

  let nova: PIXI.Graphics | null = null
  addEnt({
    update(now) {
      const t = (now - start) / 180
      if (!nova && stage) {
        nova = new PIXI.Graphics()
        stage.addChild(nova)
      }
      if (t >= 1) return false
      if (nova) {
        nova.clear()
        nova.circle(x, y, 2 + t * (big ? 8 : 4)).fill({ color: 0xffffff, alpha: 1 - t })
      }
      return true
    },
    destroy() {
      if (nova) {
        stage?.removeChild(nova)
        nova.destroy()
      }
    },
  })
}

export function impactBurst(x: number, y: number, color: string, type: SubmissionType): void {
  if (type === SubmissionType.Unaccepted) return
  const big = type === SubmissionType.FirstBlood
  const colorNum = hexToNum(color)
  const start = performance.now()
  const ringDur = big ? 650 : 450
  const ringMax = big ? 80 : 45

  let ringG: PIXI.Graphics | null = null
  addEnt({
    update(now) {
      const t = (now - start) / ringDur
      if (!ringG && stage) ringG = mkGraphics('add')
      if (t >= 1) return false
      if (ringG) {
        const e = 1 - (1 - t) * (1 - t)
        ringG.clear()
        ringG.circle(x, y, 3 + e * ringMax).stroke({
          width: (big ? 3 : 2) * (1 - t * 0.87),
          color: colorNum,
          alpha: 1 - t,
        })
      }
      return true
    },
    destroy() {
      if (ringG) {
        stage?.removeChild(ringG)
        ringG.destroy()
      }
    },
  })

  const flashDur = big ? 380 : 260
  let flashSp: PIXI.Sprite | null = null
  let flashCore: PIXI.Graphics | null = null
  addEnt({
    update(now) {
      const t = (now - start) / flashDur
      if (!flashSp && stage) {
        flashSp = mkGlowSprite(color, 2, 'add')
        flashSp.x = x
        flashSp.y = y
        flashCore = new PIXI.Graphics()
        stage.addChild(flashCore)
      }
      if (t >= 1) return false
      if (flashSp && flashCore) {
        flashSp.alpha = 1 - t
        const d = (big ? 60 : 36) + t * (big ? 32 : 20)
        flashSp.width = flashSp.height = d
        flashCore.clear()
        flashCore
          .circle(x, y, (big ? 4 : 2.5) * (1 - t * 0.6))
          .fill({ color: 0xffffff, alpha: 1 - t })
      }
      return true
    },
    destroy() {
      if (flashSp) {
        stage?.removeChild(flashSp)
        flashSp.destroy()
      }
      if (flashCore) {
        stage?.removeChild(flashCore)
        flashCore.destroy()
      }
    },
  })

  if (big) {
    for (let i = 0; i < 8; i++) {
      const ang = (i / 8) * Math.PI * 2 + Math.random() * 0.3
      const maxLen = 26 + Math.random() * 18
      let sparkG: PIXI.Graphics | null = null
      addEnt({
        update(now) {
          const t = (now - start) / 520
          if (!sparkG && stage) sparkG = mkGraphics('add')
          if (t >= 1) return false
          if (sparkG) {
            const e = 1 - (1 - t) * (1 - t)
            const endL = 2 + e * maxLen
            sparkG.clear()
            sparkG.moveTo(x + Math.cos(ang) * 2, y + Math.sin(ang) * 2)
            sparkG.lineTo(x + Math.cos(ang) * endL, y + Math.sin(ang) * endL)
            sparkG.stroke({
              width: 2.5 * (1 - t * 0.76),
              color: colorNum,
              alpha: 1 - t,
              cap: 'round',
            })
          }
          return true
        },
        destroy() {
          if (sparkG) {
            stage?.removeChild(sparkG)
            sparkG.destroy()
          }
        },
      })
    }
  }
}

export function fireBullet(
  x0: number,
  y0: number,
  x1: number,
  y1: number,
  color: string,
  type: SubmissionType
): void {
  const dx = x1 - x0
  const dy = y1 - y0
  const dist = Math.hypot(dx, dy) || 1
  const nrmX = -dy / dist
  const nrmY = dx / dist
  const jitter = (Math.random() - 0.5) * 140
  const mx = (x0 + x1) / 2 + nrmX * jitter + (Math.random() - 0.5) * 40
  const my = (y0 + y1) / 2 + nrmY * jitter + (Math.random() - 0.5) * 40

  const fat = type === SubmissionType.FirstBlood
  const soft = type === SubmissionType.Unaccepted
  const thick = fat ? 4 : soft ? 1.4 : 2.6
  const dur = fat ? 950 : 1050
  const headR = fat ? 6 : soft ? 2.5 : 4.5
  const tailFrac = fat ? 0.26 : 0.18
  const colorNum = hexToNum(color)

  if (!soft) muzzleFlash(x0, y0, color, fat)

  const start = performance.now()
  let fired = false
  let created = false
  let tailG: PIXI.Graphics | null = null
  let headSp: PIXI.Sprite | null = null
  let coreG: PIXI.Graphics | null = null

  addEnt({
    update(now) {
      if (!created && stage) {
        tailG = mkGraphics('add')
        if (!soft) {
          headSp = mkGlowSprite(color, headR * (fat ? 8 : 6), 'add')
          coreG = new PIXI.Graphics()
          coreG.circle(0, 0, headR * 0.55).fill(0xffffff)
          stage.addChild(coreG)
        }
        created = true
      }
      if (!created) return true
      const t = (now - start) / dur
      if (t >= 1) {
        if (!fired) {
          fired = true
          if (!soft) impactBurst(x1, y1, color, type)
        }
        return false
      }
      const e = 1 - (1 - t) * (1 - t)
      const [hx, hy] = bezPt(e, x0, y0, mx, my, x1, y1)
      const tailStart = Math.max(0, e - tailFrac)

      if (tailG) {
        tailG.clear()
        const SAMPLES = 8
        let first = true
        for (let s = 0; s <= SAMPLES; s++) {
          const tt = tailStart + (s / SAMPLES) * (e - tailStart)
          const [px, py] = bezPt(tt, x0, y0, mx, my, x1, y1)
          if (first) {
            tailG.moveTo(px, py)
            first = false
          } else {
            tailG.lineTo(px, py)
          }
        }
        tailG.stroke({
          width: thick,
          color: colorNum,
          alpha: soft ? 0.55 : 1,
          cap: 'round',
        })
      }
      if (headSp && coreG) {
        headSp.x = hx
        headSp.y = hy
        coreG.x = hx
        coreG.y = hy
      }
      return true
    },
    destroy() {
      if (tailG) {
        stage?.removeChild(tailG)
        tailG.destroy()
      }
      if (headSp) {
        stage?.removeChild(headSp)
        headSp.destroy()
      }
      if (coreG) {
        stage?.removeChild(coreG)
        coreG.destroy()
      }
    },
  })
}

/* ---------------------------------------------------------------- */
/* First-blood LASER sequence (10 seconds total)                    */
/* ---------------------------------------------------------------- */

export interface FirstBloodOptions {
  onImpact?: () => void
  /** The INNER hex element (not the centering wrapper) — receives the
   *  scale/rotate punch animation at impact. */
  hexElement?: HTMLElement | null
}

export function spawnFirstBlood(
  x0: number,
  y0: number,
  x1: number,
  y1: number,
  color: string,
  opts: FirstBloodOptions = {}
): void {
  const CHARGE_MS = 7000
  playLaser(CHARGE_MS / 1000)

  const chargeStart = performance.now()
  const chargeAlive = { v: true }
  const colorNum = hexToNum(color)

  // DOM overlays (vignette + dim, tracked so we can remove on charge end)
  const vignette = spawnVignette(CHARGE_MS)
  const dim = spawnDim(CHARGE_MS)

  // Heartbeat core + halo + 6 collapsing rings
  const coreFrames: Array<[number, number]> = [
    [0, 2], [0.07, 5], [0.12, 3.5], [0.22, 9], [0.27, 7], [0.42, 14], [0.48, 12],
    [0.65, 22], [0.72, 19], [0.87, 32], [1, 46],
  ]
  const coreAlphaFrames: Array<[number, number]> = [
    [0, 0.8], [0.07, 0.95], [0.12, 0.75], [0.22, 1], [0.27, 0.85], [0.42, 1],
    [0.48, 0.9], [0.65, 1], [0.72, 0.9], [0.87, 1], [1, 1],
  ]
  const interp1 = (t: number, kf: Array<[number, number]>): number => {
    for (let i = 0; i < kf.length - 1; i++) {
      if (t <= kf[i + 1][0]) {
        const u = (t - kf[i][0]) / (kf[i + 1][0] - kf[i][0])
        return kf[i][1] + u * (kf[i + 1][1] - kf[i][1])
      }
    }
    return kf[kf.length - 1][1]
  }

  let halo: PIXI.Sprite | null = null
  let coreGlow: PIXI.Sprite | null = null
  let coreDisc: PIXI.Graphics | null = null
  const ringG: PIXI.Graphics[] = []
  addEnt({
    update(now) {
      if (!chargeAlive.v || now - chargeStart >= CHARGE_MS) return false
      if (!halo && stage) {
        halo = mkGlowSprite(color, 2, 'add')
        halo.x = x0
        halo.y = y0
        coreGlow = mkGlowSprite(color, 2, 'add')
        coreGlow.x = x0
        coreGlow.y = y0
        coreDisc = new PIXI.Graphics()
        stage.addChild(coreDisc)
        for (let i = 0; i < 6; i++) ringG.push(mkGraphics('add'))
      }
      if (!halo) return true
      const t = Math.min(1, (now - chargeStart) / CHARGE_MS)
      const haloR = (4 + 66 * t) * 3
      halo.alpha = 0.3 + 0.5 * t
      halo.width = halo.height = haloR * 2

      for (let i = 0; i < 6; i++) {
        const delay = i * 240
        const lt = (now - chargeStart - delay) / (CHARGE_MS - delay)
        const g = ringG[i]
        g.clear()
        if (lt < 0 || lt >= 1) continue
        const startR = 140 + i * 34
        let r: number, opa: number, w: number
        if (lt < 0.25) {
          const u = lt / 0.25
          r = startR + (startR * 0.75 - startR) * u
          opa = u * 0.9
          w = 1.2 + u * 1.6
        } else {
          const u = (lt - 0.25) / 0.75
          r = startR * 0.75 + (10 - startR * 0.75) * u
          opa = 0.9 + u * 0.1
          w = 2.8 + u * 1.7
        }
        g.circle(x0, y0, r).stroke({ width: w, color: colorNum, alpha: opa })
      }

      const cr = interp1(t, coreFrames)
      const ca = interp1(t, coreAlphaFrames)
      coreGlow!.alpha = ca
      coreGlow!.width = coreGlow!.height = cr * 2.8 * 2
      coreDisc!.clear()
      coreDisc!.circle(x0, y0, cr).fill({ color: 0xffffff, alpha: ca })
      return true
    },
    destroy() {
      if (halo) {
        stage?.removeChild(halo)
        halo.destroy()
      }
      if (coreGlow) {
        stage?.removeChild(coreGlow)
        coreGlow.destroy()
      }
      if (coreDisc) {
        stage?.removeChild(coreDisc)
        coreDisc.destroy()
      }
      for (const g of ringG) {
        stage?.removeChild(g)
        g.destroy()
      }
    },
  })

  // Orbiting particle spawner
  let particleLastSpawn = 0
  addEnt({
    update(now) {
      if (!chargeAlive.v || now - chargeStart >= CHARGE_MS) return false
      if (now - particleLastSpawn < 220) return true
      particleLastSpawn = now
      const ang = Math.random() * Math.PI * 2
      const dist = 100 + Math.random() * 200
      const sx = x0 + Math.cos(ang) * dist
      const sy = y0 + Math.sin(ang) * dist
      const ex = x0 + Math.cos(ang) * 12
      const ey = y0 + Math.sin(ang) * 12
      const dur = 500 + Math.random() * 360
      const r = 1.4 + Math.random() * 1.8
      const pStart = now
      const d = r * 3.5 * 2
      const sp = mkGlowSprite(color, d, 'add')
      sp.x = sx
      sp.y = sy
      addEnt({
        update(now2) {
          const t = (now2 - pStart) / dur
          if (t >= 1) return false
          const e = 1 - (1 - t) * (1 - t) * (1 - t)
          sp.x = sx + (ex - sx) * e
          sp.y = sy + (ey - sy) * e
          sp.alpha = 0.9 - t * 0.7
          return true
        },
        destroy() {
          stage?.removeChild(sp)
          sp.destroy()
        },
      })
      return true
    },
  })

  // Arc spoke spawner
  let arcLastSpawn = 0
  addEnt({
    update(now) {
      if (!chargeAlive.v || now - chargeStart >= CHARGE_MS) return false
      if (now - arcLastSpawn < 220) return true
      arcLastSpawn = now
      const frac = (now - chargeStart) / CHARGE_MS
      const spokes = 1 + Math.floor(frac * 1.5)
      const reach = 20 + frac * 120
      for (let i = 0; i < spokes; i++) {
        const baseAng = Math.random() * Math.PI * 2
        const len = reach * (0.6 + Math.random() * 0.8)
        const segs = 3 + Math.floor(frac * 2)
        const pts: Array<[number, number]> = [[x0, y0]]
        let cx = x0,
          cy = y0,
          a = baseAng
        for (let s = 0; s < segs; s++) {
          a += (Math.random() - 0.5) * 1.3
          cx += Math.cos(a) * (len / segs)
          cy += Math.sin(a) * (len / segs)
          pts.push([cx, cy])
        }
        const sStart = now
        const lw = 1 + frac * 1.2
        const g = mkGraphics('add')
        g.moveTo(pts[0][0], pts[0][1])
        for (let k = 1; k < pts.length; k++) g.lineTo(pts[k][0], pts[k][1])
        g.stroke({ width: lw, color: 0xffffff, alpha: 1 })
        addEnt({
          update(now2) {
            const t = (now2 - sStart) / 160
            if (t >= 1) return false
            g.alpha = 1 - t
            return true
          },
          destroy() {
            stage?.removeChild(g)
            g.destroy()
          },
        })
      }
      return true
    },
  })

  // Target warning pulses
  let warnLastSpawn = 0
  addEnt({
    update(now) {
      if (!chargeAlive.v || now - chargeStart >= CHARGE_MS - 60) return false
      const frac = (now - chargeStart) / CHARGE_MS
      const interval = 480 - frac * 410
      if (now - warnLastSpawn < interval) return true
      warnLastSpawn = now
      const pStart = now
      const g = mkGraphics('add')
      addEnt({
        update(now2) {
          const t = (now2 - pStart) / 560
          if (t >= 1) return false
          const e = 1 - (1 - t) * (1 - t)
          const r = 20 + e * 120
          g.clear()
          g.circle(x1, y1, r).stroke({
            width: 3 - e * 2.5,
            color: 0xff2a2a,
            alpha: 1 - t,
          })
          return true
        },
        destroy() {
          stage?.removeChild(g)
          g.destroy()
        },
      })
      return true
    },
  })

  // Pre-impact rumble — last 28% of charge
  const rumbleTimer = setTimeout(() => rumbleOn(), Math.floor(CHARGE_MS * 0.72))
  // INCOMING banner — at 30% of charge, pulses until impact
  const incomingTimer = setTimeout(
    () => showIncomingBanner(CHARGE_MS * 0.7),
    Math.floor(CHARGE_MS * 0.3)
  )
  // Scan bars (3x)
  const scanTimers: Array<ReturnType<typeof setTimeout>> = []
  for (let i = 0; i < 3; i++) {
    scanTimers.push(setTimeout(() => slideScanBar(i), 1200 + i * 1600))
  }

  // Beam fires at charge end
  setTimeout(() => {
    chargeAlive.v = false
    vignette.remove()
    dim.remove()
    rumbleOff()
    clearTimeout(rumbleTimer)
    clearTimeout(incomingTimer)
    scanTimers.forEach(clearTimeout)

    const REVEAL = 170
    const SUSTAIN = 220
    const FADE = 360
    const TOTAL = REVEAL + SUSTAIN + FADE
    const beamStart = performance.now()

    const gGlow1 = mkGraphics('add')
    const gGlow2 = mkGraphics('add')
    const gCore = mkGraphics('add')
    addEnt({
      update(now) {
        const el = now - beamStart
        if (el >= TOTAL) return false
        let revealFrac = 1,
          thick = 8,
          glowThick = 22,
          glowAlpha = 0.65,
          beamAlpha = 1
        if (el < REVEAL) {
          revealFrac = 1 - Math.pow(1 - el / REVEAL, 2.5)
        } else if (el < REVEAL + SUSTAIN) {
          const t = (el - REVEAL) / SUSTAIN
          if (t < 0.3) thick = 8 + (16 - 8) * (t / 0.3)
          else if (t < 0.6) thick = 16 + (12 - 16) * ((t - 0.3) / 0.3)
          else thick = 12 + (22 - 12) * ((t - 0.6) / 0.4)
          if (t < 0.5) {
            glowThick = 22 + (44 - 22) * (t / 0.5)
            glowAlpha = 0.65 + (0.85 - 0.65) * (t / 0.5)
          } else {
            glowThick = 44 + (56 - 44) * ((t - 0.5) / 0.5)
            glowAlpha = 0.85 + (1 - 0.85) * ((t - 0.5) / 0.5)
          }
        } else {
          const t = (el - REVEAL - SUSTAIN) / FADE
          thick = 22 + (2 - 22) * t
          beamAlpha = 1 - t
          glowAlpha = 1 - t
          glowThick = 56
        }
        const ex = x0 + (x1 - x0) * revealFrac
        const ey = y0 + (y1 - y0) * revealFrac
        gGlow1.clear()
        gGlow1
          .moveTo(x0, y0)
          .lineTo(ex, ey)
          .stroke({
            width: glowThick * 2,
            color: colorNum,
            alpha: glowAlpha * 0.3,
            cap: 'round',
          })
        gGlow2.clear()
        gGlow2
          .moveTo(x0, y0)
          .lineTo(ex, ey)
          .stroke({
            width: glowThick,
            color: colorNum,
            alpha: glowAlpha * 0.7,
            cap: 'round',
          })
        gCore.clear()
        gCore
          .moveTo(x0, y0)
          .lineTo(ex, ey)
          .stroke({ width: thick, color: 0xffffff, alpha: beamAlpha, cap: 'round' })
        return true
      },
      destroy() {
        stage?.removeChild(gGlow1)
        gGlow1.destroy()
        stage?.removeChild(gGlow2)
        gGlow2.destroy()
        stage?.removeChild(gCore)
        gCore.destroy()
      },
    })

    // Impact at reveal + sustain
    setTimeout(() => {
      whiteOutScreen()
      flashScreen(color)
      quakeScreen()
      shatterAt(x1, y1, color)
      megaShockwaves(x1, y1, color)
      if (opts.hexElement) hqPunch(opts.hexElement)
      debrisChunks(x1, y1, color)
      impactBurst(x1, y1, color, SubmissionType.FirstBlood)
      playShatter()
      playBigImpact()
      spawnAftermathWash()

      if (opts.onImpact) {
        const now = performance.now()
        if (now - lastBloodSoundAt > 1500) {
          lastBloodSoundAt = now
          opts.onImpact()
        }
      }

      setTimeout(() => {
        shakeScreen()
        playAftershock(0.8)
      }, 900)
      setTimeout(() => {
        shakeScreen()
        playAftershock(0.45)
      }, 2000)
    }, REVEAL + SUSTAIN)
  }, CHARGE_MS)
}

/* ---------------------------------------------------------------- */
/* Mega shockwaves + debris                                         */
/* ---------------------------------------------------------------- */

export function megaShockwaves(x: number, y: number, color: string): void {
  const maxR = Math.hypot(window.innerWidth, window.innerHeight)
  const colorNum = hexToNum(color)
  for (let i = 0; i < 5; i++) {
    const w0 = 5 - i * 0.7
    const dur = 1400 - i * 90
    const delay = i * 70
    const strokeNum = i === 0 ? 0xffffff : colorNum
    const start = performance.now() + delay
    let g: PIXI.Graphics | null = null
    addEnt({
      update(now) {
        if (!g && stage) g = mkGraphics('add')
        if (now < start) return true
        const t = (now - start) / dur
        if (t >= 1) return false
        if (g) {
          const e = 1 - Math.pow(1 - t, 3.5)
          const r = 6 + e * (maxR * 0.75 - 6)
          g.clear()
          g.circle(x, y, r).stroke({
            width: (w0 + (0.3 - w0) * t) * 3,
            color: colorNum,
            alpha: (1 - t) * 0.5,
          })
          g.circle(x, y, r).stroke({
            width: w0 + (0.3 - w0) * t,
            color: strokeNum,
            alpha: 1 - t,
          })
        }
        return true
      },
      destroy() {
        if (g) {
          stage?.removeChild(g)
          g.destroy()
        }
      },
    })
  }
}

export function debrisChunks(x: number, y: number, color: string): void {
  const chunks = 22
  const colorNum = hexToNum(color)
  for (let i = 0; i < chunks; i++) {
    const ang = (i / chunks) * Math.PI * 2 + (Math.random() - 0.5) * 0.45
    const speed = 220 + Math.random() * 520
    const lifetime = 900 + Math.random() * 700
    const rot = (Math.random() - 0.5) * Math.PI * 5
    const baseR = 3 + Math.random() * 5
    const sides = 3 + Math.floor(Math.random() * 3)
    const pts: Array<[number, number]> = []
    for (let s = 0; s < sides; s++) {
      const a = (s / sides) * Math.PI * 2
      const rr = baseR * (0.55 + Math.random() * 0.55)
      pts.push([Math.cos(a) * rr, Math.sin(a) * rr])
    }
    const fillNum = Math.random() > 0.3 ? colorNum : 0xffffff
    const endX = x + Math.cos(ang) * speed
    const endY = y + Math.sin(ang) * speed + Math.random() * 60
    const midX = (x + endX) / 2
    const midY = (y + endY) / 2 - 30
    const start = performance.now()
    const g = new PIXI.Graphics()
    g.moveTo(pts[0][0], pts[0][1])
    for (let s = 1; s < pts.length; s++) g.lineTo(pts[s][0], pts[s][1])
    g.closePath().fill({ color: fillNum })
    stage?.addChild(g)
    addEnt({
      update(now) {
        if (!g.parent && stage) stage.addChild(g)
        const t = (now - start) / lifetime
        if (t >= 1) return false
        const e = 1 - Math.pow(1 - t, 2.5)
        const mt = 1 - e
        g.x = mt * mt * x + 2 * mt * e * midX + e * e * endX
        g.y = mt * mt * y + 2 * mt * e * midY + e * e * endY
        g.rotation = rot * e
        g.alpha = 1 - t
        return true
      },
      destroy() {
        if (g.parent) stage?.removeChild(g)
        g.destroy()
      },
    })
  }
}
