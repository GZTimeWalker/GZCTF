import { Center, Loader, Stack, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { LearningRecord } from '@Components/learning/LearningRecord'
import { WithNavBar } from '@Components/WithNavbar'
import { useMyLearning } from '@Hooks/useLearning'
import { usePageTitle } from '@Hooks/usePageTitle'
import { useUser } from '@Hooks/useUser'
import { useLanguage } from '@Utils/I18n'

const AccountLearning = () => {
  const { user } = useUser()
  const { locale } = useLanguage()
  const { t } = useTranslation('learning')
  const { data: record, error } = useMyLearning(locale, !!user)
  usePageTitle(t('recordTitle'))

  return (
    <WithNavBar minWidth={0}>
      <Stack gap="xl">
        <Title order={1}>{t('recordTitle')}</Title>
        {!user ? <Text c="dimmed">{t('signInToStudy')}</Text> : !record && !error ? <Center><Loader /></Center> : error ? <Text c="red">{t('loadFailed')}</Text> : <LearningRecord record={record!} />}
      </Stack>
    </WithNavBar>
  )
}

export default AccountLearning

