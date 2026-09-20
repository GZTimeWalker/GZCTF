import { Button, Group, Text } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { useLearningEnrollments, useLearningMutations } from '@Hooks/useLearning'
import { useLanguage } from '@Utils/I18n'
import { useUser } from '@Hooks/useUser'

export const EnrollmentControls = ({ pathId }: { pathId: string }) => {
  const { user } = useUser()
  const { locale } = useLanguage()
  const { t } = useTranslation('learning')
  const { data: enrollments, mutate } = useLearningEnrollments(locale, !!user)
  const { enroll, selectCurrent } = useLearningMutations()
  const enrollment = enrollments?.find((item) => item.pathId === pathId)

  if (!user) return <Text c="dimmed">{t('signInToStudy')}</Text>

  return (
    <Group>
      {!enrollment && <Button onClick={async () => { await enroll(pathId, locale); await mutate() }}>{t('enroll')}</Button>}
      {enrollment && !enrollment.isCurrent && (
        <Button variant="light" onClick={async () => { await selectCurrent(pathId, locale); await mutate() }}>
          {t('makeCurrent')}
        </Button>
      )}
      {enrollment?.isCurrent && <Text c="teal" fw={600}>{t('currentRoute')}</Text>}
    </Group>
  )
}

