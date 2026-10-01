import { Button, Card, Group, Stack, Text } from '@mantine/core'
import { mdiDownload, mdiTrashCanOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { HunamizeSize } from '@Utils/Shared'
import { WriteupExampleModel } from '@Api'

interface WriteupExampleListProps {
  examples: WriteupExampleModel[]
  disabled?: boolean
  onDelete?: (example: WriteupExampleModel) => void
}

export const WriteupExampleList: FC<WriteupExampleListProps> = ({ examples, disabled, onDelete }) => {
  const { t } = useTranslation()

  return (
    <Stack gap="xs">
      {examples.map((example) => (
        <Card key={example.id} withBorder p="sm">
          <Group justify="space-between" wrap="nowrap" gap="sm">
            <Stack gap={0} style={{ flex: 1, minWidth: 0 }}>
              <Text size="sm" fw={600} truncate title={example.name}>
                {example.name}
              </Text>
              <Text size="xs" c="dimmed" ff="monospace">
                {HunamizeSize(example.fileSize)}
              </Text>
            </Stack>
            <Group gap="xs" wrap="nowrap">
              <Button
                component="a"
                href={example.url}
                download={example.name}
                size="xs"
                variant="light"
                leftSection={<Icon path={mdiDownload} size={0.7} />}
              >
                {t('common.button.download')}
              </Button>
              {onDelete && (
                <Button
                  size="xs"
                  color="red"
                  variant="light"
                  disabled={disabled}
                  leftSection={<Icon path={mdiTrashCanOutline} size={0.7} />}
                  onClick={() => onDelete(example)}
                >
                  {t('common.button.delete')}
                </Button>
              )}
            </Group>
          </Group>
        </Card>
      ))}
    </Stack>
  )
}
