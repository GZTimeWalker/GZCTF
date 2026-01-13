import { Skeleton, Text, TextProps, Typography, useMantineColorScheme } from '@mantine/core'
import 'katex/dist/katex.min.css'
import { Marked } from 'marked'
import { forwardRef, useMemo, Suspense, FC, useEffect, useRef } from 'react'
import { KatexExtension } from '@Utils/marked/KatexExtension'
import { MermaidExtension } from '@Utils/marked/MermaidExtension'
import { ShikiExtension } from '@Utils/marked/ShikiExtension'
import { requestIdle } from '@Utils/requestIdle'
import classes from '@Styles/Typography.module.css'

let mermaidPromise: Promise<any> | null = null
let mermaidInitialized = false
let mermaidRenderSeq = 0
let mermaidTheme: 'dark' | 'default' | null = null

async function getMermaid(theme: 'dark' | 'default') {
  if (!mermaidPromise) {
    console.debug('[Mermaid] loading bundle...')
    mermaidPromise = import('mermaid')
  }

  const mod = await mermaidPromise
  const mermaid = (mod as unknown as { default?: any }).default ?? mod

  if (!mermaidInitialized || mermaidTheme !== theme) {
    try {
      mermaid.initialize({
        startOnLoad: false,
        securityLevel: 'loose',
        theme,
      })
      mermaidInitialized = true
      mermaidTheme = theme
      console.debug('[Mermaid] initialized')
    } catch (e) {
      console.error('[Mermaid] initialize failed', e)
      throw e
    }
  }

  return mermaid
}

export interface MarkdownProps extends React.ComponentPropsWithoutRef<'div'> {
  source: string
}

interface InlineMarkdownProps extends TextProps {
  source: string
}

export const InlineMarkdown = forwardRef<HTMLParagraphElement, InlineMarkdownProps>((props, ref) => {
  const { source, ...others } = props

  const inlineMarked = useMemo(() => {
    const instance = new Marked()
    instance.use(KatexExtension()).setOptions({ silent: true })
    return instance
  }, [])

  return (
    <Text
      ref={ref}
      {...others}
      className={classes.inline}
      dangerouslySetInnerHTML={{
        __html: inlineMarked.parseInline(source) ?? '',
      }}
    />
  )
})

export const ContentPlaceholder: FC = () => (
  <>
    <Skeleton height={14} mt={8} radius="xl" />
    <Skeleton height={14} mt={8} radius="xl" />
    <Skeleton height={14} mt={8} width="60%" radius="xl" />
    <Skeleton height={14} mt={8 + 14} radius="xl" />
    <Skeleton height={14} mt={8} radius="xl" />
    <Skeleton height={14} mt={8} width="30%" radius="xl" />
    <Skeleton height={14} mt={8 + 14} radius="xl" />
    <Skeleton height={14} mt={8} width="80%" radius="xl" />
  </>
)

