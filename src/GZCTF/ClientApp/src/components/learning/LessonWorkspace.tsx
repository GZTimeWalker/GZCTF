import { Button, Card, Group, Loader, Stack, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { Markdown } from '@Components/MarkdownRenderer'
import { useLesson, useLearningMutations } from '@Hooks/useLearning'
import { useLanguage } from '@Utils/I18n'
import { useUser } from '@Hooks/useUser'

export const LessonWorkspace = ({ lessonId }: { lessonId: string }) => {
  const { locale } = useLanguage()
  const { user } = useUser()
  const { t } = useTranslation('learning')
  const { data: lesson, error } = useLesson(lessonId, locale, !!user)
  const { completeLesson } = useLearningMutations()

  if (!user) return <Text c="dimmed">{t('signInToStudy')}</Text>
  if (!lesson && !error) return <Loader />
  if (error) return <Text c="red">{t('loadFailed')}</Text>

  return (
    <Card withBorder padding="xl">
      <Stack>
        <Title order={1}>{lesson!.title}</Title>
        <Markdown source={lesson!.body} />
        <Group justify="flex-end">
          <Button onClick={() => completeLesson(lessonId)}>{t('markComplete')}</Button>
        </Group>
      </Stack>
    </Card>
  )
}

