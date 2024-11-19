import { InputBase, InputBaseProps } from '@mantine/core'
import { forwardRef, useState, useEffect, useImperativeHandle } from 'react'
import { useTranslation } from 'react-i18next'
import { CaptchaInstance } from '@Components/Captcha'
import workerScript from '@Utils/PowWorker'
import api from '@Api'
import classes from '@Styles/HashPow.module.css'

const getGradient = (nonce?: string | null) => {
  if (!nonce || nonce.length != 16) return {}
  return {
    '--pow-color-0': `#${nonce.slice(0, 6)}`,
    '--pow-color-1': `#${nonce.slice(4, 10)}`,
    '--pow-color-2': `#${nonce.slice(8, 14)}`,
    '--pow-color-3': `#${nonce.slice(12, 16)}${nonce.slice(0, 2)}`,
  }
}

export interface PowRequest {
  chall: string
  diff: number
}

export interface PowResult {
  nonce: string | null
  time: number
  rate: number
}

export const HashPow = forwardRef<CaptchaInstance, InputBaseProps>((props, ref) => {
  const { data: chall } = api.info.useInfoPowChallenge({
    revalidateOnFocus: false,
    revalidateOnReconnect: false,
    refreshWhenHidden: false,
    refreshInterval: 4 * 60 * 1000,
  })
  const { t } = useTranslation()
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<boolean>(false)
  const [result, setNonce] = useState<PowResult | null>(null)
  const [worker, setWorker] = useState<Worker | null>(null)

  useEffect(() => {
    const worker = new Worker(workerScript)
    worker.onmessage = (event: MessageEvent<PowResult>) => {
      setPending(false)
      if (event.data.nonce) {
        setNonce(event.data)
      } else {
        setError(true)
      }
    }
    setWorker(worker)
    return () => {
      worker.terminate()
    }
  }, [chall])

  useEffect(() => {
    if (worker && chall) {
      worker.postMessage({
        chall: chall.challenge,
        diff: chall.difficulty,
      } as PowRequest)
      setPending(true)
    }
  }, [worker, chall])

  useImperativeHandle(
    ref,
    () => ({
      getToken: async () => {
        if (chall && !pending && result) {
          return { valid: true, token: `${chall?.id}:${result}` }
        } else {
          return { valid: false }
        }
      },
    }),
    [chall, pending, result]
  )

  return (
    <InputBase
      {...props}
      w="100%"
      required
      readOnly
      label={t('account.label.captcha')}
      description={
        pending || error || !result
          ? undefined
          : `${result.time / 1000}s @ ${result.rate.toFixed(2)} H/s`
      }
      value={result ? `>>> ${result.nonce} <<<` : ''}
      placeholder={t('account.placeholder.computing')}
      __vars={getGradient(result?.nonce)}
      classNames={classes}
    />
  )
})
