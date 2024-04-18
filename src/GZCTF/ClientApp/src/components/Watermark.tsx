import React, { CSSProperties, useEffect, useMemo, useRef, useState } from 'react'

/**
 * props for Watermark component
 */
export interface WatermarkProps {
  /**
   * show watermark or not
   */
  show?: boolean
  /**
   * text content of watermark
   */
  text: string
  /**
   * text color of watermark, apply to svg fill attribute
   */
  textColor?: string
  /**
   * text size of watermark, apply to svg font-size attribute
   */
  textSize?: number
  /**
   * font family of watermark, apply to svg font-family attribute
   */
  fontFamily?: string
  /**
   * line height of watermark, it will be applied to svg tspan dy attribute
   * only works when multiline is true
   */
  lineHeight?: string
  /**
   * whether watermark is multiline or not
   * text will be split by '\n', and wrapped by svg tspan
   * default is false
   */
  multiline?: boolean
  /**
   * opacity of watermark, apply to svg opacity attribute
   */
  opacity?: number
  /**
   * rotate degree of watermark, apply to svg transform attribute
   */
  rotate?: number
  /**
   * gutter between text
   */
  gutter?: number
  /**
   * style of wrapper element
   */
  wrapperStyle?: CSSProperties
  /**
   * element of wrapper, default is div
   */
  wrapperElement?: React.ElementType
}

/**
 * generate svg string for watermark
 */
function generateSvg(
  options: Required<Omit<WatermarkProps, 'wrapperStyle' | 'wrapperElement' | 'show'>>
) {
  const { text, textColor, textSize, fontFamily, lineHeight, multiline, opacity, gutter, rotate } =
    options
  const rect = calcTextRenderedRect(text, textSize, fontFamily)
  const size = Math.sqrt(rect.width * rect.width + rect.height * rect.height) + gutter * 2
  const center = size / 2

  let textContent = text
  if (multiline) {
    const texts = text.split('\n').map((textByLine, index) => {
      return `<tspan x='50%' dy='${index === 0 ? '0' : lineHeight}'>${textByLine}</tspan>`
    })
    textContent = texts.join('')
  }

  const textEl = `<text fill='${textColor}' x='50%' y='50%' font-size='${textSize}' text-anchor='middle' font-family='${fontFamily}' transform='rotate(${rotate} ${center} ${center})' opacity='${opacity}'>${textContent}</text>`

  return `<svg width='${size}' height='${Math.ceil(
    size / 3
  )}' xmlns='http://www.w3.org/2000/svg'>${textEl}</svg>`
}

function calcTextRenderedRect(text: string, fontSize: number, fontFamily: string): DOMRect {
  const span = document.createElement('span')
  span.innerText = text
  span.style.fontSize = fontSize + 'px'
  span.style.fontFamily = fontFamily
  span.style.visibility = 'hidden'
  document.body.appendChild(span)
  const rect = span.getBoundingClientRect()
  document.body.removeChild(span)
  return rect
}

const Watermark: React.FC<React.PropsWithChildren<WatermarkProps>> = ({
  show = true,
  text,
  textColor = '#cccccc',
  textSize = 24,
  fontFamily = '"JetBrains Mono", "Ubuntu Mono", Courier, Consolas, monospace',
  opacity = 0.2,
  lineHeight = '1.2rem',
  multiline = false,
  wrapperStyle,
  wrapperElement = 'div',
  gutter = 0,
  rotate = -45,
  children,
}) => {
  const [backgroundImage, setBackgroundImage] = useState<string>('')

  useEffect(() => {
    const svg = generateSvg({
      text,
      textColor,
      textSize,
      fontFamily,
      opacity,
      gutter,
      rotate,
      multiline,
      lineHeight,
    })
    setBackgroundImage(`url("data:image/svg+xml;base64,${window.btoa(svg)}")`)
  }, [show, text, textColor, textSize, opacity, gutter, rotate])

  const Wrapper = wrapperElement

  const watermarkStyle = useMemo(
    () => ({
      display: 'block !important',
      visibility: 'visible !important',
      opacity: '1 !important',
      position: 'fixed !important',
      inset: '0px !important',
      width: '100vw !important',
      height: '100vh !important',
      minWidth: '100vw !important',
      minHeight: '100vh !important',
      transform: 'translate(0px) scale(1) rotate(0deg) skew(0deg) !important',
      margin: '0px !important',
      padding: '0px !important',
      clipPath: 'none !important',
      backgroundRepeat: 'repeat !important',
      top: '0 !important',
      left: '0 !important',
      pointerEvents: 'none',
      zIndex: `${Math.floor(Math.random() * 10000) + 50000} !important`,
      backgroundImage,
    }),
    [backgroundImage]
  )

  const MutationObserver = window.MutationObserver
  const kebabCase = (str: string) => str.replace(new RegExp(/[A-Z]/g), (v) => `-${v.toLowerCase()}`)

  const watermarkCSS = Object.entries(watermarkStyle)
    .map(([key, value]) => `${kebabCase(key)}:${value}`)
    .join(';')

  const wrapperRef = useRef<HTMLDivElement>()
  const watermarkRef = useRef<HTMLDivElement>()

  const watermarkBox: (layers: number, child: HTMLDivElement) => HTMLDivElement = (
    layers,
    child
  ) => {
    if (layers > 0) {
      const box = document.createElement('div')
      box.appendChild(child)
      return watermarkBox(layers - 1, box)
    } else {
      return child
    }
  }

  const updateWatermark = () => {
    const wrapper = wrapperRef.current
    if (!wrapper) return

    const watermark = watermarkRef.current

    if (!watermark || !wrapper.contains(watermark)) {
      const div = document.createElement('div')
      div.setAttribute('style', watermarkCSS)
      watermarkRef.current = div
      wrapper.appendChild(watermarkBox(Math.ceil(Math.random() * 8), div))
    } else if (watermark.getAttribute('style') !== watermarkCSS) {
      watermark.setAttribute('style', watermarkCSS)
    }
  }

  const observer = new MutationObserver(updateWatermark)

  // add listener to wrapper element
  useEffect(() => {
    if (!show) return

    updateWatermark()
    const wrapper = wrapperRef.current

    if (wrapper) {
      observer.observe(wrapper, {
        childList: true,
        attributes: true,
        subtree: true,
      })
    }

    return () => observer.disconnect()
  }, [show, backgroundImage])

  return (
    <Wrapper style={{ position: 'relative', ...wrapperStyle }} ref={wrapperRef}>
      {children}
    </Wrapper>
  )
}

export default Watermark
