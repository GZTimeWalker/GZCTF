import { useEffect, useState } from 'react'
import { useMantineTheme } from '@mantine/core'
import api, { CaptchaProvider } from '@Api'

declare global {
  interface Window {
    turnstile: any
    grecaptcha: any
    onloadTurnstileCallback: any
  }
}

interface Captcha {
  siteKey: string | null
  action: string
  getChallenge(): Promise<null | string>
}

export class reCAPTCHA implements Captcha {
  siteKey: string | null
  action: string

  static removeCaptcha() {
    const script = document.getElementById('grecaptcha-script')
    if (script) {
      script.remove()
    }

    const recaptchaElems = document.getElementsByClassName('grecaptcha-badge')
    if (recaptchaElems.length) {
      recaptchaElems[0].remove()
    }
  }

  async getChallenge(): Promise<null | string> {
    if (this.siteKey === null) return null

    let token = ''
    await window.grecaptcha.execute(this.siteKey, { action: this.action }).then((res: string) => {
      token = res
    })
    return token.length > 0 ? token : null
  }

  constructor(siteKey: string, action: string) {
    this.loadReCaptcha(siteKey)
    this.siteKey = siteKey
    this.action = action
  }

  public loadReCaptcha(siteKey: string) {
    if (!siteKey || siteKey === 'NOTOKEN') return
    const script = document.createElement('script')
    script.id = 'grecaptcha-script'
    script.src = `https://www.recaptcha.net/recaptcha/api.js?render=${siteKey}`
    document.body.appendChild(script)
  }
}

export class Turnstile implements Captcha {
  siteKey: string | null
  action: string
  challenge: string | null

  static removeCaptcha() {
    const script = document.getElementById('turnstile-script')
    if (script) {
      script.remove()
    }
  }

  async getChallenge(): Promise<null | string> {
    if (this.siteKey === null) return null

    return this.challenge
  }

  public loadTurnstile(siteKey: string, theme: string = 'light') {
    if (!siteKey) return

    const script = document.createElement('script')
    script.id = 'turnstile-script'
    script.src =
      'https://challenges.cloudflare.com/turnstile/v0/api.js?onload=onloadTurnstileCallback'
    script.async = true
    script.defer = true
    document.body.appendChild(script)

    window.onloadTurnstileCallback = () => {
      window.turnstile.render('#captcha', {
        sitekey: siteKey,
        theme: theme,
        callback: (token: string) => (this.challenge = token),
      })
    }
  }

  constructor(siteKey: string, action: string, theme: string = 'light') {
    this.loadTurnstile(siteKey, theme)
    this.siteKey = siteKey
    this.action = action
    this.challenge = null
  }
}

export const useCaptcha = (action: string) => {
  const { data: info, error } = api.info.useInfoGetClientCaptchaInfo({
    refreshInterval: 0,
    revalidateOnFocus: false,
    revalidateOnReconnect: false,
    refreshWhenHidden: false,
    shouldRetryOnError: false,
    refreshWhenOffline: false,
  })

  const theme = useMantineTheme()
  const [reCaptcha, setReCaptcha] = useState<Captcha | null>(null)

  useEffect(() => {
    if (info?.type === CaptchaProvider.GoogleRecaptcha) {
      setReCaptcha(info?.siteKey && !error ? new reCAPTCHA(info.siteKey, action) : null)
      return reCAPTCHA.removeCaptcha
    } else if (info?.type === CaptchaProvider.CloudflareTurnstile) {
      setReCaptcha(
        info?.siteKey && !error ? new Turnstile(info.siteKey, action, theme.colorScheme) : null
      )
      return Turnstile.removeCaptcha
    }
  }, [info, error, action])

  return reCaptcha
}
