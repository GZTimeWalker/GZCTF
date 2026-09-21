import { Center, Loader, Stack, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { MySkillTreeRecord } from '@Components/skill-trees/MySkillTreeRecord'
import { WithNavBar } from '@Components/WithNavbar'
import { useMyLearning } from '@Hooks/useSkillTrees'
import { usePageTitle } from '@Hooks/usePageTitle'
import { useUser } from '@Hooks/useUser'
import { useLanguage } from '@Utils/I18n'

const AccountLearning = () => {
  const { user } = useUser()
  const { locale } = useLanguage()
  const { t } = useTranslation('skillTrees')
  const { data: record, error } = useMyLearning(!!user, locale)
  usePageTitle(t('list.title'))

  return (
    <WithNavBar minWidth={0}>
      <Stack gap="xl">
        <Title order={1}>{t('list.title')}</Title>
        {!user ? <Text c="dimmed">Sign in to view your skill trees.</Text> : !record && !error ? <Center><Loader /></Center> : error ? <Text c="red">{t('errors.generic')}</Text> : <MySkillTreeRecord record={record!} />}
      </Stack>
    </WithNavBar>
  )
}

export default AccountLearning

