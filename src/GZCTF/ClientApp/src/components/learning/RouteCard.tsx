import { Badge, Card, Group, Stack, Text, Title } from '@mantine/core'
import { Link } from 'react-router'
import type { LearningPathSummary } from '@Hooks/useLearning'

export const RouteCard = ({ path }: { path: LearningPathSummary }) => (
  <Card component={Link} to={`/learn/${path.slug}`} withBorder shadow="sm" padding="lg" radius="md">
    <Stack gap="xs">
      <Title order={3}>{path.title}</Title>
      <Text c="dimmed" lineClamp={3}>
        {path.summary}
      </Text>
      <Group gap="xs">
        <Badge variant="light">{path.moduleCount} modules</Badge>
        <Badge variant="light">{path.itemCount} items</Badge>
        <Badge variant="light">{path.expectedMinutes} min</Badge>
      </Group>
    </Stack>
  </Card>
)

