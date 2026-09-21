import { Badge, Group, Paper, Stack, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'
import type { MyLearningResponse, MySkillTreeRecordResponse } from '@Api'

type MySkillTreeRecordProps = {
  record: MyLearningResponse
}

export const MySkillTreeRecord = ({ record }: MySkillTreeRecordProps) => {
  const { t } = useTranslation('skillTrees')

  const percent = (item: MySkillTreeRecordResponse) => {
    const total = (item.challengeCount ?? 0) + (item.lessonCount ?? 0)
    if (total === 0) return '0%'
    const completed = (item.completedChallengeCount ?? 0) + (item.completedLessonCount ?? 0)
    return `${Math.round((completed * 100) / total)}%`
  }

  return (
    <Stack gap="lg">
      <Title order={2}>My skill trees</Title>
      {record.skillTrees?.length === 0 ? (
        <Text c="dimmed">No skill trees joined yet.</Text>
      ) : (
        <Stack gap="md">
          {record.skillTrees?.map((item) => (
            <Paper key={item.skillTreeId} withBorder p="md">
              <Stack gap="xs">
                <Group gap="sm">
                  <Text size="xl" aria-hidden>
                    {skillTreeIcons[(item.iconKey as SkillTreeIconKey) ?? 'flag']}
                  </Text>
                  <Text fw={600}>{item.name}</Text>
                  {item.isCurrent && <Badge color="teal">Current</Badge>}
                  {item.isDeleted && <Badge color="gray">Historical</Badge>}
                </Group>
                <Text size="sm">Progress: {percent(item)}</Text>
                <Text size="sm">
                  Categories: {item.completedCategoryCount ?? 0} / {item.categoryCount ?? 0}
                </Text>
                <Text size="sm">
                  Challenges: {item.completedChallengeCount ?? 0} / {item.challengeCount ?? 0}
                </Text>
                <Text size="sm">
                  Lessons: {item.completedLessonCount ?? 0} / {item.lessonCount ?? 0}
                </Text>
              </Stack>
            </Paper>
          ))}
        </Stack>
      )}
      {record.recentActivity && record.recentActivity.length > 0 && (
        <Stack gap="xs">
          <Title order={3}>Recent activity</Title>
          {record.recentActivity.map((activity) => (
            <Text key={`${activity.kind}-${activity.contentId}-${activity.completedAtUtc}`} size="sm">
              {activity.title ?? activity.contentId} · {activity.kind}
              {activity.solveMode ? ` · ${activity.solveMode}` : ''}
            </Text>
          ))}
        </Stack>
      )}
    </Stack>
  )
}
