import { Code, Text } from '@mantine/core'
import { CSSProperties, FC, ReactElement, useMemo } from 'react'

export type ViewMode = 'ascii' | 'hex'

interface HexAsciiViewProps {
  /** Raw bytes for this chunk. */
  bytes: Uint8Array
  /** Render mode. */
  mode: ViewMode
  /** Byte offsets in `bytes` where a known flag begins. */
  flagOffsets?: number[]
  /** Flag byte length used to highlight a range (defaults to 24, a reasonable upper bound). */
  flagLengths?: number[]
  style?: CSSProperties
}

const printable = (b: number) => (b >= 0x20 && b < 0x7f ? String.fromCharCode(b) : '.')

/**
 * Renders a payload buffer either as a printable-ASCII dump or a classic 16-byte
 * hex dump. Flag occurrences are wrapped in a yellow <mark> so operators can spot
 * them at a glance.
 */
export const HexAsciiView: FC<HexAsciiViewProps> = ({ bytes, mode, flagOffsets, flagLengths, style }) => {
  const segments = useMemo(() => buildSegments(bytes, flagOffsets ?? [], flagLengths ?? []), [
    bytes,
    flagOffsets,
    flagLengths,
  ])

  if (bytes.length === 0) {
    return (
      <Text c="dimmed" size="sm">
        (empty)
      </Text>
    )
  }

  if (mode === 'ascii') {
    return (
      <Code
        block
        style={{
          fontSize: '12px',
          lineHeight: 1.4,
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-all',
          maxHeight: '60vh',
          overflowY: 'auto',
          ...style,
        }}
      >
        {segments.map((seg, i) =>
          seg.flag ? (
            <mark key={i} style={{ backgroundColor: '#ffec99' }}>
              {Array.from(seg.bytes, printable).join('')}
            </mark>
          ) : (
            <span key={i}>{Array.from(seg.bytes, printable).join('')}</span>
          )
        )}
      </Code>
    )
  }

  return (
    <Code
      block
      style={{
        fontSize: '12px',
        lineHeight: 1.4,
        whiteSpace: 'pre',
        maxHeight: '60vh',
        overflowY: 'auto',
        ...style,
      }}
    >
      {renderHexRows(bytes, flagOffsets ?? [], flagLengths ?? [])}
    </Code>
  )
}

interface Segment {
  bytes: Uint8Array
  flag: boolean
}

const buildSegments = (bytes: Uint8Array, offsets: number[], lengths: number[]): Segment[] => {
  if (offsets.length === 0) return [{ bytes, flag: false }]

  const segs: Segment[] = []
  let cursor = 0
  offsets.forEach((off, idx) => {
    if (off < cursor) return
    if (off > cursor) segs.push({ bytes: bytes.slice(cursor, off), flag: false })
    const len = lengths[idx] ?? 24
    const end = Math.min(off + len, bytes.length)
    segs.push({ bytes: bytes.slice(off, end), flag: true })
    cursor = end
  })
  if (cursor < bytes.length) segs.push({ bytes: bytes.slice(cursor), flag: false })
  return segs
}

const renderHexRows = (bytes: Uint8Array, offsets: number[], lengths: number[]) => {
  const rows: ReactElement[] = []
  const flagRanges = offsets.map((off, idx) => [off, off + (lengths[idx] ?? 24)] as const)
  const isInFlag = (i: number) => flagRanges.some(([s, e]) => i >= s && i < e)

  for (let line = 0; line < bytes.length; line += 16) {
    const slice = bytes.slice(line, line + 16)
    const hex: ReactElement[] = []
    const ascii: ReactElement[] = []
    for (let i = 0; i < slice.length; i++) {
      const abs = line + i
      const b = slice[i]
      const flagged = isInFlag(abs)
      const hexByte = b.toString(16).padStart(2, '0')
      hex.push(
        <span key={i} style={flagged ? { backgroundColor: '#ffec99' } : undefined}>
          {hexByte}
          {i === 7 ? '  ' : ' '}
        </span>
      )
      ascii.push(
        <span key={i} style={flagged ? { backgroundColor: '#ffec99' } : undefined}>
          {printable(b)}
        </span>
      )
    }
    rows.push(
      <div key={line}>
        <span style={{ color: '#888' }}>{line.toString(16).padStart(8, '0')}  </span>
        {hex}
        {'  '}
        {ascii}
      </div>
    )
  }
  return rows
}
