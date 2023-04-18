import { Stack, Text } from '@mantine/core'

export const SwitchLabel = (title: string, desrc: string) => (
  <Stack spacing={1}>
    <Text size="md" weight={500}>
      {title}
    </Text>
    <Text size="xs" color="dimmed">
      {desrc}
    </Text>
  </Stack>
)
