import { FC, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Button, Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import AccountView from '@Components/AccountView'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'
import { Trans, useTranslation } from 'react-i18next'
import i18nKeyOf from '../../utils/I18n'

const Confirm: FC = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const sp = new URLSearchParams(location.search)
  const token = sp.get('token')
  const email = sp.get('email')
  const [disabled, setDisabled] = useState(false)
  const { t } = useTranslation()

  usePageTitle(t(i18nKeyOf('Page_ConfirmEmail')))

  const verify = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!token || !email) {
      showNotification({
        color: 'red',
        title: t(i18nKeyOf('Email_ConfirmFailed')),
        message: t(i18nKeyOf('Param_Missing')),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)
    api.account
      .accountMailChangeConfirm({ token, email })
      .then(() => {
        showNotification({
          color: 'teal',
          title: t(i18nKeyOf('Email_Confirm')),
          message: window.atob(email),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        navigate('/')
      })
      .catch(() => {
        showNotification({
          color: 'red',
          title: t(i18nKeyOf('Email_ConfirmFailed')),
          message: t(i18nKeyOf('Param_Error')),
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <AccountView onSubmit={verify}>
      {email && token ? (
        <>
          <Text size="md" fw={500}>
            {window.atob(email)} {t(i18nKeyOf('HelloWithHand'))}
          </Text>
          <Text size="md" fw={500}>
            <Trans i18nKey={i18nKeyOf('Email_ConfirmInstruction')} />
          </Text>
          <Button mt="lg" type="submit" w="50%" disabled={disabled}>
            <Trans i18nKey={i18nKeyOf('Email_Confirm')} />
          </Button>
        </>
      ) : (
        <>
          <Text size="md" fw={500}>
            <Trans i18nKey={i18nKeyOf('Email_ConfirmInvalidLink')} />
          </Text>
          <Text size="md" fw={500}>
            <Trans i18nKey={i18nKeyOf('Email_ConfirmInvalidLinkInstruction')} />
          </Text>
        </>
      )}
    </AccountView>
  )
}

export default Confirm
