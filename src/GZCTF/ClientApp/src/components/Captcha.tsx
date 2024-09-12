import { Box, BoxProps, useMantineColorScheme } from '@mantine/core'
import { Turnstile, TurnstileInstance } from '@marsidev/react-turnstile'
import { forwardRef, useImperativeHandle, useRef } from 'react'
import { GoogleReCaptchaProvider, useGoogleReCaptcha } from 'react-google-recaptcha-v3'
import { useCaptchaConfig } from '@Utils/useConfig'
import { CaptchaProvider } from '@Api'

interface CaptchaProps extends BoxProps {
  action: string
}

interface CaptchaResult {
  valid: boolean
  token?: string | null
}

export interface CaptchaInstance {
  getToken: () => Promise<CaptchaResult>
}

export const useCaptchaRef = () => {
  const captchaRef = useRef<CaptchaInstance>(null)

  const getToken = async () => {
    const res = await captchaRef.current?.getToken()
    return res ?? { valid: false }
  }

  return { captchaRef, getToken } as const
}

const ReCaptchaBox = forwardRef<CaptchaInstance, CaptchaProps>((props, ref) => {
  const { action, ...others } = props
  const { executeRecaptcha } = useGoogleReCaptcha()

  useImperativeHandle(
    ref,
    () => ({
      getToken: async () => {
        if (!executeRecaptcha) {
          return { valid: false }
        }

        const token = await executeRecaptcha(action)
        return { valid: !!token, token }
      },
    }),
    [executeRecaptcha, action]
  )

  return <Box {...others} />
})

const Captcha = forwardRef<CaptchaInstance, CaptchaProps>((props, ref) => {
  const { action, ...others } = props

  const { info, error } = useCaptchaConfig()
  const { colorScheme } = useMantineColorScheme()
  const type = info?.type ?? CaptchaProvider.None
  const turnstileRef = useRef<TurnstileInstance>(null)
  const reCaptchaRef = useRef<CaptchaInstance>(null)
  const nonce = document.getElementById('nonce-container')?.getAttribute('data-nonce') ?? undefined

  useImperativeHandle(
    ref,
    () => ({
      getToken: async () => {
        if (error || !info) {
          return { valid: false }
        }

        if (!info?.siteKey || type === CaptchaProvider.None) {
          return { valid: true }
        }

        if (type === CaptchaProvider.GoogleRecaptcha) {
          const res = await reCaptchaRef.current?.getToken()
          return res ?? { valid: false }
        }

        const token = turnstileRef.current?.getResponse()
        return { valid: !!token, token }
      },
    }),
    [error, info, type]
  )

  if (error || !info?.siteKey || type === CaptchaProvider.None) {
    return <Box {...others} />
  }

  if (type === CaptchaProvider.GoogleRecaptcha) {
    return (
      <GoogleReCaptchaProvider
        reCaptchaKey={info.siteKey}
        scriptProps={{
          nonce,
        }}
        container={{
          parameters: {
            theme: colorScheme == 'auto' ? undefined : colorScheme,
          },
        }}
      >
        <ReCaptchaBox ref={reCaptchaRef} action={action} {...others} />
      </GoogleReCaptchaProvider>
    )
  }

  return (
    <Box {...others}>
      <Turnstile
        ref={turnstileRef}
        siteKey={info.siteKey}
        options={{
          theme: colorScheme,
          action,
        }}
        scriptOptions={{
          nonce,
        }}
      />
    </Box>
  )
})

export default Captcha
