import { Stack, Text } from '@mantine/core'

export const SwitchLabel = (title: string, desrc: string) => (
  <Stack gap={1}>
    <Text size="md" fw={500}>
      {title}
    </Text>
    <Text size="xs" c="dimmed">
      {desrc}
    </Text>
  </Stack>
)
