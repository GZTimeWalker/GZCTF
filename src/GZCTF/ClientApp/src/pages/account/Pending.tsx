import { Anchor, Center, List, Stack, Text, Title } from '@mantine/core'
import { FC } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate } from 'react-router'
import { LogoHeader } from '@Components/LogoHeader'
import { usePageTitle } from '@Hooks/usePageTitle'
import misc from '@Styles/Misc.module.css'

const EmailConfirmationPending: FC = () => {
  const location = useLocation()
  const email = location.state?.email || 'ctf@example.com'

  const { t } = useTranslation()
  const navigate = useNavigate()

  usePageTitle(t('account.title.verify_email'))

  return (
    <Center h="100vh">
      <Stack align="center" justify="center" maw={400} px="md">
        <LogoHeader onClick={() => navigate('/')} />
        <Stack gap="xs" align="center" justify="center">
          <Title order={3} ta="center">
            {t('account.content.verify_email.title')}
          </Title>
          <Text size="md" fw="bold" ta="center">
            <Trans i18nKey="account.content.verify_email.message" />
          </Text>
          <Text size="md" fw="bold" ff="monospace" c="brand" ta="center">
            {email}
          </Text>
          <Stack gap={4} mt="sm" align="stretch" w="100%">
            <Text size="xs" fw="bold" ta="center">
              {t('account.content.verify_email.not_received.title')}
            </Text>
            <List spacing={4} size="xs" c="dimmed" withPadding>
              <Trans i18nKey="account.content.verify_email.not_received.list">
                <List.Item />
                <List.Item />
              </Trans>
            </List>
          </Stack>
          <Anchor fz="xs" className={misc.alignSelfEnd} component={Link} to="/account/login" mt="sm">
            {t('account.anchor.login')}
          </Anchor>
        </Stack>
      </Stack>
    </Center>
  )
}

export default EmailConfirmationPending