const MarkdownRenderer: FC<Pick<MarkdownProps, 'source'>> = (props) => {
  const { source } = props
  const rootRef = useRef<HTMLDivElement>(null)
  const { colorScheme } = useMantineColorScheme()
  const desiredTheme: 'dark' | 'default' = colorScheme === 'dark' ? 'dark' : 'default'

  const marked = useMemo(() => {
    const instance = new Marked()
    instance.use(KatexExtension()).use(MermaidExtension()).use(ShikiExtension()).setOptions({ silent: true })
    return instance
  }, [])

  const html = useMemo(() => marked.parse(source), [marked, source])

  useEffect(() => {
    if (!/```[\t ]*mermaid\b/i.test(source)) return
    void getMermaid(desiredTheme)
  }, [source, desiredTheme])

  useEffect(() => {
    let cancelled = false

    const run = async () => {
      const root = rootRef.current
      if (!root) return

      try {
        const mermaid = await getMermaid(desiredTheme)
        if (cancelled) return

        const nodes = Array.from(root.querySelectorAll<HTMLElement>('.mermaid'))
        if (nodes.length === 0) return

        const needsRender = (el: HTMLElement) => {
          const renderedTheme = el.getAttribute('data-mermaid-theme')
          return !el.hasAttribute('data-processed') || renderedTheme !== desiredTheme
        }

        const queue: HTMLElement[] = []
        const queued = new WeakSet<HTMLElement>()
        let pumping = false

        const enqueue = (n: HTMLElement) => {
          if (queued.has(n)) return
          queued.add(n)
          queue.push(n)
          pump()
        }

        const waitForLayout = async () => {
          await new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())))
          await (document as unknown as { fonts?: { ready: Promise<void> } }).fonts?.ready?.catch?.(() => undefined)
        }

        const renderOne = async (node: HTMLElement) => {
          if (!node.isConnected) return
          if (!needsRender(node)) return

          const encoded = node.getAttribute('data-mermaid-src')
          const code = encoded ? decodeURIComponent(encoded) : (node.textContent ?? '').trim()
          if (!code) return

          const id = `mmd-${Date.now()}-${++mermaidRenderSeq}`
          console.debug('[Mermaid] render start', { id })

          const attemptRender = async () => {
            await waitForLayout()
            if (cancelled) return

            const res = await mermaid.render(id, code, node)
            const svg = (res && (res.svg ?? res)) as string
            const bind = res?.bindFunctions as undefined | ((el: Element) => void)

            if (!svg || typeof svg !== 'string' || svg.trim().length === 0) {
              throw new Error('Empty SVG returned by mermaid.render')
            }

            node.innerHTML = svg
            node.setAttribute('data-processed', 'true')
            node.setAttribute('data-mermaid-theme', desiredTheme)
            bind?.(node)
          }

          try {
            await attemptRender()
          } catch (e) {
            await new Promise((r) => setTimeout(r, 30))
            await attemptRender()
          }

          console.debug('[Mermaid] render done', { id })
        }

        let cancelIdle: null | (() => void) = null

        const pump = () => {
          if (pumping) return
          pumping = true

          cancelIdle?.()
          cancelIdle = requestIdle(async () => {
            try {
              while (!cancelled && queue.length > 0) {
                const node = queue.shift()!
                await renderOne(node)
                await new Promise((r) => setTimeout(r, 0))
              }
            } catch (e) {
              console.error('[Mermaid] render failed', e)
            } finally {
              pumping = false
            }
          })
        }

        const logTag = `[Mermaid] queue (${nodes.length} node${nodes.length > 1 ? 's' : ''})`
        console.time(logTag)

        const observer = new IntersectionObserver(
          (entries) => {
            for (const entry of entries) {
              if (!entry.isIntersecting) continue
              const el = entry.target as HTMLElement
              if (needsRender(el)) enqueue(el)
              observer.unobserve(entry.target)
            }
          },
          { root: null, rootMargin: '800px 0px', threshold: 0.01 }
        )

        for (const node of nodes) observer.observe(node)

        const fallbackId = window.setTimeout(() => {
          for (const node of nodes) {
            if (needsRender(node)) enqueue(node)
          }
        }, 1200)

        const cleanup = () => {
          window.clearTimeout(fallbackId)
          cancelIdle?.()
          observer.disconnect()
          console.timeEnd(logTag)
        }

        return cleanup
      } catch (e) {
        console.error('[Mermaid] render failed', e)
      }
    }

    const cleanupPromise = run()
    return () => {
      cancelled = true
      void cleanupPromise.then((cleanup) => cleanup?.())
    }
  }, [html, desiredTheme])

  return <div ref={rootRef} className={classes.root} dangerouslySetInnerHTML={{ __html: html ?? '' }} />
}

export const Markdown = forwardRef<HTMLDivElement, MarkdownProps>((props, ref) => {
  const { source, ...others } = props

  return (
    <Typography ref={ref} {...others}>
      <Suspense fallback={<ContentPlaceholder />}>
        <MarkdownRenderer source={source} />
      </Suspense>
    </Typography>
  )
})
