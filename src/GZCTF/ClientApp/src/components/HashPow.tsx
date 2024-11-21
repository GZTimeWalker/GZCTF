import { BoxProps, Group, InputBase, Text, InputBaseProps } from '@mantine/core'
import { forwardRef, useState, useEffect, useImperativeHandle } from 'react'
import { useTranslation } from 'react-i18next'
import { CaptchaInstance } from '@Components/Captcha'
import { PowWorker } from '@Components/icon/PowWorker'
import workerScript from '@Utils/PowWorker'
import api from '@Api'
import classes from '@Styles/HashPow.module.css'

export interface PowRequest {
  chall: string
  diff: number
}

export interface PowResult {
  nonce: string | null
  time: number
  rate: number
}

export const usePowChallenge = () => {
  const { data: chall, mutate } = api.info.useInfoPowChallenge({
    revalidateOnFocus: false,
    revalidateOnMount: false,
    refreshWhenHidden: false,
    refreshInterval: 4 * 60 * 1000,
  })

  const [result, setNonce] = useState<PowResult | null>(null)
  const [error, setError] = useState<boolean>(false)
  const [worker, setWorker] = useState<Worker | null>(null)
  const [pending, setPending] = useState(false)

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
    } else if (!worker && !chall) {
      mutate()
    }
  }, [worker, chall])

  return { chall, error, mutate, result: pending ? null : result }
}

interface PowBoxProps extends BoxProps {
  nonce?: string | null
}

const PowBox = forwardRef<HTMLDivElement, PowBoxProps>((props, ref) => {
  const { nonce, ...rest } = props
  const [rand, setRand] = useState<string>('0e5cd7b6c765abbf')

  useEffect(() => {
    if (nonce) return

    const array = new Uint32Array(2)
    const interval = setInterval(() => {
      crypto.getRandomValues(array)
      setRand(array.reduce((acc, val) => acc + val.toString(16).padStart(8, '0'), ''))
    }, 76)
    return () => clearInterval(interval)
  }, [nonce])

  const done = !!nonce || undefined

  return (
    <Group
      {...rest}
      ref={ref}
      display="flex"
      wrap="nowrap"
      gap={0}
      justify="space-between"
      className={classes.container}
    >
      <PowWorker done={done} />
      <Text data-done={done} className={classes.text}>
        {nonce || rand}
      </Text>
    </Group>
  )
})

export const HashPow = forwardRef<CaptchaInstance, InputBaseProps>((props, ref) => {
  const { t } = useTranslation()
  const { chall, result, error, mutate } = usePowChallenge()

  useImperativeHandle(
    ref,
    () => ({
      getToken: async () => {
        if (chall && result) {
          return { valid: true, token: `${chall?.id}:${result.nonce}` }
        } else {
          return { valid: false }
        }
      },
      cleanUp: (success?: boolean) => {
        if (!success) {
          // refresh challenge on failure
          mutate()
        }
      },
    }),
    [chall, result]
  )

  return (
    <>
      <InputBase
        {...props}
        w="100%"
        required
        variant="unstyled"
        label={t('account.label.captcha')}
        description={
          !error && result
            ? `${result.time / 1000}s @ ${result.rate.toFixed(2)} kH/s`
            : t('account.placeholder.computing')
        }
        component={PowBox}
        nonce={result?.nonce}
      />
    </>
  )
})
