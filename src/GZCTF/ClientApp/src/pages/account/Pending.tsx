import { Anchor, Center, Stack, Text, Title } from '@mantine/core'
import { FC, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate } from 'react-router'
import { LogoHeader } from '@Components/LogoHeader'
import { usePageTitle } from '@Hooks/usePageTitle'
import misc from '@Styles/Misc.module.css'

// Email validation regex
const isValidEmail = (email: string): boolean => {
  return typeof email === 'string' && email.trim().length > 0 && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())
}

const EmailConfirmationPending: FC = () => {
  const location = useLocation()
  const emailFromState = location.state?.email || ''

  const email = useMemo(() => (isValidEmail(emailFromState) ? emailFromState.trim() : ''), [emailFromState])

  const { t } = useTranslation()
  const navigate = useNavigate()

  usePageTitle(t('account.title.verify_email'))

  return (
    <Center h="100vh">
      <Stack align="center" justify="center">
        <LogoHeader onClick={() => navigate('/')} />
        <Stack gap="xs" align="center" justify="center">
          <Title order={3} fw={600} ta="center">
            {t('account.content.verify_email.title')}
          </Title>
          <Text size="md" fw={500} ta="center">
            {t('account.content.verify_email.message')}
          </Text>
          <Text size="md" fw={600} c="brand" ta="center">
            {email || 'email@example.com'}
          </Text>
          <Text size="sm" c="dimmed" ta="center">
            {t('account.content.verify_email.check_spam')}
          </Text>
          <Anchor fz="xs" className={misc.alignSelfEnd} component={Link} to="/account/login">
            {t('account.anchor.login')}
          </Anchor>
        </Stack>
      </Stack>
    </Center>
  )
}

export default EmailConfirmationPending
