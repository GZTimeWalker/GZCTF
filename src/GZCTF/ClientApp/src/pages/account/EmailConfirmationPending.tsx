import { Anchor, Stack, Text } from '@mantine/core'
import { FC, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import { AccountView } from '@Components/AccountView'
import { usePageTitle } from '@Hooks/usePageTitle'
import misc from '@Styles/Misc.module.css'

// Email validation regex
const isValidEmail = (email: string): boolean => {
  return (
    typeof email === 'string' &&
    email.trim().length > 0 &&
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())
  )
}

const EmailConfirmationPending: FC = () => {
  const location = useLocation()
  const emailFromState = location.state?.email || ''

  const email = useMemo(
    () => (isValidEmail(emailFromState) ? emailFromState.trim() : ''),
    [emailFromState]
  )

  const { t } = useTranslation()

  usePageTitle(t('account.title.verify_email'))

  return (
    <AccountView>
      <Text size="lg" fw={600} ta="center">
        {t('account.content.verify_email.title')}
      </Text>
      <Text size="md" fw={500} ta="center">
        {t('account.content.verify_email.message')}
      </Text>
      {email && (
        <Text size="md" fw={600} c="brand" ta="center">
          {email}
        </Text>
      )}
      <Text size="sm" c="dimmed" ta="center">
        {t('account.content.verify_email.check_spam')}
      </Text>
      <Anchor fz="xs" className={misc.alignSelfEnd} component={Link} to="/account/login">
        {t('account.anchor.login')}
      </Anchor>
    </AccountView>
  )
}

export default EmailConfirmationPending
