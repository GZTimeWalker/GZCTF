import { Badge, Card, Group, List, Stack, Text, Title } from '@mantine/core'
import { Link } from 'react-router'
import type { LearningPathPreview } from '@Hooks/useLearning'

export const RouteOutline = ({ path }: { path: LearningPathPreview }) => (
  <Stack gap="lg">
    <Stack gap={4}>
      <Title order={1}>{path.title}</Title>
      <Text c="dimmed">{path.summary}</Text>
    </Stack>
    {path.modules.map((module) => (
      <Card key={module.id} withBorder padding="lg">
        <Group justify="space-between" align="flex-start">
          <Stack gap={2}>
            <Title order={3}>{module.title}</Title>
            <Text c="dimmed" size="sm">{module.summary}</Text>
          </Stack>
          <Badge variant="outline">{module.expectedMinutes} min</Badge>
        </Group>
        <List mt="md" spacing="xs">
          {module.items.map((item) => (
            <List.Item key={item.id}>
              <Link to={`/learn/${path.slug}/${module.id}/${item.id}`}>
                <Text span fw={500}>{item.title}</Text>
                <Text span c="dimmed" size="sm"> · {item.kind}</Text>
              </Link>
            </List.Item>
          ))}
        </List>
      </Card>
    ))}
  </Stack>
)

