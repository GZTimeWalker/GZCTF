import { useEffect, useState } from 'react'
import api from '@Api'

declare global {
  interface Window {
    grecaptcha: any
  }
}

export class reCAPTCHA {
  siteKey: string
  action: string

  constructor(siteKey: string, action: string) {
    loadReCaptcha(siteKey)
    this.siteKey = siteKey
    this.action = action
  }

  async getToken(): Promise<'NOTOKEN' | string | undefined> {
    if (this.siteKey === 'NOTOKEN') return 'NOTOKEN'

    let token = ''
    await window.grecaptcha.execute(this.siteKey, { action: this.action }).then((res: string) => {
      token = res
    })
    return token.length > 0 ? token : undefined
  }
}

const removeReCaptcha = () => {
  const script = document.getElementById('grecaptcha-script')
  if (script) {
    script.remove()
  }

  const recaptchaElems = document.getElementsByClassName('grecaptcha-badge')
  if (recaptchaElems.length) {
    recaptchaElems[0].remove()
  }
}

const loadReCaptcha = (siteKey: string) => {
  if (!siteKey || siteKey === 'NOTOKEN') return
  const script = document.createElement('script')
  script.id = 'grecaptcha-script'
  script.src = `https://www.recaptcha.net/recaptcha/api.js?render=${siteKey}`
  document.body.appendChild(script)
  return script
}

export const useReCaptcha = (action: string) => {
  const { data: sitekey, error } = api.info.useInfoGetRecaptchaSiteKey({
    refreshInterval: 0,
    revalidateOnFocus: false,
    revalidateOnReconnect: false,
    refreshWhenHidden: false,
    shouldRetryOnError: false,
    refreshWhenOffline: false,
  })

  const [reCaptcha, setReCaptcha] = useState<reCAPTCHA | null>(null)

  useEffect(() => {
    setReCaptcha(sitekey && !error ? new reCAPTCHA(sitekey, action) : null)
    return removeReCaptcha
  }, [sitekey, error, action])

  return reCaptcha
}
