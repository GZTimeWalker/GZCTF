import { Center, Loader, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { RouteCard } from '@Components/learning/RouteCard'
import { WithNavBar } from '@Components/WithNavbar'
import { useLearningPaths } from '@Hooks/useLearning'
import { usePageTitle } from '@Hooks/usePageTitle'
import { useLanguage } from '@Utils/I18n'

const LearningIndex = () => {
  const { locale } = useLanguage()
  const { t } = useTranslation('learning')
  const { data: paths, error } = useLearningPaths(locale)
  usePageTitle(t('title'))

  return (
    <WithNavBar minWidth={0}>
      <Stack gap="xl">
        <Stack gap={4}>
          <Title order={1}>{t('title')}</Title>
          <Text c="dimmed">{t('discovery')}</Text>
        </Stack>
        {error ? <Text c="red">{t('loadFailed')}</Text> : !paths ? <Center><Loader /></Center> : (
          <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }}>
            {paths.map((path) => <RouteCard key={path.pathId} path={path} />)}
          </SimpleGrid>
        )}
      </Stack>
    </WithNavBar>
  )
}

export default LearningIndex

