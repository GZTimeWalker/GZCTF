import { Button, Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { AccountView } from '@Components/AccountView'
import { usePageTitle } from '@Hooks/usePageTitle'
import api from '@Api'

const Confirm: FC = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const sp = new URLSearchParams(location.search)
  const token = sp.get('token')
  const email = sp.get('email')
  const [disabled, setDisabled] = useState(false)
  const { t } = useTranslation()
  const decodeEmail = window.atob(email ?? '')

  usePageTitle(t('account.title.confirm'))

  const verify = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!token || !email) {
      showNotification({
        color: 'red',
        title: t('account.notification.confirm.failed'),
        message: t('common.error.param_missing'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)

    try {
      await api.account.accountMailChangeConfirm({ token, email })
      showNotification({
        color: 'teal',
        title: t('account.notification.confirm.success'),
        message: decodeEmail,
        icon: <Icon path={mdiCheck} size={1} />,
      })
      navigate('/')
    } catch {
      showNotification({
        color: 'red',
        title: t('account.notification.confirm.failed'),
        message: t('common.error.param_error'),
        icon: <Icon path={mdiClose} size={1} />,
      })
    } finally {
      setDisabled(false)
    }
  }

  return (
    <AccountView onSubmit={verify}>
      {email && token ? (
        <>
          <Text size="md" fw={500}>
            {t('account.content.welcome', { decodeEmail })}
          </Text>
          <Text size="md" fw={500}>
            {t('account.content.confirm.message')}
          </Text>
          <Button mt="lg" type="submit" w="50%" disabled={disabled}>
            {t('account.button.confirm_email')}
          </Button>
        </>
      ) : (
        <>
          <Text size="md" fw={500}>
            {t('account.content.link_invalid')}
          </Text>
          <Text size="md" fw={500}>
            {t('account.content.link_check')}
          </Text>
        </>
      )}
    </AccountView>
  )
}

export default Confirm
