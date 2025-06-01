import { Stack, Text } from '@mantine/core'

export const SwitchLabel = (title: string, desrc: string, error?: null | string) => (
  <Stack gap={1}>
    <Text size="md" fw={500}>
      {title}
    </Text>
    <Text size="xs" c="dimmed">
      {desrc}
    </Text>
    {error && (
      <Text size="xs" c="alert" fw={500}>
        {error}
      </Text>
    )}
  </Stack>
)
