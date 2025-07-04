import { Anchor, Button, Grid, PasswordInput, TextInput } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { AccountView } from '@Components/AccountView'
import { Captcha, useCaptchaRef } from '@Components/Captcha'
import { encryptApiData } from '@Utils/Crypto'
import { tryGetClientError } from '@Utils/Shared'
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

  const { captchaRef, getToken, cleanUp } = useCaptchaRef()
  const { user, mutate } = useUser()
  const { config } = useConfig()

  const { t } = useTranslation()

  usePageTitle(t('account.title.login'))

  useEffect(() => {
    if (needRedirect && user) {
      setNeedRedirect(false)
      setTimeout(() => {
        navigate(params.get('from') ?? '/')
      }, 200)
    }
  }, [user, needRedirect])

  const onLogin = async (event: React.FormEvent) => {
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
    </AccountView>
  )
}

export default Login
