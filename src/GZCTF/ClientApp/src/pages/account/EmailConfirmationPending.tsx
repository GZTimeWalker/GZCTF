import { Anchor, Stack, Text, Title } from '@mantine/core'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import { AccountView } from '@Components/AccountView'
import { usePageTitle } from '@Hooks/usePageTitle'
import misc from '@Styles/Misc.module.css'

const EmailConfirmationPending: FC = () => {
  const location = useLocation()
  const email = location.state?.email || ''

  const { t } = useTranslation()

  usePageTitle(t('account.title.email_confirmation_pending'))

  return (
    <AccountView>
      <Stack gap="md" align="center" justify="center" maw={500}>
        <Title order={2} ta="center">
          {t('account.content.email_confirmation_pending.title')}
        </Title>
        <Text size="md" fw={500} ta="center">
          {t('account.content.email_confirmation_pending.message')}
        </Text>
        {email && typeof email === 'string' && email.trim() && (
          <Text size="md" fw={700} c="blue" ta="center">
            {email.trim()}
          </Text>
        )}
        <Text size="sm" c="dimmed" ta="center">
          {t('account.content.email_confirmation_pending.check_spam')}
        </Text>
        <Anchor fz="sm" className={misc.alignSelfEnd} component={Link} to="/account/login" mt="md">
          {t('account.anchor.login')}
        </Anchor>
      </Stack>
    </AccountView>
  )
}

export default EmailConfirmationPending
