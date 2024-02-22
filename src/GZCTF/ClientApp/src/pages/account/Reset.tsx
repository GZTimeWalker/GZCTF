import { Button, PasswordInput } from '@mantine/core'
import { getHotkeyHandler, useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router-dom'
import AccountView from '@Components/AccountView'
import StrengthPasswordInput from '@Components/StrengthPasswordInput'
import { showErrorNotification } from '@Utils/ApiHelper'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'

const Reset: FC = () => {
  const location = useLocation()
  const sp = new URLSearchParams(location.search)
  const token = sp.get('token')
  const email = sp.get('email')
  const navigate = useNavigate()
  const [pwd, setPwd] = useInputState('')
  const [retypedPwd, setRetypedPwd] = useInputState('')
  const [disabled, setDisabled] = useState(false)

  const { t } = useTranslation()

  usePageTitle(t('account.title.reset'))

  const onReset = () => {
    if (pwd !== retypedPwd) {
      showNotification({
        color: 'red',
        title: t('common.error.check_input'),
        message: t('account.password.not_match'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    if (!(token && email)) {
      showNotification({
        color: 'red',
        message: t('common.error.param_error'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)
    api.account
      .accountPasswordReset({
        rToken: token,
        email: email,
        password: pwd,
      })
      .then(() => {
        showNotification({
          color: 'teal',
          title: t('account.notification.reset.success.title'),
          message: t('account.notification.reset.success.message'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        navigate('/account/login')
      })
      .catch((err) => {
        showErrorNotification(err, t)
        setDisabled(false)
      })
  }

  const enterHandler = getHotkeyHandler([['Enter', onReset]])

  return (
    <AccountView>
      <StrengthPasswordInput
        value={pwd}
        onChange={(event) => setPwd(event.currentTarget.value)}
        label={t('account.label.password')}
        disabled={disabled}
        onKeyDown={enterHandler}
      />
      <PasswordInput
        required
        value={retypedPwd}
        onChange={(event) => setRetypedPwd(event.currentTarget.value)}
        label={t('account.label.password_retype')}
        w="100%"
        disabled={disabled}
        error={pwd !== retypedPwd}
        onKeyDown={enterHandler}
      />
      <Button fullWidth onClick={onReset} disabled={disabled}>
        {t('account.button.reset')}
      </Button>
    </AccountView>
  )
}

export default Reset
