import { Badge, Card, Group, Progress, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import type { MyLearningRecord } from '@Hooks/useLearning'

export const LearningRecord = ({ record }: { record: MyLearningRecord }) => (
  <Stack gap="xl">
    <SimpleGrid cols={{ base: 1, sm: 3 }}>
      <Card withBorder><Text c="dimmed">Solved challenges</Text><Title order={2}>{record.solvedChallengeCount}</Title></Card>
      <Card withBorder><Text c="dimmed">Completed lessons</Text><Title order={2}>{record.completedLessonIds.length}</Title></Card>
      <Card withBorder><Text c="dimmed">Recent activities</Text><Title order={2}>{record.recentActivity.length}</Title></Card>
    </SimpleGrid>

    <Stack>
      <Title order={2}>Routes</Title>
      {record.routes.map((route) => (
        <Card key={route.pathId} withBorder>
          <Stack gap="xs">
            <Group justify="space-between">
              <Title order={3}>{route.title}</Title>
              {route.isCurrent && <Badge color="teal">Current</Badge>}
            </Group>
            <Progress value={route.progressPercent} />
            <Text size="sm" c="dimmed">{route.progressPercent}% · {route.completedModules}/{route.totalModules} modules · {route.completedLessons}/{route.totalLessons} lessons</Text>
            {route.modules.map((module) => <Text key={module.moduleId} size="sm">{module.title}: {module.progressPercent}%</Text>)}
          </Stack>
        </Card>
      ))}
    </Stack>

    <Stack>
      <Title order={2}>Recent activity</Title>
      {record.recentActivity.map((activity) => <Text key={`${activity.kind}-${activity.contentId}-${activity.completedAtUtc}`}>{activity.title ?? activity.contentId} · {activity.kind}{activity.solveMode ? ` · ${activity.solveMode}` : ''}</Text>)}
    </Stack>
  </Stack>
)

