import { Button, Center, Group, Loader, Stack, Text, Title } from '@mantine/core'
import { Link, useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { LessonWorkspace } from '@Components/learning/LessonWorkspace'
import { WithNavBar } from '@Components/WithNavbar'
import { useLearningPathPreview } from '@Hooks/useLearning'
import { useLanguage } from '@Utils/I18n'
import { usePageTitle } from '@Hooks/usePageTitle'

const LearningItem = () => {
  const { slug, moduleId, itemId } = useParams()
  const { locale } = useLanguage()
  const { t } = useTranslation('learning')
  const { data: path, error } = useLearningPathPreview(slug, locale)
  const module = path?.modules.find((item) => item.id === moduleId)
  const itemIndex = module?.items.findIndex((item) => item.id === itemId) ?? -1
  const item = itemIndex >= 0 ? module?.items[itemIndex] : undefined
  const flatItems = path?.modules.flatMap((currentModule) => currentModule.items.map((content) => ({
    moduleId: currentModule.id,
    item: content,
  }))) ?? []
  const flatIndex = flatItems.findIndex((entry) => entry.item.id === itemId)
  const previous = flatItems[flatIndex - 1]
  const next = flatItems[flatIndex + 1]
  usePageTitle(item?.title ?? t('title'))

  return (
    <WithNavBar minWidth={0}>
      {!path && !error ? <Center><Loader /></Center> : error || !item ? <Text c="red">{t('loadFailed')}</Text> : (
        <Stack gap="lg">
          <Title order={1}>{item.title}</Title>
          {item.kind === 'lesson' ? <LessonWorkspace lessonId={item.contentId} /> : (
            <Stack gap="xs">
              <Text>{item.summary}</Text>
              <Text c="dimmed">{t('challengeWorkspaceComingSoon')}</Text>
            </Stack>
          )}
          <Group justify="space-between">
            {previous ? <Button component={Link} variant="subtle" to={`/learn/${slug}/${previous.moduleId}/${previous.item.id}`}>{t('previous')}</Button> : <span />}
            {next ? <Button component={Link} variant="subtle" to={`/learn/${slug}/${next.moduleId}/${next.item.id}`}>{t('next')}</Button> : <span />}
          </Group>
        </Stack>
      )}
    </WithNavBar>
  )
}

export default LearningItem

