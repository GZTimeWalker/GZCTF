import { Anchor, Button, PasswordInput, TextInput } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import AccountView from '@Components/AccountView'
import Captcha, { useCaptchaRef } from '@Components/Captcha'
import StrengthPasswordInput from '@Components/StrengthPasswordInput'
import { usePageTitle } from '@Utils/usePageTitle'
import api, { RegisterStatus } from '@Api'

const Register: FC = () => {
  const [pwd, setPwd] = useInputState('')
  const [retypedPwd, setRetypedPwd] = useInputState('')
  const [uname, setUname] = useInputState('')
  const [email, setEmail] = useInputState('')
  const [disabled, setDisabled] = useState(false)

  const navigate = useNavigate()
  const { captchaRef, getToken } = useCaptchaRef()

  const { t } = useTranslation()

  const RegisterStatusMap = new Map([
    [
      RegisterStatus.LoggedIn,
      {
        message: t('account.notification.register.logged_in'),
      },
    ],
    [
      RegisterStatus.AdminConfirmationRequired,
      {
        title: t('account.notification.register.request_sent.title'),
        message: t('account.notification.register.request_sent.message'),
      },
    ],
    [
      RegisterStatus.EmailConfirmationRequired,
      {
        title: t('common.email.sent.title'),
        message: t('common.email.sent.message'),
      },
    ],
    [undefined, undefined],
  ])

  usePageTitle(t('account.title.register'))

  const onRegister = async (event: React.FormEvent) => {
    event.preventDefault()

    if (pwd !== retypedPwd) {
      showNotification({
        color: 'red',
        title: t('common.error.check_input'),
        message: t('account.password.not_match'),
        icon: <Icon path={mdiClose} size={1} />,
      })
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
      id: 'register-status',
      title: t('account.notification.captcha.request_sent.title'),
      message: t('account.notification.captcha.request_sent.message'),
      loading: true,
      autoClose: false,
    })

    try {
      const res = await api.account.accountRegister({
        userName: uname,
        password: pwd,
        email: email,
        challenge: token,
      })
      const data = RegisterStatusMap.get(res.data.data)
      if (data) {
        updateNotification({
          id: 'register-status',
          color: 'teal',
          title: data.title,
          message: data.message,
          icon: <Icon path={mdiCheck} size={1} />,
          loading: false,
          autoClose: true,
        })

        if (res.data.data === RegisterStatus.LoggedIn) navigate('/')
        else navigate('/account/login')
      }
    } catch (err: any) {
      updateNotification({
        id: 'register-status',
        color: 'red',
        title: t('common.error.encountered'),
        message: `${err.response.data.title}`,
        icon: <Icon path={mdiClose} size={1} />,
        loading: false,
        autoClose: true,
      })
    } finally {
      setDisabled(false)
    }
  }

  return (
    <AccountView onSubmit={onRegister}>
      <TextInput
        required
        label={t('account.label.email')}
        type="email"
        placeholder="ctf@example.com"
        w="100%"
        value={email}
        disabled={disabled}
        onChange={(event) => setEmail(event.currentTarget.value)}
      />
      <TextInput
        required
        label={t('account.label.username')}
        type="text"
        placeholder="ctfer"
        w="100%"
        value={uname}
        disabled={disabled}
        onChange={(event) => setUname(event.currentTarget.value)}
      />
      <StrengthPasswordInput
        value={pwd}
        onChange={(event) => setPwd(event.currentTarget.value)}
        disabled={disabled}
      />
      <PasswordInput
        required
        label={t('account.label.password_retype')}
        value={retypedPwd}
        onChange={(event) => setRetypedPwd(event.currentTarget.value)}
        disabled={disabled}
        w="100%"
        error={pwd !== retypedPwd}
      />
      <Captcha action="register" ref={captchaRef} />
      <Anchor fz="xs" style={{ alignSelf: 'end' }} component={Link} to="/account/login">
        {t('account.anchor.login')}
      </Anchor>
      <Button type="submit" fullWidth onClick={onRegister} disabled={disabled}>
        {t('account.button.register')}
      </Button>
    </AccountView>
  )
}

export default Register
