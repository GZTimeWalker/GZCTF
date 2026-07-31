import { Anchor, Button, Divider, Grid, PasswordInput, TextInput } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiKey } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { AccountView } from '@Components/AccountView'
import { Captcha, useCaptchaRef } from '@Components/Captcha'
import { encryptApiData } from '@Utils/Crypto'
import { tryGetClientError } from '@Utils/Shared'
import { base64urlToBuffer, bufferToBase64url } from '@Utils/WebAuthn'
import { useConfig } from '@Hooks/useConfig'
import { usePageTitle } from '@Hooks/usePageTitle'
import { useUser } from '@Hooks/useUser'
import api from '@Api'
import misc from '@Styles/Misc.module.css'

const Login: FC = () => {
  const params = useSearchParams()[0]
  const navigate = useNavigate()

  const [pwd, setPwd] = useInputState('')
  const [uname, setUname] = useInputState('')
  const [disabled, setDisabled] = useState(false)
  const [needRedirect, setNeedRedirect] = useState(false)
  const [passkeySupported, setPasskeySupported] = useState(false)

  const { captchaRef, getToken, cleanUp } = useCaptchaRef()
  const { user, mutate } = useUser()
  const { config } = useConfig()

  const { t } = useTranslation()

  usePageTitle(t('account.title.login'))

  useEffect(() => {
    // Check if passkeys are supported
    setPasskeySupported(
      window.PublicKeyCredential !== undefined &&
        typeof window.PublicKeyCredential === 'function'
    )
  }, [])

  useEffect(() => {
    if (needRedirect && user) {
      setNeedRedirect(false)
      setTimeout(() => {
        navigate(params.get('from') ?? '/')
      }, 200)
    }
  }, [user, needRedirect])

  const onLogin = async (event: React.SyntheticEvent) => {
    event.preventDefault()

    if (uname.length === 0 || pwd.length < 6) {
      showNotification({
        color: 'red',
        title: t('account.notification.login.invalid'),
        message: t('common.error.check_input'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      setDisabled(false)
      return
    }

    const { valid, token } = await getToken()

    if (!valid) {
      showNotification({
        color: 'orange',
        title: t('account.notification.captcha.not_valid'),
        message: t('common.error.try_later'),
        loading: true,
      })
      return
    }

    setDisabled(true)

    showNotification({
      color: 'orange',
      id: 'login-status',
      title: t('account.notification.captcha.request_sent.title'),
      message: t('account.notification.captcha.request_sent.message'),
      loading: true,
      autoClose: false,
    })

    try {
      await api.account.accountLogIn({
        userName: uname,
        password: await encryptApiData(t, pwd, config.apiPublicKey),
        challenge: token,
      })

      updateNotification({
        id: 'login-status',
        color: 'teal',
        title: t('account.notification.login.success.title'),
        message: t('account.notification.login.success.message'),
        icon: <Icon path={mdiCheck} size={1} />,
        autoClose: true,
        loading: false,
      })
      cleanUp(true)
      setNeedRedirect(true)
      mutate()
    } catch (err: any) {
      const { title, message } = tryGetClientError(err, t)
      updateNotification({
        id: 'login-status',
        color: 'red',
        title,
        message,
        icon: <Icon path={mdiClose} size={1} />,
        autoClose: true,
        loading: false,
      })
      cleanUp(false)
    } finally {
      setDisabled(false)
    }
  }

  const onPasskeyLogin = async () => {
    if (!passkeySupported) {
      showNotification({
        color: 'red',
        title: t('account.passkey.not_supported'),
        message: '',
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)

    try {
      // Step 1: Get assertion options from server
      const optionsRes = await api.account.accountPasskeyAssertionOptions({
        userName: uname || undefined,
      })
      const options = optionsRes.data as PublicKeyCredentialRequestOptions

      // Convert base64url strings to ArrayBuffers for WebAuthn API
      const publicKeyOptions: PublicKeyCredentialRequestOptions = {
        ...options,
        challenge: base64urlToBuffer(options.challenge as unknown as string),
        allowCredentials: options.allowCredentials?.map((cred) => ({
          ...cred,
          id: base64urlToBuffer(cred.id as unknown as string),
        })),
      }

      // Step 2: Get credential using WebAuthn API
      const credential = (await navigator.credentials.get({
        publicKey: publicKeyOptions,
      })) as PublicKeyCredential

      if (!credential) {
        throw new Error('Failed to get credential')
      }

      // Step 3: Send credential to server
      const response = credential.response as AuthenticatorAssertionResponse
      const credentialJson = JSON.stringify({
        id: credential.id,
        rawId: bufferToBase64url(credential.rawId),
        type: credential.type,
        response: {
          clientDataJSON: bufferToBase64url(response.clientDataJSON),
          authenticatorData: bufferToBase64url(response.authenticatorData),
          signature: bufferToBase64url(response.signature),
          userHandle: response.userHandle ? bufferToBase64url(response.userHandle) : null,
        },
        clientExtensionResults: credential.getClientExtensionResults(),
      })

      await api.account.accountPasskeyAssertion({ credentialJson })

      showNotification({
        color: 'teal',
        title: t('account.notification.login.success.title'),
        message: t('account.notification.login.success.message'),
        icon: <Icon path={mdiCheck} size={1} />,
      })

      setNeedRedirect(true)
      mutate()
    } catch (err: any) {
      if (err.name !== 'NotAllowedError') {
        const { title, message } = tryGetClientError(err, t)
        showNotification({
          color: 'red',
          title: title || t('account.passkey.login_failed'),
          message,
          icon: <Icon path={mdiClose} size={1} />,
        })
      }
    } finally {
      setDisabled(false)
    }
  }

  return (
    <AccountView onSubmit={onLogin}>
      <TextInput
        required
        label={t('account.label.username_or_email')}
        placeholder="ctfer"
        type="text"
        w="100%"
        value={uname}
        disabled={disabled}
        onChange={(event) => setUname(event.currentTarget.value)}
      />
      <PasswordInput
        required
        label={t('account.label.password')}
        id="your-password"
        placeholder="P4ssW@rd"
        w="100%"
        value={pwd}
        disabled={disabled}
        onChange={(event) => setPwd(event.currentTarget.value)}
      />
      <Captcha action="login" ref={captchaRef} />
      <Anchor fz="xs" className={misc.alignSelfEnd} component={Link} to="/account/recovery">
        {t('account.anchor.recovery')}
      </Anchor>
      <Grid grow w="100%">
        <Grid.Col span={2}>
          <Button fullWidth variant="outline" component={Link} to="/account/register">
            {t('account.button.register')}
          </Button>
        </Grid.Col>
        <Grid.Col span={2}>
          <Button fullWidth disabled={disabled} onClick={onLogin}>
            {t('account.button.login')}
          </Button>
        </Grid.Col>
      </Grid>
      {passkeySupported && (
        <>
          <Divider w="100%" my="xs" label="or" labelPosition="center" />
          <Button
            fullWidth
            variant="light"
            leftSection={<Icon path={mdiKey} size={0.9} />}
            disabled={disabled}
            onClick={onPasskeyLogin}
          >
            {t('account.button.login_with_passkey')}
          </Button>
        </>
      )}
    </AccountView>
  )
}

export default Login
